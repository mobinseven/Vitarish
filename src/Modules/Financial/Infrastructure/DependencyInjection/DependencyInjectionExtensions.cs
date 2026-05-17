namespace Vitarish.Financial.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddFinancialInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishFinancialInfrastructure();

        services.TryAddTransient<IFinancialLedgerCommandService, FinancialLedgerService>();
        services.TryAddTransient<IFinancialLedgerQueryService, FinancialLedgerService>();
        services.TryAddTransient<IFinancialAdapterIngressService, FinancialAdapterIngressService>();

        return services;
    }
}
