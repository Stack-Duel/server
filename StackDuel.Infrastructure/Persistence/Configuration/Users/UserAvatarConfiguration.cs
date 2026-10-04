using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Users;

internal sealed class UserAvatarConfiguration : IEntityTypeConfiguration<UserAvatar>
{
    public void Configure(EntityTypeBuilder<UserAvatar> builder)
    {
        builder.ToTable("user_avatars");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();

        builder
            .Property(a => a.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(ImageUrl.MaxLength)
            .IsRequired()
            .HasConversion(i => i.Value, v => new ImageUrl(v));

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(a => new { a.UserId, a.CreatedAt }).HasDatabaseName("IX_user_avatars_user_id_created_at");

        builder
            .HasOne<StackDuel.Domain.Users.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}