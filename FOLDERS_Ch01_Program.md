# Folder Chapter 1: `Program.cs` — The Main Switchboard

Chidi clicks "Place Order". Before anything else happens, this file runs. It is the restaurant's main switchboard — the power panel that turns every other system on.

```
Chidi's request arrives
        │
        ▼
Program.cs flips the switches:
  ├── Turns on the lights        → Serilog logging pipeline
  ├── Opens the front door       → CORS (who can walk in)
  ├── Assigns security guards    → Authentication + Authorization
  ├── Sets up the menu boards    → Carter endpoint discovery
  ├── Opens the kitchen pantry   → Database context registration
  ├── Wires the intercom         → Mediator registration
  ├── Hangs the daily specials   → OpenAPI + Scalar docs
  └── Starts the night-shift     → TickerQ background jobs
```

`Program.cs` does not cook food itself. It does not take orders. It ensures every station in the restaurant has power, water, and gas before the doors open. Every folder below is "turned on" here.

The file reads configuration, adds services to the dependency injection container, configures middleware that processes every request, and maps endpoints. Without this file, the restaurant is just an empty building.
