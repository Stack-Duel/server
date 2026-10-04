using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application;

public sealed class UserContext
{
    public UserDto? User { get; set; }

    public IReadOnlyList<string> Permissions { get; set; } = [];

    public IReadOnlyList<string> Roles { get; set; } = [];

    public IReadOnlyDictionary<string, bool> Flags { get; set; } = new Dictionary<string, bool>();

    public bool IsAuthenticated => User is not null;

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public bool HasFeature(string key) => Flags.TryGetValue(key, out bool enabled) && enabled;
}