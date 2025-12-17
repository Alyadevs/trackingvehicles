using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using MQTTnet;
using MQTTnet.Client;

class Program
{
    private static Random _rnd = new Random();

    
    class VehiculeSim
    {
        public string Id { get; set; }
        public IMqttClient Client { get; set; }

        public double Lat { get; set; }
        public double Lon { get; set; }

        public VehiculeSim(string id, double lat, double lon)
        {
            Id = id;
            Lat = lat;
            Lon = lon;
        }
    }

    static async Task Main(string[] args)
    {
        
        List<string> vehicules = new List<string>
        {
            "veh-001",
            "veh-002",
            "veh-003",
            "veh-004"
        };

        if (args.Length > 0)
            vehicules = new List<string>(args); 

        Console.WriteLine("=== SIMULATION MULTI-VÉHICULES ===");

        // Démarrer tous les simulateurs en parallèle
        List<VehiculeSim> sims = new List<VehiculeSim>();

        foreach (var id in vehicules)
        {
            sims.Add(await DemarrerSimulateur(id));
        }

        Console.WriteLine("Simulateurs démarrés. Appuyez sur Q pour quitter.");

        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

        foreach (var sim in sims)
        {
            await sim.Client.DisconnectAsync();
        }

        Console.WriteLine("Tous les véhicules sont déconnectés.");
    }

    
    private static async Task<VehiculeSim> DemarrerSimulateur(string id)
    {
        var sim = new VehiculeSim(id, 36.80 + _rnd.NextDouble() / 100, 10.18 + _rnd.NextDouble() / 100);

        sim.Client = new MqttFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId($"simulateur-{id}")
            .WithTcpServer("127.0.0.1")
            .Build();

       
        sim.Client.ConnectedAsync += async e =>
        {
            Console.WriteLine($"[{id}] connecté.");

            await sim.Client.SubscribeAsync($"vehicle/{id}/command");
            Console.WriteLine($"[{id}] souscription OK.");
        };

        
        sim.Client.ApplicationMessageReceivedAsync += e =>
        {
            string topic = e.ApplicationMessage.Topic;
            string cmd = e.ApplicationMessage.ConvertPayloadToString();

            Console.WriteLine($"[{id}] Commande reçue => {cmd}");

            return Task.CompletedTask;
        };

        await sim.Client.ConnectAsync(options);

       
        _ = Task.Run(async () =>
        {
            while (true)
            {
                if (sim.Client.IsConnected)
                    await PublierPosition(sim);

                await Task.Delay(3000);
            }
        });

        return sim;
    }

    private static async Task PublierPosition(VehiculeSim sim)
    {
     
        sim.Lat += (_rnd.NextDouble() - 0.5) * 0.002;
        sim.Lon += (_rnd.NextDouble() - 0.5) * 0.002;

        var payload = new
        {
            id = sim.Id,
            lat = sim.Lat,
            lon = sim.Lon,
            vitesse = _rnd.Next(10, 100),
            timestamp = DateTime.UtcNow
        };

        string json = JsonSerializer.Serialize(payload);

        var msg = new MqttApplicationMessageBuilder()
            .WithTopic($"vehicle/{sim.Id}/coords")
            .WithPayload(json)
            .Build();

        await sim.Client.PublishAsync(msg);

        Console.WriteLine($"[{sim.Id}] Position publiée : {json}");
    }
}
