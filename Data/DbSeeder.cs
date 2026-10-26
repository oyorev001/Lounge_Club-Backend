using System.Security.Claims;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Authorization.Handlers;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var db = serviceProvider.GetRequiredService<AppDbContext>();

            // 1. Seed Roles
            foreach (var role in Enum.GetValues<AppRole>())
            {
                var roleName = role.ToString();
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Default Admin User — password from SeedAdmin:Password
            // (env var in production). The dev fallback must never go live.
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var env = serviceProvider.GetRequiredService<IHostEnvironment>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
            var adminPassword = config["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                if (env.IsProduction())
                    throw new InvalidOperationException(
                        "SeedAdmin__Password must be set in production — refusing to seed the admin with the built-in development password.");
                adminPassword = "P@ssword123!";
                logger.LogWarning(
                    "SeedAdmin:Password is not configured — seeding the default admin with the built-in development password. Set the SeedAdmin__Password environment variable before deploying.");
            }

            var adminEmail = "admin@oyolc.gov.ng";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    IsVerified = true,
                    LockoutEnabled = true
                };
                await userManager.CreateAsync(adminUser, adminPassword);
                await userManager.AddToRoleAsync(adminUser, AppRole.Admin.ToString());
            }
            else if (!adminUser.LockoutEnabled)
            {
                adminUser.LockoutEnabled = true;
                await userManager.UpdateAsync(adminUser);
            }

            // 3. Seed Permission claims per role
            var rolePermissions = new Dictionary<AppRole, AppPermission[]>
            {
                [AppRole.Admin] = Enum.GetValues<AppPermission>(),
                                [AppRole.Officer] =
                [
                    AppPermission.EstablishmentsViewAll, AppPermission.EstablishmentsReview,
                    AppPermission.EstablishmentsAssignTier, AppPermission.EstablishmentsSuspend,
                    AppPermission.LeviesViewAll, AppPermission.InspectionsConduct,
                    AppPermission.InspectionsViewAll, AppPermission.FinesIssue,
                    AppPermission.FinesViewAll, AppPermission.CertificatesViewAll,
                    AppPermission.CertificatesRevoke, AppPermission.ReportsView
                ]
            };

            foreach (var (role, permissions) in rolePermissions)
            {
                var roleName = role.ToString();
                var identityRole = await roleManager.FindByNameAsync(roleName);
                if (identityRole is null) continue;

                var existingClaims = await roleManager.GetClaimsAsync(identityRole);
                foreach (var permission in permissions)
                {
                    var permissionValue = permission.GetDescription();
                    if (!existingClaims.Any(c => c.Type == PermissionAuthorizationHandler.PermissionClaimType && c.Value == permissionValue))
                    {
                        await roleManager.AddClaimAsync(identityRole, new Claim(PermissionAuthorizationHandler.PermissionClaimType, permissionValue));
                    }
                }
            }

            // 4. Seed License Tiers
            if (!db.LicenseTiers.Any())
            {
                db.LicenseTiers.AddRange(
                    new LicenseTier
                    {
                        Code = "PLATINUM",
                        Name = "Platinum (Category A)",
                        Description = "Major nightclubs and super-lounges: capacity 500+, live band/DJ, multiple halls, outdoor events.",
                        AnnualFee = 500_000m,
                        SortOrder = 1,
                        MinScore = 80
                    },
                    new LicenseTier
                    {
                        Code = "GOLD",
                        Name = "Gold (Category B)",
                        Description = "Established lounges and clubs: capacity 200-499, full bar and entertainment.",
                        AnnualFee = 250_000m,
                        SortOrder = 2,
                        MinScore = 50
                    },
                    new LicenseTier
                    {
                        Code = "SILVER",
                        Name = "Silver (Category C)",
                        Description = "Standard bars and lounges: capacity 50-199, regular bar service.",
                        AnnualFee = 100_000m,
                        SortOrder = 3,
                        MinScore = 25
                    },
                    new LicenseTier
                    {
                        Code = "BRONZE",
                        Name = "Bronze (Category D)",
                        Description = "Neighbourhood bars and beer parlours: capacity below 50.",
                        AnnualFee = 40_000m,
                        SortOrder = 4,
                        MinScore = 0
                    });
                await db.SaveChangesAsync();
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("SeedAdmin__Password"))
        {
            throw; // production safety gate — the app must NOT start without it
        }
        catch (Exception ex)
        {
            // Log the error but don't stop the application
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
