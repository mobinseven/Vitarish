namespace Vitarish.Sea.Contracts.Responses;

public sealed record EventCapabilitiesReadDto(bool CanRead, bool CanWrite, bool CanManage);
