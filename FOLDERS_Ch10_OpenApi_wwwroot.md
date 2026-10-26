# Folder Chapter 10: `OpenApi/` and `wwwroot/` — The Menu Display Board

## `OpenApi/` — Menu Customization Workshop

The restaurant has a beautiful menu display. It shows every dish, the price, ingredients, and dietary warnings. But the display board itself is generated automatically from what the chefs actually cook. The `OpenApi/` folder customizes how that board looks.

### `BearerSecuritySchemeTransformer.cs`

The display board needs a section explaining how customers prove they are members. This file adds that section:

```csharp
public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, ...)
    {
        // Adds a "lock" icon to every endpoint that requires login
        // Explains: "Present your JWT badge in the Authorization header"
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT token"
                },
                new List<string>()
            }
        });
    }
}
```

Without this, the menu board would not show which dishes require membership. Ahmed (the frontend developer) would try to call a protected endpoint, get a mysterious 401 error, and have no idea why. The transformer makes security requirements explicit on the display board.

### `OperationIdTransformer.cs`

Every dish on the menu gets a unique code. This ensures frontend code generation tools can create methods like `placeOrder()` instead of generic `postApiV1Orders()`. It is purely for developer convenience.

## `wwwroot/` — Customer-Facing Decorations

This folder contains things customers can see and touch. It is the public face of the restaurant.

### `wwwroot/openapi/` — Pre-Printed Menu Cards

When the restaurant opens, the menu board is already printed and sitting in a display case. `Microsoft.Extensions.ApiDescription.Server` generates these during `dotnet build` — it briefly starts the restaurant, captures the menu, and prints it as JSON files:

```
wwwroot/
  └── openapi/
      └── v1.json      ← Pre-printed menu for API version 1
      └── v2.json      ← Pre-printed menu for API version 2
```

Why pre-print? Because the menu board (Scalar) can show the menu before the kitchen is fully warmed up. And because Ahmed can read the JSON file in git to see what changed between versions without starting the restaurant.

### `wwwroot/assets/` — Decorative Images

Logos, background images, PDF report templates — anything the restaurant serves visually. When QuestPDF generates a daily sales report, it might pull a logo from here.

## The Menu Board Analogy

- `Microsoft.AspNetCore.OpenApi` → The machine that reads every recipe and generates a raw ingredient list
- `Microsoft.OpenApi` → The paper and ink used by the machine
- `Microsoft.Extensions.ApiDescription.Server` → The printer that pre-prints menu cards at 5 AM before customers arrive
- `Scalar.AspNetCore` → The beautiful glass display case that shows the menu cards to customers
- `OpenApi/` transformers → The graphic designers who customize how the display case looks (adding security icons, styling, ordering)
- `wwwroot/openapi/` → The physical stack of printed menus behind the counter
