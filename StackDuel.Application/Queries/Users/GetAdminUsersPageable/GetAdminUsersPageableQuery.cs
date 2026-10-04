using StackDuel.Application.Pagination;
using StackDuel.Application.Users.Dtos.Admin;

namespace StackDuel.Application.Queries.Users.GetAdminUsersPageable;

public sealed record GetAdminUsersPageableQuery(PaginationRequest PaginationRequest, string? Search)
    : IQuery<PageResult<AdminUserDto>>;