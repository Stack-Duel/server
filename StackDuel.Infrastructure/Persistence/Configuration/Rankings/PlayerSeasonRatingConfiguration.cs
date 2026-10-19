using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Rankings.Entities;
using StackDuel.Domain.Users.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Rankings;

internal sealed class PlayerSeasonRatingConfiguration 
: IEntityTypeConfiguration<PlayerSeasonRating>
{
    public void Configure(EntityTypeBuilder<PlayerSeasonRating> builder)
    {
        builder.ToTable("player_season_ratings");

        builder.HasKey(rating => rating.Id);

        builder.Property(rating => rating.PlayerId)
            .HasColumnName("player_id")
            .IsRequired();

        builder.Property(rating => rating.SeasonId)
            .HasColumnName("season_id")
            .IsRequired();

        builder.Property(rating => rating.Rating)
            .HasColumnName("rating")
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(rating => rating.GamesPlayed)
            .HasColumnName("games_played")
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(rating => new
        {
            rating.PlayerId,
            rating.SeasonId
        })
        .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rating => rating.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}