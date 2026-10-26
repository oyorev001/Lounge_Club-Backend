# Folder Chapter 7: `Features/` — The Food Stations ⭐

**This is where the actual cooking happens.** Every feature is a self-contained station with everything it needs.

## Vertical Slice: Login Station

```
Features/
├── Auth/
│   ├── Login/
│   │   ├── LoginEndpoint.cs        — The counter where Chidi hands over his badge
│   │   ├── LoginCommand.cs         — The form he fills out
│   │   ├── LoginCommandHandler.cs  — The clerk who verifies and issues a new badge
│   │   └── LoginResponse.cs        — The new badge they hand back
│   ├── Register/                   — "I want to become a customer"
│   ├── RefreshToken/               — "My badge expired, give me a new one"
│   └── ...
```

Everything about logging in lives in one folder. The endpoint, the command, the handler, the response. You do not need to open five different folders to understand login. One folder tells the whole story.

## Vertical Slice: Order Station

```
├── Orders/
│   ├── PlaceOrder/
│   │   ├── PlaceOrderEndpoint.cs   — The counter where Chidi submits his order
│   │   ├── PlaceOrderCommand.cs    — The order form he fills out
│   │   ├── PlaceOrderHandler.cs    — The chef who processes the order
│   │   ├── PlaceOrderValidator.cs  — The inspector who checks the form
│   │   └── OrderResponse.cs        — The receipt handed back
│   ├── GetOrder/                   — "Where is my order now?"
│   ├── GetOrders/                  — "Show me my order history"
│   ├── UpdateOrderStatus/          — "The order is now ready for pickup"
│   └── CancelOrder/                — "Cancel my order"
```

## Vertical Slice: User Management

```
├── Users/
│   ├── GetUsers/                   — "Show me all customers"
│   ├── CreateUser/                 — "Add a new staff member"
│   ├── UpdateUser/                 — "Change someone's details"
│   └── DeactivateUser/             — "This person no longer works here"
```

## Why Vertical Slices?

In traditional layered architecture, you would have:
- `Controllers/` — All counters together
- `Services/` — All chefs together
- `Repositories/` — All pantry workers together
- `Models/` — All ingredients together

This means changing login requires touching files in four different folders. With vertical slices, changing login requires touching files in one folder. Adding a new feature means creating one new folder. Nothing else changes.

## Inside Every Slice

Every food station contains everything needed for that job:
- **Endpoint** (`*.Endpoint.cs`) — The counter where customers arrive
- **Command/Query** (`*.Command.cs` / `*.Query.cs`) — The order form
- **Handler** (`*.Handler.cs`) — The chef who does the cooking
- **Response** (`*.Response.cs`) — The finished plate
- **Validator** (`*.Validator.cs`) — The inspector who checks the form before cooking

This is why it is called **Vertical Slice Architecture**. Each slice goes from top to bottom — from the HTTP endpoint through the business logic to the database — in one self-contained folder.
