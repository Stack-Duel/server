namespace StackDuel.Api;

/// <summary>
/// Entry-point marker for <c>WebApplicationFactory</c>. The API and the Data CLI are both
/// executables with top-level statements, so each has its own implicit global <c>Program</c>;
/// naming one of those from a project that references both is ambiguous. This type is in the
/// API assembly, which is all the factory needs to locate the host.
/// </summary>
public sealed class ApiAssemblyMarker;