using System.Text.Json;

namespace TrackingVehicule.Models
{
    public class Vehicles
    {
        public string Id { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public JsonElement RawPayload { get; set; }
    }
}
