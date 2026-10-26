# Folder Chapter 3: `GlobalUsings.cs`, `Attributes/`, `Extensions/` — Shared Infrastructure

## `GlobalUsings.cs` — The Shared Kitchen Vocabulary

Every chef speaks the same language. This file is a vocabulary list posted on the wall that everyone automatically knows without importing in every conversation:

```csharp
global using LoungeClubsApi.Data;      // Every chef knows where the pantry is
global using LoungeClubsApi.Entities;   // Every chef knows what ingredients look like
global using Cortex.Mediator;     // Every chef knows how the intercom works
global using Carter;              // Every chef knows how menu boards work
```

Without this, every single file would start with 8-10 `using` lines. With it, 90% of those disappear. It is purely about reducing noise so chefs can focus on cooking.

## `Attributes/` — Staff Name Badges

When Mama Tee hires someone, she hands them a badge that determines their shift pattern:

- `[TransientService]` → "Fresh apron every time you clock in" (new instance per task)
- `[ScopedService]` → "Same apron for your entire shift" (one instance per HTTP request)
- `[SingletonService]` → "You are the head chef — same apron forever" (one instance for the whole restaurant)

```csharp
[ScopedService]
public class CurrentUserService : ICurrentUserService
{
    // Tracks which customer (Chidi) placed this request
}
```

`Scrutor` walks around, reads everyone's badge, and automatically registers them in the staff roster. No manual paperwork. Without these attributes, you would manually register every service in `Program.cs`.

## `Extensions/` — Kitchen Tool Sets

These are pre-configured tool sets — reusable helpers every chef can grab:

- `ServiceExtensions.cs` — "Add all security tools" / "Add all database tools"
- `AuthorizationExtensions.cs` — Permission-checking shortcuts
- `PaginationExtensions.cs` — Page-numbering helpers for long lists

```csharp
// Instead of wiring 30 tools manually in Program.cs:
services.AddIdentity();
services.AddJwt();
services.AddCors();
// ... 27 more lines

// You call one helper method:
services.ConfigureInfrastructure();
```

Without extensions, `Program.cs` would be thousands of lines long. Extensions keep it clean by grouping related registrations into named tool sets.
