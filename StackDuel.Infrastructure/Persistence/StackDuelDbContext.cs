using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Audit.Entities;
using StackDuel.Domain.Authorization.Rbac.Entities;
using StackDuel.Domain.Authorization.Security.Entities;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.DailyChallenges.Entities;
using StackDuel.Domain.ExecutionAssets.Entities;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.FeatureFlags.Entities;
using StackDuel.Domain.Feedback.Entities;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.Enums;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.RequiredLanguages.Entities;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.Tracks.Entities;
using StackDuel.Domain.Users.Entities;
using StackDuel.Infrastructure.ExecutionEngine.Assert;
using StackDuel.Infrastructure.ExecutionEngine.Judge0;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Persistence;

internal sealed class StackDuelDbContext(DbContextOptions<StackDuelDbContext> options) : DbContext(options)
{
    public DbSet<AdditionalFileBundle> AdditionalFileBundles { get; init; }

    public DbSet<AchievementDefinition> AchievementDefinitions { get; init; }

    public DbSet<UserAchievement> UserAchievements { get; init; }

    public DbSet<UserAchievementStats> UserAchievementStats { get; init; }

    public DbSet<UserAchievementStatLanguage> UserAchievementStatLanguages { get; init; }

    public DbSet<Campaign> Campaigns { get; init; }

    public DbSet<CampaignModule> CampaignModules { get; init; }

    public DbSet<CampaignUnit> CampaignUnits { get; init; }

    public DbSet<UnitProblem> UnitProblems { get; init; }

    public DbSet<CampaignPrerequisite> CampaignPrerequisites { get; init; }

    public DbSet<CampaignEnrollment> CampaignEnrollments { get; init; }

    public DbSet<UnitCompletion> UnitCompletions { get; init; }

    public DbSet<DailyChallenge> DailyChallenges { get; init; }

    public DbSet<AuditLogEntry> AuditLogEntries { get; init; }

    public DbSet<ExecutionPipeline> ExecutionPipelines { get; init; }

    public DbSet<FeedbackSubmission> FeedbackSubmissions { get; init; }

    public DbSet<FeatureFlag> FeatureFlags { get; init; }

    public DbSet<FeatureFlagUserOverride> FeatureFlagUserOverrides { get; init; }

    public DbSet<FeatureFlagGroupOverride> FeatureFlagGroupOverrides { get; init; }

    public DbSet<Group> Groups { get; init; }

    public DbSet<Game> Games { get; init; }

    public DbSet<GameMode> GameModes { get; init; }

    public DbSet<GameModeTimeOption> GameModeTimeOptions { get; init; }

    public DbSet<GameParticipant> GameParticipants { get; init; }

    public DbSet<GameTrack> GameTracks { get; init; }

    public DbSet<Leaderboard> Leaderboards { get; init; }

    public DbSet<LeaderboardParticipant> LeaderboardParticipants { get; init; }

    public DbSet<Language> Languages { get; init; }

    public DbSet<Permission> Permissions { get; init; }

    public DbSet<Problem> Problems { get; init; }

    public DbSet<ProblemPool> ProblemPools { get; init; }

    public DbSet<ProblemReaction> ProblemReactions { get; init; }

    public DbSet<ProblemReactionType> ProblemReactionTypes { get; init; }

    public DbSet<ProblemTag> Tags { get; init; }

    public DbSet<RequiredProblemLanguage> RequiredProblemLanguages { get; init; }

    public DbSet<ProblemValidationJob> ProblemValidationJobs { get; init; }

    public DbSet<Role> Roles { get; init; }

    public DbSet<RolePermission> RolePermissions { get; init; }

    public DbSet<SecurityRestriction> SecurityRestrictions { get; init; }

    public DbSet<Submission> Submissions { get; init; }

    public DbSet<SubmissionJob> SubmissionJobs { get; init; }

    public DbSet<TestCaseGenerationSpec> TestCaseGenerationSpecs { get; init; }

    public DbSet<TestCaseGenerationJob> TestCaseGenerationJobs { get; init; }

    public DbSet<TestSuite> TestSuites { get; init; }

    public DbSet<Track> Tracks { get; init; }

    public DbSet<User> Users { get; init; }

    public DbSet<UserAvatar> UserAvatars { get; init; }

    // Infrastructure-only — not domain aggregates
    public DbSet<Judge0StepConfiguration> Judge0StepConfigurations { get; init; }
    public DbSet<AssertStepConfiguration> AssertStepConfigurations { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(StackDuelDbContext).Assembly,
            t => t.Namespace is null || !t.Namespace.StartsWith("StackDuel.Infrastructure.Persistence.Read")
        );

        modelBuilder.Entity<Problem>().HasQueryFilter(problem => problem.Status == ProblemStatus.Published);
        modelBuilder.Entity<Language>().HasQueryFilter(language => language.Status == LanguageStatus.Active);
        modelBuilder
            .Entity<LanguageVersionEntry>()
            .HasQueryFilter(version => version.Status == LanguageVersionStatus.Active);
    }
}