namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class ReceiptsService(
    IMediator mediator,
    IAttachmentsService attachmentsService,
    Vitarish.Communications.Application.Abstractions.IRemindersService remindersService)
    : EntityService<Receipt, ReceiptDescriptor, ReceiptReadDto, ReceiptWriteDto, ReceiptDescriptorDto>
    (
        mediator,
        receipt=>receipt.ToDto(),
        receipt => receipt.ToDomain(),
        receipt => receipt.ToDomain()
    ),
    IReceiptsService
{
    private async Task RemoveReceiptAsync(string createdBy, Guid objectId, CancellationToken cancellationToken) => (await RemoveByDescriptorAsync(
            new ReceiptDescriptor
            {
                CreatedBy = createdBy,
                ObjectId = objectId
            },
            cancellationToken)).EnsureSuccess("Remove receipt");

    public async Task<bool> HasReceiptAsync(Guid userId, Guid objectId, CancellationToken cancellationToken)
    {
        ReceiptReadDto? receiptDto = (await GetListAsync(new ReceiptDescriptorDto { CreatedBy = userId.ToString(), ObjectId = objectId }, cancellationToken))
            .EnsureValue("Get receipts")
            .FirstOrDefault();
        return receiptDto is not null;
    }

    public async Task<IReadOnlyDictionary<Guid, bool>> HasReceiptsAsync(Guid userId, IEnumerable<Guid> objectIds, CancellationToken cancellationToken)
    {
        Guid[] ids = [.. objectIds.Distinct()];
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, bool>();
        }

        IReadOnlyList<ReceiptReadDto> receipts = (await GetListAsync(new ReceiptDescriptorDto { CreatedBy = userId.ToString() }, cancellationToken))
            .EnsureValue("Get receipts");

        HashSet<Guid> receiptIds = [.. receipts.Select(r => r.ObjectId)];

        return ids.ToDictionary(id => id, receiptIds.Contains);
    }
    public async Task<Guid> AddAsync(Guid userId, Guid objectId, CancellationToken cancellationToken)
    {
        ReceiptReadDto? receiptDto = (await GetListAsync(new ReceiptDescriptorDto { CreatedBy = userId.ToString(), ObjectId = objectId }, CancellationToken.None))
            .EnsureValue()
            .FirstOrDefault();
        if (receiptDto is not null)
        {
            return receiptDto.Id;
        }
        Receipt receipt = new(objectId);
        _ = (await AddEntityAsync(receipt, cancellationToken)).EnsureValue("Add receipt");
        return receipt.Id;
    }

    public async Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReceiptReadDto> receipts = (await GetListAsync(new ReceiptDescriptorDto { ObjectId = objectId }, cancellationToken))
            .EnsureValue();

        HashSet<Guid> readers = [];

        foreach (ReceiptReadDto receipt in receipts)
        {
            if (Guid.TryParse(receipt.CreatedBy, out Guid userId))
            {
                _ = readers.Add(userId);
            }
        }

        Guid[] result = new Guid[readers.Count];
        readers.CopyTo(result);
        return result;
    }

    public Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) => AddAsync(userId, projectId, cancellationToken);

    public Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) => RemoveReceiptAsync(userId.ToString(), projectId, cancellationToken);

    public Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken) => AddAsync(userId, procedureId, cancellationToken);

    public Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken) => RemoveReceiptAsync(userId.ToString(), procedureId, cancellationToken);

    public async Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        _ = await AddAsync(userId, eventId, cancellationToken);

        IReadOnlyList<AttachmentReadDto> attachments = await attachmentsService.GetByEventAsync(eventId, cancellationToken);
        foreach (AttachmentReadDto attachment in attachments)
        {
            _ = await AddAsync(userId, attachment.Id, cancellationToken);
        }

        IReadOnlyList<Vitarish.Communications.Domain.Dto.ReminderReadDto> reminders = await remindersService.GetBySourceAsync(eventId, cancellationToken);
        foreach (Vitarish.Communications.Domain.Dto.ReminderReadDto reminder in reminders)
        {
            _ = await AddAsync(userId, reminder.Id, cancellationToken);
        }
    }

    public async Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        string createdBy = userId.ToString();
        await RemoveReceiptAsync(createdBy, eventId, cancellationToken);

        IReadOnlyList<AttachmentReadDto> attachments = await attachmentsService.GetByEventAsync(eventId, cancellationToken);
        foreach (AttachmentReadDto attachment in attachments)
        {
            await RemoveReceiptAsync(createdBy, attachment.Id, cancellationToken);
        }

        IReadOnlyList<Vitarish.Communications.Domain.Dto.ReminderReadDto> reminders = await remindersService.GetBySourceAsync(eventId, cancellationToken);
        foreach (Vitarish.Communications.Domain.Dto.ReminderReadDto reminder in reminders)
        {
            await RemoveReceiptAsync(createdBy, reminder.Id, cancellationToken);
        }
    }
}
