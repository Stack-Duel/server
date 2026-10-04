using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StackDuel.Application.Behaviors;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Application.Problems;
using StackDuel.Application.Services.Audit;
using StackDuel.Application.Services.Campaigns;
using StackDuel.Application.Services.DailyChallenges;
using StackDuel.Application.Services.Dashboard;
using StackDuel.Application.Services.FeatureFlags;
using StackDuel.Application.Services.Feedback;
using StackDuel.Application.Services.Games;
using StackDuel.Application.Services.Groups;
using StackDuel.Application.Services.Languages;
using StackDuel.Application.Services.Leaderboards;
using StackDuel.Application.Services.Problems;
using StackDuel.Application.Services.Submissions;
using StackDuel.Application.Services.Users;
using StackDuel.Domain.Feedback.Entities;
using StackDuel.Domain.Feedback.Factories;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Factories;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.Factories;

namespace StackDuel.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly, includeInternalTypes: true);
        services.AddMemoryCache();

        services.AddFactories();
        services.AddServices();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped<UserContext>();

        return services;
    }

    private static IServiceCollection AddFactories(this IServiceCollection services)
    {
        services.AddScoped<IAggregateFactory<User, CreateUserParams>, UserFactory>();
        services.AddScoped<IAggregateFactory<Submission, CreateSubmissionParams>, SubmissionFactory>();
        services.AddScoped<
            IAggregateFactory<FeedbackSubmission, CreateFeedbackSubmissionParams>,
            FeedbackSubmissionFactory
        >();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUsernameGeneratorService, UsernameGeneratorService>();
        services.AddScoped<IProblemService, ProblemService>();
        services.AddScoped<IProblemPoolService, ProblemPoolService>();
        services.AddScoped<IRequiredProblemLanguageAdminService, RequiredProblemLanguageAdminService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDailyChallengeService, DailyChallengeService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<IFeatureFlagResolutionService, FeatureFlagResolutionService>();
        services.AddScoped<IFeatureFlagAdminService, FeatureFlagAdminService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        foreach (string gameModeKey in new[] { "solo_rush", "duel", "ffa" })
        {
            string key = gameModeKey;
            services.AddScoped<IProblemSelectionStrategy>(sp => new DifficultyRampProblemSelectionStrategy(
                key,
                sp.GetRequiredService<IProblemReadRepository>()
            ));
        }
        services.AddScoped<IProblemSelectionStrategyResolver, ProblemSelectionStrategyResolver>();
        services.AddScoped<IGameProblemSequencer, GameProblemSequencer>();
        services.AddScoped<IGameExpiryCanceller, GameExpiryCanceller>();
        services.AddScoped<IGameProblemAdvancer, GameProblemAdvancer>();

        return services;
    }
}