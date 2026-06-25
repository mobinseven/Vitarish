using Vitarish.Files.Contracts.Metadata;

namespace Vitarish.Files.Contracts.Requests;

public sealed record ListFilesRequest(FileInformationDescriptorDto Descriptor);
