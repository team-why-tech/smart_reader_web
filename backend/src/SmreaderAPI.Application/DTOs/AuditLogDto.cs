namespace SmreaderAPI.Application.DTOs;

public record AuditLogDto(int Id, int? UserId, string Action, string EntityName, int? EntityId, DateTime Timestamp, string? Details);
