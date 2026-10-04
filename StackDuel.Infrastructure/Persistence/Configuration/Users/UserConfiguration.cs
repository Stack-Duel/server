using StackDuel.Domain.Authorization.Rbac.Entities;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Sub).HasColumnName("sub").HasMaxLength(255).IsRequired();

        builder.HasIndex(u => u.Sub).IsUnique();

        builder.Property(u => u.Tenant).HasColumnName("tenant").HasMaxLength(64);

        builder
            .Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(Username.MaxLength)
            .IsRequired()
            .HasConversion(u => u.Value, v => new Username(v));

        builder.HasIndex(u => u.Username).IsUnique();

        builder
            .Property(u => u.Bio)
            .HasColumnName("bio")
            .HasMaxLength(Bio.MaxLength)
            .HasConversion(b => b != null ? b.Value : null, v => v != null ? new Bio(v) : null);

        builder
            .Property(u => u.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(ImageUrl.MaxLength)
            .HasConversion(i => i != null ? i.Value : null, v => v != null ? new ImageUrl(v) : null);

        builder.Property(u => u.UsernameLastChangedAt).HasColumnName("username_last_changed_at");

        builder.Property(u => u.CreatedAt).HasColumnName("created_at");

        builder.Property(u => u.SetupCompletedAt).HasColumnName("setup_completed_at");

        builder.Property(u => u.IsPrivate).HasColumnName("is_private");

        builder
            .HasMany<Group>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "user_groups",
                right => right.HasOne<Group>().WithMany().HasForeignKey("group_id").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<User>().WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("user_groups");

                    join.Property<Guid>("user_id").HasColumnName("user_id");

                    join.Property<GroupId>("group_id")
                        .HasColumnName("group_id")
                        .HasConversion(g => g.Value, v => new GroupId(v));

                    join.HasKey("user_id", "group_id");
                    join.HasIndex("group_id").HasDatabaseName("IX_user_groups_group_id");
                }
            );

        builder
            .HasMany<Language>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "user_language_preferences",
                right =>
                    right.HasOne<Language>().WithMany().HasForeignKey("language_id").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<User>().WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("user_language_preferences");

                    join.Property<Guid>("user_id").HasColumnName("user_id");

                    join.Property<Guid>("language_id").HasColumnName("language_id");

                    join.Property<int>("position").HasColumnName("position");

                    join.HasKey("user_id", "language_id");
                    join.HasIndex("language_id").HasDatabaseName("IX_user_language_preferences_language_id");
                    join.HasIndex("user_id", "position")
                        .IsUnique()
                        .HasDatabaseName("IX_user_language_preferences_user_id_position");
                }
            );
    }
}