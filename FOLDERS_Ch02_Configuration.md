# Folder Chapter 2: `appsettings.json` & `Configuration/` — The Operating Manual

## `appsettings.json` — The Restaurant Operating Manual

Before the first customer walks in, the manager reads this file. It contains all the rules and settings that change between locations:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Kitchen-Pantry-Server",
    "TickerQConnection": "Night-Shift-Office"
  },
  "JwtSettings": {
    "Secret": "Master-Key-For-Staff-Only",
    "TokenValidityInMinutes": 30
  },
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "FromAddress": "orders@mamatees.ng"
  }
}
```

When Mama Tee opens a second branch in Abuja, she copies this file, changes the database server address, changes the SMTP server, and the entire codebase runs there without a single line of C# changing. This is why configuration lives outside code — it changes per environment.

## `Configuration/` — The Settings Rulebook

This folder defines what valid settings look like. Not the settings themselves — the **shape** and **validation rules**.

```csharp
public class JwtSettings
{
    public string Secret { get; set; }              // Must exist
    public int TokenValidityInMinutes { get; set; }  // Must be > 0
}

public class JwtSettingsValidator : AbstractValidator<JwtSettings>
{
    public JwtSettingsValidator()
    {
        RuleFor(x => x.Secret).MinimumLength(32);     // Too short = easily forged badge
        RuleFor(x => x.TokenValidityInMinutes).GreaterThan(0);  // Zero = instant expiry
    }
}
```

When the restaurant opens, `Program.cs` reads `appsettings.json` and immediately validates it against the rules in this folder. If the secret key is only 10 characters, the restaurant refuses to open. This prevents running with dangerous misconfiguration.
