using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.Modules.Base.Infrastructure.Idempotency;

public static class OperationReceiptRegistrationExtensions
{
    public static IServiceCollection AddRedisOperationReceipts(this IServiceCollection services)
    {
        _ = services.AddSingleton(
            typeof(IOperationReceiptStore<>),
            typeof(RedisOperationReceiptStore<>));
        return services;
    }
}
