using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.ValueTypes.Entities;
using StackDuel.Domain.ValueTypes.ValueObjects;
using System.Text.Json;

namespace StackDuel.Infrastructure.Persistence.Configuration.ValueTypes;

internal sealed class ValueTypeDefinitionConfiguration : IEntityTypeConfiguration<ValueTypeDefinition>
{
    public void Configure(EntityTypeBuilder<ValueTypeDefinition> builder)
    {
        builder.ToTable("value_type_definitions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id).HasColumnName("id");

        builder
            .Property(v => v.Key)
            .HasColumnName("key")
            .HasMaxLength(ValueTypeKey.MaxLength)
            .IsRequired()
            .HasConversion(key => key.Value, value => new ValueTypeKey(value));

        builder.HasIndex(v => v.Key).IsUnique();

        builder.Property(v => v.Name).HasColumnName("name").HasMaxLength(ValueTypeDefinition.MaxNameLength).IsRequired();

        builder
            .Property(v => v.Description)
            .HasColumnName("description")
            .HasMaxLength(ValueTypeDefinition.MaxDescriptionLength)
            .IsRequired(false);

        builder
            .Property(v => v.Spec)
            .HasColumnName("spec")
            .HasColumnType("jsonb")
            .HasConversion(
                spec => JsonSerializer.Serialize(spec, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<ValueTypeSpec>(json, (JsonSerializerOptions?)null)!
            )
            .IsRequired();

        builder.Property(v => v.IsBuiltIn).HasColumnName("is_built_in").IsRequired();

        builder.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at").IsRequired(false);
    }
}