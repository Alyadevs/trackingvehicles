using System.Collections.Concurrent;
using System.Text.Json;
using TrackingVehicule.Models;
using TrackingVehicule.Services;

namespace TrackingVehicule.Controllers
{
    public class VehicleController
    {
        public static void MapVehicleEndpoints(WebApplication app,
        ConcurrentDictionary<string, Vehicles> latestPositions,
        MqttService mqttService)
        {
            app.MapGet("/api/vehicles", () =>
            {
                var list = latestPositions.Select(kvp => new { id = kvp.Key, data = kvp.Value.RawPayload });
                return Results.Json(list);
            });

            app.MapPost("/api/vehicles/{id}/command", async (string id, HttpRequest req) =>
            {
                using var sr = new StreamReader(req.Body);
                var body = await sr.ReadToEndAsync();
                string cmd = "ALARM";

                try
                {
                    var je = JsonSerializer.Deserialize<JsonElement>(body);
                    if (je.TryGetProperty("command", out var c)) cmd = c.GetString() ?? cmd;
                    else if (je.ValueKind == JsonValueKind.String) cmd = je.GetString() ?? cmd;
                }
                catch
                {
                    if (!string.IsNullOrWhiteSpace(body)) cmd = body.Trim('"', ' ', '\n', '\r');
                }

                await mqttService.PublishCommand(id, cmd.ToUpperInvariant());
                return Results.Ok(new { status = "sent", id, command = cmd });
            });
        }
    }
}
