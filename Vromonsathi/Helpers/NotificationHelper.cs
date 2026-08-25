namespace Vromonsathi.Helpers
{
    public static class NotificationHelper
    {
        public static void AddNotification(Vromonsathi.Data.ApplicationDbContext context, int userId, string title, string message, string? linkUrl = null)
        {
            context.Notifications.Add(new Vromonsathi.Models.Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                IsRead = false
            });
        }
    }
}