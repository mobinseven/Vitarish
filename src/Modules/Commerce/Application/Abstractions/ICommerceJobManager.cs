namespace Vitarish.Commerce.Application.Abstractions;

public interface ICommerceJobManager
{
    void ScheduleCatalogSync();
    void TriggerCatalogSync();
    void ScheduleOrderSync();
    void TriggerOrderSync();
    void ScheduleCartCleanup();
}
