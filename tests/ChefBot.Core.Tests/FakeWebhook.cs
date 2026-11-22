using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>A webhook that keeps what it was sent and answers with <see cref="Answer"/>.</summary>
internal sealed class FakeWebhook : INotificationAccess
{
    public List<Notification> Sent { get; } = [];

    public Delivery Answer { get; set; } = new(true);

    public Task<Delivery> SendAsync(string webhookUrl, Notification notification, CancellationToken cancellationToken)
    {
        if (Answer.Delivered)
        {
            Sent.Add(notification);
        }

        return Task.FromResult(Answer);
    }
}
