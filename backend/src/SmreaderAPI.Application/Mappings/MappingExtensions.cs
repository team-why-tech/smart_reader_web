using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Mappings;

public static class MappingExtensions
{
    public static UserDto ToDto(this User user, string roleName) => new(
        user.Id,
        user.Name,
        user.Email,
        roleName,
        user.IsActive,
        user.CreatedAt
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

    public static RoleDto ToDto(this Role role) => new(
        role.Id,
        role.Name,
        role.Description
    );
}
