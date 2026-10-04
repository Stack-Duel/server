using StackDuel.Domain.Authorization.Rbac;

namespace StackDuel.Api.Authorization;

public static class WellKnownPermissions
{
    public const string CreateSubmission = WellKnownAuthorization.CreateSubmissionPermission;
    public const string ViewSubmission = WellKnownAuthorization.ViewSubmissionPermission;
    public const string PlaySoloRush = WellKnownAuthorization.PlaySoloRushPermission;
    public const string PlayDuel = WellKnownAuthorization.PlayDuelPermission;
    public const string PlayFfa = WellKnownAuthorization.PlayFfaPermission;
    public const string ReadAdminProblems = WellKnownAuthorization.ReadAdminProblemsPermission;
    public const string UpdateAdminProblems = WellKnownAuthorization.UpdateAdminProblemsPermission;
    public const string ReadAdminUsers = WellKnownAuthorization.ReadAdminUsersPermission;
    public const string UpdateAdminUserGroups = WellKnownAuthorization.UpdateAdminUserGroupsPermission;
    public const string ReadAdminSubmissions = WellKnownAuthorization.ReadAdminSubmissionsPermission;
    public const string CreateFeedback = WellKnownAuthorization.CreateFeedbackPermission;
    public const string ReadAdminFeedback = WellKnownAuthorization.ReadAdminFeedbackPermission;
    public const string UpdateAdminFeedback = WellKnownAuthorization.UpdateAdminFeedbackPermission;
    public const string ReadAdminDashboard = WellKnownAuthorization.ReadAdminDashboardPermission;
    public const string ReadAdminGames = WellKnownAuthorization.ReadAdminGamesPermission;
    public const string ManageFeatureFlags = WellKnownAuthorization.ManageFeatureFlagsPermission;
    public const string ReadAdminCampaigns = WellKnownAuthorization.ReadAdminCampaignsPermission;
    public const string ManageAdminCampaigns = WellKnownAuthorization.ManageAdminCampaignsPermission;
    public const string ReadAdminAuditLog = WellKnownAuthorization.ReadAdminAuditLogPermission;
    public const string CreateAdminProblems = WellKnownAuthorization.CreateAdminProblemsPermission;
    public const string SubmitAdminProblems = WellKnownAuthorization.SubmitAdminProblemsPermission;
    public const string ManageRequiredProblemLanguages =
        WellKnownAuthorization.ManageRequiredProblemLanguagesPermission;
}