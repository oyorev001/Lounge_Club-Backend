# Folder Chapter 9: `Services/` — Specialist Chefs

Some tasks require specialist skills that many stations need. Rather than teaching every chef to send emails or generate tokens, the restaurant hires specialists. Any station that needs that skill simply calls the specialist.

## `CurrentUserService` — The Receptionist Who Knows Who Is at the Counter

```csharp
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId =>
        Guid.Parse(_httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    public string Email =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email)!;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
```

When Chidi is logged in and places an order, the handler needs to know: "Who placed this?" It does not ask the HTTP request directly. It asks `ICurrentUserService`. The specialist looks at the HTTP context, reads the JWT claims, and returns Chidi's ID and email.

This is important because it decouples the handler from HTTP details. The handler just asks for a `UserId`. It does not know that the ID came from a JWT token, or a cookie, or an API key. If tomorrow the authentication method changes, only this service changes. Every handler using `ICurrentUserService` continues to work.

## `TokenService` — The Badge Maker

```csharp
public class TokenService : ITokenService
{
    public string GenerateAccessToken(User user)
    {
        // Creates a JWT containing: user ID, email, roles, permissions, expiry
        // Signs it with the secret key from appsettings.json
    }

    public string GenerateRefreshToken()
    {
        // Creates a long random string stored in the database
        // Used to get a new access token when the old one expires
    }
}
```

When Chidi logs in, the login handler asks `ITokenService` to make his badge. The service uses cryptographic signing to ensure no one can forge the badge. The badge contains claims: Chidi's ID, email, roles. It expires in 30 minutes. The refresh token lives longer and allows Chidi to get a new badge without logging in again.

## Why Services Are Separate

These specialists could theoretically live inside each feature folder. But multiple features need them:
- `CurrentUserService` is needed by almost every handler
- `TokenService` is needed by login, refresh token, and password reset

Putting them in a shared `Services/` folder means they are hired once and reused everywhere. They are registered with `Singleton` or `Scoped` lifetime because they are stateless and expensive to recreate.

## The Rule: Services Have No Business Logic

Services do not decide whether an order is valid. They do not calculate discounts. They do not check inventory. They provide infrastructure capabilities:
- "Tell me who the current user is"
- "Generate a secure token"
- "Send an email"
- "Hash a password"

Business decisions live in `Features/` handlers. Infrastructure capabilities live in `Services/`.
