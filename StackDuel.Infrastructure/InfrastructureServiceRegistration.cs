using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using RabbitMQ.Client;
using StackDuel.Application.Audit;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Configuration;
using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Dashboard;
using StackDuel.Application.ExecutionAssets;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.FeatureFlags;
using StackDuel.Application.Feedback;
using StackDuel.Application.Games;
using StackDuel.Application.Groups;
using StackDuel.Application.Jobs.DailyChallenges;
using StackDuel.Application.Jobs.Submissions;
using StackDuel.Application.Jobs.Users;
using StackDuel.Application.Languages;
using StackDuel.Application.LanguageServer;
using StackDuel.Application.Leaderboards;
using StackDuel.Application.Messaging;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.RequiredLanguages;
using StackDuel.Application.Services.Users;
using StackDuel.Application.Settings;
using StackDuel.Application.Submissions;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Domain.Authorization;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.Feedback;
using StackDuel.Domain.Games;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.RequiredLanguages;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Domain.TestSuites;
using StackDuel.Domain.Users;
using StackDuel.Infrastructure.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using StackDuel.Infrastructure.ExecutionEngine.Judge0;
using StackDuel.Infrastructure.ExecutionEngine.StepHandlers;
using StackDuel.Infrastructure.Jobs.DailyChallenges;
using StackDuel.Application.Jobs.Games;
using StackDuel.Infrastructure.Jobs.Games;
using StackDuel.Infrastructure.Jobs.ProblemValidation;
using StackDuel.Infrastructure.Jobs.Submissions;
using StackDuel.Infrastructure.Jobs.TestCaseGeneration;
using StackDuel.Infrastructure.Jobs.Users;
using StackDuel.Infrastructure.LanguageServer;
using StackDuel.Infrastructure.Messaging;
using StackDuel.Infrastructure.Messaging.Consumers;
using StackDuel.Infrastructure.Persistence;
using StackDuel.Infrastructure.Persistence.Read;
using StackDuel.Infrastructure.Persistence.Seeders;
using StackDuel.Infrastructure.Persistence.Seeders.Campaigns;
using StackDuel.Infrastructure.Persistence.Seeders.Problems;
using StackDuel.Infrastructure.ProblemValidation;
using StackDuel.Infrastructure.Repositories.AuditLog;
using StackDuel.Infrastructure.Repositories.Authorization;
using StackDuel.Infrastructure.Repositories.Campaigns;
using StackDuel.Infrastructure.Repositories.DailyChallenges;
using StackDuel.Infrastructure.Repositories.Dashboard;
using StackDuel.Infrastructure.Repositories.ExecutionAssets;
using StackDuel.Infrastructure.Repositories.ExecutionPipelines;
using StackDuel.Infrastructure.Repositories.FeatureFlags;
using StackDuel.Infrastructure.Repositories.Feedback;
using StackDuel.Infrastructure.Repositories.Games;
using StackDuel.Infrastructure.Repositories.Languages;
using StackDuel.Infrastructure.Repositories.Leaderboards;
using StackDuel.Infrastructure.Repositories.Problems;
using StackDuel.Infrastructure.Repositories.ProblemValidation;
using StackDuel.Infrastructure.Repositories.SubmissionJobs;
using StackDuel.Infrastructure.Repositories.Submissions;
using StackDuel.Infrastructure.Repositories.TestCaseGeneration;
using StackDuel.Infrastructure.Repositories.TestSuites;
using StackDuel.Infrastructure.Repositories.Tracks;
using StackDuel.Infrastructure.Repositories.Users;
using StackDuel.Infrastructure.Settings;
using StackDuel.Infrastructure.Storage;
using StackDuel.Infrastructure.TestCaseGeneration;

