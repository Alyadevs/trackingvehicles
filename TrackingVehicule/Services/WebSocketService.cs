using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TrackingVehicule.Services
{
    public class WebSocketService
    {
        private readonly ConcurrentBag<WebSocket> _clients = new();

        public void AddClient(WebSocket ws) => _clients.Add(ws);

        public async Task BroadcastAsync(object obj)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
            foreach (var ws in _clients)
            {
                if (ws.State == WebSocketState.Open)
                {
                    try { await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None); }
                    catch { }
                }
            }
        }
    }
}
