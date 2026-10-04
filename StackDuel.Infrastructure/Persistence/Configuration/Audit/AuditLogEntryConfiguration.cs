using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Audit.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Audit;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.ActorUserId).HasColumnName("actor_user_id");

        builder.Property(a => a.ActorUsername).HasColumnName("actor_username").HasMaxLength(256).IsRequired();

        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(128).IsRequired();

        builder.Property(a => a.TargetType).HasColumnName("target_type").HasMaxLength(64);

        builder.Property(a => a.TargetId).HasColumnName("target_id").HasMaxLength(128);

        builder.Property(a => a.DetailsJson).HasColumnName("details_json").HasColumnType("jsonb");

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_log_entries_created_at");

        builder.HasIndex(a => a.ActorUserId).HasDatabaseName("ix_audit_log_entries_actor_user_id");
    }
}