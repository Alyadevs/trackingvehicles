using MQTTnet;
using MQTTnet.Client;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using TrackingVehicule.Models;

namespace TrackingVehicule.Services
{
    public class MqttService
    {
        private readonly IMqttClient _mqtt;
        private readonly ConcurrentDictionary<string, Vehicles> _latestPositions;
        private readonly ConcurrentDictionary<string, (double lat, double lon)> _previousPositions;
        private readonly WebSocketService _wsService;
        private readonly Random _rnd = new Random();

        public MqttService(ConcurrentDictionary<string, Vehicles> latestPositions,
                           ConcurrentDictionary<string, (double lat, double lon)> previousPositions,
                           WebSocketService wsService)
        {
            _latestPositions = latestPositions;
            _previousPositions = previousPositions;
            _wsService = wsService;

            var factory = new MqttFactory();
            _mqtt = factory.CreateMqttClient();
        }

        public async Task ConnectAndSubscribeAsync()
        {
            var opts = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .Build();

            await _mqtt.ConnectAsync(opts);

            await _mqtt.SubscribeAsync("vehicle/+/coords");

            _mqtt.ApplicationMessageReceivedAsync += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload ?? Array.Empty<byte>());
            try
            {
                var je = JsonSerializer.Deserialize<JsonElement>(payload);
                if (!je.TryGetProperty("id", out var idProp)) return;
                string id = idProp.GetString() ?? "";

                if (!je.TryGetProperty("lat", out var latProp) || !je.TryGetProperty("lon", out var lonProp)) return;
                double lat = latProp.GetDouble();
                double lon = lonProp.GetDouble();

                var pos = new Vehicles { Id = id, Lat = lat, Lon = lon, RawPayload = je };
                _latestPositions[id] = pos;

                if (_previousPositions.TryGetValue(id, out var prev))
                {
                    double moved = _rnd.Next(1, 11);
                    Console.WriteLine($"[API] Vehicle {id} simulated movement = {moved} km");

                    string cmd = moved > 9 ? "STOP" : moved > 5 ? "ALARM" : null;
                    if (cmd != null)
                    {
                        var msg = new MqttApplicationMessageBuilder()
                            .WithTopic($"vehicle/{id}/command")
                            .WithPayload(cmd)
                            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                            .Build();
                        await _mqtt.PublishAsync(msg);
                        Console.WriteLine($"[API] Published {cmd} to {id}");
                    }
                }

                _previousPositions[id] = (lat, lon);

                await _wsService.BroadcastAsync(new { type = "coords", payload = je });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed parsing MQTT payload: " + ex.Message);
            }
        }

        public async Task PublishCommand(string vehicleId, string command)
        {
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic($"vehicle/{vehicleId}/command")
                .WithPayload(command)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();



            await _mqtt.PublishAsync(msg);
        }
    }
}

