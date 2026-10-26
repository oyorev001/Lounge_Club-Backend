using Microsoft.AspNetCore.Authorization;
using LoungeClubsApi.Authorization.Requirements;

namespace LoungeClubsApi.Authorization.Handlers;

public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (context.User.Identity is not { IsAuthenticated: true })
        {
            return Task.CompletedTask;
        }


        if (requirement.RequireAll)
        {
            // Check if user has ALL of the required roles
            if (requirement.AllowedRoles.All(role => context.User.IsInRole(role)))
            {
                context.Succeed(requirement);
            }
        }
        else
        {
            // Check if user has ANY of the allowed roles
            if (requirement.AllowedRoles.Any(role => context.User.IsInRole(role)))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
