namespace Vitarish.Financial.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddFinancialInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishFinancialInfrastructure();

        services.TryAddTransient<IFinancialLedgerCommandService, Services.FinancialLedgerService>();
        services.TryAddTransient<IFinancialLedgerQueryService, Services.FinancialLedgerService>();
        services.TryAddTransient<IFinancialAdapterIngressService, Services.FinancialAdapterIngressService>();

        return services;
    }
}