namespace StackDuel.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions(configuration);

        services.AddPersistence();
        services.AddRepositories();
        services.AddExecutionEngine(configuration);
        services.AddTestCaseGeneration();
        services.AddLanguageServer(configuration);
        services.AddAvatarStorage(configuration);

        services.AddMessageBus(configuration);
        services.AddJobs(configuration);

        services.AddSeeder();

        return services;
    }

    /// <summary>
    /// Registration for the Seeder CLI — persistence and repositories only. Seeders don't run
    /// test case generation directly anymore; they enqueue a TestCaseGenerationJob row (via
    /// ITestCaseGenerationJobRepository, from AddRepositories) and the API's background
    /// processor picks it up later. That means the Seeder needs no Judge0/execution-engine
    /// dependency at all, and no message bus or Quartz jobs since it's a one-shot CLI, not a host.
    /// </summary>
    public static IServiceCollection AddInfrastructureForSeeder(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOption<ConnectionStringOptions>(configuration);
        services.AddPersistence();
        services.AddRepositories();
        services.AddSeeder();

        return services;
    }

    private static IServiceCollection AddMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        var opts =
            configuration.GetSection(MessageBusOptions.SectionName).Get<MessageBusOptions>() ?? new MessageBusOptions();

        if (opts.Transport.Equals("AzureServiceBus", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(new ServiceBusClient(opts.AzureServiceBus.ConnectionString));
            services.AddScoped<IMessagePublisher, AzureServiceBusMessagePublisher>();
            services.AddHostedService<AzureServiceBusConsumerService>();
        }
        else
        {
            services.AddSingleton<IConnection>(_ =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = opts.RabbitMQ.Host,
                    VirtualHost = opts.RabbitMQ.VirtualHost,
                    UserName = opts.RabbitMQ.Username,
                    Password = opts.RabbitMQ.Password,
                    DispatchConsumersAsync = true,
                };
                return factory.CreateConnection();
            });
            services.AddScoped<IMessagePublisher, RabbitMqMessagePublisher>();
            services.AddHostedService<RabbitMqConsumerService>();
        }

        return services;
    }

    private static IServiceCollection AddExecutionEngine(this IServiceCollection services, IConfiguration configuration)
    {
        var opts =
            configuration.GetSection(ExecutionEngineOptions.SectionName).Get<ExecutionEngineOptions>()
            ?? new ExecutionEngineOptions();

        if (opts.Judge0.Enabled)
        {
            var judge0Opts = opts.Judge0;
            services.AddSingleton(judge0Opts);
            services.AddHttpClient(
                nameof(Judge0ExecutionEngineStrategy),
                client =>
                {
                    client.BaseAddress = new Uri(judge0Opts.BaseUrl.TrimEnd('/') + "/");
                    if (!string.IsNullOrWhiteSpace(judge0Opts.ApiKey))
                        client.DefaultRequestHeaders.Add("X-RapidAPI-Key", judge0Opts.ApiKey);
                    if (!string.IsNullOrWhiteSpace(judge0Opts.Host))
                        client.DefaultRequestHeaders.Add("X-RapidAPI-Host", judge0Opts.Host);
                }
            );
            services.AddScoped<IExecutionEngineStrategy>(sp =>
            {
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var http = httpFactory.CreateClient(nameof(Judge0ExecutionEngineStrategy));
                return new Judge0ExecutionEngineStrategy(http, judge0Opts);
            });
        }

        services.AddScoped<ICodeTemplateStrategy, JavaScriptCodeTemplateStrategy>();
        services.AddScoped<ICodeTemplateStrategy, TypeScriptCodeTemplateStrategy>();
        services.AddScoped<ICodeTemplateStrategy, PythonCodeTemplateStrategy>();
        services.AddScoped<ICodeTemplateStrategy, SQLiteCodeTemplateStrategy>();
        services.AddScoped<ICodeTemplateStrategy, JavaCodeTemplateStrategy>();
        services.AddScoped<ICodeTemplateStrategy, CppCodeTemplateStrategy>();
        services.AddSingleton(opts.JsxTranspiler);
        services.AddScoped<IJsxTranspiler, EsbuildJsxTranspiler>();
        services.AddScoped<ICodeTemplateStrategy, ReactCodeTemplateStrategy>();
        services.AddScoped<ICodeTemplateStrategy, VanillaJsCodeTemplateStrategy>();
        services.AddScoped<ICodeTemplateStrategyResolver, CodeTemplateStrategyResolver>();

        services.AddScoped<IBatchCodeTemplateStrategy, VitestJavaScriptBatchTemplateStrategy>();
        services.AddScoped<IBatchCodeTemplateStrategyResolver, BatchCodeTemplateStrategyResolver>();

        services.AddScoped<IStepHandler, Judge0ExecutionStepHandler>();
        services.AddScoped<IStepHandler, Judge0PollStepHandler>();
        services.AddScoped<IStepHandler, EvaluateStepHandler>();
        services.AddScoped<IStepHandlerRegistry, StepHandlerRegistry>();

        services.AddSingleton<SubmissionJobProcessorService>();

        return services;
    }

    private static IServiceCollection AddLanguageServer(this IServiceCollection services, IConfiguration configuration)
    {
        var opts =
            configuration.GetSection(LanguageServerOptions.SectionName).Get<LanguageServerOptions>()
            ?? new LanguageServerOptions();

        services.AddSingleton(opts);
        services.AddSingleton<ILanguageServerAdapter, JavaLanguageServerAdapter>();
        services.AddSingleton<ILanguageServerSessionManager, LanguageServerSessionManager>();
        services.AddHostedService<LanguageServerIdleSessionReaper>();

        return services;
    }

    private static IServiceCollection AddJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISubmissionCleanupService, SubmissionCleanupService>();
        services.AddScoped<IGameExpirySweepService, GameExpirySweepService>();
        services.AddScoped<IAvatarCleanupService, AvatarCleanupService>();
        services.AddScoped<IDailyChallengeAssignmentService, DailyChallengeAssignmentService>();

        var opts =
            configuration.GetSection(JobScheduleOptions.SectionName).Get<JobScheduleOptions>()
            ?? new JobScheduleOptions();

        services.AddQuartz(q =>
        {
            q.AddJob<GameExpirySweepJob>(GameExpirySweepJob.Key, j => j.StoreDurably());
            q.AddTrigger(t =>
                t.ForJob(GameExpirySweepJob.Key)
                    .WithIdentity("GameExpirySweepJob-trigger")
                    .WithCronSchedule(opts.GameExpirySweepJob.CronExpression)
            );

            q.AddJob<SubmissionCleanupJob>(SubmissionCleanupJob.Key, j => j.StoreDurably());
            q.AddTrigger(t =>
                t.ForJob(SubmissionCleanupJob.Key)
                    .WithIdentity("SubmissionCleanupJob-trigger")
                    .WithCronSchedule(opts.SubmissionCleanupJob.CronExpression)
            );

            q.AddJob<SubmissionJobProcessorJob>(SubmissionJobProcessorJob.Key, j => j.StoreDurably());
            q.AddTrigger(t =>
                t.ForJob(SubmissionJobProcessorJob.Key)
                    .WithIdentity("SubmissionJobProcessorJob-trigger")
                    .WithCronSchedule(opts.SubmissionJobProcessorJob.CronExpression)
            );

            q.AddJob<TestCaseGenerationJobProcessorJob>(TestCaseGenerationJobProcessorJob.Key, j => j.StoreDurably());
            q.AddTrigger(t =>
                t.ForJob(TestCaseGenerationJobProcessorJob.Key)
                    .WithIdentity("TestCaseGenerationJobProcessorJob-trigger")
                    .WithCronSchedule(opts.TestCaseGenerationJobProcessorJob.CronExpression)
            );

            q.AddJob<TestCaseRefreshJob>(TestCaseRefreshJob.Key, j => j.StoreDurably());
            q.AddTrigger(t =>
                t.ForJob(TestCaseRefreshJob.Key)
                    .WithIdentity("TestCaseRefreshJob-trigger")
                    .WithCronSchedule(opts.TestCaseRefreshJob.CronExpression)
            );

            q.AddJob<ProblemValidationJobProcessorJob>(ProblemValidationJobProcessorJob.Key, j => j.StoreDurably());
            q.AddTrigger(t =>
                t.ForJob(ProblemValidationJobProcessorJob.Key)
                    .WithIdentity("ProblemValidationJobProcessorJob-trigger")
                    .WithCronSchedule(opts.ProblemValidationJobProcessorJob.CronExpression)
            );

            q.AddJob<AvatarCleanupJob>(AvatarCleanupJob.Key, j => j.StoreDurably());
            q.AddTrigger(t =>
                t.ForJob(AvatarCleanupJob.Key)
                    .WithIdentity("AvatarCleanupJob-trigger")
                    .WithCronSchedule(opts.AvatarCleanupJob.CronExpression)
            );

            q.AddJob<DailyChallengeAssignmentJob>(DailyChallengeAssignmentJob.Key, j => j.StoreDurably());
            q.AddTrigger(t =>
                t.ForJob(DailyChallengeAssignmentJob.Key)
                    .WithIdentity("DailyChallengeAssignmentJob-trigger")
                    .WithCronSchedule(opts.DailyChallengeAssignmentJob.CronExpression)
            );
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }

    private static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOption<ConnectionStringOptions>(configuration);
        services.AddOption<MessageBusOptions>(configuration);
        services.AddOption<AzureStorageOptions>(configuration);

        return services;
    }

    private static IServiceCollection AddAvatarStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var opts =
            configuration.GetSection(AzureStorageOptions.SectionName).Get<AzureStorageOptions>()
            ?? new AzureStorageOptions();

        services.AddSingleton(new BlobServiceClient(opts.ConnectionString));
        services.AddScoped<IAvatarBlobStorage, AzureAvatarBlobStorage>();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddDbContextPool<StackDuelDbContext>(
            (serviceProvider, dbOptions) =>
            {
                var options = serviceProvider.GetRequiredService<ConnectionStringOptions>();

                dbOptions.UseNpgsql(options.DefaultConnection);

                dbOptions.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        );

        services.AddDbContextPool<StackDuelReadDbContext>(
            (serviceProvider, dbOptions) =>
            {
                var options = serviceProvider.GetRequiredService<ConnectionStringOptions>();

                dbOptions.UseNpgsql(options.DefaultConnection);
            }
        );

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAdditionalFileBundleReadRepository, AdditionalFileBundleReadRepository>();
        services.AddScoped<IAuditLogWriteRepository, AuditLogWriteRepository>();
        services.AddScoped<IAuditLogReadRepository, AuditLogReadRepository>();
        services.AddScoped<IAuthorizationReadRepository, AuthorizationReadRepository>();
        services.AddScoped<IFeatureFlagWriteRepository, FeatureFlagWriteRepository>();
        services.AddScoped<IFeatureFlagEvaluationRepository, FeatureFlagEvaluationRepository>();
        services.AddScoped<IFeatureFlagAdminReadRepository, FeatureFlagAdminReadRepository>();
        services.AddScoped<IGroupReadRepository, GroupReadRepository>();
        services.AddScoped<ILanguageReadRepository, LanguageReadRepository>();
        services.AddScoped<IProblemReadRepository, ProblemReadRepository>();
        services.AddScoped<IProblemRepository, ProblemRepository>();
        services.AddScoped<IProblemPoolRepository, ProblemPoolRepository>();
        services.AddScoped<IProblemReactionReadRepository, ProblemReactionReadRepository>();
        services.AddScoped<IProblemReactionWriteRepository, ProblemReactionWriteRepository>();
        services.AddScoped<IGameWriteRepository, GameWriteRepository>();
        services.AddScoped<IGameReadRepository, GameReadRepository>();
        services.AddScoped<ILeaderboardWriteRepository, LeaderboardWriteRepository>();
        services.AddScoped<ILeaderboardReadRepository, LeaderboardReadRepository>();
        services.AddScoped<ISubmissionWriteRepository, SubmissionWriteRepository>();
        services.AddScoped<ISubmissionReadRepository, SubmissionReadRepository>();
        services.AddScoped<IFeedbackWriteRepository, FeedbackWriteRepository>();
        services.AddScoped<IFeedbackReadRepository, FeedbackReadRepository>();
        services.AddScoped<IDashboardReadRepository, DashboardReadRepository>();
        services.AddScoped<IDailyChallengeRepository, DailyChallengeRepository>();
        services.AddScoped<ISubmissionJobRepository, SubmissionJobRepository>();
        services.AddScoped<IExecutionPipelineRepository, ExecutionPipelineRepository>();
        services.AddScoped<ITestSuiteWriteRepository, TestSuiteWriteRepository>();
        services.AddScoped<ITestCaseGenerationJobRepository, TestCaseGenerationJobRepository>();
        services.AddScoped<IRequiredProblemLanguageRepository, RequiredProblemLanguageRepository>();
        services.AddScoped<IRequiredProblemLanguageReadRepository, RequiredProblemLanguageReadRepository>();
        services.AddScoped<IProblemValidationJobRepository, ProblemValidationJobRepository>();
        services.AddScoped<IUserReadRepository, UserReadRepository>();
        services.AddScoped<IUserWriteRepository, UserWriteRepository>();
        services.AddScoped<IUserAvatarWriteRepository, UserAvatarWriteRepository>();
        services.AddScoped<ITrackReadRepository, TrackReadRepository>();
        services.AddScoped<ICampaignReadRepository, CampaignReadRepository>();
        services.AddScoped<ICampaignWriteRepository, CampaignWriteRepository>();
        services.AddScoped<ICampaignEnrollmentWriteRepository, CampaignEnrollmentWriteRepository>();
        services.AddScoped<IUnitCompletionWriteRepository, UnitCompletionWriteRepository>();
        services.AddScoped<ICampaignProgressReadRepository, CampaignProgressReadRepository>();

        return services;
    }

    private static IServiceCollection AddTestCaseGeneration(this IServiceCollection services)
    {
        services.AddScoped<ITestCaseGeneratorService, BogusTestCaseGeneratorService>();
        services.AddScoped<Judge0ReferenceSolutionRunner>();
        services.AddScoped<ITestCaseGenerationService, TestCaseGenerationService>();
        services.AddScoped<ITestCaseRefreshService, TestCaseRefreshService>();
        services.AddScoped<IReferenceSolutionValidationService, ReferenceSolutionValidationService>();
        services.AddSingleton<TestCaseGenerationJobProcessorService>();
        services.AddSingleton<ProblemValidationJobProcessorService>();

        return services;
    }

    // Registration order is seeding order (the built-in container resolves IEnumerable<T>/GetServices<T>
    // in registration order) — a seeder must be registered after everything it depends on
    // (e.g. LanguageSeeder reads Tracks, so TrackSeeder has to run first).
    private static IServiceCollection AddSeeder(this IServiceCollection services)
    {
        services.AddScoped<IStaticSeeder, AuthorizationSeeder>();
        services.AddScoped<IStaticSeeder, FeatureFlagSeeder>();
        services.AddScoped<IStaticSeeder, GameModeSeeder>();
        services.AddScoped<IStaticSeeder, TrackSeeder>();
        services.AddScoped<IStaticSeeder, ProblemReactionTypeSeeder>();

        // Also consumed by concrete type (GetOrCreateAsync) by the problem seeders below,
        // so it needs its own registration in addition to the IStaticSeeder one.
        services.AddScoped<Judge0PipelineSeeder>();
        services.AddScoped<IStaticSeeder>(sp => sp.GetRequiredService<Judge0PipelineSeeder>());

        services.AddScoped<IStaticSeeder, LanguageSeeder>();
        services.AddScoped<IStaticSeeder, RequiredProblemLanguageSeeder>();
        services.AddScoped<AdditionalFileBundleSeeder>();
        services.AddScoped<IStaticSeeder, TwoSumProblemSeeder>();
        services.AddScoped<IStaticSeeder, HelloOrGoodbyeProblemSeeder>();
        services.AddScoped<IStaticSeeder, AdditionalProblemsSeeder>();
        services.AddScoped<IStaticSeeder, SumOfNumbersSqlProblemSeeder>();
        services.AddScoped<IStaticSeeder, ChinookSqlProblemsSeeder>();
        services.AddScoped<IStaticSeeder, ClickCounterProblemSeeder>();
        services.AddScoped<IStaticSeeder, VanillaClickCounterProblemSeeder>();
        services.AddScoped<IStaticSeeder, ArtistDiscographyProblemSeeder>();
        services.AddScoped<IStaticSeeder, ProblemPoolSeeder>();
        services.AddScoped<IStaticSeeder, CodeFoundationsCampaignSeeder>();

        services.AddScoped<IDemoSeeder, DemoDataSeeder>();

        return services;
    }

    public static async Task MigrateAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StackDuelDbContext>();
        await db.Database.MigrateAsync();
    }

    public static async Task SeedAsync(
        this IServiceProvider services,
        SeederOptions options,
        CancellationToken cancellationToken = default
    )
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StackDuelDbContext>();

        if (options.SeedStaticData)
        {
            foreach (IStaticSeeder seeder in scope.ServiceProvider.GetServices<IStaticSeeder>())
            {
                await seeder.SeedAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
        }

        if (options.SeedDemoData)
        {
            foreach (IDemoSeeder seeder in scope.ServiceProvider.GetServices<IDemoSeeder>())
            {
                await seeder.SeedAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
        }
    }
}