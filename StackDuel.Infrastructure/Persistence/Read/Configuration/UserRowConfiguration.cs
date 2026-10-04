using StackDuel.Infrastructure.Persistence.Read.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Read.Configuration;

internal sealed class UserRowConfiguration : IEntityTypeConfiguration<UserRow>
{
    public void Configure(EntityTypeBuilder<UserRow> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Sub).HasColumnName("sub");
        builder.Property(x => x.Username).HasColumnName("username");
        builder.Property(x => x.Bio).HasColumnName("bio");
        builder.Property(x => x.ImageUrl).HasColumnName("image_url");
        builder.Property(x => x.UsernameLastChangedAt).HasColumnName("username_last_changed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.SetupCompletedAt).HasColumnName("setup_completed_at");
        builder.Property(x => x.IsPrivate).HasColumnName("is_private");
    }
}