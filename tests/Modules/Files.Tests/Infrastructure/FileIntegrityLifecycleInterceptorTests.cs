using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Files.Tests.Infrastructure;

public sealed class FileIntegrityLifecycleInterceptorTests
{
    private const string TenantId = "tenant-a";

    [Fact]
    public async Task SaveChanges_removes_the_integrity_record_when_the_file_is_deleted()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        FileInformation file = CreateFile();
        FileIntegrityRecord record = CreateRecord(file);

        await using FilesDbContext context = CreateContext(databaseName);
        _ = await context.Set<FileInformation>().AddAsync(file);
        _ = await context.Set<FileIntegrityRecord>().AddAsync(record);
        _ = await context.SaveChangesAsync();

        context.Remove(file);
        _ = await context.SaveChangesAsync();

        (await context.Set<FileIntegrityRecord>().IgnoreQueryFilters().ToListAsync()).Should().BeEmpty();
    }

    private static FilesDbContext CreateContext(string databaseName)
    {
        TestRequestContext requestContext = new();
        List<IInterceptor> interceptors = [new FileIntegrityLifecycleInterceptor()];
        DbContextOptions<FilesDbContext> options = new DbContextOptionsBuilder<FilesDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new FilesDbContext(options, interceptors, requestContext, new HttpContextAccessor());
    }

    private static FileInformation CreateFile() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
        ContentType = "text/plain",
        Checksum = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
    };

    private static FileIntegrityRecord CreateRecord(FileInformation file) => new()
    {
        FileInformationId = file.Id,
        TenantId = TenantId,
        ObjectKey = "tenant-a/file",
        Digest = file.Checksum,
        ContentType = file.ContentType,
        ContentLength = 0
    };

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => FileIntegrityLifecycleInterceptorTests.TenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { TenantId = TenantId };
    }
}
