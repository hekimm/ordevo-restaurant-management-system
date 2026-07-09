using Microsoft.AspNetCore.SignalR.Client;

namespace Ordevo.Desktop.Wpf.Services;

public sealed class RealtimeClient(OrdevoApiClient api, DesktopSession session)
{
    private readonly List<HubConnection> _connections = [];

    public event Action<string>? Changed;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await DisconnectAsync();
        if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.AccessToken))
            return;

        _connections.Add(CreateConnection("/hubs/orders", "orderChanged", "orders"));
        _connections.Add(CreateConnection("/hubs/tables", "tablesChanged", "tables"));
        _connections.Add(CreateConnection("/hubs/kds", "ticketChanged", "kds"));

        foreach (var connection in _connections)
            await connection.StartAsync(ct);
    }

    public async Task DisconnectAsync()
    {
        foreach (var connection in _connections)
        {
            try
            {
                await connection.DisposeAsync();
            }
            catch
            {
            }
        }
        _connections.Clear();
    }

    private HubConnection CreateConnection(string path, string methodName, string signal)
    {
        var url = new Uri(api.BaseAddress, path);
        var connection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(session.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<object>(methodName, _ => Changed?.Invoke(signal));
        return connection;
    }
}
