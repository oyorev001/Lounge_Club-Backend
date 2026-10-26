using LoungeClubsApi.Data;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Features.Notifications.MarkNotificationRead;

public class MarkNotificationReadCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("You must be logged in.");

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Notification not found.");

        if (notification.UserId != userId)
            throw new UnauthorizedAccessException("You do not have access to this notification.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
