using Dapper;
using SmreaderAPI.Domain.Entities;
using SmreaderAPI.Infrastructure.Data;
using FluentAssertions;

namespace SmreaderAPI.UnitTests.Infrastructure;

public class ExpressionToSqlConverterTests
{
    [Fact]
    public void Convert_EqualExpression_GeneratesCorrectSql()
    {
        var (whereClause, parameters) = ExpressionToSqlConverter.Convert<User>(x => x.Email == "test@test.com");

        whereClause.Should().Be("WHERE Email = @p0");
        parameters.Get<string>("p0").Should().Be("test@test.com");
    }

    [Fact]
    public void Convert_NotEqualExpression_GeneratesCorrectSql()
    {
        var (whereClause, _) = ExpressionToSqlConverter.Convert<User>(x => x.Email != "test@test.com");

        whereClause.Should().Be("WHERE Email != @p0");
    }

    [Fact]
    public void Convert_AndExpression_GeneratesCorrectSql()
    {
        var (whereClause, parameters) = ExpressionToSqlConverter.Convert<User>(x => x.Status == 1 && x.OwnerGuid == 1);

        whereClause.Should().Be("WHERE (Status = @p0) AND (OwnerGuid = @p1)");
    }

    [Fact]
    public void Convert_OrExpression_GeneratesCorrectSql()
    {
        var (whereClause, _) = ExpressionToSqlConverter.Convert<User>(x => x.Status == 1 || x.Status == 0);

        whereClause.Should().Be("WHERE (Status = @p0) OR (Status = @p1)");
    }

    [Fact]
    public void Convert_ContainsExpression_GeneratesLikeSql()
    {
        var (whereClause, parameters) = ExpressionToSqlConverter.Convert<User>(x => x.Name.Contains("john"));

        whereClause.Should().Be("WHERE Name LIKE @p0");
        parameters.Get<string>("p0").Should().Be("%john%");
    }

    [Fact]
    public void Convert_StartsWithExpression_GeneratesLikeSql()
    {
        var (whereClause, parameters) = ExpressionToSqlConverter.Convert<User>(x => x.Name.StartsWith("J"));

        whereClause.Should().Be("WHERE Name LIKE @p0");
        parameters.Get<string>("p0").Should().Be("J%");
    }

    [Fact]
    public void Convert_EndsWithExpression_GeneratesLikeSql()
    {
        var (whereClause, parameters) = ExpressionToSqlConverter.Convert<User>(x => x.Name.EndsWith("son"));

        whereClause.Should().Be("WHERE Name LIKE @p0");
        parameters.Get<string>("p0").Should().Be("%son");
    }

    [Fact]
    public void Convert_NullEqualExpression_GeneratesIsNull()
    {
        var (whereClause, _) = ExpressionToSqlConverter.Convert<User>(x => x.UpdatedAt == null);

        whereClause.Should().Be("WHERE UpdatedAt IS NULL");
    }

    [Fact]
    public void Convert_NullNotEqualExpression_GeneratesIsNotNull()
    {
        var (whereClause, _) = ExpressionToSqlConverter.Convert<User>(x => x.UpdatedAt != null);

        whereClause.Should().Be("WHERE UpdatedAt IS NOT NULL");
    }

    [Fact]
    public void Convert_GreaterThanExpression_GeneratesCorrectSql()
    {
        var (whereClause, _) = ExpressionToSqlConverter.Convert<User>(x => x.Id > 5);

        whereClause.Should().Be("WHERE Id > @p0");
    }
}
