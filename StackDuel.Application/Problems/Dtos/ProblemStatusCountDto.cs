using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemStatusCountDto(ProblemStatus Status, int Count);
