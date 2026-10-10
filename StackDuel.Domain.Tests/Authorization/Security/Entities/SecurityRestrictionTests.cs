using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.Authorization.Security.Entities;
using StackDuel.Domain.Authorization.Security.ValueObjects;

namespace StackDuel.Domain.Tests.Authorization.Security.Entities;

public class SecurityRestrictionTests
{
    private static SecurityRestriction CreateRestriction(
        UserId? userId = null,
        PermissionCode? permissionCode = null,
        DateTimeOffset? expiresAt = null,
        Reason? reason = null
    ) =>
        SecurityRestriction.CreateDenyTemporary(
            Guid.NewGuid(),
            userId ?? new UserId(Guid.NewGuid()),
            permissionCode ?? new PermissionCode("submission:create"),
            expiresAt ?? DateTimeOffset.UtcNow.AddHours(1),
            reason ?? new Reason("Automated abuse detection."),
            new DetectionEventId(Guid.NewGuid())
        );

    [Test]
    public void CreateDenyTemporary_SetsEveryField()
    {
        Guid id = Guid.NewGuid();
        UserId userId = new(Guid.NewGuid());
        PermissionCode permissionCode = new("game:duel:play");
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddDays(1);
        Reason reason = new("Cheating detected.");
        DetectionEventId detectionEventId = new(Guid.NewGuid());

        SecurityRestriction restriction = SecurityRestriction.CreateDenyTemporary(
            id,
            userId,
            permissionCode,
            expiresAt,
            reason,
            detectionEventId
        );

        Assert.Multiple(() =>
        {
            Assert.That(restriction.Id, Is.EqualTo(id));
            Assert.That(restriction.UserId, Is.EqualTo(userId));
            Assert.That(restriction.PermissionCode, Is.EqualTo(permissionCode));
            Assert.That(restriction.ExpiresAt, Is.EqualTo(expiresAt));
            Assert.That(restriction.Reason, Is.EqualTo(reason));
            Assert.That(restriction.DetectionEventId, Is.EqualTo(detectionEventId));
        });
    }

    [Test]
    public void CreateDenyTemporary_AlwaysUsesTheDenyEffect()
    {
        Assert.That(CreateRestriction().Effect, Is.EqualTo(DecisionEffect.Deny));
    }

    [Test]
    public void CreateDenyTemporary_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateRestriction(userId: new UserId(Guid.Empty)));
    }

    [Test]
    public void CreateDenyTemporary_MissingPermissionCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateRestriction(permissionCode: default(PermissionCode)));
    }

    [Test]
    public void CreateDenyTemporary_ExpiryInThePast_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateRestriction(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [Test]
    public void CreateDenyTemporary_MissingReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateRestriction(reason: default(Reason)));
    }

    [Test]
    public void IsActiveAt_BeforeExpiry_IsTrue()
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        SecurityRestriction restriction = CreateRestriction(expiresAt: expiresAt);

        Assert.That(restriction.IsActiveAt(expiresAt.AddMinutes(-1)), Is.True);
    }

    [Test]
    public void IsActiveAt_ExactlyAtExpiry_IsStillActive()
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        SecurityRestriction restriction = CreateRestriction(expiresAt: expiresAt);

        Assert.That(restriction.IsActiveAt(expiresAt), Is.True);
    }

    [Test]
    public void IsActiveAt_AfterExpiry_IsFalse()
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        SecurityRestriction restriction = CreateRestriction(expiresAt: expiresAt);

        Assert.That(restriction.IsActiveAt(expiresAt.AddTicks(1)), Is.False);
    }
}

public class ReasonTests
{
    [Test]
    public void Constructor_KeepsValueVerbatim()
    {
        Assert.That(new Reason("Cheating detected.").Value, Is.EqualTo("Cheating detected."));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new Reason(value!));
    }

    [Test]
    public void Equality_IsByValue()
    {
        Reason reason = new("Spam.");
        Reason sameValue = new("Spam.");

        Assert.That(reason, Is.EqualTo(sameValue));
    }
}