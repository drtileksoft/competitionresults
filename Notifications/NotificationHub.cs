using Microsoft.AspNetCore.SignalR;

namespace CompetitionResults.Notifications
{
    public class NotificationHub : Hub
    {
		private readonly IHubContext<NotificationHub> _notificationHubContext;
		public NotificationHub(IHubContext<NotificationHub> notificationHubContext)
		{
			_notificationHubContext = notificationHubContext;
		}

		public async Task NotifyCompetitionChanged()
        {
            await _notificationHubContext.Clients.All.SendAsync("CompetitionChanged");
        }
    }
}
