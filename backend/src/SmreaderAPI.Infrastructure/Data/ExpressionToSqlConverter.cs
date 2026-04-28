using System.Linq.Expressions;
using System.Reflection;
using Dapper;

namespace SmreaderAPI.Infrastructure.Data;

public static class ExpressionToSqlConverter
{
    public static (string WhereClause, DynamicParameters Parameters) Convert<T>(Expression<Func<T, bool>> predicate)
    {
        var parameters = new DynamicParameters();
        var paramIndex = 0;
        var sql = ParseExpression(predicate.Body, parameters, ref paramIndex);
        return ($"WHERE {sql}", parameters);
    }

    private static string ParseExpression(Expression expression, DynamicParameters parameters, ref int paramIndex)
    {
        return expression switch
        {
            BinaryExpression binary => ParseBinary(binary, parameters, ref paramIndex),
            MethodCallExpression methodCall => ParseMethodCall(methodCall, parameters, ref paramIndex),
            UnaryExpression { NodeType: ExpressionType.Not } unary => $"NOT ({ParseExpression(unary.Operand, parameters, ref paramIndex)})",
            MemberExpression member when member.Type == typeof(bool) => ParseBooleanMember(member, parameters, ref paramIndex),
            _ => throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported.")
        };
    }

    private static string ParseBinary(BinaryExpression binary, DynamicParameters parameters, ref int paramIndex)
    {
        if (binary.NodeType == ExpressionType.AndAlso)
        {
            var left = ParseExpression(binary.Left, parameters, ref paramIndex);
            var right = ParseExpression(binary.Right, parameters, ref paramIndex);
            return $"({left}) AND ({right})";
        }

        if (binary.NodeType == ExpressionType.OrElse)
        {
            var left = ParseExpression(binary.Left, parameters, ref paramIndex);
            var right = ParseExpression(binary.Right, parameters, ref paramIndex);
            return $"({left}) OR ({right})";
        }

        var columnName = GetColumnName(binary.Left);
        var value = GetValue(binary.Right);

        // Handle null comparisons
        if (value is null)
        {
            return binary.NodeType switch
            {
                ExpressionType.Equal => $"{columnName} IS NULL",
                ExpressionType.NotEqual => $"{columnName} IS NOT NULL",
                _ => throw new NotSupportedException($"Null comparison with '{binary.NodeType}' is not supported.")
            };
        }

        var paramName = $"@p{paramIndex++}";
        parameters.Add(paramName, value);

        var op = binary.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Binary operator '{binary.NodeType}' is not supported.")
        };

        return $"{columnName} {op} {paramName}";
    }

    private static string ParseMethodCall(MethodCallExpression methodCall, DynamicParameters parameters, ref int paramIndex)
    {
        var columnName = GetColumnName(methodCall.Object!);
        var value = GetValue(methodCall.Arguments[0]);
        var paramName = $"@p{paramIndex++}";

        return methodCall.Method.Name switch
        {
            "Contains" => SetParamAndReturn(parameters, paramName, $"%{value}%", $"{columnName} LIKE {paramName}"),
            "StartsWith" => SetParamAndReturn(parameters, paramName, $"{value}%", $"{columnName} LIKE {paramName}"),
            "EndsWith" => SetParamAndReturn(parameters, paramName, $"%{value}", $"{columnName} LIKE {paramName}"),
            _ => throw new NotSupportedException($"Method '{methodCall.Method.Name}' is not supported.")
        };
    }

    private static string ParseBooleanMember(MemberExpression member, DynamicParameters parameters, ref int paramIndex)
    {
        var columnName = GetColumnName(member);
        var paramName = $"@p{paramIndex++}";
        parameters.Add(paramName, true);
        return $"{columnName} = {paramName}";
    }

    private static string SetParamAndReturn(DynamicParameters parameters, string paramName, object value, string sql)
    {
        parameters.Add(paramName, value);
        return sql;
    }

    private static string GetColumnName(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression { Operand: MemberExpression member } => member.Member.Name,
            _ => throw new NotSupportedException($"Cannot extract column name from expression type '{expression.NodeType}'.")
        };
    }

    private static object? GetValue(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => constant.Value,
            MemberExpression member => GetMemberValue(member),
            UnaryExpression { NodeType: ExpressionType.Convert } unary => GetValue(unary.Operand),
            _ => Expression.Lambda(expression).Compile().DynamicInvoke()
        };
    }

    private static object? GetMemberValue(MemberExpression member)
    {
        if (member.Expression is ConstantExpression constant)
        {
            return member.Member switch
            {
                FieldInfo field => field.GetValue(constant.Value),
                PropertyInfo property => property.GetValue(constant.Value),
                _ => throw new NotSupportedException()
            };
        }

        return Expression.Lambda(member).Compile().DynamicInvoke();
    }
}
