using StackDuel.Domain.Languages.Enums;
using StackDuel.Domain.Languages.Exceptions;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Languages.Entities;

public sealed class Language : AggregateRoot
{
    public Language(LanguageName name, LanguageSlug slug, Guid trackId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        TrackId =
            trackId != Guid.Empty
                ? trackId
                : throw new ArgumentException("Track id must not be empty.", nameof(trackId));
        Status = LanguageStatus.Active;
    }

    public void Activate()
    {
        Status = LanguageStatus.Active;
    }

    public LanguageVersionEntry AddVersion(LanguageVersion version, Judge0Id judge0Id)
    {
        var entry = new LanguageVersionEntry(version, judge0Id);
        _versions.Add(entry);
        return entry;
    }

    public void Deactivate()
    {
        Status = LanguageStatus.Inactive;
    }

    public void DeprecateVersion(Guid versionId)
    {
        var version =
            _versions.FirstOrDefault(v => v.Id == versionId) ?? throw new LanguageVersionNotFoundException(versionId);

        version.Deprecate();
    }

    private Language() { }

    public bool IsActive => Status == LanguageStatus.Active;
    public LanguageName Name { get; private set; } = null!;
    public LanguageSlug Slug { get; private set; } = null!;
    public Guid TrackId { get; private set; }
    public LanguageStatus Status { get; private set; }
    public IReadOnlyCollection<LanguageVersionEntry> Versions => _versions.AsReadOnly();

    private readonly List<LanguageVersionEntry> _versions = [];
}