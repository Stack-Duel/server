using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameMode : AggregateRoot
{
    public GameMode(
        string key,
        string name,
        string description,
        bool isBuiltIn,
        int minPlayers,
        int maxPlayers,
        Guid defaultPoolId
    )
    {
        Key = !string.IsNullOrWhiteSpace(key)
            ? key.Trim().ToLowerInvariant()
            : throw new ArgumentException("Mode key must not be empty.", nameof(key));

        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Mode name must not be empty.", nameof(name));

        Description = !string.IsNullOrWhiteSpace(description)
            ? description.Trim()
            : throw new ArgumentException("Mode description must not be empty.", nameof(description));

        if (minPlayers <= 0)
            throw new ArgumentException("Min players must be greater than zero.", nameof(minPlayers));

        if (maxPlayers < minPlayers)
            throw new ArgumentException(
                "Max players must be greater than or equal to min players.",
                nameof(maxPlayers)
            );

        IsBuiltIn = isBuiltIn;
        IsActive = true;
        MinPlayers = minPlayers;
        MaxPlayers = maxPlayers;
        CreatedAt = DateTime.UtcNow;
        DefaultPoolId = defaultPoolId;
    }

    public GameModeTimeOption AddTimeOption(int durationSeconds, bool isDefault = false)
    {
        if (_timeOptions.Any(option => option.DurationSeconds == durationSeconds))
            throw new InvalidOperationException($"Time option {durationSeconds} seconds already exists for this mode.");

        if (isDefault)
        {
            foreach (GameModeTimeOption option in _timeOptions)
                option.MarkAsDefault(false);
        }

        GameModeTimeOption timeOption = new(durationSeconds, isDefault);
        _timeOptions.Add(timeOption);
        return timeOption;
    }

    public void Deactivate() => IsActive = false;

    private GameMode() { }

    public string Key { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public bool IsBuiltIn { get; private set; }

    public bool IsActive { get; private set; }

    public int MinPlayers { get; private set; }

    public int MaxPlayers { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Guid DefaultPoolId { get; private set; }

    public IReadOnlyCollection<GameModeTimeOption> TimeOptions => _timeOptions.AsReadOnly();

    private readonly List<GameModeTimeOption> _timeOptions = [];
}