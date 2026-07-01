using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Appointments.Contracts.Enums;
using DomainDto = Vitarish.Appointments.Domain.Dto;
using DomainEnum = Vitarish.Appointments.Domain.Enums;

namespace Vitarish.Appointments.Api;

public static class AppointmentsContractDomainMapper
{
    // Reservation mappings
    public static DomainDto.ReservationDescriptorDto ToDomain(this ReservationDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Status = dto.Status.HasValue ? (DomainEnum.ReservationStatus)(int)dto.Status.Value : null,
        ShiftId = dto.ShiftId,
        DateFrom = dto.DateFrom,
        DateTo = dto.DateTo,
        ReservingPartyId = dto.ReservingPartyId,
        IsHold = dto.IsHold,
        Take = dto.Take,
        Skip = dto.Skip
    };

    public static DomainDto.ReservationWriteDto ToDomain(this ReservationWriteDto dto) => new()
    {
        Id = dto.Id,
        Status = (DomainEnum.ReservationStatus)(int)dto.Status,
        ShiftId = dto.ShiftId,
        Date = dto.Date,
        ReservingPartyId = dto.ReservingPartyId,
        EstimatedSessionLength = dto.EstimatedSessionLength
    };

    public static ReservationReadDto ToContract(this DomainDto.ReservationReadDto dto) => new()
    {
        Id = dto.Id,
        Status = (ReservationStatus)(int)dto.Status,
        ShiftId = dto.ShiftId,
        Date = dto.Date,
        ReservingPartyId = dto.ReservingPartyId,
        EstimatedSessionLength = dto.EstimatedSessionLength,
        ConfirmedAt = dto.ConfirmedAt,
        CancelledAt = dto.CancelledAt,
        IsHold = dto.IsHold,
        ExpiresAt = dto.ExpiresAt,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        UpdatedAt = dto.UpdatedAt,
        UpdatedBy = dto.UpdatedBy,
        DeletedAt = dto.DeletedAt,
        DeletedBy = dto.DeletedBy
    };

    // Shift mappings
    public static DomainDto.ShiftDescriptorDto ToDomain(this ShiftDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Status = dto.Status.HasValue ? (DomainEnum.ShiftStatus)(int)dto.Status.Value : null,
        Visibility = dto.Visibility.HasValue ? (DomainEnum.ShiftVisibility)(int)dto.Visibility.Value : null,
        DayOfWeek = dto.DayOfWeek,
        StartTime = dto.StartTime,
        EndTime = dto.EndTime,
        SortBy = dto.SortBy.HasValue ? (DomainEnum.ShiftSortBy)(int)dto.SortBy.Value : null,
        Descending = dto.Descending,
        ServiceProviderId = dto.ServiceProviderId,
        ServiceId = dto.ServiceId,
        Take = dto.Take,
        Skip = dto.Skip
    };

    public static DomainDto.ShiftWriteDto ToDomain(this ShiftWriteDto dto) => new()
    {
        Id = dto.Id,
        Status = (DomainEnum.ShiftStatus)(int)dto.Status,
        Visibility = (DomainEnum.ShiftVisibility)(int)dto.Visibility,
        DayOfWeek = dto.DayOfWeek,
        StartTime = dto.StartTime,
        EndTime = dto.EndTime,
        TimeZone = dto.TimeZone,
        TotalSeats = dto.TotalSeats,
        BookingOpensBefore = dto.BookingOpensBefore,
        BookingCloseBefore = dto.BookingCloseBefore,
        ServiceProviderId = dto.ServiceProviderId,
        ServiceId = dto.ServiceId
    };

    public static ShiftReadDto ToContract(this DomainDto.ShiftReadDto dto) => new()
    {
        Id = dto.Id,
        Status = (ShiftStatus)(int)dto.Status,
        Visibility = (ShiftVisibility)(int)dto.Visibility,
        DayOfWeek = dto.DayOfWeek,
        StartTime = dto.StartTime,
        EndTime = dto.EndTime,
        TimeZone = dto.TimeZone,
        TotalSeats = dto.TotalSeats,
        BookingOpensBefore = dto.BookingOpensBefore,
        BookingCloseBefore = dto.BookingCloseBefore,
        ServiceProviderId = dto.ServiceProviderId,
        ServiceId = dto.ServiceId,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        UpdatedAt = dto.UpdatedAt,
        UpdatedBy = dto.UpdatedBy,
        DeletedAt = dto.DeletedAt,
        DeletedBy = dto.DeletedBy
    };

    // Service mappings
    public static DomainDto.ServiceDescriptorDto ToDomain(this ServiceDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        ServiceTypeId = dto.ServiceTypeId,
        Take = dto.Take,
        Skip = dto.Skip
    };

    public static DomainDto.ServiceWriteDto ToDomain(this ServiceWriteDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        ServiceTypeId = dto.ServiceTypeId
    };

    public static ServiceReadDto ToContract(this DomainDto.ServiceReadDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        ServiceTypeId = dto.ServiceTypeId,
        ServiceTypeName = dto.ServiceTypeName,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        UpdatedAt = dto.UpdatedAt,
        UpdatedBy = dto.UpdatedBy,
        DeletedAt = dto.DeletedAt,
        DeletedBy = dto.DeletedBy
    };

