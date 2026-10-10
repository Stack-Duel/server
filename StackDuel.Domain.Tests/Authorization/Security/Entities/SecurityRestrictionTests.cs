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

    [Fact]
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

        Assert.Equal(id, restriction.Id);
        Assert.Equal(userId, restriction.UserId);
        Assert.Equal(permissionCode, restriction.PermissionCode);
        Assert.Equal(expiresAt, restriction.ExpiresAt);
        Assert.Equal(reason, restriction.Reason);
        Assert.Equal(detectionEventId, restriction.DetectionEventId);
    }

    [Fact]
    public void CreateDenyTemporary_AlwaysUsesTheDenyEffect()
    {
        Assert.Equal(DecisionEffect.Deny, CreateRestriction().Effect);
    }

    [Fact]
    public void CreateDenyTemporary_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateRestriction(userId: new UserId(Guid.Empty)));
    }

    [Fact]
    public void CreateDenyTemporary_MissingPermissionCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateRestriction(permissionCode: default(PermissionCode)));
    }

    [Fact]
    public void CreateDenyTemporary_ExpiryInThePast_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateRestriction(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public void CreateDenyTemporary_MissingReason_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateRestriction(reason: default(Reason)));
    }

    [Fact]
    public void IsActiveAt_BeforeExpiry_IsTrue()
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        SecurityRestriction restriction = CreateRestriction(expiresAt: expiresAt);

        Assert.True(restriction.IsActiveAt(expiresAt.AddMinutes(-1)));
    }

    [Fact]
    public void IsActiveAt_ExactlyAtExpiry_IsStillActive()
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        SecurityRestriction restriction = CreateRestriction(expiresAt: expiresAt);

        Assert.True(restriction.IsActiveAt(expiresAt));
    }

    [Fact]
    public void IsActiveAt_AfterExpiry_IsFalse()
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        SecurityRestriction restriction = CreateRestriction(expiresAt: expiresAt);

        Assert.False(restriction.IsActiveAt(expiresAt.AddTicks(1)));
    }
}

public class ReasonTests
{
    [Fact]
    public void Constructor_KeepsValueVerbatim()
    {
        Assert.Equal("Cheating detected.", new Reason("Cheating detected.").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new Reason(value!));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Reason reason = new("Spam.");
        Reason sameValue = new("Spam.");

        Assert.Equal(sameValue, reason);
    }
}