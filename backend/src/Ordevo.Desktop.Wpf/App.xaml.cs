using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Ordevo.Desktop.Wpf.Services;

namespace Ordevo.Desktop.Wpf;

public partial class App : Application
{
    private RealtimeClient? _realtime;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var baseUrl = config["OrdevoApi:BaseUrl"] ?? "http://localhost:5144";
        var timeoutSeconds = int.TryParse(config["OrdevoApi:TimeoutSeconds"], out var timeout) ? timeout : 20;
        var offlinePath = config["Offline:DatabasePath"] ?? "ordevo-desktop-cache.db";

        var session = new DesktopSession();
        var offline = new OfflineStore(offlinePath);
        await offline.InitializeAsync();

        var http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        var api = new OrdevoApiClient(http, session, offline);
        _realtime = new RealtimeClient(api, session);

        var window = new MainWindow(api, session, _realtime, offline);
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_realtime is not null)
            await _realtime.DisconnectAsync();

        base.OnExit(e);
    }
}
