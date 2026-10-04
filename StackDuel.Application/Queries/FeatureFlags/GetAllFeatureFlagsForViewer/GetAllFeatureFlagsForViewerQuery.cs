namespace StackDuel.Application.Queries.FeatureFlags.GetAllFeatureFlagsForViewer;

public sealed record GetAllFeatureFlagsForViewerQuery(Guid? UserId) : IQuery<IReadOnlyDictionary<string, bool>>;