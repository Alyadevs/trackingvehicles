namespace TrackingVehicule.Services
{
    public class WebSocketHandler
    {
        private readonly WebSocketService _wsService;

        public WebSocketHandler(WebSocketService wsService)
        {
            _wsService = wsService;
        }

        public async Task HandleAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)

            {
                context.Response.StatusCode = 400;
                return;
            }

            var ws = await context.WebSockets.AcceptWebSocketAsync();
            _wsService.AddClient(ws);
            Console.WriteLine("[API] WebSocket client connected.");

            var buffer = new byte[1024 * 4];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            Console.WriteLine("[API] WebSocket client disconnected.");
        }
    }
}
