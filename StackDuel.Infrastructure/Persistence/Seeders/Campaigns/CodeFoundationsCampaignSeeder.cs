using StackDuel.Application.Campaigns;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;
using StackDuel.Domain.Problems.Entities;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Persistence.Seeders.Campaigns;

internal sealed class CodeFoundationsCampaignSeeder(
    StackDuelDbContext context,
    ICampaignReadRepository campaignReadRepository,
    ICampaignWriteRepository campaignWriteRepository
) : IStaticSeeder
{
    private const string CampaignSlug = "code-foundations";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await campaignReadRepository.SlugExistsAsync(CampaignSlug, cancellationToken))
            return;

        Guid? twoSumProblemId = await FindProblemIdBySlugAsync("two-sum", cancellationToken);
        Guid? helloOrGoodbyeProblemId = await FindProblemIdBySlugAsync("hello-or-goodbye", cancellationToken);

        Campaign campaign = new(
            CampaignSlug,
            "Code Foundations",
            "Start here if you're brand new to programming. Learn the core building blocks you'll use in every language, then put them into practice.",
            CampaignDifficulty.Beginner
        );

        CampaignModule gettingStarted = campaign.AddModule(
            "Getting Started",
            "What programming is, and how to write and run your first lines of code."
        );

        gettingStarted.AddUnit(
            "What Is Programming?",
            "A program is a sequence of instructions a computer follows, one step at a time. "
                + "Every app, website, and game you've ever used is built from the same small set of "
                + "building blocks: storing information, making decisions, and repeating work. "
                + "This course walks through those building blocks from scratch.",
            UnitType.Lesson,
            5
        );

        gettingStarted.AddUnit(
            "Variables and Data Types",
            "A variable is a named container for a value. Values come in a few basic types — "
                + "numbers, text (strings), and true/false (booleans) — and the type determines what "
                + "you can do with a value, like adding two numbers or joining two strings together.",
            UnitType.Lesson,
            10
        );

        gettingStarted.AddUnit(
            "Your First Program",
            "Most languages start with a program that prints a message to the screen. Writing, "
                + "running, and seeing the output of your first program is the fastest way to confirm "
                + "your environment works before building anything more complex.",
            UnitType.Lesson,
            10
        );

        CampaignModule controlFlow = campaign.AddModule("Control Flow", "Make decisions and repeat work in your code.");

        controlFlow.AddUnit(
            "Conditionals",
            "An if statement runs a block of code only when a condition is true, and an else "
                + "branch runs otherwise. Chaining conditions lets a program react differently "
                + "depending on the data it's given.",
            UnitType.Lesson,
            10
        );

        controlFlow.AddUnit(
            "Loops",
            "A loop repeats a block of code until a condition is no longer true, so you can "
                + "process a list of items or repeat an action a fixed number of times without "
                + "writing it out by hand.",
            UnitType.Lesson,
            10
        );

        CampaignUnit controlFlowPractice = controlFlow.AddUnit(
            "Practice: Hello or Goodbye",
            "Put conditionals to work: given a name, decide whether to greet or say farewell.",
            UnitType.Challenge,
            15
        );

        if (helloOrGoodbyeProblemId is { } helloId)
            controlFlowPractice.SetProblems([helloId]);

        CampaignModule functions = campaign.AddModule("Functions", "Package up logic so you can reuse it.");

        functions.AddUnit(
            "Writing Functions",
            "A function packages up a sequence of steps under a name, so you can run that logic "
                + "again elsewhere without copying it. Functions take inputs (parameters) and can hand "
                + "back a result (a return value).",
            UnitType.Lesson,
            10
        );

        CampaignUnit functionsPractice = functions.AddUnit(
            "Practice: Two Sum",
            "Write a function that finds two numbers in a list that add up to a target value.",
            UnitType.Challenge,
            20
        );

        if (twoSumProblemId is { } twoSumId)
            functionsPractice.SetProblems([twoSumId]);

        functions.AddUnit(
            "Foundations Check",
            "A short check-in on variables, conditionals, loops, and functions before you move "
                + "on to the next path.",
            UnitType.Quiz,
            10
        );

        campaign.Publish();

        await campaignWriteRepository.AddAsync(campaign, cancellationToken);
    }

    private async Task<Guid?> FindProblemIdBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await context
            .Set<Problem>()
            .AsNoTracking()
            .Where(p => p.Slug.Value == slug)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}