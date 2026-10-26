# Folder Chapter 4: `Authorization/` — Security Guards and Access Passes

Chidi is a customer. Ada is a manager. Mama Tee is the owner. Not everyone can walk into every room. This folder is the security department.

## The Security Check

```
Chidi tries to view the "Daily Sales Report" endpoint
        │
        ▼
AppPermission.cs  →  Checks: Does Chidi have "Reports.View"?
        │          (No — Chidi only has "Customer" role)
        ▼
PermissionAuthorizationHandler.cs
        └── Inspects Chidi's JWT token, finds his role is "Customer"
        └── Looks up what permissions the "Customer" role has
        └── "Reports.View" is NOT in that list
        └── Returns 403 Forbidden — Chidi cannot enter the manager's office
```

## What's Inside

- `Enums/AppPermission.cs` — The master list of every possible access pass. Every door in the restaurant has a permission key.
- `Enums/AppRole.cs` — The list of staff ranks: `Customer`, `Cook`, `Manager`, `Owner`. A role is a bundle of permissions.
- `Handlers/` — The security guards who physically inspect badges and make decisions. A handler looks at the incoming request, checks the permission required by the endpoint, and either opens the door or blocks it.
- `Requirements/` — The rule cards each guard follows. "To enter the kitchen, you need the `Kitchen.Enter` permission."
- `Providers/` — The dispatch office that assigns the right guard to each door. When an endpoint says `[Authorize(Policy = "CanViewReports")]`, the provider finds the handler that knows how to check that policy.

## Why Roles AND Permissions?

You might ask: why not just check if someone is a Manager? Because roles are broad and permissions are specific. Two managers might have different access levels. Checking permissions means you can grant or revoke access to specific doors without creating new roles. It is more flexible than role-based authorization alone.
