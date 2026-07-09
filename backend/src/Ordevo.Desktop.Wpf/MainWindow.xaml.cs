 using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Ordevo.Desktop.Wpf.Models;
using Ordevo.Desktop.Wpf.Services;

namespace Ordevo.Desktop.Wpf;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly OrdevoApiClient _api;
    private readonly DesktopSession _session;
    private readonly RealtimeClient _realtime;
    private readonly OfflineStore _offline;
    private bool _busy;
    private string _statusMessage = "Hazır";
    private string _userName = "";
    private string _selectedOrderText = "Adisyon detay";
    private string _paymentText = "Ödemeler";
    private string _syncStatusText = "Cihaz kaydı yok";
    private string _reportRangeText = "";
    private string _financeSummaryText = "";
    private string _receiptPreview = "";
    private string _kitchenTicketPreview = "";
    private OrderDto? _selectedOrder;
    private long _lastHighWatermark;
    private string? _deviceId;
    private DateTime _reportStart = DateTime.Today.AddDays(-30);
    private DateTime _reportEnd = DateTime.Today;
    private DateTime _financeStart = DateTime.Today.AddDays(-30);
    private DateTime _financeEnd = DateTime.Today;

    public MainWindow(OrdevoApiClient api, DesktopSession session, RealtimeClient realtime, OfflineStore offline)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _realtime = realtime;
        _offline = offline;
        _realtime.Changed += OnRealtimeChanged;
        DataContext = this;
        ApiBaseText = $"API: {_api.BaseAddress}";
        UpdateReportRangeText();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ApiBaseText { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public string UserName
    {
        get => _userName;
        private set => SetField(ref _userName, value);
    }

    public string SelectedOrderText
    {
        get => _selectedOrderText;
        private set => SetField(ref _selectedOrderText, value);
    }

    public string PaymentText
    {
        get => _paymentText;
        private set => SetField(ref _paymentText, value);
    }

    public string SyncStatusText
    {
        get => _syncStatusText;
        private set => SetField(ref _syncStatusText, value);
    }

    public string ReportRangeText
    {
        get => _reportRangeText;
        private set => SetField(ref _reportRangeText, value);
    }

    public string FinanceSummaryText
    {
        get => _financeSummaryText;
        private set => SetField(ref _financeSummaryText, value);
    }

    public string ReceiptPreview
    {
        get => _receiptPreview;
        private set => SetField(ref _receiptPreview, value);
    }

    public string KitchenTicketPreview
    {
        get => _kitchenTicketPreview;
        private set => SetField(ref _kitchenTicketPreview, value);
    }

    public ObservableCollection<DashboardMetric> Metrics { get; } = [];
    public ObservableCollection<OrderSummaryDto> OpenOrders { get; } = [];
    public ObservableCollection<SectionDto> Sections { get; } = [];
    public ObservableCollection<TableDto> Tables { get; } = [];
    public ObservableCollection<OrderSummaryDto> Orders { get; } = [];
    public ObservableCollection<OrderItemDto> OrderItems { get; } = [];
    public ObservableCollection<PaymentLineDto> Payments { get; } = [];
    public ObservableCollection<KitchenLine> KitchenLines { get; } = [];
    public ObservableCollection<StationDto> Stations { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public ObservableCollection<MenuItemDto> MenuItems { get; } = [];
    public ObservableCollection<ModifierGroupDto> ModifierGroups { get; } = [];
    public ObservableCollection<ModifierFlat> ModifierRows { get; } = [];
    public ObservableCollection<CustomerDto> Customers { get; } = [];
    public ObservableCollection<ReservationDto> Reservations { get; } = [];
    public ObservableCollection<CampaignDto> Campaigns { get; } = [];
    public ObservableCollection<CourierDto> Couriers { get; } = [];
    public ObservableCollection<DeliveryZoneDto> DeliveryZones { get; } = [];
    public ObservableCollection<UnitDto> Units { get; } = [];
    public ObservableCollection<StockItemDto> StockItems { get; } = [];
    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<StockMovementDto> StockMovements { get; } = [];
    public ObservableCollection<RegisterDto> Registers { get; } = [];
    public ObservableCollection<SessionDto> ShiftSessions { get; } = [];
    public ObservableCollection<PaymentBreakdownDto> PaymentBreakdown { get; } = [];
    public ObservableCollection<FinanceAccountDto> FinanceAccounts { get; } = [];
    public ObservableCollection<CounterpartyDto> Counterparties { get; } = [];
    public ObservableCollection<FinanceTransactionDto> FinanceTransactions { get; } = [];
    public ObservableCollection<CashflowDayDto> CashflowDays { get; } = [];
    public ObservableCollection<PrintJobDto> PrintJobs { get; } = [];
    public ObservableCollection<ConnectorDto> Connectors { get; } = [];
    public ObservableCollection<WebhookSubscriptionDto> Webhooks { get; } = [];
    public ObservableCollection<IntegrationEventDto> IntegrationEvents { get; } = [];
    public ObservableCollection<TerminalDto> Terminals { get; } = [];
    public ObservableCollection<TerminalCommandDto> TerminalCommands { get; } = [];
    public ObservableCollection<SyncEntityDto> SyncEntities { get; } = [];
    public ObservableCollection<PendingMutationDto> PendingMutations { get; } = [];
    public ObservableCollection<SyncConflictDto> SyncConflicts { get; } = [];
    public ObservableCollection<SyncChangeDto> SyncChanges { get; } = [];
    public ObservableCollection<DailySummaryDto> DailySummaries { get; } = [];
    public ObservableCollection<TopItemDto> TopItems { get; } = [];
    public ObservableCollection<HourlyDto> HourlyStats { get; } = [];
    public ObservableCollection<CategorySalesDto> CategorySales { get; } = [];
    public ObservableCollection<PaymentMethodDto> PaymentMethods { get; } = [];

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        LoginError.Text = "";
        await RunBusyAsync(async () =>
        {
            var result = await _api.LoginAsync(new LoginRequest(
                TenantBox.Text.Trim(),
                EmailBox.Text.Trim(),
                PasswordBox.Password,
                $"wpf:{Environment.MachineName}"));

            if (!result.IsSuccess || result.Value is null)
            {
                LoginError.Text = result.Error ?? "Giriş başarısız.";
                return;
            }

            _session.SignIn(result.Value);
            UserName = $"{result.Value.User.FullName} ({result.Value.User.TenantSlug})";
            _deviceId = await _offline.LoadStateAsync("sync.deviceId");
            if (long.TryParse(await _offline.LoadStateAsync("sync.highWatermark"), out var highWatermark))
                _lastHighWatermark = highWatermark;
            UpdateSyncStatus();

            LoginPanel.Visibility = Visibility.Collapsed;
            ShellPanel.Visibility = Visibility.Visible;
            StatusMessage = "Oturum açıldı.";

            await _realtime.ConnectAsync();
            await RefreshCurrentTabCoreAsync();
        });
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            await _realtime.DisconnectAsync();
            await _api.LogoutAsync();
            _session.SignOut();
            ClearAll();
            UserName = "";
            PasswordBox.Password = "";
            ShellPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
            StatusMessage = "Oturum kapandı.";
        });
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await RefreshCurrentTabAsync();

    private async void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source == MainTabs && _session.IsAuthenticated)
            await RefreshCurrentTabAsync();
    }

    private async void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_busy || OrdersGrid.SelectedItem is not OrderSummaryDto order)
            return;

        await RunBusyAsync(() => LoadOrderDetailAsync(order.Id));
    }

    private async void OnRealtimeChanged(string signal)
    {
        var refreshTask = await Dispatcher.InvokeAsync(() =>
        {
            StatusMessage = $"Realtime güncelleme: {signal}";
            return _session.IsAuthenticated ? RefreshCurrentTabAsync() : Task.CompletedTask;
        });
        await refreshTask;
    }

    private async Task RefreshCurrentTabAsync()
    {
        if (_busy || !_session.IsAuthenticated)
            return;

        await RunBusyAsync(RefreshCurrentTabCoreAsync, "Güncellendi.");
    }

    private async Task RefreshCurrentTabCoreAsync()
    {
        var tag = (MainTabs.SelectedItem as TabItem)?.Tag?.ToString() ?? "dashboard";
        switch (tag)
        {
            case "dashboard":
                await LoadDashboardAsync();
                break;
            case "tables":
                await LoadTablesAsync();
                break;
            case "orders":
                await LoadOrdersAsync();
                break;
            case "kitchen":
                await LoadKitchenAsync();
                break;
            case "menu":
                await LoadMenuAsync();
                break;
            case "crm":
                await LoadCrmAsync();
                break;
            case "inventory":
                await LoadInventoryAsync();
                break;
            case "shift":
                await LoadShiftAsync();
                break;
            case "finance":
                await LoadFinanceAsync();
                break;
            case "print":
                await LoadPrintAsync();
                break;
            case "integrations":
                await LoadIntegrationsAsync();
                break;
            case "sync":
                await LoadSyncAsync();
                break;
            case "reports":
                await LoadReportsAsync();
                break;
        }
    }

    private async Task LoadDashboardAsync()
    {
        var daily = await GetOneAsync<DailyStatsDto>("/api/reporting/daily");
        await FillAsync(OpenOrders, "/api/ordering/orders?status=open");
        await LoadKitchenAsync();
        await FillAsync(SyncEntities, "/api/sync/entities");
        await FillAsync(PendingMutations, "/api/sync/mutations/pending?take=20");
        await FillAsync(IntegrationEvents, "/api/integrations/events?status=pending&take=20");

        Metrics.Clear();
        Metrics.Add(new("Ciro", Money(daily?.Revenue ?? 0), "Bugün"));
        Metrics.Add(new("Sipariş", (daily?.OrderCount ?? 0).ToString(CultureInfo.CurrentCulture), "Bugün"));
        Metrics.Add(new("Açık adisyon", OpenOrders.Count.ToString(CultureInfo.CurrentCulture), "Salon ve paket"));
        Metrics.Add(new("Mutfak", KitchenLines.Count.ToString(CultureInfo.CurrentCulture), "Bekleyen kalem"));
        Metrics.Add(new("Sync/Outbox", $"{PendingMutations.Count}/{IntegrationEvents.Count}", "Pending"));
    }

    private async Task LoadTablesAsync()
    {
        await FillAsync(Sections, "/api/ordering/sections");
        await FillAsync(Tables, "/api/ordering/tables");
    }

    private async Task LoadOrdersAsync()
    {
        await FillAsync(Orders, "/api/ordering/orders?status=open");
        if (_selectedOrder is not null && Orders.Any(o => o.Id == _selectedOrder.Id))
            await LoadOrderDetailAsync(_selectedOrder.Id);
        else
            ClearOrderDetail();
    }

    private async Task LoadOrderDetailAsync(string orderId)
    {
        var order = await GetOneAsync<OrderDto>($"/api/ordering/orders/{orderId}");
        _selectedOrder = order;
        OrderItems.Clear();
        Payments.Clear();

        if (order is null)
        {
            ClearOrderDetail();
            return;
        }

        foreach (var item in order.Items)
            OrderItems.Add(item);

        SelectedOrderText = $"#{order.OrderNo} - {order.Status} - Toplam {Money(order.Total)}";

        var payments = await _api.GetAsync<PaymentsViewDto>($"/api/payment/orders/{order.Id}/payments");
        if (payments.IsSuccess && payments.Value is not null)
        {
            foreach (var payment in payments.Value.Payments)
                Payments.Add(payment);
            PaymentText = $"Ödemeler - Kalan {Money(payments.Value.Balance)}";
        }
        else
        {
            PaymentText = "Ödemeler";
        }
    }

    private async Task LoadKitchenAsync()
    {
        await FillAsync(Stations, "/api/kitchen/stations");
        var tickets = await GetListAsync<KdsTicketDto>("/api/kitchen/board");
        KitchenLines.Clear();
        foreach (var ticket in tickets)
        {
            foreach (var item in ticket.Items)
            {
                KitchenLines.Add(new KitchenLine(
                    ticket.OrderId,
                    item.OrderItemId,
                    ticket.OrderNo,
                    ticket.TableName,
                    item.ItemName,
                    item.Quantity,
                    item.CourseNo,
                    item.Status,
                    item.Station,
                    item.ElapsedSeconds / 60,
                    item.Note));
            }
        }
    }

    private async Task LoadMenuAsync()
    {
        await FillAsync(Categories, "/api/menu/categories");
        await FillAsync(MenuItems, "/api/menu/items");
        await FillAsync(ModifierGroups, "/api/menu/modifier-groups");

        ModifierRows.Clear();
        foreach (var group in ModifierGroups.OrderBy(g => g.Name))
        {
            if (group.Modifiers.Count == 0)
                ModifierRows.Add(new(group.Id, group.Name, "", "(boş)", 0, 0, group.IsRequired, true));

            foreach (var modifier in group.Modifiers.OrderBy(m => m.SortOrder))
                ModifierRows.Add(new(group.Id, group.Name, modifier.Id, modifier.Name, modifier.PriceDelta, modifier.SortOrder, group.IsRequired, modifier.IsActive));
        }
    }

    private async Task LoadCrmAsync()
    {
        await FillAsync(Customers, "/api/m9-crm/customers?take=100");
        await FillAsync(Reservations, $"/api/m9-crm/reservations?date={DateTime.Today:yyyy-MM-dd}");
        await FillAsync(Campaigns, "/api/m9-crm/campaigns?activeOnly=false");
        await FillAsync(Couriers, "/api/m9-crm/delivery/couriers");
        await FillAsync(DeliveryZones, "/api/m9-crm/delivery/zones");
    }

    private async Task LoadInventoryAsync()
    {
        await FillAsync(Units, "/api/inventory/units");
        await FillAsync(StockItems, "/api/inventory/stock-items");
        await FillAsync(Suppliers, "/api/inventory/suppliers");
    }

    private Task LoadShiftAsync() => FillAsync(Registers, "/api/shift/registers");

    private async Task LoadFinanceAsync()
    {
        var start = _financeStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = _financeEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var summary = await GetOneAsync<FinanceSummaryDto>($"/api/finance/summary?start={start}&end={end}");
        FinanceSummaryText = summary is null
            ? $"{start} - {end}"
            : $"{start} - {end} | Satış {Money(summary.SalesRevenue)} | Gider {Money(summary.Expenses + summary.PurchaseCosts)} | Net {Money(summary.NetProfit)}";

        await FillAsync(FinanceTransactions, $"/api/finance/transactions?start={start}&end={end}");
        await FillAsync(CashflowDays, $"/api/finance/cashflow?start={start}&end={end}");
        await FillAsync(FinanceAccounts, "/api/finance/accounts");
        await FillAsync(Counterparties, "/api/finance/counterparties");
    }

    private Task LoadPrintAsync() => FillAsync(PrintJobs, "/api/print/jobs?take=100");

    private async Task LoadIntegrationsAsync()
    {
        await FillAsync(Connectors, "/api/integrations/connectors");
        await FillAsync(Webhooks, "/api/integrations/webhooks/subscriptions");
        await FillAsync(IntegrationEvents, "/api/integrations/events?take=100");
        await FillAsync(Terminals, "/api/integrations/terminals");
        await FillAsync(TerminalCommands, "/api/integrations/terminal-commands?take=100");
    }

    private async Task LoadSyncAsync()
    {
        await FillAsync(SyncEntities, "/api/sync/entities");
        await FillAsync(PendingMutations, "/api/sync/mutations/pending?take=100");
        await FillAsync(SyncConflicts, "/api/sync/conflicts/open?take=100");
        UpdateSyncStatus();
    }

    private async Task LoadReportsAsync()
    {
        UpdateReportRangeText();
        var start = _reportStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = _reportEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await FillAsync(DailySummaries, $"/api/reporting/daily-summary?start={start}&end={end}");
        await FillAsync(TopItems, $"/api/reporting/top-items?start={start}&end={end}&limit=30");
        await FillAsync(HourlyStats, $"/api/reporting/hourly?date={_reportEnd:yyyy-MM-dd}");
        await FillAsync(CategorySales, $"/api/reporting/category-sales?start={start}&end={end}");
        await FillAsync(PaymentMethods, $"/api/reporting/payment-methods?start={start}&end={end}");
    }

    private async Task FillAsync<T>(ObservableCollection<T> target, string path)
    {
        var rows = await GetListAsync<T>(path);
        target.Clear();
        foreach (var row in rows)
            target.Add(row);
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string path)
    {
        var result = await _api.GetAsync<List<T>>(path);
        if (result.IsSuccess)
        {
            if (result.FromCache)
                StatusMessage = "API erişilemedi; lokal cache gösteriliyor.";
            return result.Value ?? [];
        }

        StatusMessage = result.Error ?? "API isteği başarısız.";
        return [];
    }

    private async Task<T?> GetOneAsync<T>(string path)
    {
        var result = await _api.GetAsync<T>(path);
        if (result.IsSuccess)
        {
            if (result.FromCache)
                StatusMessage = "API erişilemedi; lokal cache gösteriliyor.";
            return result.Value;
        }

        StatusMessage = result.Error ?? "API isteği başarısız.";
        return default;
    }

    private async Task RunBusyAsync(Func<Task> action, string? successMessage = null)
    {
        if (_busy)
            return;

        _busy = true;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            await action();
            if (!string.IsNullOrWhiteSpace(successMessage))
                StatusMessage = $"{successMessage} {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception)
        {
            StatusMessage = "İşlem şu anda tamamlanamadı. Lütfen tekrar deneyin.";
            MessageBox.Show(this, StatusMessage, "Ordevo Desktop", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            _busy = false;
        }
    }

    private async Task ExecuteCommandAsync(Func<Task<bool>> command)
    {
        await RunBusyAsync(async () =>
        {
            if (await command())
                await RefreshCurrentTabCoreAsync();
        });
    }

    private bool Report<T>(ApiResult<T> result, string success)
    {
        if (result.IsSuccess)
        {
            StatusMessage = success;
            return true;
        }

        StatusMessage = result.Error ?? "İşlem başarısız.";
        MessageBox.Show(this, StatusMessage, "Ordevo Desktop", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void ClearOrderDetail()
    {
        _selectedOrder = null;
        OrderItems.Clear();
        Payments.Clear();
        SelectedOrderText = "Adisyon detay";
        PaymentText = "Ödemeler";
    }

    private void ClearAll()
    {
        Metrics.Clear();
        OpenOrders.Clear();
        Sections.Clear();
        Tables.Clear();
        Orders.Clear();
        OrderItems.Clear();
        Payments.Clear();
        KitchenLines.Clear();
        Stations.Clear();
        Categories.Clear();
        MenuItems.Clear();
        ModifierGroups.Clear();
        ModifierRows.Clear();
        Customers.Clear();
        Reservations.Clear();
        Campaigns.Clear();
        Couriers.Clear();
        DeliveryZones.Clear();
        Units.Clear();
        StockItems.Clear();
        Suppliers.Clear();
        StockMovements.Clear();
        Registers.Clear();
        ShiftSessions.Clear();
        PaymentBreakdown.Clear();
        FinanceAccounts.Clear();
        Counterparties.Clear();
        FinanceTransactions.Clear();
        CashflowDays.Clear();
        PrintJobs.Clear();
        ReceiptPreview = "";
        KitchenTicketPreview = "";
        Connectors.Clear();
        Webhooks.Clear();
        IntegrationEvents.Clear();
        Terminals.Clear();
        TerminalCommands.Clear();
        SyncEntities.Clear();
        PendingMutations.Clear();
        SyncConflicts.Clear();
        SyncChanges.Clear();
        DailySummaries.Clear();
        TopItems.Clear();
        HourlyStats.Clear();
        CategorySales.Clear();
        PaymentMethods.Clear();
        ClearOrderDetail();
    }

    private async void AddSectionButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("name", "Bölüm adı"), Int("sort", "Sıra", "10") };
        if (!ShowForm("Bölüm Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(
            await _api.PostAsync<SectionDto>("/api/ordering/sections", new UpsertSectionRequest(S(fields, "name"), I(fields, "sort"))),
            "Bölüm oluşturuldu."));
    }

    private async void AddTableButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureTablesLookupAsync();
        var fields = TableFields();
        if (!ShowForm("Masa Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<TableDto>("/api/ordering/tables", TableRequest(fields)), "Masa oluşturuldu."));
    }

    private async void EditTableButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<TableDto>(TablesGrid, "Masa") is not { } table) return;
        await EnsureTablesLookupAsync();
        var fields = TableFields(table);
        if (!ShowForm("Masa Düzenle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<TableDto>($"/api/ordering/tables/{table.Id}", TableRequest(fields)), "Masa güncellendi."));
    }

    private async void DeleteTableButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<TableDto>(TablesGrid, "Masa") is not { } table || !Confirm($"{table.Name} silinsin mi?")) return;
        await ExecuteCommandAsync(async () => Report(await _api.DeleteAsync<NoContent>($"/api/ordering/tables/{table.Id}"), "Masa silindi."));
    }

    private async void OpenOrderButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureTablesLookupAsync();
        var fields = new[]
        {
            Combo("table", "Masa", TableOptions(includeEmpty: true)),
            Combo("type", "Tip", Options(("dinein", "Salon"), ("takeaway", "Gel-al"), ("delivery", "Paket"))),
            Int("guests", "Kişi", "1")
        };
        if (!ShowForm("Adisyon Aç", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<OrderDto>("/api/ordering/orders", new OpenOrderRequest(NS(fields, "table"), S(fields, "type"), I(fields, "guests"))), "Adisyon açıldı."));
    }

    private async void AddOrderItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderSummaryDto>(OrdersGrid, "Adisyon") is not { } order) return;
        await EnsureMenuLookupAsync();
        var fields = new[]
        {
            Combo("item", "Ürün", MenuItemOptions()),
            DecimalF("quantity", "Adet", "1"),
            Int("course", "Course", "1"),
            Multi("note", "Not", required: false)
        };
        if (!ShowForm("Ürün Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(
            await _api.PostAsync<OrderDto>($"/api/ordering/orders/{order.Id}/items", new AddItemRequest(S(fields, "item"), D(fields, "quantity"), null, I(fields, "course"), NS(fields, "note"))),
            "Ürün adisyona eklendi."));
    }

    private async void SetQuantityButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderItemDto>(OrderItemsGrid, "Adisyon kalemi") is not { } item) return;
        var fields = new[] { DecimalF("quantity", "Yeni adet", item.Quantity.ToString(CultureInfo.CurrentCulture)) };
        if (!ShowForm("Adet Değiştir", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<OrderDto>($"/api/ordering/orders/items/{item.Id}/quantity", new SetQuantityRequest(D(fields, "quantity"))), "Adet güncellendi."));
    }

    private async void VoidItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderItemDto>(OrderItemsGrid, "Adisyon kalemi") is not { } item) return;
        var fields = new[] { Multi("reason", "İptal nedeni", required: false) };
        if (!ShowForm("Kalem İptal", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<OrderDto>($"/api/ordering/orders/items/{item.Id}/void", new VoidItemRequest(NS(fields, "reason"))), "Kalem iptal edildi."));
    }

    private async void CompItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderItemDto>(OrderItemsGrid, "Adisyon kalemi") is not { } item) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<OrderDto>($"/api/ordering/orders/items/{item.Id}/comp", new { }), "Kalem ikram işaretlendi."));
    }

    private async void DiscountButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderSummaryDto>(OrdersGrid, "Adisyon") is not { } order) return;
        var fields = new[]
        {
            Combo("type", "Tip", Options(("percent", "Yüzde"), ("amount", "Tutar"))),
            DecimalF("value", "Değer", "10"),
            Multi("reason", "Neden", required: false)
        };
        if (!ShowForm("İndirim", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<OrderDto>($"/api/ordering/orders/{order.Id}/discounts", new ApplyDiscountRequest(S(fields, "type"), D(fields, "value"), NS(fields, "reason"))), "İndirim uygulandı."));
    }

    private async void TransferOrderButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderSummaryDto>(OrdersGrid, "Adisyon") is not { } order) return;
        await EnsureTablesLookupAsync();
        var fields = new[] { Combo("table", "Yeni masa", TableOptions(includeEmpty: false)) };
        if (!ShowForm("Adisyon Transfer", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<OrderDto>($"/api/ordering/orders/{order.Id}/transfer", new TransferRequest(S(fields, "table"))), "Adisyon transfer edildi."));
    }

    private async void AddPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderSummaryDto>(OrdersGrid, "Adisyon") is not { } order) return;
        var amount = (_selectedOrder?.Total ?? order.Total).ToString(CultureInfo.CurrentCulture);
        var fields = new[]
        {
            Combo("method", "Yöntem", Options(("cash", "Nakit"), ("card", "Kart"), ("online", "Online"), ("meal_card", "Yemek Kartı"))),
            DecimalF("amount", "Tutar", amount),
            DecimalF("tip", "Bahşiş", "0", required: false),
            Text("reference", "Referans", required: false)
        };
        if (!ShowForm("Ödeme Al", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<PaymentResultDto>($"/api/payment/orders/{order.Id}/payments", new AddPaymentRequest(S(fields, "method"), D(fields, "amount"), D(fields, "tip"), NS(fields, "reference"))), "Ödeme alındı."));
    }

    private async void CloseOrderButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderSummaryDto>(OrdersGrid, "Adisyon") is not { } order || !Confirm($"#{order.OrderNo} kapatılsın mı?")) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<OrderDto>($"/api/ordering/orders/{order.Id}/close", new { }), "Adisyon kapatıldı."));
    }

    private async void CancelOrderButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<OrderSummaryDto>(OrdersGrid, "Adisyon") is not { } order) return;
        var fields = new[] { Multi("reason", "İptal nedeni", required: false) };
        if (!ShowForm("Adisyon İptal", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<OrderDto>($"/api/ordering/orders/{order.Id}/cancel", new CancelOrderRequest(NS(fields, "reason"))), "Adisyon iptal edildi."));
    }

    private async void KitchenPreparingButton_Click(object sender, RoutedEventArgs e) => await SetKitchenStatusAsync("preparing");
    private async void KitchenReadyButton_Click(object sender, RoutedEventArgs e) => await SetKitchenStatusAsync("ready");
    private async void KitchenServedButton_Click(object sender, RoutedEventArgs e) => await SetKitchenStatusAsync("served");

    private async Task SetKitchenStatusAsync(string status)
    {
        if (Selected<KitchenLine>(KitchenGrid, "Mutfak kalemi") is not { } line) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/kitchen/items/{line.OrderItemId}/status", new SetItemStatusRequest(status)), "Mutfak durumu güncellendi."));
    }

    private async void KitchenBumpButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<KitchenLine>(KitchenGrid, "Mutfak kalemi") is not { } line) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/kitchen/orders/{line.OrderId}/bump", new { }), "Order bump edildi."));
    }

    private async void AddStationButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = StationFields();
        if (!ShowForm("İstasyon Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<StationDto>("/api/kitchen/stations", StationRequest(fields)), "İstasyon oluşturuldu."));
    }

    private async void EditStationButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<StationDto>(StationsGrid, "İstasyon") is not { } station) return;
        var fields = StationFields(station);
        if (!ShowForm("İstasyon Düzenle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<StationDto>($"/api/kitchen/stations/{station.Id}", StationRequest(fields)), "İstasyon güncellendi."));
    }

    private async void DeleteStationButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<StationDto>(StationsGrid, "İstasyon") is not { } station || !Confirm($"{station.Name} silinsin mi?")) return;
        await ExecuteCommandAsync(async () => Report(await _api.DeleteAsync<NoContent>($"/api/kitchen/stations/{station.Id}"), "İstasyon silindi."));
    }

    private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = CategoryFields();
        if (!ShowForm("Kategori Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<CategoryDto>("/api/menu/categories", CategoryRequest(fields)), "Kategori oluşturuldu."));
    }

    private async void EditCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<CategoryDto>(CategoriesGrid, "Kategori") is not { } category) return;
        var fields = CategoryFields(category);
        if (!ShowForm("Kategori Düzenle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<CategoryDto>($"/api/menu/categories/{category.Id}", CategoryRequest(fields)), "Kategori güncellendi."));
    }

    private async void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<CategoryDto>(CategoriesGrid, "Kategori") is not { } category || !Confirm($"{category.Name} silinsin mi?")) return;
        await ExecuteCommandAsync(async () => Report(await _api.DeleteAsync<NoContent>($"/api/menu/categories/{category.Id}"), "Kategori silindi."));
    }

    private async void AddMenuItemButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureMenuLookupAsync();
        var fields = MenuItemFields();
        if (!ShowForm("Ürün Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<MenuItemDto>("/api/menu/items", MenuItemRequest(fields)), "Ürün oluşturuldu."));
    }

    private async void EditMenuItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<MenuItemDto>(MenuItemsGrid, "Ürün") is not { } item) return;
        await EnsureMenuLookupAsync();
        var fields = MenuItemFields(item);
        if (!ShowForm("Ürün Düzenle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<MenuItemDto>($"/api/menu/items/{item.Id}", MenuItemRequest(fields)), "Ürün güncellendi."));
    }

    private async void DeleteMenuItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<MenuItemDto>(MenuItemsGrid, "Ürün") is not { } item || !Confirm($"{item.Name} silinsin mi?")) return;
        await ExecuteCommandAsync(async () => Report(await _api.DeleteAsync<NoContent>($"/api/menu/items/{item.Id}"), "Ürün silindi."));
    }

    private async void AddBarcodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<MenuItemDto>(MenuItemsGrid, "Ürün") is not { } item) return;
        var fields = new[] { Text("barcode", "Barkod") };
        if (!ShowForm("Barkod Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/menu/items/{item.Id}/barcodes", new AddBarcodeRequest(S(fields, "barcode"))), "Barkod eklendi."));
    }

    private async void AddModifierGroupButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("name", "Grup adı"), Int("min", "Min seçim", "0"), Int("max", "Max seçim", "1"), Bool("required", "Zorunlu", false) };
        if (!ShowForm("Modifier Grup", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<object>("/api/menu/modifier-groups", new UpsertModifierGroupRequest(S(fields, "name"), I(fields, "min"), I(fields, "max"), B(fields, "required"))), "Modifier grup oluşturuldu."));
    }

    private async void AddModifierButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureMenuLookupAsync();
        var selectedGroupId = (ModifiersGrid.SelectedItem as ModifierFlat)?.GroupId;
        var fields = new[] { Combo("group", "Grup", ModifierGroupOptions(), selectedGroupId), Text("name", "Ad"), DecimalF("price", "Fiyat farkı", "0"), Int("sort", "Sıra", "10"), Bool("active", "Aktif", true) };
        if (!ShowForm("Modifier Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<object>($"/api/menu/modifier-groups/{S(fields, "group")}/modifiers", new UpsertModifierRequest(S(fields, "name"), D(fields, "price"), I(fields, "sort"), B(fields, "active"))), "Modifier eklendi."));
    }

    private async void AssignModifierGroupsButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<MenuItemDto>(MenuItemsGrid, "Ürün") is not { } item) return;
        var defaultGroup = (ModifiersGrid.SelectedItem as ModifierFlat)?.GroupId ?? "";
        var fields = new[] { Text("groups", "Grup ID listesi (,)", defaultGroup) };
        if (!ShowForm("Modifier Grup Ata", fields)) return;
        var groupIds = S(fields, "groups").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<NoContent>($"/api/menu/items/{item.Id}/modifier-groups", new AssignModifierGroupsRequest(groupIds)), "Modifier grupları atandı."));
    }

    private async void AddCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = CustomerFields();
        if (!ShowForm("Müşteri Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<CustomerDto>("/api/m9-crm/customers", new CreateCustomerRequest(S(fields, "phone"), NS(fields, "name"), NS(fields, "email"), DateOrNull(fields, "birthday"), B(fields, "sms"), B(fields, "emailConsent"))), "Müşteri oluşturuldu."));
    }

    private async void EditCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<CustomerDto>(CustomersGrid, "Müşteri") is not { } customer) return;
        var fields = CustomerFields(customer);
        if (!ShowForm("Müşteri Düzenle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<CustomerDto>($"/api/m9-crm/customers/{customer.Id}", new UpdateCustomerRequest(NS(fields, "name"), NS(fields, "email"), DateOrNull(fields, "birthday"), NS(fields, "notes"), NS(fields, "preferences"), B(fields, "sms"), B(fields, "emailConsent"))), "Müşteri güncellendi."));
    }

    private async void AddCustomerAddressButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<CustomerDto>(CustomersGrid, "Müşteri") is not { } customer) return;
        var fields = new[] { Text("label", "Etiket", "Ev"), Text("line1", "Adres"), Text("line2", "Adres 2", required: false), Text("district", "İlçe", required: false), Text("city", "Şehir", required: false), Text("postal", "Posta kodu", required: false), Multi("note", "Teslimat notu", required: false), Bool("default", "Varsayılan", false) };
        if (!ShowForm("Adres Ekle", fields)) return;
        var request = new CreateCustomerAddressRequest(S(fields, "label"), S(fields, "line1"), NS(fields, "line2"), NS(fields, "district"), NS(fields, "city"), NS(fields, "postal"), null, null, NS(fields, "note"), B(fields, "default"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<object>($"/api/m9-crm/customers/{customer.Id}/addresses", request), "Adres eklendi."));
    }

    private async void BlockCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<CustomerDto>(CustomersGrid, "Müşteri") is not { } customer) return;
        var fields = new[] { Multi("reason", "Blok nedeni") };
        if (!ShowForm("Müşteri Blokla", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/m9-crm/customers/{customer.Id}/block", new BlockCustomerRequest(S(fields, "reason"))), "Müşteri bloklandı."));
    }

    private async void UnblockCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<CustomerDto>(CustomersGrid, "Müşteri") is not { } customer) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/m9-crm/customers/{customer.Id}/unblock", new { }), "Müşteri bloku kaldırıldı."));
    }

    private async void AddReservationButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureTablesLookupAsync();
        var customer = CustomersGrid.SelectedItem as CustomerDto;
        var fields = new[] { Text("name", "Ad", customer?.FullName ?? ""), Text("phone", "Telefon", customer?.Phone ?? ""), DateF("date", "Tarih", DateTime.Today), Text("time", "Saat", "19:00"), Int("guest", "Kişi", "2"), Combo("table", "Masa", TableOptions(includeEmpty: true)), Multi("notes", "Not", required: false) };
        if (!ShowForm("Rezervasyon", fields)) return;
        var request = new CreateReservationRequest(customer?.Id, S(fields, "name"), S(fields, "phone"), Date(fields, "date"), S(fields, "time"), I(fields, "guest"), NS(fields, "table"), NS(fields, "notes"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<ReservationDto>("/api/m9-crm/reservations", request), "Rezervasyon oluşturuldu."));
    }

    private async void ReservationStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<ReservationDto>(ReservationsGrid, "Rezervasyon") is not { } reservation) return;
        var fields = new[] { Combo("status", "Durum", Options(("confirmed", "Onaylı"), ("seated", "Oturdu"), ("cancelled", "İptal"), ("no_show", "Gelmedi"))), Multi("reason", "Neden", required: false) };
        if (!ShowForm("Rezervasyon Durum", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<ReservationDto>($"/api/m9-crm/reservations/{reservation.Id}/status", new SetReservationStatusRequest(S(fields, "status"), NS(fields, "reason"))), "Rezervasyon durumu güncellendi."));
    }

    private async void AddCampaignButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("code", "Kod"), Text("name", "Ad"), Multi("desc", "Açıklama", required: false), Combo("type", "İndirim tipi", Options(("percent", "Yüzde"), ("amount", "Tutar"))), DecimalF("value", "Değer", "10"), DecimalF("min", "Min sepet", "0", required: false), DateF("start", "Başlangıç", DateTime.Today), DateF("end", "Bitiş", DateTime.Today.AddMonths(1), required: false), Bool("active", "Aktif", true), Bool("auto", "Otomatik", false), Int("priority", "Öncelik", "10") };
        if (!ShowForm("Kampanya", fields)) return;
        var request = new CreateCampaignRequest(null, S(fields, "code"), S(fields, "name"), NS(fields, "desc"), S(fields, "type"), D(fields, "value"), null, DecimalOrNull(fields, "min"), null, null, Date(fields, "start"), DateOrNull(fields, "end"), B(fields, "active"), B(fields, "auto"), I(fields, "priority"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<object>("/api/m9-crm/campaigns", request), "Kampanya oluşturuldu."));
    }

    private async void AddCourierButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("name", "Ad"), Text("phone", "Telefon"), Text("plate", "Plaka", required: false), Combo("vehicle", "Araç", Options(("motorbike", "Motor"), ("car", "Araç"), ("bike", "Bisiklet"), ("walk", "Yaya"))), Bool("active", "Aktif", true) };
        if (!ShowForm("Kurye", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<CourierDto>("/api/m9-crm/delivery/couriers", new CreateCourierRequest(null, S(fields, "name"), S(fields, "phone"), NS(fields, "plate"), S(fields, "vehicle"), B(fields, "active"))), "Kurye oluşturuldu."));
    }

    private async void CourierStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<CourierDto>(CouriersGrid, "Kurye") is not { } courier) return;
        var fields = new[] { Combo("status", "Durum", Options(("available", "Müsait"), ("busy", "Meşgul"), ("offline", "Offline"))) };
        if (!ShowForm("Kurye Durum", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<CourierDto>($"/api/m9-crm/delivery/couriers/{courier.Id}/status", new SetCourierStatusRequest(S(fields, "status"))), "Kurye durumu güncellendi."));
    }

    private async void AddDeliveryZoneButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("name", "Zone adı"), DecimalF("lat", "Merkez lat", "0"), DecimalF("lng", "Merkez lng", "0"), DecimalF("radius", "Yarıçap km", "3"), DecimalF("fee", "Teslimat ücreti", "0"), DecimalF("min", "Min sepet", "0"), DecimalF("free", "Ücretsiz üstü", "0", required: false), Int("minutes", "Tahmini dakika", "30"), Bool("active", "Aktif", true) };
        if (!ShowForm("Delivery Zone", fields)) return;
        var request = new CreateDeliveryZoneRequest(S(fields, "name"), D(fields, "lat"), D(fields, "lng"), D(fields, "radius"), D(fields, "fee"), D(fields, "min"), DecimalOrNull(fields, "free"), I(fields, "minutes"), B(fields, "active"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<DeliveryZoneDto>("/api/m9-crm/delivery/zones", request), "Delivery zone oluşturuldu."));
    }

    private async void AddUnitButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("code", "Kod"), Text("name", "Ad") };
        if (!ShowForm("Birim Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<UnitDto>("/api/inventory/units", new CreateUnitRequest(S(fields, "code"), S(fields, "name"))), "Birim oluşturuldu."));
    }

    private async void AddStockItemButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureInventoryLookupAsync();
        var fields = StockItemFields();
        if (!ShowForm("Stok Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<StockItemDto>("/api/inventory/stock-items", StockItemRequest(fields)), "Stok kalemi oluşturuldu."));
    }

    private async void EditStockItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<StockItemDto>(StockItemsGrid, "Stok kalemi") is not { } stock) return;
        await EnsureInventoryLookupAsync();
        var fields = StockItemFields(stock);
        if (!ShowForm("Stok Düzenle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PutAsync<StockItemDto>($"/api/inventory/stock-items/{stock.Id}", StockItemRequest(fields)), "Stok kalemi güncellendi."));
    }

    private async void AdjustStockButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<StockItemDto>(StockItemsGrid, "Stok kalemi") is not { } stock) return;
        var fields = new[] { DecimalF("quantity", "Yeni miktar", stock.OnHand.ToString(CultureInfo.CurrentCulture)), Multi("reason", "Neden", required: false) };
        if (!ShowForm("Stok Ayarla", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<StockItemDto>($"/api/inventory/stock-items/{stock.Id}/adjust", new AdjustStockRequest(D(fields, "quantity"), NS(fields, "reason"))), "Stok miktarı güncellendi."));
    }

    private async void AddSupplierButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("name", "Ad"), Text("phone", "Telefon", required: false), Text("email", "E-posta", required: false), Text("tax", "Vergi no", required: false) };
        if (!ShowForm("Tedarikçi Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<SupplierDto>("/api/inventory/suppliers", new CreateSupplierRequest(S(fields, "name"), NS(fields, "phone"), NS(fields, "email"), NS(fields, "tax"))), "Tedarikçi oluşturuldu."));
    }

    private async void CreatePurchaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<StockItemDto>(StockItemsGrid, "Stok kalemi") is not { } stock) return;
        await EnsureInventoryLookupAsync();
        var fields = new[] { Combo("supplier", "Tedarikçi", SupplierOptions(includeEmpty: true)), DecimalF("quantity", "Miktar", "1"), DecimalF("cost", "Birim maliyet", stock.UnitCost.ToString(CultureInfo.CurrentCulture)), Multi("note", "Not", required: false) };
        if (!ShowForm("Satın Alma", fields)) return;
        var request = new CreatePurchaseRequest(NS(fields, "supplier"), NS(fields, "note"), [new PurchaseLineInput(stock.Id, D(fields, "quantity"), D(fields, "cost"))]);
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<object>("/api/inventory/purchases", request), "Satın alma oluşturuldu."));
    }

    private async void RecordWastageButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<StockItemDto>(StockItemsGrid, "Stok kalemi") is not { } stock) return;
        var fields = new[] { DecimalF("quantity", "Fire miktarı", "1"), Multi("reason", "Neden", required: false) };
        if (!ShowForm("Fire Kaydet", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<object>("/api/inventory/wastage", new RecordWastageRequest(stock.Id, D(fields, "quantity"), NS(fields, "reason"))), "Fire kaydedildi."));
    }

    private async void LoadMovementsButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<StockItemDto>(StockItemsGrid, "Stok kalemi") is not { } stock) return;
        await RunBusyAsync(async () => await FillAsync(StockMovements, $"/api/inventory/stock-items/{stock.Id}/movements"), "Hareketler yüklendi.");
    }

    private async void AddRegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("name", "Kasa adı") };
        if (!ShowForm("Kasa Ekle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<RegisterDto>("/api/shift/registers", new CreateRegisterRequest(S(fields, "name"))), "Kasa oluşturuldu."));
    }

    private async void OpenSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<RegisterDto>(RegistersGrid, "Kasa") is not { } register) return;
        var fields = new[] { DecimalF("amount", "Açılış tutarı", "0") };
        if (!ShowForm("Kasa Oturumu Aç", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<SessionDto>("/api/shift/sessions/open", new OpenSessionRequest(register.Id, D(fields, "amount"))), "Kasa oturumu açıldı."));
    }

    private async void LoadOpenSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<RegisterDto>(RegistersGrid, "Kasa") is not { } register) return;
        await RunBusyAsync(async () => await LoadOpenSessionForRegisterAsync(register.Id), "Açık oturum yüklendi.");
    }

    private async Task LoadOpenSessionForRegisterAsync(string registerId)
    {
        var session = await GetOneAsync<SessionDto>($"/api/shift/registers/{registerId}/open-session");
        ShiftSessions.Clear();
        if (session is not null)
            ShiftSessions.Add(session);
    }

    private async void PayInButton_Click(object sender, RoutedEventArgs e) => await CashMoveAsync("pay-in", "Para Giriş");
    private async void PayOutButton_Click(object sender, RoutedEventArgs e) => await CashMoveAsync("pay-out", "Para Çıkış");

    private async Task CashMoveAsync(string path, string title)
    {
        var session = ShiftSessions.FirstOrDefault();
        if (session is null)
        {
            StatusMessage = "Önce açık kasa oturumunu yükleyin.";
            return;
        }

        var fields = new[] { DecimalF("amount", "Tutar", "0"), Multi("note", "Not", required: false) };
        if (!ShowForm(title, fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<SessionDto>($"/api/shift/sessions/{session.Id}/{path}", new CashMoveRequest(D(fields, "amount"), NS(fields, "note"))), $"{title} kaydedildi."));
    }

    private async void CloseSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var session = ShiftSessions.FirstOrDefault();
        if (session is null)
        {
            StatusMessage = "Önce açık kasa oturumunu yükleyin.";
            return;
        }

        var fields = new[] { DecimalF("counted", "Sayım tutarı", "0"), Multi("note", "Not", required: false) };
        if (!ShowForm("Kasa Oturumu Kapat", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<CloseSessionResult>($"/api/shift/sessions/{session.Id}/close", new CloseSessionRequest(D(fields, "counted"), NS(fields, "note"))), "Kasa oturumu kapatıldı."));
    }

    private async void ZReportButton_Click(object sender, RoutedEventArgs e)
    {
        var session = ShiftSessions.FirstOrDefault();
        if (session is null)
        {
            StatusMessage = "Önce kasa oturumunu yükleyin.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            var report = await GetOneAsync<ZReportDto>($"/api/shift/sessions/{session.Id}/z-report");
            PaymentBreakdown.Clear();
            if (report is null) return;
            foreach (var row in report.PaymentBreakdown)
                PaymentBreakdown.Add(row);
            StatusMessage = $"Z rapor: satış {Money(report.GrossSales)}, beklenen kasa {Money(report.ExpectedCash)}";
        });
    }

    private async void RefreshFinanceButton_Click(object sender, RoutedEventArgs e)
    {
        var fields =
            new[] { DateF("start", "Başlangıç", _financeStart), DateF("end", "Bitiş", _financeEnd) };
        if (!ShowForm("Finans Aralığı", fields)) return;
        _financeStart = Date(fields, "start");
        _financeEnd = Date(fields, "end");
        await RunBusyAsync(LoadFinanceAsync, "Finans güncellendi.");
    }

    private async void AddFinanceTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureFinanceLookupAsync();
        var fields = new[]
        {
            Combo("type", "Tip", Options(("income", "Gelir"), ("expense", "Gider"), ("adjustment", "Düzeltme")), "expense"),
            Combo("method", "Yöntem", Options(("cash", "Nakit"), ("card", "Kart"), ("bank", "Banka"), ("online", "Online"), ("meal_voucher", "Yemek kartı"), ("on_account", "Cari"), ("other", "Diğer")), "cash"),
            Text("category", "Kategori", "Genel"),
            DecimalF("amount", "Tutar", "0"),
            DecimalF("tax", "Vergi", "0"),
            DateF("date", "Tarih", DateTime.Today),
            Combo("account", "Hesap", FinanceAccountOptions(includeEmpty: true), required: false),
            Combo("counterparty", "Cari", CounterpartyOptions(includeEmpty: true), required: false),
            Multi("description", "Açıklama", required: false)
        };
        if (!ShowForm("Gelir/Gider", fields)) return;
        var request = new CreateFinanceTransactionRequest(NS(fields, "account"), NS(fields, "counterparty"), S(fields, "type"), S(fields, "category"), S(fields, "method"), D(fields, "amount"), D(fields, "tax"), Date(fields, "date"), NS(fields, "description"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<FinanceTransactionDto>("/api/finance/transactions", request), "Finans hareketi kaydedildi."));
        await LoadFinanceAsync();
    }

    private async void AddFinanceAccountButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[]
        {
            Text("name", "Hesap adı"),
            Combo("type", "Tip", Options(("cash", "Nakit"), ("bank", "Banka"), ("card", "Kart"), ("online", "Online"), ("supplier", "Tedarikçi"), ("customer", "Müşteri"), ("other", "Diğer")), "cash"),
            Text("currency", "Para birimi", "TRY"),
            DecimalF("opening", "Açılış bakiyesi", "0")
        };
        if (!ShowForm("Finans Hesabı", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<FinanceAccountDto>("/api/finance/accounts", new CreateFinanceAccountRequest(S(fields, "name"), S(fields, "type"), S(fields, "currency"), D(fields, "opening"))), "Finans hesabı oluşturuldu."));
        await LoadFinanceAsync();
    }

    private async void AddCounterpartyButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[]
        {
            Combo("type", "Tip", Options(("supplier", "Tedarikçi"), ("customer", "Müşteri"), ("staff", "Personel"), ("other", "Diğer")), "supplier"),
            Text("name", "Ad"),
            Text("phone", "Telefon", required: false),
            Text("email", "E-posta", required: false),
            Text("tax", "Vergi no", required: false)
        };
        if (!ShowForm("Cari", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<CounterpartyDto>("/api/finance/counterparties", new CreateCounterpartyRequest(S(fields, "type"), null, S(fields, "name"), NS(fields, "phone"), NS(fields, "email"), NS(fields, "tax"))), "Cari oluşturuldu."));
        await LoadFinanceAsync();
    }

    private async void RefreshPrintButton_Click(object sender, RoutedEventArgs e)
        => await RunBusyAsync(LoadPrintAsync, "Yazdırma kuyruğu güncellendi.");

    private async void PreviewReceiptButton_Click(object sender, RoutedEventArgs e)
    {
        var orderId = AskOrderId("Hesap Önizle");
        if (orderId is null) return;
        await RunBusyAsync(async () =>
        {
            var doc = await GetOneAsync<ReceiptDocumentDto>($"/api/print/orders/{Uri.EscapeDataString(orderId)}/receipt");
            if (doc is not null)
                ReceiptPreview = doc.PlainText;
        });
    }

    private async void PreviewKitchenTicketButton_Click(object sender, RoutedEventArgs e)
    {
        var orderId = AskOrderId("Mutfak Önizle");
        if (orderId is null) return;
        await RunBusyAsync(async () =>
        {
            var doc = await GetOneAsync<KitchenTicketDocumentDto>($"/api/print/orders/{Uri.EscapeDataString(orderId)}/kitchen-ticket");
            if (doc is not null)
                KitchenTicketPreview = doc.PlainText;
        });
    }

    private async void QueueReceiptButton_Click(object sender, RoutedEventArgs e)
        => await QueuePrintAsync("receipt", "Hesap Yazdır");

    private async void QueueKitchenTicketButton_Click(object sender, RoutedEventArgs e)
        => await QueuePrintAsync("kitchen-ticket", "Mutfak Yazdır");

    private async Task QueuePrintAsync(string kind, string title)
    {
        var fields = new[]
        {
            Text("order", "Adisyon ID"),
            Text("terminal", "Terminal ID", required: false),
            Text("printer", "Yazıcı", required: false),
            Int("copies", "Kopya", "1")
        };
        if (!ShowForm(title, fields)) return;
        var orderId = S(fields, "order");
        var endpointKind = kind == "receipt" ? "receipt" : "kitchen-ticket";
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<PrintJobDto>($"/api/print/orders/{Uri.EscapeDataString(orderId)}/{endpointKind}/queue", new QueuePrintRequest(NS(fields, "terminal"), I(fields, "copies"), NS(fields, "printer"))), "Yazdırma kuyruğuna alındı."));
        await LoadPrintAsync();
    }

    private async void SaveEscPosButton_Click(object sender, RoutedEventArgs e)
    {
        var orderId = AskOrderId("ESC/POS Kaydet");
        if (orderId is null) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"receipt-{orderId}.escpos",
            Filter = "ESC/POS (*.escpos)|*.escpos|Tüm dosyalar (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        var target = dialog.FileName;
        await RunBusyAsync(async () =>
        {
            var res = await _api.DownloadBytesAsync($"/api/print/orders/{Uri.EscapeDataString(orderId)}/receipt/escpos");
            if (res.Value is not null)
                await System.IO.File.WriteAllBytesAsync(target, res.Value);
        }, "ESC/POS fişi kaydedildi.");
    }

    private async void IssueEInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[]
        {
            Text("order", "Adisyon ID"),
            Combo("type", "Belge tipi", Options(("", "Otomatik"), ("efatura", "e-Fatura"), ("earsiv", "e-Arşiv"))),
            Text("buyer", "Alıcı adı", required: false),
            Text("taxno", "VKN/TCKN", required: false),
            Text("taxoffice", "Vergi dairesi", required: false)
        };
        if (!ShowForm("e-Fatura / e-Arşiv Kes", fields)) return;
        var orderId = S(fields, "order");
        var request = new IssueEInvoiceRequest(NS(fields, "type"), NS(fields, "buyer"), NS(fields, "taxno"), NS(fields, "taxoffice"), null, null, null, null);
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<EInvoiceDocumentDto>($"/api/einvoice/orders/{Uri.EscapeDataString(orderId)}/issue", request), "e-Fatura/e-Arşiv belgesi kesildi."));
    }

    private async void AddConnectorButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("code", "Kod"), Text("name", "Ad"), Text("type", "Connector tipi", "payment"), Text("provider", "Provider", "manual"), Text("url", "Base URL", required: false), Combo("auth", "Auth", Options(("none", "Yok"), ("api_key", "API Key"), ("oauth2", "OAuth2"))), Text("secret", "Secret ref", required: false), Multi("settings", "Settings JSON", "{}", required: false) };
        if (!ShowForm("Connector", fields)) return;
        var request = new CreateConnectorRequest(S(fields, "code"), S(fields, "name"), S(fields, "type"), S(fields, "provider"), null, NS(fields, "url"), S(fields, "auth"), NS(fields, "secret"), NS(fields, "settings"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<ConnectorDto>("/api/integrations/connectors", request), "Connector oluşturuldu."));
    }

    private async void ConnectorStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<ConnectorDto>(ConnectorsGrid, "Connector") is not { } connector) return;
        var fields = new[] { Combo("status", "Durum", Options(("active", "Aktif"), ("disabled", "Pasif"), ("failed", "Hatalı"))), Multi("reason", "Neden", required: false) };
        if (!ShowForm("Connector Durum", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/integrations/connectors/{connector.Id}/status", new SetConnectorStatusRequest(S(fields, "status"), NS(fields, "reason"))), "Connector durumu güncellendi."));
    }

    private async void AddWebhookButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureIntegrationLookupAsync();
        var fields = new[] { Text("name", "Ad"), Text("url", "Target URL"), Combo("connector", "Connector", ConnectorOptions(includeEmpty: true)), Text("pattern", "Event pattern", "*"), Multi("headers", "Headers JSON", "{}", required: false), Int("attempts", "Max deneme", "5"), Int("timeout", "Timeout sn", "15") };
        if (!ShowForm("Webhook", fields)) return;
        var request = new CreateWebhookSubscriptionRequest(S(fields, "name"), S(fields, "url"), null, NS(fields, "connector"), null, S(fields, "pattern"), null, NS(fields, "headers"), I(fields, "attempts"), I(fields, "timeout"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<WebhookSubscriptionDto>("/api/integrations/webhooks/subscriptions", request), "Webhook oluşturuldu."));
    }

    private async void WebhookStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<WebhookSubscriptionDto>(WebhooksGrid, "Webhook") is not { } webhook) return;
        var fields = new[] { Combo("status", "Durum", Options(("active", "Aktif"), ("disabled", "Pasif"))) };
        if (!ShowForm("Webhook Durum", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/integrations/webhooks/subscriptions/{webhook.Id}/status", new SetWebhookStatusRequest(S(fields, "status"))), "Webhook durumu güncellendi."));
    }

    private async void QueueEventButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("module", "Source module", "desktop"), Text("event", "Event type", "manual.event"), Text("aggregateType", "Aggregate type", "manual"), Text("aggregateId", "Aggregate id", Guid.NewGuid().ToString("N")), Multi("payload", "Payload JSON", "{}"), Text("correlation", "Correlation", required: false) };
        if (!ShowForm("Event Kuyruk", fields)) return;
        var request = new QueueIntegrationEventRequest(S(fields, "module"), S(fields, "event"), S(fields, "aggregateType"), S(fields, "aggregateId"), S(fields, "payload"), null, NS(fields, "correlation"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<object>("/api/integrations/events", request), "Event kuyruğa alındı."));
    }

    private async void AddTerminalButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureIntegrationLookupAsync();
        var fields = new[] { Text("name", "Ad"), Text("type", "Terminal tipi", "pos"), Combo("connector", "Connector", ConnectorOptions(includeEmpty: true)), Text("providerId", "Provider terminal id", required: false), Combo("mode", "Bağlantı", Options(("cloud", "Cloud"), ("lan", "LAN"), ("serial", "Serial"))), Text("ip", "IP", required: false), Int("port", "Port", "0", required: false), Text("serial", "Serial path", required: false), Multi("settings", "Settings JSON", "{}", required: false) };
        if (!ShowForm("Terminal", fields)) return;
        var port = I(fields, "port");
        var request = new RegisterTerminalRequest(S(fields, "name"), S(fields, "type"), null, NS(fields, "connector"), null, NS(fields, "providerId"), S(fields, "mode"), NS(fields, "ip"), port == 0 ? null : port, NS(fields, "serial"), NS(fields, "settings"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<TerminalDto>("/api/integrations/terminals", request), "Terminal oluşturuldu."));
    }

    private async void QueueTerminalCommandButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureIntegrationLookupAsync();
        var selectedOrderId = (OrdersGrid.SelectedItem as OrderSummaryDto)?.Id;
        var fields = new[] { Text("type", "Komut tipi", "payment.request"), Multi("payload", "Payload JSON", "{}"), Combo("connector", "Connector", ConnectorOptions(includeEmpty: true)), Combo("terminal", "Terminal", TerminalOptions(includeEmpty: true)), Text("order", "Order id", selectedOrderId ?? "", required: false), Text("payment", "Payment id", required: false), Text("idem", "Idempotency", Guid.NewGuid().ToString("N"), required: false) };
        if (!ShowForm("Terminal Komutu", fields)) return;
        var request = new QueueTerminalCommandRequest(S(fields, "type"), S(fields, "payload"), null, NS(fields, "connector"), NS(fields, "terminal"), NS(fields, "order"), NS(fields, "payment"), NS(fields, "idem"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<object>("/api/integrations/terminal-commands", request), "Terminal komutu kuyruğa alındı."));
    }

    private async void MarkCommandSentButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<TerminalCommandDto>(CommandsGrid, "Komut") is not { } command) return;
        var fields = new[] { Text("ref", "Provider ref", required: false) };
        if (!ShowForm("Komut Sent", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/integrations/terminal-commands/{command.Id}/sent", new MarkCommandSentRequest(NS(fields, "ref"))), "Komut sent işaretlendi."));
    }

    private async void MarkCommandCompletedButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<TerminalCommandDto>(CommandsGrid, "Komut") is not { } command) return;
        var fields = new[] { Text("ref", "Provider ref", required: false), Multi("payload", "Result JSON", "{}", required: false) };
        if (!ShowForm("Komut Completed", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/integrations/terminal-commands/{command.Id}/completed", new MarkCommandCompletedRequest(NS(fields, "ref"), NS(fields, "payload"))), "Komut tamamlandı."));
    }

    private async void MarkCommandFailedButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected<TerminalCommandDto>(CommandsGrid, "Komut") is not { } command) return;
        var fields = new[] { Text("code", "Hata kodu", required: false), Multi("message", "Hata mesajı", required: false), Multi("payload", "Result JSON", "{}", required: false) };
        if (!ShowForm("Komut Failed", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/integrations/terminal-commands/{command.Id}/failed", new MarkCommandFailedRequest(NS(fields, "code"), NS(fields, "message"), NS(fields, "payload"))), "Komut failed işaretlendi."));
    }

    private async void RegisterDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Text("name", "Cihaz adı", Environment.MachineName), Combo("type", "Tip", Options(("desktop", "Desktop"), ("pos", "POS"), ("kds", "KDS"))), Text("fingerprint", "Fingerprint", $"wpf:{Environment.MachineName}") };
        if (!ShowForm("Cihaz Kaydet", fields)) return;
        await RunBusyAsync(async () =>
        {
            var result = await _api.PostAsync<DeviceDto>("/api/sync/devices/register-approved", new RegisterDeviceRequest(S(fields, "name"), S(fields, "type"), S(fields, "fingerprint")));
            if (!Report(result, "Cihaz kaydedildi.") || result.Value is null) return;
            _deviceId = result.Value.Id;
            await _offline.SaveStateAsync("sync.deviceId", _deviceId);
            UpdateSyncStatus();
            await LoadSyncAsync();
        });
    }

    private async void HeartbeatButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>("/api/sync/heartbeat", new HeartbeatRequest(_deviceId, "wpf-sqlite", "1.0.0")), "Heartbeat gönderildi."));
    }

    private async void PullChangesButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { Int("take", "Adet", "250") };
        if (!ShowForm("Pull Changes", fields)) return;
        await RunBusyAsync(async () =>
        {
            var result = await _api.GetAsync<PullChangesResponse>($"/api/sync/pull?since={_lastHighWatermark}&take={I(fields, "take")}");
            if (!Report(result, "Değişiklikler çekildi.") || result.Value is null) return;
            SyncChanges.Clear();
            foreach (var change in result.Value.Changes)
                SyncChanges.Add(change);
            _lastHighWatermark = result.Value.HighWatermark;
            await _offline.SaveStateAsync("sync.highWatermark", _lastHighWatermark.ToString(CultureInfo.InvariantCulture));
            UpdateSyncStatus();
        });
    }

    private async void AckPullButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>("/api/sync/ack", new AckPullRequest(_deviceId, _lastHighWatermark)), "Pull ack gönderildi."));
    }

    private async void PushMutationButton_Click(object sender, RoutedEventArgs e)
    {
        await EnsureSyncLookupAsync();
        var fields = new[] { Combo("entity", "Entity", SyncEntityOptions()), Text("entityId", "Entity id"), Combo("op", "Operation", Options(("insert", "Insert"), ("update", "Update"), ("delete", "Delete"))), Multi("payload", "Payload JSON", "{}"), Text("base", "Base change version", required: false), Text("row", "Expected row version", required: false) };
        if (!ShowForm("Push Mutation", fields)) return;
        var mutation = new ClientMutationRequest(Guid.NewGuid().ToString("N"), S(fields, "entity"), S(fields, "entityId"), S(fields, "op"), LongOrNull(fields, "base"), LongOrNull(fields, "row"), NS(fields, "payload"));
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<PushChangesResponse>("/api/sync/push", new PushChangesRequest(_deviceId, [mutation])), "Mutation gönderildi."));
    }

    private async void RefreshReportsButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = ReportRangeFields();
        if (!ShowForm("Rapor Aralığı", fields)) return;
        _reportStart = Date(fields, "start");
        _reportEnd = Date(fields, "end");
        await RunBusyAsync(LoadReportsAsync, "Raporlar güncellendi.");
    }

    private async void ArchiveDayButton_Click(object sender, RoutedEventArgs e)
    {
        var fields = new[] { DateF("date", "Gün", DateTime.Today) };
        if (!ShowForm("Gün Arşivle", fields)) return;
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>($"/api/reporting/archive?date={Date(fields, "date"):yyyy-MM-dd}", new { }), "Gün arşivlendi."));
    }

    private async void RefreshMaterializedViewButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCommandAsync(async () => Report(await _api.PostAsync<NoContent>("/api/reporting/refresh-mv", new { }), "Materialized view yenilendi."));
    }

    private async void ExportMlCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var start = _reportStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = _reportEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await RunBusyAsync(async () =>
        {
            var result = await _api.DownloadStringAsync($"/api/reporting/ml-export?start={start}&end={end}");
            if (!result.IsSuccess || result.Value is null)
            {
                Report(result, "");
                return;
            }

            var dialog = new SaveFileDialog
            {
                FileName = $"ordevo-export-{DateTime.Today:yyyyMMdd}.csv",
                Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) == true)
            {
                await File.WriteAllTextAsync(dialog.FileName, result.Value);
                StatusMessage = $"CSV kaydedildi: {dialog.FileName}";
            }
        });
    }

    private async Task EnsureTablesLookupAsync()
    {
        if (Tables.Count == 0 || Sections.Count == 0)
            await LoadTablesAsync();
    }

    private async Task EnsureMenuLookupAsync()
    {
        if (Categories.Count == 0 || MenuItems.Count == 0 || ModifierGroups.Count == 0)
            await LoadMenuAsync();
    }

    private async Task EnsureInventoryLookupAsync()
    {
        if (Units.Count == 0 || Suppliers.Count == 0 || StockItems.Count == 0)
            await LoadInventoryAsync();
    }

    private async Task EnsureFinanceLookupAsync()
    {
        if (FinanceAccounts.Count == 0 || Counterparties.Count == 0)
            await LoadFinanceAsync();
    }

    private async Task EnsureIntegrationLookupAsync()
    {
        if (Connectors.Count == 0 || Terminals.Count == 0)
            await LoadIntegrationsAsync();
    }

    private async Task EnsureSyncLookupAsync()
    {
        if (SyncEntities.Count == 0)
            await LoadSyncAsync();
    }

    private FormField[] TableFields(TableDto? table = null) =>
    [
        Text("name", "Masa adı", table?.Name ?? ""),
        Combo("section", "Bölüm", SectionOptions(includeEmpty: true), table?.SectionId),
        Int("capacity", "Kapasite", (table?.Capacity ?? 4).ToString(CultureInfo.CurrentCulture)),
        Int("sort", "Sıra", (table?.SortOrder ?? 10).ToString(CultureInfo.CurrentCulture)),
        Bool("active", "Aktif", table?.IsActive ?? true)
    ];

    private UpsertTableRequest TableRequest(FormField[] fields) =>
        new(S(fields, "name"), NS(fields, "section"), I(fields, "capacity"), I(fields, "sort"), B(fields, "active"));

    private FormField[] StationFields(StationDto? station = null) =>
    [
        Text("name", "Ad", station?.Name ?? ""),
        Text("code", "Kod", station?.Code ?? ""),
        Int("sort", "Sıra", (station?.SortOrder ?? 10).ToString(CultureInfo.CurrentCulture)),
        Bool("active", "Aktif", station?.IsActive ?? true)
    ];

    private UpsertStationRequest StationRequest(FormField[] fields) =>
        new(S(fields, "name"), S(fields, "code"), I(fields, "sort"), B(fields, "active"));

    private FormField[] CategoryFields(CategoryDto? category = null) =>
    [
        Text("name", "Ad", category?.Name ?? ""),
        Text("color", "Renk", category?.Color ?? "#2f6f62", required: false),
        Int("sort", "Sıra", (category?.SortOrder ?? 10).ToString(CultureInfo.CurrentCulture)),
        Bool("active", "Aktif", category?.IsActive ?? true)
    ];

    private UpsertCategoryRequest CategoryRequest(FormField[] fields) =>
        new(S(fields, "name"), NS(fields, "color"), I(fields, "sort"), B(fields, "active"));

    private FormField[] MenuItemFields(MenuItemDto? item = null) =>
    [
        Combo("category", "Kategori", CategoryOptions(), item?.CategoryId),
        Text("name", "Ad", item?.Name ?? ""),
        Multi("desc", "Açıklama", item?.Description ?? "", required: false),
        DecimalF("price", "Fiyat", (item?.Price ?? 0).ToString(CultureInfo.CurrentCulture)),
        DecimalF("vat", "KDV", (item?.VatRate ?? 10).ToString(CultureInfo.CurrentCulture)),
        Text("sku", "SKU", item?.Sku ?? "", required: false),
        Text("image", "Görsel URL", item?.ImageUrl ?? "", required: false),
        Text("station", "Hazırlık istasyonu", item?.PrepStation ?? "", required: false),
        Int("sort", "Sıra", (item?.SortOrder ?? 10).ToString(CultureInfo.CurrentCulture)),
        Bool("active", "Aktif", item?.IsActive ?? true)
    ];

    private UpsertMenuItemRequest MenuItemRequest(FormField[] fields) =>
        new(S(fields, "category"), S(fields, "name"), NS(fields, "desc"), D(fields, "price"), D(fields, "vat"), NS(fields, "sku"), NS(fields, "image"), NS(fields, "station"), I(fields, "sort"), B(fields, "active"));

    private FormField[] CustomerFields(CustomerDto? customer = null) =>
    [
        Text("phone", "Telefon", customer?.Phone ?? "", required: customer is null),
        Text("name", "Ad soyad", customer?.FullName ?? "", required: false),
        Text("email", "E-posta", customer?.Email ?? "", required: false),
        DateF("birthday", "Doğum günü", customer?.Birthday, required: false),
        Multi("notes", "Not", required: false),
        Multi("preferences", "Tercihler", required: false),
        Bool("sms", "SMS izin", customer?.SmsConsent ?? true),
        Bool("emailConsent", "E-posta izin", customer?.EmailConsent ?? true)
    ];

    private FormField[] StockItemFields(StockItemDto? stock = null) =>
    [
        Text("name", "Ad", stock?.Name ?? ""),
        Text("sku", "SKU", stock?.Sku ?? "", required: false),
        Combo("unit", "Birim", UnitOptions(), stock?.UnitId),
        DecimalF("reorder", "Kritik seviye", (stock?.ReorderLevel ?? 0).ToString(CultureInfo.CurrentCulture)),
        DecimalF("cost", "Birim maliyet", (stock?.UnitCost ?? 0).ToString(CultureInfo.CurrentCulture)),
        Bool("active", "Aktif", stock?.IsActive ?? true)
    ];

    private UpsertStockItemRequest StockItemRequest(FormField[] fields) =>
        new(S(fields, "name"), NS(fields, "sku"), S(fields, "unit"), D(fields, "reorder"), D(fields, "cost"), B(fields, "active"));

    private FormField[] ReportRangeFields() =>
    [
        DateF("start", "Başlangıç", _reportStart),
        DateF("end", "Bitiş", _reportEnd)
    ];

    private IReadOnlyList<FormOption> SectionOptions(bool includeEmpty) =>
        BuildOptions(Sections.Select(x => new FormOption(x.Id, x.Name)), includeEmpty ? "Bölümsüz" : null);

    private IReadOnlyList<FormOption> TableOptions(bool includeEmpty) =>
        BuildOptions(Tables.Where(t => t.IsActive).OrderBy(t => t.SortOrder).Select(t => new FormOption(t.Id, $"{t.Name} ({t.Status})")), includeEmpty ? "Masasız" : null);

    private IReadOnlyList<FormOption> CategoryOptions() =>
        BuildOptions(Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).Select(c => new FormOption(c.Id, c.Name)), null);

    private IReadOnlyList<FormOption> MenuItemOptions() =>
        BuildOptions(MenuItems.Where(i => i.IsActive).OrderBy(i => i.Name).Select(i => new FormOption(i.Id, $"{i.Name} - {Money(i.Price)}")), null);

    private IReadOnlyList<FormOption> UnitOptions() =>
        BuildOptions(Units.OrderBy(u => u.Code).Select(u => new FormOption(u.Id, $"{u.Code} - {u.Name}")), null);

    private IReadOnlyList<FormOption> SupplierOptions(bool includeEmpty) =>
        BuildOptions(Suppliers.OrderBy(s => s.Name).Select(s => new FormOption(s.Id, s.Name)), includeEmpty ? "Tedarikçi yok" : null);

    private IReadOnlyList<FormOption> FinanceAccountOptions(bool includeEmpty) =>
        BuildOptions(FinanceAccounts.OrderBy(a => a.Name).Select(a => new FormOption(a.Id, $"{a.Name} ({a.AccountType})")), includeEmpty ? "Hesap yok" : null);

    private IReadOnlyList<FormOption> CounterpartyOptions(bool includeEmpty) =>
        BuildOptions(Counterparties.OrderBy(c => c.Name).Select(c => new FormOption(c.Id, $"{c.Name} ({c.CounterpartyType})")), includeEmpty ? "Cari yok" : null);

    private IReadOnlyList<FormOption> ModifierGroupOptions() =>
        BuildOptions(ModifierGroups.OrderBy(g => g.Name).Select(g => new FormOption(g.Id, g.Name)), null);

    private IReadOnlyList<FormOption> ConnectorOptions(bool includeEmpty) =>
        BuildOptions(Connectors.OrderBy(c => c.Name).Select(c => new FormOption(c.Id, c.Name)), includeEmpty ? "Connector yok" : null);

    private IReadOnlyList<FormOption> TerminalOptions(bool includeEmpty) =>
        BuildOptions(Terminals.OrderBy(t => t.Name).Select(t => new FormOption(t.Id, t.Name)), includeEmpty ? "Terminal yok" : null);

    private IReadOnlyList<FormOption> SyncEntityOptions() =>
        BuildOptions(SyncEntities.Where(e => e.AllowClientPush).OrderBy(e => e.SortOrder).Select(e => new FormOption(e.EntityName, e.EntityName)), null);

    private static IReadOnlyList<FormOption> BuildOptions(IEnumerable<FormOption> options, string? emptyLabel)
    {
        var list = new List<FormOption>();
        if (emptyLabel is not null)
            list.Add(new FormOption("", emptyLabel));
        list.AddRange(options);
        return list;
    }

    private static IReadOnlyList<FormOption> Options(params (string Value, string Label)[] values) =>
        values.Select(x => new FormOption(x.Value, x.Label)).ToArray();

    private bool ShowForm(string title, IReadOnlyList<FormField> fields)
    {
        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 430,
            MaxHeight = 740,
            Background = Background
        };

        var root = new DockPanel { Margin = new Thickness(18) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var ok = new Button { Content = "Tamam", MinWidth = 88, IsDefault = true };
        var cancel = new Button { Content = "Vazgeç", MinWidth = 88, IsCancel = true };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var panel = new StackPanel();
        foreach (var field in fields)
        {
            panel.Children.Add(new TextBlock { Text = field.Label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4) });
            var editor = CreateEditor(field);
            field.Editor = editor;
            panel.Children.Add(editor);
        }

        root.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        dialog.Content = root;

        ok.Click += (_, _) =>
        {
            foreach (var field in fields)
            {
                field.Value = ReadEditor(field);
                if (field.Required && string.IsNullOrWhiteSpace(field.Value))
                {
                    MessageBox.Show(dialog, $"{field.Label} zorunlu.", title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            dialog.DialogResult = true;
        };

        return dialog.ShowDialog() == true;
    }

    private static Control CreateEditor(FormField field)
    {
        return field.Kind switch
        {
            FieldKind.Boolean => new CheckBox { IsChecked = field.Value.Equals("true", StringComparison.OrdinalIgnoreCase) },
            FieldKind.Combo => new ComboBox
            {
                ItemsSource = field.Options,
                DisplayMemberPath = nameof(FormOption.Label),
                SelectedValuePath = nameof(FormOption.Value),
                SelectedValue = string.IsNullOrWhiteSpace(field.Value) && field.Options.Count > 0 ? field.Options[0].Value : field.Value
            },
            FieldKind.Multiline => new TextBox { Text = field.Value, MinHeight = 86, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            _ => new TextBox { Text = field.Value }
        };
    }

    private static string ReadEditor(FormField field) =>
        field.Editor switch
        {
            CheckBox checkBox => checkBox.IsChecked == true ? "true" : "false",
            ComboBox comboBox => comboBox.SelectedValue?.ToString() ?? "",
            TextBox textBox => textBox.Text,
            _ => field.Value
        };

    private T? Selected<T>(DataGrid grid, string label) where T : class
    {
        if (grid.SelectedItem is T value)
            return value;

        StatusMessage = $"{label} seçin.";
        return null;
    }

    private bool Confirm(string message) =>
        MessageBox.Show(this, message, "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private string? AskOrderId(string title)
    {
        var fields = new[] { Text("order", "Adisyon ID") };
        return ShowForm(title, fields) ? S(fields, "order") : null;
    }

    private static FormField Text(string key, string label, string value = "", bool required = true) =>
        new(key, label, FieldKind.Text, value, required);

    private static FormField Multi(string key, string label, string value = "", bool required = true) =>
        new(key, label, FieldKind.Multiline, value, required);

    private static FormField Int(string key, string label, string value = "0", bool required = true) =>
        new(key, label, FieldKind.Integer, value, required);

    private static FormField DecimalF(string key, string label, string value = "0", bool required = true) =>
        new(key, label, FieldKind.Decimal, value, required);

    private static FormField DateF(string key, string label, DateTime? value, bool required = true) =>
        new(key, label, FieldKind.Date, value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", required);

    private static FormField Bool(string key, string label, bool value) =>
        new(key, label, FieldKind.Boolean, value ? "true" : "false", required: false);

    private static FormField Combo(string key, string label, IReadOnlyList<FormOption> options, string? value = null, bool required = true) =>
        new(key, label, FieldKind.Combo, value ?? "", required, options);

    private static string S(IReadOnlyList<FormField> fields, string key) =>
        fields.First(f => f.Key == key).Value.Trim();

    private static string? NS(IReadOnlyList<FormField> fields, string key)
    {
        var value = S(fields, key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool B(IReadOnlyList<FormField> fields, string key) =>
        bool.TryParse(S(fields, key), out var value) && value;

    private static int I(IReadOnlyList<FormField> fields, string key)
    {
        var value = S(fields, key);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed) ||
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : 0;
    }

    private static long? LongOrNull(IReadOnlyList<FormField> fields, string key)
    {
        var value = S(fields, key);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed) ||
               long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : null;
    }

    private static decimal D(IReadOnlyList<FormField> fields, string key) =>
        DecimalOrNull(fields, key) ?? 0;

    private static decimal? DecimalOrNull(IReadOnlyList<FormField> fields, string key)
    {
        var value = S(fields, key);
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) ||
               decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : null;
    }

    private static DateTime Date(IReadOnlyList<FormField> fields, string key) =>
        DateOrNull(fields, key) ?? DateTime.Today;

    private static DateTime? DateOrNull(IReadOnlyList<FormField> fields, string key)
    {
        var value = S(fields, key);
        return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed) ||
               DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            ? parsed.Date
            : null;
    }

    private void UpdateSyncStatus()
    {
        var device = string.IsNullOrWhiteSpace(_deviceId) ? "cihaz yok" : _deviceId;
        SyncStatusText = $"Device: {device} | Watermark: {_lastHighWatermark}";
    }

    private void UpdateReportRangeText()
    {
        ReportRangeText = $"{_reportStart:yyyy-MM-dd} - {_reportEnd:yyyy-MM-dd}";
    }

    private static string Money(decimal value) => value.ToString("N2", CultureInfo.CurrentCulture);

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed record FormOption(string Value, string Label);

    private enum FieldKind
    {
        Text,
        Integer,
        Decimal,
        Date,
        Boolean,
        Combo,
        Multiline
    }

    private sealed class FormField(
        string key,
        string label,
        FieldKind kind,
        string value,
        bool required = true,
        IReadOnlyList<FormOption>? options = null)
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
        public FieldKind Kind { get; } = kind;
        public string Value { get; set; } = value;
        public bool Required { get; } = required;
        public IReadOnlyList<FormOption> Options { get; } = options ?? [];
        public Control? Editor { get; set; }
    }
}
