using Ordevo.BuildingBlocks.Abstractions;

namespace Ordevo.Api.Modules;

public static class ModuleRegistry
{
    public static IReadOnlyList<IModule> DiscoverModules() =>
    [
        new Ordevo.Modules.Identity.IdentityModule(),
        new Ordevo.Modules.Menu.MenuModule(),
        new Ordevo.Modules.Ordering.OrderingModule(),
        new Ordevo.Modules.Payment.PaymentModule(),
        new Ordevo.Modules.Kitchen.KitchenModule(),
        new Ordevo.Modules.Inventory.InventoryModule(),
        new Ordevo.Modules.Shift.ShiftModule(),
        new Ordevo.Modules.Reporting.ReportingModule(),
        new Ordevo.Modules.Finance.FinanceModule(),
        new Ordevo.Modules.Print.PrintModule(),
        new Ordevo.Modules.M9Crm.M9CrmModule(),
        new Ordevo.Modules.Sync.SyncModule(),
        new Ordevo.Modules.Integration.IntegrationModule(),
        new Ordevo.Modules.EInvoice.EInvoiceModule(),
        new Ordevo.Modules.Fiscal.FiscalModule(),
    ];
}
