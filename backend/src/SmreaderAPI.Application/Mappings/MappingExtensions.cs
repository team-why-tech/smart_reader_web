using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Domain.Entities;

namespace SmreaderAPI.Application.Mappings;

public static class MappingExtensions
{
    public static AuditLogDto ToDto(this AuditLog log)
    {
        return new AuditLogDto(
            log.Id,
            log.UserId,
            log.Action,
            log.EntityName,
            log.EntityId,
            log.Timestamp,
            log.Details
        );
    }

    public static RoleDto ToDto(this Role role)
    {
        return new RoleDto(role.Id, role.Name, role.Description);
    }

    public static UserDto ToDto(this User user)
    {
        return new UserDto(user.Id, user.Name, user.Email, user.Mobile ?? "", user.Address, user.Status);
    }
}
