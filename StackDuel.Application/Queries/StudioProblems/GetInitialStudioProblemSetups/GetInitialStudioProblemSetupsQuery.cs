using StackDuel.Application.StudioProblems.Dtos;

namespace StackDuel.Application.Queries.StudioProblems.GetInitialStudioProblemSetups;

public sealed record GetInitialStudioProblemSetupsQuery() : IQuery<InitialStudioProblemSetupDto>;
