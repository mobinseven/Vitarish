using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vorn.Entities.Models;

namespace Vitarish.Files.Infrastructure.Services;

public class FileLoadCors
{
    private readonly RequestDelegate _next;
    private readonly FilesOptions _options;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMultiTenantContextAccessor<TenantInfo> _tenantAccessor;

    public FileLoadCors(
        RequestDelegate next,
        IOptions<FilesOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        IMultiTenantContextAccessor<TenantInfo> tenantAccessor)
    {
        _next = next;
        _options = options.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _tenantAccessor = tenantAccessor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/" + _options.UploadPath))
        {
            await _next(context);
            return;
        }

        string? fileName = context.Request.Path.Value?.Split('/').Last();

        if (fileName is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using IDisposable tenantScope = _tenantAccessor.UseTenant(Defaults.RootTenant);
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IFileInformationService fileInformations = scope.ServiceProvider.GetRequiredService<IFileInformationService>();
        EntityResult<IReadOnlyList<FileInformationDto>> fisRes = await fileInformations.GetListAsync(new FileInformationDescriptorDto { Name = fileName }, context.RequestAborted);
        IReadOnlyList<FileInformationDto>? fis = fisRes.Value;
        FileInformationDto? fileInformation = fis?.FirstOrDefault();

        if (fileInformation is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        context.Items[typeof(FileInformationDto)] = fileInformation;
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

        await _next(context);
    }

    private static bool IsAllowedOrigin(string[] allowedOrigins, string[] origins) => allowedOrigins.Intersect(origins).Any() || allowedOrigins.Contains("*");
}
