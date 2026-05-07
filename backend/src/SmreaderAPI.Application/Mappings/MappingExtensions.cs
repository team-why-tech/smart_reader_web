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
        user.Address,
        user.OwnerGuid,
        user.Status,
        user.Privilages,
        user.CategoryGuid,
        user.CreatedAt,
        user.LastSyncDate,
        user.VanSale,
        user.Tech,
        user.UserInactive,
        user.CollectionAgent,
        user.SuperAdmin,
        user.Printertype,
        user.Moduletype,
        user.Billnumber,
        user.ReadBillnumber,
        user.Panchayatname,
        user.Panchayatname1,
        user.Panchayatname2,
        user.Panchayatname3,
        user.Panchayatname4,
        user.EmailCRM
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
