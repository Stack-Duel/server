using StackDuel.Application.Users.Dtos.Admin;

namespace StackDuel.Application.Queries.Users.GetAdminUserDetail;

public sealed record GetAdminUserDetailQuery(Guid UserId) : IQuery<AdminUserDetailDto>;