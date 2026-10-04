using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Authorization.Rbac.Entities;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class AuthorizationSeeder(StackDuelDbContext context) : IStaticSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        PermissionCode createSubmissionCode = new(WellKnownAuthorization.CreateSubmissionPermission);

        Permission createSubmissionPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == createSubmissionCode, cancellationToken)
            ?? Permission.Create(createSubmissionCode, "Allows creating code submissions");

        if (context.Entry(createSubmissionPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(createSubmissionPermission, cancellationToken);

        PermissionCode readAdminProblemsCode = new(WellKnownAuthorization.ReadAdminProblemsPermission);

        Permission readAdminProblemsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == readAdminProblemsCode, cancellationToken)
            ?? Permission.Create(readAdminProblemsCode, "Allows viewing admin-only problem data");

        if (context.Entry(readAdminProblemsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(readAdminProblemsPermission, cancellationToken);

        PermissionCode updateAdminProblemsCode = new(WellKnownAuthorization.UpdateAdminProblemsPermission);

        Permission updateAdminProblemsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == updateAdminProblemsCode, cancellationToken)
            ?? Permission.Create(updateAdminProblemsCode, "Allows updating problems in the admin panel");

        if (context.Entry(updateAdminProblemsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(updateAdminProblemsPermission, cancellationToken);

        PermissionCode readAdminUsersCode = new(WellKnownAuthorization.ReadAdminUsersPermission);

        Permission readAdminUsersPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == readAdminUsersCode, cancellationToken)
            ?? Permission.Create(readAdminUsersCode, "Allows viewing users in the admin panel");

        if (context.Entry(readAdminUsersPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(readAdminUsersPermission, cancellationToken);

        PermissionCode updateAdminUserGroupsCode = new(WellKnownAuthorization.UpdateAdminUserGroupsPermission);

        Permission updateAdminUserGroupsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == updateAdminUserGroupsCode, cancellationToken)
            ?? Permission.Create(
                updateAdminUserGroupsCode,
                "Allows updating a user's group memberships in the admin panel"
            );

        if (context.Entry(updateAdminUserGroupsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(updateAdminUserGroupsPermission, cancellationToken);

        PermissionCode readAdminSubmissionsCode = new(WellKnownAuthorization.ReadAdminSubmissionsPermission);

        Permission readAdminSubmissionsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == readAdminSubmissionsCode, cancellationToken)
            ?? Permission.Create(readAdminSubmissionsCode, "Allows viewing submissions in the admin panel");

        if (context.Entry(readAdminSubmissionsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(readAdminSubmissionsPermission, cancellationToken);

        PermissionCode viewSubmissionCode = new(WellKnownAuthorization.ViewSubmissionPermission);

        Permission viewSubmissionPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == viewSubmissionCode, cancellationToken)
            ?? Permission.Create(viewSubmissionCode, "Allows viewing code submissions");

        if (context.Entry(viewSubmissionPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(viewSubmissionPermission, cancellationToken);

        PermissionCode playSoloRushCode = new(WellKnownAuthorization.PlaySoloRushPermission);

        Permission playSoloRushPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == playSoloRushCode, cancellationToken)
            ?? Permission.Create(playSoloRushCode, "Allows playing Solo Rush mode");

        if (context.Entry(playSoloRushPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(playSoloRushPermission, cancellationToken);

        PermissionCode createFeedbackCode = new(WellKnownAuthorization.CreateFeedbackPermission);

        Permission createFeedbackPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == createFeedbackCode, cancellationToken)
            ?? Permission.Create(createFeedbackCode, "Allows submitting product feedback");

        if (context.Entry(createFeedbackPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(createFeedbackPermission, cancellationToken);

        PermissionCode readAdminFeedbackCode = new(WellKnownAuthorization.ReadAdminFeedbackPermission);

        Permission readAdminFeedbackPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == readAdminFeedbackCode, cancellationToken)
            ?? Permission.Create(readAdminFeedbackCode, "Allows viewing feedback in the admin panel");

        if (context.Entry(readAdminFeedbackPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(readAdminFeedbackPermission, cancellationToken);

        PermissionCode updateAdminFeedbackCode = new(WellKnownAuthorization.UpdateAdminFeedbackPermission);

        Permission updateAdminFeedbackPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == updateAdminFeedbackCode, cancellationToken)
            ?? Permission.Create(updateAdminFeedbackCode, "Allows updating feedback status in the admin panel");

        if (context.Entry(updateAdminFeedbackPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(updateAdminFeedbackPermission, cancellationToken);

        PermissionCode readAdminDashboardCode = new(WellKnownAuthorization.ReadAdminDashboardPermission);

        Permission readAdminDashboardPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == readAdminDashboardCode, cancellationToken)
            ?? Permission.Create(readAdminDashboardCode, "Allows viewing the admin dashboard overview");

        if (context.Entry(readAdminDashboardPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(readAdminDashboardPermission, cancellationToken);

        PermissionCode readAdminGamesCode = new(WellKnownAuthorization.ReadAdminGamesPermission);

        Permission readAdminGamesPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == readAdminGamesCode, cancellationToken)
            ?? Permission.Create(readAdminGamesCode, "Allows viewing games in the admin panel");

        if (context.Entry(readAdminGamesPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(readAdminGamesPermission, cancellationToken);

        PermissionCode playDuelCode = new(WellKnownAuthorization.PlayDuelPermission);

        Permission playDuelPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == playDuelCode, cancellationToken)
            ?? Permission.Create(playDuelCode, "Allows playing Duel mode");

        if (context.Entry(playDuelPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(playDuelPermission, cancellationToken);

        PermissionCode playFfaCode = new(WellKnownAuthorization.PlayFfaPermission);

        Permission playFfaPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == playFfaCode, cancellationToken)
            ?? Permission.Create(playFfaCode, "Allows playing FFA mode");

        if (context.Entry(playFfaPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(playFfaPermission, cancellationToken);

        PermissionCode manageFeatureFlagsCode = new(WellKnownAuthorization.ManageFeatureFlagsPermission);

        Permission manageFeatureFlagsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == manageFeatureFlagsCode, cancellationToken)
            ?? Permission.Create(manageFeatureFlagsCode, "Allows managing feature flags in the admin panel");

        if (context.Entry(manageFeatureFlagsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(manageFeatureFlagsPermission, cancellationToken);

        PermissionCode readAdminCampaignsCode = new(WellKnownAuthorization.ReadAdminCampaignsPermission);

        Permission readAdminCampaignsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == readAdminCampaignsCode, cancellationToken)
            ?? Permission.Create(readAdminCampaignsCode, "Allows viewing campaigns in the admin panel");

        if (context.Entry(readAdminCampaignsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(readAdminCampaignsPermission, cancellationToken);

        PermissionCode manageAdminCampaignsCode = new(WellKnownAuthorization.ManageAdminCampaignsPermission);

        Permission manageAdminCampaignsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == manageAdminCampaignsCode, cancellationToken)
            ?? Permission.Create(manageAdminCampaignsCode, "Allows creating and editing campaigns in the admin panel");

        if (context.Entry(manageAdminCampaignsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(manageAdminCampaignsPermission, cancellationToken);

        PermissionCode readAdminAuditLogCode = new(WellKnownAuthorization.ReadAdminAuditLogPermission);

        Permission readAdminAuditLogPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == readAdminAuditLogCode, cancellationToken)
            ?? Permission.Create(readAdminAuditLogCode, "Allows viewing the admin audit log");

        if (context.Entry(readAdminAuditLogPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(readAdminAuditLogPermission, cancellationToken);

        PermissionCode createAdminProblemsCode = new(WellKnownAuthorization.CreateAdminProblemsPermission);

        Permission createAdminProblemsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == createAdminProblemsCode, cancellationToken)
            ?? Permission.Create(createAdminProblemsCode, "Allows creating and authoring problems in the admin panel");

        if (context.Entry(createAdminProblemsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(createAdminProblemsPermission, cancellationToken);

        PermissionCode submitAdminProblemsCode = new(WellKnownAuthorization.SubmitAdminProblemsPermission);

        Permission submitAdminProblemsPermission =
            await context.Permissions.FirstOrDefaultAsync(p => p.Code == submitAdminProblemsCode, cancellationToken)
            ?? Permission.Create(submitAdminProblemsCode, "Allows submitting an authored problem for validation");

        if (context.Entry(submitAdminProblemsPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(submitAdminProblemsPermission, cancellationToken);

        PermissionCode manageRequiredProblemLanguagesCode = new(
            WellKnownAuthorization.ManageRequiredProblemLanguagesPermission
        );

        Permission manageRequiredProblemLanguagesPermission =
            await context.Permissions.FirstOrDefaultAsync(
                p => p.Code == manageRequiredProblemLanguagesCode,
                cancellationToken
            )
            ?? Permission.Create(
                manageRequiredProblemLanguagesCode,
                "Allows managing the languages required for new problems"
            );

        if (context.Entry(manageRequiredProblemLanguagesPermission).State == EntityState.Detached)
            await context.Permissions.AddAsync(manageRequiredProblemLanguagesPermission, cancellationToken);

        Role role =
            await context
                .Roles.Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Name == new Name(WellKnownAuthorization.DefaultUserRole), cancellationToken)
            ?? Role.Create(new Name(WellKnownAuthorization.DefaultUserRole));

        if (context.Entry(role).State == EntityState.Detached)
            await context.Roles.AddAsync(role, cancellationToken);

        Role adminRole =
            await context
                .Roles.Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Name == new Name(WellKnownAuthorization.AdminRole), cancellationToken)
            ?? Role.Create(new Name(WellKnownAuthorization.AdminRole));

        if (context.Entry(adminRole).State == EntityState.Detached)
            await context.Roles.AddAsync(adminRole, cancellationToken);

        Group group =
            await context
                .Groups.Include(g => g.RoleGrants)
                .FirstOrDefaultAsync(
                    g => g.Name == new Name(WellKnownAuthorization.DefaultUserGroup),
                    cancellationToken
                )
            ?? Group.Create(new Name(WellKnownAuthorization.DefaultUserGroup));

        if (context.Entry(group).State == EntityState.Detached)
            await context.Groups.AddAsync(group, cancellationToken);

        Group adminGroup =
            await context
                .Groups.Include(g => g.RoleGrants)
                .FirstOrDefaultAsync(g => g.Name == new Name(WellKnownAuthorization.AdminGroup), cancellationToken)
            ?? Group.Create(new Name(WellKnownAuthorization.AdminGroup));

        if (context.Entry(adminGroup).State == EntityState.Detached)
            await context.Groups.AddAsync(adminGroup, cancellationToken);

        role.GrantPermission(createSubmissionPermission.Id);
        role.GrantPermission(viewSubmissionPermission.Id);
        role.GrantPermission(playSoloRushPermission.Id);
        role.GrantPermission(playDuelPermission.Id);
        role.GrantPermission(playFfaPermission.Id);
        role.GrantPermission(createFeedbackPermission.Id);

        adminRole.GrantPermission(readAdminProblemsPermission.Id);
        adminRole.GrantPermission(updateAdminProblemsPermission.Id);
        adminRole.GrantPermission(readAdminUsersPermission.Id);
        adminRole.GrantPermission(updateAdminUserGroupsPermission.Id);
        adminRole.GrantPermission(readAdminSubmissionsPermission.Id);
        adminRole.GrantPermission(readAdminFeedbackPermission.Id);
        adminRole.GrantPermission(updateAdminFeedbackPermission.Id);
        adminRole.GrantPermission(readAdminDashboardPermission.Id);
        adminRole.GrantPermission(readAdminGamesPermission.Id);
        adminRole.GrantPermission(manageFeatureFlagsPermission.Id);
        adminRole.GrantPermission(readAdminCampaignsPermission.Id);
        adminRole.GrantPermission(manageAdminCampaignsPermission.Id);
        adminRole.GrantPermission(readAdminAuditLogPermission.Id);
        adminRole.GrantPermission(createAdminProblemsPermission.Id);
        adminRole.GrantPermission(submitAdminProblemsPermission.Id);
        adminRole.GrantPermission(manageRequiredProblemLanguagesPermission.Id);

        group.GrantRole(role);
        adminGroup.GrantRole(adminRole);

        await context.SaveChangesAsync(cancellationToken);
    }
}