using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Vitarish.Entities.Server.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;

namespace Vitarish.Files.Infrastructure.Services;

public class FileLoadCors(
    RequestDelegate next,
    IOptions<FilesOptions> options,
    IServiceScopeFactory serviceScopeFactory,
    IMultiTenantContextAccessor<TenantInfo> tenantAccessor)
{
    private readonly FilesOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/" + _options.UploadPath))
        {
            await next(context);
            return;
        }

        string? fileName = context.Request.Path.Value?.Split('/').Last();

        if (fileName is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using IDisposable tenantScope = tenantAccessor.UseTenant(TenantDefaults.RootTenant);
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IFileInformationService fileInformations = scope.ServiceProvider.GetRequiredService<IFileInformationService>();
        EntityResult<IReadOnlyList<FileInformationReadDto>> fisRes = await fileInformations.GetListAsync(new FileInformationDescriptorDto { Name = fileName }, context.RequestAborted);
        IReadOnlyList<FileInformationReadDto>? fis = fisRes.Value;
        FileInformationReadDto? fileInformation = fis?.FirstOrDefault();

        if (fileInformation is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        context.Items[typeof(FileInformationReadDto)] = fileInformation;
        context.Items["FileName"] = fileName;

        string[] origins = [];
        if (context.Request.Headers.TryGetValue("Referer", out StringValues referer))
        {
            origins = referer.ToArray()!;
        }

        if (context.Request.Headers.TryGetValue("Origin", out StringValues origin))
        {
            origins = origin.ToArray()!;
        }

        bool allowed = IsAllowedOrigin(fileInformation.AllowedOrigins.Split(','), origins);
        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");

        await next(context);
    }

    private static bool IsAllowedOrigin(string[] allowedOrigins, string[] origins) => allowedOrigins.Intersect(origins).Any() || allowedOrigins.Contains("*");
}
