# Folder Chapter 5: `Data/` — Kitchen Pantry, Recipe Book, and Stock Ledger

## `AppDbContext.cs` — The Pantry Inventory System

This is the master ledger. Every ingredient entering or leaving the pantry is recorded here. It knows where the pantry is (SQL Server or MySQL), what shelves exist (tables like Orders, Products, Users), and how ingredients relate (Orders have OrderItems, OrderItems reference Products).

When Chidi's order is saved, `AppDbContext` writes it to the database. When the manager queries today's orders, `AppDbContext` reads them back. It tracks changes in memory and applies them as a single transaction when `SaveChangesAsync()` is called.

## `DbSeeder.cs` — Grand Opening Setup

The first day the restaurant opens, someone must stock the shelves with initial inventory — default menu items, the owner account, basic roles and permissions. This runs once in development to create a working environment. It does not run in production because production data is real customer data, not seeded test data.

## `Configurations/` — Shelf Labels

Tells the pantry exactly how to store each ingredient:

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200);    // Name max 200 chars
        builder.Property(p => p.Price).HasPrecision(18, 2);   // Price: DECIMAL(18,2)
        builder.HasIndex(p => p.Name);                        // Index for fast search
    }
}
```

Without shelf labels, the pantry guesses. Guesses are wrong. A string might become `nvarchar(max)` instead of `nvarchar(200)`. A decimal might lose precision. Labels make storage predictable.

## `Migrations/` — Renovation Blueprints

When Mama Tee adds a "DeliveryAddress" column to the Orders shelf, she does not manually rearrange the pantry. She generates a migration — a blueprint that describes exactly what to change:

```bash
dotnet ef migrations add AddDeliveryAddressToOrders
```

Migrations are applied in order, tracked in a history table, and can be undone if something breaks. The database evolves alongside the code, version-controlled and reversible.
