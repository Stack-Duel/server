using Ardalis.Result;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetProblemSetup;

internal sealed class GetProblemSetupHandler(IProblemReadRepository problemReadRepository)
    : IQueryHandler<GetProblemSetupQuery, ProblemSetupDto>
{
    public async Task<Result<ProblemSetupDto>> Handle(GetProblemSetupQuery request, CancellationToken cancellationToken)
    {
        var problem = await problemReadRepository.FindBySlugAsync(request.Slug, cancellationToken);

        if (problem is null)
        {
            return Result.NotFound();
        }

        var foundSetup = problem.FindSetupByLanguageVersionId(request.LanguageVersionId);

        if (foundSetup is null)
        {
            return Result.NotFound();
        }

        return Result.Success(
            new ProblemSetupDto(
                foundSetup.Id,
                foundSetup.InitialCode,
                foundSetup.FunctionName,
                foundSetup
                    .PublicTestSuites()
                    .SelectMany(
                        testSuite => testSuite.TestCases,
                        (testSuite, testCase) =>
                            new ProblemSetupTestCaseDto(
                                string.Join(", ", testCase.Inputs.Select(i => i.Value)),
                                string.Join(", ", testCase.ExpectedOutputs.Select(o => o.Value))
                            )
                    ),
                foundSetup.AdditionalFiles.Select(f => new ProblemSetupFileDto(f.Path, f.Content))
            )
        );
    }
}