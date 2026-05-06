using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Mappings;

public static class MappingExtensions
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Mobile,
        user.Status
    );

    public static AuditLogDto ToDto(this AuditLog log) => new(
        log.Id,
        log.UserId,
        log.Action,
        log.EntityName,
        log.EntityId,
        log.Timestamp,
        log.Details
    );
}
