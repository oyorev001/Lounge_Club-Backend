# Folder Chapter 6: `Entities/` — The Raw Ingredients

These are the things that exist in Mama Tee's world before any cooking happens. They are the nouns of the restaurant.

## Example: Product (a Shawarma)

```csharp
public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;     // "Shawarma"
    public decimal Price { get; set; }                   // ₦2500
    public int StockQuantity { get; set; }               // 47 left in fridge
    public bool IsActive { get; set; } = true;           // Still on menu?
}
```

## Example: Order (Chidi's actual order)

```csharp
public class Order : AuditableEntity
{
    public Guid CustomerId { get; set; }                  // Chidi's ID
    public List<OrderItem> Items { get; set; } = new();   // Shawarma + Pepsi
    public decimal TotalAmount { get; set; }              // ₦3500
    public OrderStatus Status { get; set; }               // New → Ready → Delivered
}
```

## The Base Classes

- `Base/BaseEntity.cs` — Every ingredient gets a unique barcode (`Guid`). No two things are confused, ever.
- `Base/AuditableEntity.cs` — Every ingredient gets a timestamp: when it arrived, who brought it. The security camera for data.
- `User.cs` — The person record. Chidi, Ada, Mama Tee.
- `AuditLog.cs` — The security camera that records who touched what and when. When a price changes, this records who changed it, what it was before, and what it became.

## What Entities Do NOT Do

Entities are purely descriptive. They do not cook. They do not validate. They do not send emails. They are the data shapes. The behavior lives in `Features/` handlers. The validation lives in `Features/` validators. The entities simply describe what the world looks like.
