using StackDuel.Application.Commands.ProblemPools.AddProblemsToPool;
using StackDuel.Application.Commands.ProblemPools.AddProblemToPool;
using StackDuel.Application.Commands.ProblemPools.CreateProblemPool;
using StackDuel.Application.Commands.ProblemPools.RemoveProblemFromPool;
using StackDuel.Application.Commands.ProblemPools.ReorderProblemPool;
using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Queries.ProblemPools.GetOrderedProblemPoolMembers;
using StackDuel.Application.Queries.ProblemPools.GetProblemPoolMemberIds;
using StackDuel.Application.Queries.ProblemPools.GetProblemPoolMembersPaged;
using StackDuel.Application.Queries.ProblemPools.GetProblemPools;
using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Services.Problems;

public interface IProblemPoolService
{
    Task<Result<IReadOnlyList<ProblemPoolDto>>> GetProblemPoolsAsync(CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<Guid>>> GetProblemPoolMemberIdsAsync(string poolKey, CancellationToken cancellationToken);

    Task<Result<PageResult<AdminProblemListItemDto>>> GetProblemPoolMembersPagedAsync(
        string poolKey,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken
    );

    Task<Result<Guid>> CreateProblemPoolAsync(
        string key,
        string name,
        string? description,
        CancellationToken cancellationToken
    );

    Task<Result> AddProblemToPoolAsync(string poolKey, Guid problemId, CancellationToken cancellationToken);

    Task<Result<int>> AddProblemsToPoolAsync(
        string poolKey,
        IReadOnlyList<Guid> problemIds,
        bool selectAllMatching,
        string? search,
        IReadOnlyList<Guid> excludedProblemIds,
        CancellationToken cancellationToken
    );

    Task<Result> RemoveProblemFromPoolAsync(string poolKey, Guid problemId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AdminProblemListRowDto>>> GetOrderedProblemPoolMembersAsync(
        string poolKey,
        CancellationToken cancellationToken
    );

    Task<Result> ReorderProblemPoolAsync(
        string poolKey,
        IReadOnlyList<Guid> problemIds,
        CancellationToken cancellationToken
    );
}

internal sealed class ProblemPoolService(IMediator mediator) : IProblemPoolService
{
    public async Task<Result<IReadOnlyList<ProblemPoolDto>>> GetProblemPoolsAsync(CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetProblemPoolsQuery(), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetProblemPoolMemberIdsAsync(
        string poolKey,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetProblemPoolMemberIdsQuery(poolKey), cancellationToken);
    }

    public async Task<Result<PageResult<AdminProblemListItemDto>>> GetProblemPoolMembersPagedAsync(
        string poolKey,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetProblemPoolMembersPagedQuery(poolKey, paginationRequest), cancellationToken);
    }

    public async Task<Result<Guid>> CreateProblemPoolAsync(
        string key,
        string name,
        string? description,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new CreateProblemPoolCommand(key, name, description), cancellationToken);
    }

    public async Task<Result> AddProblemToPoolAsync(string poolKey, Guid problemId, CancellationToken cancellationToken)
    {
        return await mediator.Send(new AddProblemToPoolCommand(poolKey, problemId), cancellationToken);
    }

    public async Task<Result<int>> AddProblemsToPoolAsync(
        string poolKey,
        IReadOnlyList<Guid> problemIds,
        bool selectAllMatching,
        string? search,
        IReadOnlyList<Guid> excludedProblemIds,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(
            new AddProblemsToPoolCommand(poolKey, problemIds, selectAllMatching, search, excludedProblemIds),
            cancellationToken
        );
    }

    public async Task<Result> RemoveProblemFromPoolAsync(
        string poolKey,
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new RemoveProblemFromPoolCommand(poolKey, problemId), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<AdminProblemListRowDto>>> GetOrderedProblemPoolMembersAsync(
        string poolKey,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new GetOrderedProblemPoolMembersQuery(poolKey), cancellationToken);
    }

    public async Task<Result> ReorderProblemPoolAsync(
        string poolKey,
        IReadOnlyList<Guid> problemIds,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new ReorderProblemPoolCommand(poolKey, problemIds), cancellationToken);
    }
}