//using System.Collections.Concurrent;
//using System.Net.WebSockets;
//using System.Text;
//using System.Text.Json;
//using MQTTnet;
//using MQTTnet.Client;


//var builder = WebApplication.CreateBuilder(args);
//var app = builder.Build();

//// In-memory storage of latest positions
//var latestPositions = new ConcurrentDictionary<string, JsonElement>(); // store JSON payloads

//// Setup MQTT client
//var factory = new MqttFactory();
//var mqtt = factory.CreateMqttClient();
//var mqttOpts = new MqttClientOptionsBuilder()
//    .WithTcpServer("localhost", 1883)
//    .Build();

//await mqtt.ConnectAsync(mqttOpts);

//// Subscribe to all vehicle coords
//await mqtt.SubscribeAsync("vehicle/+/coords");

//// WebSocket clients bag
//var webSocketClients = new ConcurrentBag<WebSocket>();

//mqtt.ApplicationMessageReceivedAsync += async e =>
//{
//    var topic = e.ApplicationMessage.Topic;
//    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload ?? Array.Empty<byte>());

//    // parse payload
//    try
//    {
//        var je = JsonSerializer.Deserialize<JsonElement>(payload);
//        if (je.TryGetProperty("id", out var idProp))
//        {
//            var id = idProp.GetString() ?? "unknown";
//            latestPositions[id] = je;
//            // Push to websocket clients
//            var data = JsonSerializer.Serialize(new { type = "coords", payload = je });
//            var bytes = Encoding.UTF8.GetBytes(data);
//            foreach (var ws in webSocketClients)
//            {
//                if (ws.State == WebSocketState.Open)
//                {
//                    try
//                    {
//                        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
//                    }
//                    catch { /* ignore send errors */ }
//                }
//            }

//            // business rules: check distance using stored previous if available
//            // For simplicity: store previous coords inside latestPositions as needed
//            // We'll compute distance by checking if we have previous value
//            // Implement naive check: if "prev" exists stored under key "__prev_{id}" - optional
//            if (latestPositions.TryGetValue(id, out var current))
//            {
//                // we only have current (overwritten). To implement rule, we need prev => use separate dict
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine("Failed to parse MQTT payload: " + ex.Message);
//    }
//};

//// Déplacez la déclaration de previousPositions AVANT toute utilisation dans les handlers MQTT
//var previousPositions = new ConcurrentDictionary<string, (double lat, double lon)>();

//mqtt.ApplicationMessageReceivedAsync += async e =>
//{
//    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload ?? Array.Empty<byte>());
//    try
//    {
//        var je = JsonSerializer.Deserialize<JsonElement>(payload);
//        if (!je.TryGetProperty("id", out var idp)) return;
//        string id = idp.GetString() ?? "";
//        if (!je.TryGetProperty("lat", out var latp) || !je.TryGetProperty("lon", out var lonp)) return;
//        double lat = latp.GetDouble();
//        double lon = lonp.GetDouble();

//        // update storage
//        latestPositions[id] = je;

//        if (previousPositions.TryGetValue(id, out var prev))
//        {
//            // RANDOM movement (1 to 10 km)
//            var random = new Random();
//            double moved = random.Next(1, 11);
//            Console.WriteLine($"[API] vehicle {id} simulated movement = {moved} km");

//            if (moved > 10.0)
//            {
//                var msg = new MqttApplicationMessageBuilder()
//                    .WithTopic($"vehicle/{id}/command")
//                    .WithPayload("STOP")
//                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
//                    .Build();
//                await mqtt.PublishAsync(msg);
//                Console.WriteLine($"[API] Published STOP to {id}");
//            }
//            else if (moved > 5.0)
//            {
//                var msg = new MqttApplicationMessageBuilder()
//                    .WithTopic($"vehicle/{id}/command")
//                    .WithPayload("ALARM")
//                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
//                    .Build();
//                await mqtt.PublishAsync(msg);
//                Console.WriteLine($"[API] Published ALARM to {id}");
//            }
//        }


//        previousPositions[id] = (lat, lon);

//        // forward to websocket clients
//        var data = JsonSerializer.Serialize(new { type = "coords", payload = je });
//        var bytes = Encoding.UTF8.GetBytes(data);
//        foreach (var ws in webSocketClients)
//        {
//            if (ws.State == WebSocketState.Open)
//            {
//                try
//                {
//                    await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
//                }
//                catch { }
//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine("Err parsing payload: " + ex.Message);
//    }

//};

//// To implement distance rules properly, maintain previous positions separately:




//// HTTP endpoint: GET all vehicles (latest)
//app.MapGet("/api/vehicles", () =>
//{
//    var list = latestPositions.Select(kvp => new { id = kvp.Key, data = kvp.Value });
//    return Results.Json(list);
//});

//// HTTP endpoint: POST command to vehicle
//app.MapPost("/api/vehicles/{id}/command", async (string id, HttpRequest req) =>
//{
//    using var sr = new StreamReader(req.Body);
//    var body = await sr.ReadToEndAsync();
//    // body expected like { "command":"ALARM" } or "ALARM"
//    string cmd = "ALARM";
//    try
//    {
//        var je = JsonSerializer.Deserialize<JsonElement>(body);
//        if (je.TryGetProperty("command", out var c)) cmd = c.GetString() ?? cmd;
//        else if (je.ValueKind == JsonValueKind.String) cmd = je.GetString() ?? cmd;
//    }
//    catch
//    {
//        if (!string.IsNullOrWhiteSpace(body)) cmd = body.Trim('"', ' ', '\n', '\r');
//    }

//    var message = new MqttApplicationMessageBuilder()
//        .WithTopic($"vehicle/{id}/command")
//        .WithPayload(cmd.ToUpperInvariant())
//        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
//        .Build();
//    await mqtt.PublishAsync(message);
//    return Results.Ok(new { status = "sent", id, command = cmd });
//});

//// WebSocket endpoint
//app.UseWebSockets();
//app.Map("/ws/vehicles", async context =>
//{
//    if (context.WebSockets.IsWebSocketRequest)
//    {
//        var ws = await context.WebSockets.AcceptWebSocketAsync();
//        webSocketClients.Add(ws);
//        Console.WriteLine("[API] WebSocket client connected.");

//        var buffer = new byte[1024 * 4];
//        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//        while (!result.CloseStatus.HasValue)
//        {
//            // echo or ignore inbound messages
//            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//        }
//        await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
//    }
//    else
//    {
//        context.Response.StatusCode = 400;
//    }
//});

//app.Run();


using System.Collections.Concurrent;
using TrackingVehicule.Controllers;
using TrackingVehicule.Models;
using TrackingVehicule.Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var latestPositions = new ConcurrentDictionary<string, Vehicles>();
var previousPositions = new ConcurrentDictionary<string, (double lat, double lon)>();
var wsService = new WebSocketService();
var mqttService = new MqttService(latestPositions, previousPositions, wsService);
var wsHandler = new WebSocketHandler(wsService);

await mqttService.ConnectAndSubscribeAsync();


app.UseWebSockets();


app.UseWebSockets();
app.Map("/ws/vehicles", async context =>
{
    await wsHandler.HandleAsync(context);
});

VehicleController.MapVehicleEndpoints(app, latestPositions, mqttService);

app.Run();

