namespace Ordevo.Modules.Identity.Domain;

public static class Permissions
{
    public const string UsersRead = "identity.users.read";
    public const string UsersWrite = "identity.users.write";
    public const string RolesManage = "identity.roles.manage";
    public const string DevicesManage = "identity.devices.manage";
    public const string AuditRead = "identity.audit.read";

    public const string MenuRead = "menu.read";
    public const string MenuManage = "menu.manage";

    public const string OrderRead = "order.read";
    public const string OrderCreate = "order.create";
    public const string OrderManage = "order.manage";

    public const string PaymentProcess = "payment.process";
    public const string PaymentRefund = "payment.refund";

    public const string KitchenView = "kitchen.view";
    public const string KitchenManage = "kitchen.manage";

    public const string InventoryRead = "inventory.read";
    public const string InventoryManage = "inventory.manage";

    public const string ShiftManage = "shift.manage";

    public const string ReportView = "report.view";

    public const string FinanceRead = "finance.read";
    public const string FinanceManage = "finance.manage";

    public const string PrintRead = "print.read";
    public const string PrintManage = "print.manage";

    public const string CrmCustomersRead = "crm.customers.read";
    public const string CrmCustomersManage = "crm.customers.manage";
    public const string CrmLoyaltyManage = "crm.loyalty.manage";
    public const string CrmCampaignsRead = "crm.campaigns.read";
    public const string CrmCampaignsManage = "crm.campaigns.manage";
    public const string CrmCampaignsApply = "crm.campaigns.apply";
    public const string CrmReservationsRead = "crm.reservations.read";
    public const string CrmReservationsManage = "crm.reservations.manage";
    public const string CrmDeliveryRead = "crm.delivery.read";
    public const string CrmDeliveryManage = "crm.delivery.manage";

    public const string SyncRead = "sync.read";
    public const string SyncPush = "sync.push";
    public const string SyncManage = "sync.manage";

    public const string IntegrationRead = "integration.read";
    public const string IntegrationManage = "integration.manage";
    public const string IntegrationDispatch = "integration.dispatch";
    public const string IntegrationTerminal = "integration.terminal";

    public const string EInvoiceRead = "einvoice.read";
    public const string EInvoiceManage = "einvoice.manage";

    public const string SettingsManage = "settings.manage";

    public static readonly IReadOnlyDictionary<string, string> Catalogue = new Dictionary<string, string>
    {
        [UsersRead] = "View users",
        [UsersWrite] = "Create/update/deactivate users",
        [RolesManage] = "Manage roles and their permissions",
        [DevicesManage] = "Register and approve devices",
        [AuditRead] = "Read the audit log",
        [MenuRead] = "View menu",
        [MenuManage] = "Manage menu categories, items, modifiers, pricing",
        [OrderRead] = "View orders/tables",
        [OrderCreate] = "Open orders and add items",
        [OrderManage] = "Transfer, split, merge, comp and void orders",
        [PaymentProcess] = "Take payments and close orders",
        [PaymentRefund] = "Issue refunds",
        [KitchenView] = "View kitchen display tickets",
        [KitchenManage] = "Advance/route kitchen tickets and stations",
        [InventoryRead] = "View stock",
        [InventoryManage] = "Manage stock, recipes, purchases, counts, wastage",
        [ShiftManage] = "Open/close cash registers and reconcile shifts",
        [ReportView] = "View reports",
        [FinanceRead] = "View income, expenses, cashflow, counterparties, and profit/loss",
        [FinanceManage] = "Create and manage finance accounts, counterparties, income, and expenses",
        [PrintRead] = "Preview receipts and kitchen order tickets",
        [PrintManage] = "Queue account receipts and kitchen order tickets for printing",
        [CrmCustomersRead] = "View CRM customers",
        [CrmCustomersManage] = "Create/update/block CRM customers and addresses",
        [CrmLoyaltyManage] = "Manage loyalty point transactions",
        [CrmCampaignsRead] = "View campaigns",
        [CrmCampaignsManage] = "Manage campaigns",
        [CrmCampaignsApply] = "Apply campaigns to orders",
        [CrmReservationsRead] = "View reservations",
        [CrmReservationsManage] = "Manage reservations",
        [CrmDeliveryRead] = "View delivery zones, couriers, and deliveries",
        [CrmDeliveryManage] = "Manage delivery zones, couriers, and dispatch",
        [SyncRead] = "Pull offline sync changes",
        [SyncPush] = "Push offline sync mutations",
        [SyncManage] = "Manage sync devices, outbox, pending mutations, and conflicts",
        [IntegrationRead] = "View integration connectors, webhooks, events, terminals, and commands",
        [IntegrationManage] = "Manage integration connectors, webhook subscriptions, and terminals",
        [IntegrationDispatch] = "Queue and dispatch integration webhook events",
        [IntegrationTerminal] = "Queue and complete POS/payment terminal commands",
        [EInvoiceRead] = "View e-Fatura / e-Arşiv documents",
        [EInvoiceManage] = "Issue, refresh, and cancel e-Fatura / e-Arşiv documents",
        [SettingsManage] = "Manage tenant/branch settings"
    };

    public static IReadOnlyDictionary<string, string[]> SystemRoleGrants => new Dictionary<string, string[]>
    {
        [SystemRoles.Owner] = [.. Catalogue.Keys],
        [SystemRoles.Manager] =
        [
            UsersRead, UsersWrite, RolesManage, DevicesManage, AuditRead,
            MenuRead, MenuManage, OrderRead, OrderCreate, OrderManage,
            PaymentProcess, PaymentRefund, KitchenView, KitchenManage,
            InventoryRead, InventoryManage, ShiftManage, ReportView,
            FinanceRead, FinanceManage, PrintRead, PrintManage,
            CrmCustomersRead, CrmCustomersManage, CrmLoyaltyManage,
            CrmCampaignsRead, CrmCampaignsManage, CrmCampaignsApply,
            CrmReservationsRead, CrmReservationsManage,
            CrmDeliveryRead, CrmDeliveryManage,
            SyncRead, SyncPush, SyncManage,
            IntegrationRead, IntegrationManage, IntegrationDispatch, IntegrationTerminal,
            EInvoiceRead, EInvoiceManage,
            SettingsManage
        ],
        [SystemRoles.Cashier] =
        [
            MenuRead, OrderRead, OrderCreate, OrderManage,
            PaymentProcess, KitchenView, ShiftManage, ReportView,
            FinanceRead, PrintRead, PrintManage,
            CrmCustomersRead, CrmCustomersManage, CrmLoyaltyManage,
            CrmCampaignsRead, CrmCampaignsApply, CrmReservationsRead,
            CrmDeliveryRead, SyncRead, SyncPush,
            IntegrationRead, IntegrationTerminal,
            EInvoiceRead, EInvoiceManage
        ],
        [SystemRoles.Waiter] =
        [
            MenuRead, OrderRead, OrderCreate, KitchenView,
            PrintRead, PrintManage,
            CrmCustomersRead, CrmReservationsRead, CrmDeliveryRead,
            SyncRead, SyncPush, IntegrationRead
        ]
    };
}

public static class SystemRoles
{
    public const string Owner = "owner";
    public const string Manager = "manager";
    public const string Cashier = "cashier";
    public const string Waiter = "waiter";

    public static readonly string[] All = [Owner, Manager, Cashier, Waiter];
}
