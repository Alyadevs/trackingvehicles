📡 Vehicle Tracking – Real-Time Communication (.NET, MQTT & WebSocket)

This project demonstrates a real-time vehicle tracking system built with ASP.NET Core, MQTT, and WebSocket.
Vehicles publish their GPS coordinates via MQTT, the backend processes the data, applies business rules, and broadcasts updates to connected clients in real time using WebSockets.

The project includes:

A vehicle simulator (MQTT publisher)

A .NET backend (MQTT subscriber + WebSocket server)

A WebSocket client (console or frontend)

📌 Features
🚗 Vehicle Simulation

Multiple vehicles simulated in parallel

Periodic GPS coordinate publication via MQTT

Dynamic position updates

📡 MQTT Communication

Publish/Subscribe model

Vehicle position topics

Command topics (STOP / ALARM)

🔄 Real-Time WebSocket Communication

WebSocket server implemented in ASP.NET Core

Multiple clients supported

Real-time broadcasting of vehicle positions

🧠 Business Logic

Automatic rule-based decisions

Commands sent back to vehicles via MQTT

🌐 WebSocket Real-Time Communication
🔌 WebSocket Endpoint
ws://localhost:5231/ws/vehicles

📤 Message Sent to Clients
{
  "type": "coords",
  "payload": {
    "id": "veh-001",
    "lat": 36.80,
    "lon": 10.18
  }
}


Connected clients instantly receive vehicle position updates.

📡 MQTT Topics
Purpose	Topic
Vehicle coordinates	vehicle/{id}/coords
Vehicle commands	vehicle/{id}/command
Example MQTT Payload
{
  "id": "veh-001",
  "lat": 36.8052,
  "lon": 10.1823,
  "vitesse": 70,
  "timestamp": "2025-01-10T14:30:00Z"
}

🧠 Business Rules

Implemented in MqttService:

Condition	Action
Movement > 9 km	STOP
Movement > 5 km	ALARM
Otherwise	No action

Commands are automatically published back to the vehicle via MQTT.

🌍 REST API
🔹 Get all vehicles
GET /api/vehicles

🔹 Send a command to a vehicle
POST /api/vehicles/{id}/command


Request body:

{
  "command": "ALARM"
}

🏗️ Project Structure
TrackingVehicule/
<img width="296" height="458" alt="image" src="https://github.com/user-attachments/assets/d82abc64-c3af-4589-8a16-5c68ce1aab0d" />


🛠️ Technologies Used

.NET 7 / .NET 8

ASP.NET Core

MQTTnet

Mosquitto MQTT Broker

WebSocket

JSON

Thread-safe collections (ConcurrentDictionary)

▶️ Running the Project
1. Start MQTT Broker

mosquitto

2. Run the Backend

dotnet restore
dotnet run
3️. Run Vehicle Simulator
dotnet run --project VehicleSimulator

4️. Run WebSocket Client
dotnet run --project WebSocketClient



