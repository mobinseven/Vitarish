/// <summary>
/// Mapping extension methods for Calendar domain entities.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Calendar.Domain.Mappers.EntityMapper Mapper = new();

    // CalendarAccount
    public static global::Vitarish.Calendar.Domain.Dto.CalendarAccountReadDto ToDto(this global::Vitarish.Calendar.Domain.Entities.CalendarAccount entity) => Mapper.ToDto(entity);
    public static global::Vitarish.Calendar.Domain.Entities.CalendarAccount ToDomain(this global::Vitarish.Calendar.Domain.Dto.CalendarAccountWriteDto dto) => Mapper.ToDomain(dto);
    public static global::Vitarish.Calendar.Domain.Dto.CalendarAccountWriteDto ToWrite(this global::Vitarish.Calendar.Domain.Dto.CalendarAccountReadDto dto) => Mapper.ToWrite(dto);
    public static global::Vitarish.Calendar.Domain.Models.CalendarAccountDescriptor ToDomain(this global::Vitarish.Calendar.Domain.Dto.CalendarAccountDescriptorDto dto) => Mapper.ToDomain(dto);

    // CalendarObjectMapping
    public static global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingReadDto ToDto(this global::Vitarish.Calendar.Domain.Entities.CalendarObjectMapping entity) => Mapper.ToDto(entity);
    public static global::Vitarish.Calendar.Domain.Entities.CalendarObjectMapping ToDomain(this global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingWriteDto dto) => Mapper.ToDomain(dto);
    public static global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingWriteDto ToWrite(this global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingReadDto dto) => Mapper.ToWrite(dto);
    public static global::Vitarish.Calendar.Domain.Models.CalendarObjectMappingDescriptor ToDomain(this global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingDescriptorDto dto) => Mapper.ToDomain(dto);

    // DomainCalendarRecord
    public static global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordReadDto ToDto(this global::Vitarish.Calendar.Domain.Entities.DomainCalendarRecord entity) => Mapper.ToDto(entity);
    public static global::Vitarish.Calendar.Domain.Entities.DomainCalendarRecord ToDomain(this global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordWriteDto dto) => Mapper.ToDomain(dto);
    public static global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordWriteDto ToWrite(this global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordReadDto dto) => Mapper.ToWrite(dto);
    public static global::Vitarish.Calendar.Domain.Models.DomainCalendarRecordDescriptor ToDomain(this global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordDescriptorDto dto) => Mapper.ToDomain(dto);

    // CalendarParticipant
    public static global::Vitarish.Calendar.Domain.Dto.CalendarParticipantReadDto ToDto(this global::Vitarish.Calendar.Domain.Entities.CalendarParticipant entity) => Mapper.ToDto(entity);
    public static global::Vitarish.Calendar.Domain.Entities.CalendarParticipant ToDomain(this global::Vitarish.Calendar.Domain.Dto.CalendarParticipantWriteDto dto) => Mapper.ToDomain(dto);
    public static global::Vitarish.Calendar.Domain.Dto.CalendarParticipantWriteDto ToWrite(this global::Vitarish.Calendar.Domain.Dto.CalendarParticipantReadDto dto) => Mapper.ToWrite(dto);
    public static global::Vitarish.Calendar.Domain.Models.CalendarParticipantDescriptor ToDomain(this global::Vitarish.Calendar.Domain.Dto.CalendarParticipantDescriptorDto dto) => Mapper.ToDomain(dto);

    // AvailabilityPolicy
    public static global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyReadDto ToDto(this global::Vitarish.Calendar.Domain.Entities.AvailabilityPolicy entity) => Mapper.ToDto(entity);
    public static global::Vitarish.Calendar.Domain.Entities.AvailabilityPolicy ToDomain(this global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyWriteDto dto) => Mapper.ToDomain(dto);
    public static global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyWriteDto ToWrite(this global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyReadDto dto) => Mapper.ToWrite(dto);
    public static global::Vitarish.Calendar.Domain.Models.AvailabilityPolicyDescriptor ToDomain(this global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyDescriptorDto dto) => Mapper.ToDomain(dto);

    // OccurrenceProjection (read-only)
    public static global::Vitarish.Calendar.Domain.Dto.OccurrenceProjectionReadDto ToDto(this global::Vitarish.Calendar.Domain.Entities.OccurrenceProjection entity) => Mapper.ToDto(entity);
    public static global::Vitarish.Calendar.Domain.Models.OccurrenceProjectionDescriptor ToDomain(this global::Vitarish.Calendar.Domain.Dto.OccurrenceProjectionDescriptorDto dto) => Mapper.ToDomain(dto);

    // CalendarSyncLedger (read-only)
    public static global::Vitarish.Calendar.Domain.Dto.CalendarSyncLedgerReadDto ToDto(this global::Vitarish.Calendar.Domain.Entities.CalendarSyncLedger entity) => Mapper.ToDto(entity);
    public static global::Vitarish.Calendar.Domain.Models.CalendarSyncLedgerDescriptor ToDomain(this global::Vitarish.Calendar.Domain.Dto.CalendarSyncLedgerDescriptorDto dto) => Mapper.ToDomain(dto);

    // CalendarAuditEntry (append-only)
    public static global::Vitarish.Calendar.Domain.Dto.CalendarAuditEntryReadDto ToDto(this global::Vitarish.Calendar.Domain.Entities.CalendarAuditEntry entity) => Mapper.ToDto(entity);
}