    // ServiceType mappings
    public static DomainDto.ServiceTypeDescriptorDto ToDomain(this ServiceTypeDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Take = dto.Take,
        Skip = dto.Skip
    };

    public static DomainDto.ServiceTypeWriteDto ToDomain(this ServiceTypeWriteDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description
    };

    public static ServiceTypeReadDto ToContract(this DomainDto.ServiceTypeReadDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        UpdatedAt = dto.UpdatedAt,
        UpdatedBy = dto.UpdatedBy,
        DeletedAt = dto.DeletedAt,
        DeletedBy = dto.DeletedBy
    };

    // ServiceProvider mappings
    public static DomainDto.ServiceProviderDescriptorDto ToDomain(this ServiceProviderDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Take = dto.Take,
        Skip = dto.Skip
    };

    public static DomainDto.ServiceProviderWriteDto ToDomain(this ServiceProviderWriteDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        LinkedUserIds = dto.LinkedUserIds
    };

    public static ServiceProviderReadDto ToContract(this DomainDto.ServiceProviderReadDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        LinkedUserIds = dto.LinkedUserIds,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        UpdatedAt = dto.UpdatedAt,
        UpdatedBy = dto.UpdatedBy,
        DeletedAt = dto.DeletedAt,
        DeletedBy = dto.DeletedBy
    };

    // ShiftReminderPolicy mappings
    public static DomainDto.ShiftReminderPolicyDescriptorDto ToDomain(this ShiftReminderPolicyDescriptorDto dto) => new()
    {
        Id = dto.Id,
        ShiftId = dto.ShiftId,
        Take = dto.Take,
        Skip = dto.Skip
    };

    public static DomainDto.ShiftReminderPolicyWriteDto ToDomain(this ShiftReminderPolicyWriteDto dto) => new()
    {
        Id = dto.Id,
        ShiftId = dto.ShiftId,
        IsEnabled = dto.IsEnabled,
        Channel = (DomainEnum.ShiftReminderChannel)(int)dto.Channel,
        OffsetBefore = dto.OffsetBefore,
        QualifyingStatuses = dto.QualifyingStatuses.Select(s => (DomainEnum.ReservationStatus)(int)s).ToList(),
        SubjectTemplate = dto.SubjectTemplate,
        BodyTemplate = dto.BodyTemplate
    };

    public static ShiftReminderPolicyReadDto ToContract(this DomainDto.ShiftReminderPolicyReadDto dto) => new()
    {
        Id = dto.Id,
        ShiftId = dto.ShiftId,
        IsEnabled = dto.IsEnabled,
        Channel = (ShiftReminderChannel)(int)dto.Channel,
        OffsetBefore = dto.OffsetBefore,
        QualifyingStatuses = dto.QualifyingStatuses.Select(s => (ReservationStatus)(int)s).ToList(),
        SubjectTemplate = dto.SubjectTemplate,
        BodyTemplate = dto.BodyTemplate,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        UpdatedAt = dto.UpdatedAt,
        UpdatedBy = dto.UpdatedBy,
        DeletedAt = dto.DeletedAt,
        DeletedBy = dto.DeletedBy
    };

    // ShiftBookingPolicy mappings
    public static DomainDto.ShiftBookingPolicyDescriptorDto ToDomain(this ShiftBookingPolicyDescriptorDto dto) => new()
    {
        Id = dto.Id,
        ShiftId = dto.ShiftId,
        Date = dto.Date,
        Take = dto.Take,
        Skip = dto.Skip
    };

    public static DomainDto.ShiftBookingPolicyWriteDto ToDomain(this ShiftBookingPolicyWriteDto dto) => new()
    {
        Id = dto.Id,
        ShiftId = dto.ShiftId,
        Date = dto.Date,
        BookingOpensBefore = dto.BookingOpensBefore,
        BookingCloseBefore = dto.BookingCloseBefore,
        Closed = dto.Closed,
        Capacity = dto.Capacity,
        RequiresApproval = dto.RequiresApproval,
        CancellationDeadline = dto.CancellationDeadline,
        EstimatedSessionLength = dto.EstimatedSessionLength,
        AllowRescheduling = dto.AllowRescheduling,
        MaxReservationsPerParty = dto.MaxReservationsPerParty
    };

    public static ShiftBookingPolicyReadDto ToContract(this DomainDto.ShiftBookingPolicyReadDto dto) => new()
    {
        Id = dto.Id,
        ShiftId = dto.ShiftId,
        Date = dto.Date,
        BookingOpensBefore = dto.BookingOpensBefore,
        BookingCloseBefore = dto.BookingCloseBefore,
        Closed = dto.Closed,
        Capacity = dto.Capacity,
        RequiresApproval = dto.RequiresApproval,
        CancellationDeadline = dto.CancellationDeadline,
        EstimatedSessionLength = dto.EstimatedSessionLength,
        AllowRescheduling = dto.AllowRescheduling,
        MaxReservationsPerParty = dto.MaxReservationsPerParty,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        UpdatedAt = dto.UpdatedAt,
        UpdatedBy = dto.UpdatedBy,
        DeletedAt = dto.DeletedAt,
        DeletedBy = dto.DeletedBy
    };
}
