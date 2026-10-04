using StackDuel.Domain.Campaigns.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Campaigns.Entities;

public sealed class CampaignEnrollment : AggregateRoot
{
    public CampaignEnrollment(Guid userId, Guid campaignId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        if (campaignId == Guid.Empty)
            throw new ArgumentException("Campaign id must not be empty.", nameof(campaignId));

        UserId = userId;
        CampaignId = campaignId;
        Status = EnrollmentStatus.InProgress;
        EnrolledAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status == EnrollmentStatus.Completed)
            throw new InvalidOperationException("Enrollment is already completed.");

        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    private CampaignEnrollment() { }

    public Guid UserId { get; private set; }
    public Guid CampaignId { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
}