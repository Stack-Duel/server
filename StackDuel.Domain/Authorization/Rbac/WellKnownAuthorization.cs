namespace StackDuel.Domain.Authorization.Rbac;

public static class WellKnownAuthorization
{
    public const string CreateSubmissionPermission = "submission:create";
    public const string ViewSubmissionPermission = "submission:view";
    public const string PlaySoloRushPermission = "game:solo-rush:play";
    public const string PlayDuelPermission = "game:duel:play";
    public const string PlayFfaPermission = "game:ffa:play";
    public const string ReadAdminProblemsPermission = "problem:read:admin";
    public const string UpdateAdminProblemsPermission = "problem:update:admin";
    public const string ReadAdminUsersPermission = "user:read:admin";
    public const string UpdateAdminUserGroupsPermission = "user:groups:update:admin";
    public const string ReadAdminSubmissionsPermission = "submission:read:admin";
    public const string CreateFeedbackPermission = "feedback:create";
    public const string ReadAdminFeedbackPermission = "feedback:read:admin";
    public const string UpdateAdminFeedbackPermission = "feedback:update:admin";
    public const string ReadAdminDashboardPermission = "dashboard:read:admin";
    public const string ReadAdminGamesPermission = "games:read:admin";
    public const string ManageFeatureFlagsPermission = "feature-flag:manage:admin";
    public const string ReadAdminCampaignsPermission = "campaign:read:admin";
    public const string ManageAdminCampaignsPermission = "campaign:manage:admin";
    public const string ReadAdminAuditLogPermission = "audit-log:read:admin";
    public const string CreateAdminProblemsPermission = "problem:create:admin";
    public const string SubmitAdminProblemsPermission = "problem:submit:admin";
    public const string ManageRequiredProblemLanguagesPermission = "problem-required-language:manage:admin";

    public const string DefaultUserRole = "default-user";
    public const string DefaultUserGroup = "default-user";

    public const string AdminRole = "admin";
    public const string AdminGroup = "admin";
}