using InventoryManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Web.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);

        builder.HasIndex(i => new { i.InventoryId, i.CustomId })
            .IsUnique();

        builder.Property(i => i.Version).IsRowVersion();

        builder.HasOne(i => i.Inventory)
            .WithMany(inv => inv.Items)
            .HasForeignKey(i => i.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.CreatedBy)
            .WithMany()
            .HasForeignKey(i => i.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.SearchVector)
          .HasColumnType("tsvector")
          .IsRequired(false);

        builder.HasIndex(i => i.SearchVector)
            .HasMethod("GIST");
    }
}