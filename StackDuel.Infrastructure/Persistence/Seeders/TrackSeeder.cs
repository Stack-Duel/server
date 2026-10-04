using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class TrackSeeder(StackDuelDbContext context) : IStaticSeeder
{
    public const string GeneralPurposeKey = "general-purpose";
    public const string SqlKey = "sql";
    public const string FrontendKey = "frontend";

    private static readonly DesiredTrack[] DesiredTracks =
    [
        new(GeneralPurposeKey, "General Purpose", IsActive: true, AllowsLanguageSelection: false),
        new(SqlKey, "SQL", IsActive: true, AllowsLanguageSelection: false),
        new(FrontendKey, "Frontend", IsActive: false, AllowsLanguageSelection: true),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        List<Track> existingTracks = await context.Tracks.ToListAsync(cancellationToken);

        foreach (DesiredTrack desired in DesiredTracks)
        {
            Track? track = existingTracks.FirstOrDefault(t => t.Key == desired.Key);

            if (track is null)
            {
                track = new Track(desired.Key, desired.Name);
                context.Tracks.Add(track);

                if (!desired.IsActive)
                    track.Deactivate();

                if (!desired.AllowsLanguageSelection)
                    track.DisableLanguageSelection();

                continue;
            }

            if (desired.IsActive && !track.IsActive)
                track.Activate();
            else if (!desired.IsActive && track.IsActive)
                track.Deactivate();

            if (desired.AllowsLanguageSelection && !track.AllowsLanguageSelection)
                track.EnableLanguageSelection();
            else if (!desired.AllowsLanguageSelection && track.AllowsLanguageSelection)
                track.DisableLanguageSelection();
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record DesiredTrack(string Key, string Name, bool IsActive, bool AllowsLanguageSelection);
}