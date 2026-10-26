# Folder Chapter 8: `Filters/`, `Interfaces/`, `Models/` — Quality, Contracts, and Shared Forms

## `Filters/` — The Quality Inspector

Before any cooking starts, someone checks the order form. Is it filled out correctly? Is the address valid?

`ValidationFilter.cs` stands at the entrance to every food station and checks every form before the chef sees it.

```
Chidi submits order form
        │
        ▼
ValidationFilter inspects the form
        ├── Name filled in?          ✓
        ├── Email looks valid?       ✓
        ├── Items not empty?         ✓
        └── Quantity is positive?    ✗  ← STOPS HERE
        │
        ▼
Form is invalid → Returns to Chidi immediately with error list
        │
        (Chef never starts cooking — saves time and ingredients)
```

Without this filter, every chef would check the form themselves. Some would forget. Some would check differently. The filter ensures every station is inspected consistently.

## `Interfaces/` — Job Descriptions (Contracts)

Before hiring a specialist, Mama Tee writes a job description. It says what skills are needed, not who fills the role.

```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
```

- `IEmailService` — "We need someone who can send emails"
- `ITokenService` — "We need someone who can make security badges"
- `ICurrentUserService` — "We need someone who knows which customer is at the counter"

The actual person hired (the implementation) lives in `Services/`. But any chef in `Features/` can ask for `IEmailService` without caring whether it is `SmtpEmailService` or `SendGridEmailService`. Tomorrow, Mama Tee fires the email sender and hires a new one — no chef changes their recipe.

## `Models/` — Shared Recipe Cards

Some forms are used by multiple stations. Instead of copying the same form in every folder, they live here as shared recipe cards.

- `PagedResult` — "I need the first 10 items, and tell me how many total there are" (used by search, order history, user lists)
- `ApiResponse` — "Here is the result, wrapped in a standard envelope with success flag and message"
- `AuditEntry` — "Record of who changed what, used by audit logging throughout the system"

These are not specific to one feature. They are the common paper forms used by every station.
