Chidi clicks "Place Order"
        │
        ▼
JwtBearer validates his login token ──────────────────── Serilog logs the request
        │
        ▼
Asp.Versioning routes to v1 or v2
        │
        ▼
Carter finds the correct OrderModule endpoint
        │
        ▼
FluentValidation checks the request is valid
        │
        ▼
Cortex.Mediator sends PlaceOrderCommand to PlaceOrderHandler
        │
        ▼
Mapperly converts the request DTO → OrderEntity
        │
        ▼
Cortex.Streams pipeline runs:
  ├── EF Core saves the order to SQL Server / MySQL
  ├── Refit calls Paystack to verify payment
  ├── EF Core deducts stock (TickerQ handles scheduled stock close later)
  └── MailKit + MimeKit sends Chidi a confirmation email
        │
        ▼
Mapperly converts OrderEntity → OrderDto
        │
        ▼
Response returns to Chidi
        │
        ▼
At 11 PM — TickerQ triggers scheduled job
  └── QuestPDF generates the daily sales report PDF
        │
        ▼
If anything went wrong at any step:
  └── Serilog.Exceptions captured the full exception details
      Serilog.Sinks.File wrote it to /Logs/
      Serilog.Sinks.Console showed it in red in the terminal