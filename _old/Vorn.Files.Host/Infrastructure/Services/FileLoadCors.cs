
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Primitives;
using Vorn.Entities.Models;
using Vorn.Files.Core.Domain.Models;
using Vorn.Files.Core.Presentation.Interfaces;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Extensions;

namespace Vorn.Files.Host.Infrastructure.Services;

public class FileLoadCors(
    RequestDelegate _next,
    VornOptions options,
    IServiceScopeFactory serviceScopeFactory,
    IMultiTenantContextAccessor<TenantInfo> tenantAccessor)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if(!context.Request.Path.StartsWithSegments("/" + options.Files.UploadPath))
        {
            await _next(context);
            return;
        }

        string? fileName = context.Request.Path.Value?.Split('/').Last();

        if(fileName is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using IDisposable tenantScope = tenantAccessor.UseTenant(Defaults.RootTenant);
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IFileInformationService fileInformations = scope.ServiceProvider.GetRequiredService<IFileInformationService>();
        EntityResult<IReadOnlyList<FileInformationDto>> fisRes = await fileInformations.GetListAsync(new FileInformationDescriptorDto { Name = fileName }, context.RequestAborted);
        IReadOnlyList<FileInformationDto>? fis = fisRes.Value;
        FileInformationDto? fileInformation = fis?.FirstOrDefault();

        if(fileInformation is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        context.Items[typeof(FileInformationDto)] = fileInformation;
        context.Items["FileName"] = fileName;

        string[] origins = [];
        if(context.Request.Headers.TryGetValue("Referer", out StringValues referer))
        {
            origins = [.. referer];
        }

        if(context.Request.Headers.TryGetValue("Origin", out StringValues origin))
        {
            origins = [.. origin];
        }

        bool allowed = IsAllowedOrigin(fileInformation.AllowedOrigins.Split(','), origins);
        if(!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");

        await _next(context);
    }

    private static bool IsAllowedOrigin(string[] allowedOrigins, string[] origins)
    {
        return allowedOrigins.Intersect(origins).Any() || allowedOrigins.Contains("*");
    }
}
