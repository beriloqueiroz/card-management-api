using Cards.Domain.Cards;
using Cards.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cards.Infrastructure.Persistence.Configurations;

internal sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("credit_cards", table =>
        {
            // Same guards the provided seed script declares, kept at the database level.
            table.HasCheckConstraint("ck_credit_cards_first_four_digits", "first_four_digits ~ '^[0-9]{4}$'");
            table.HasCheckConstraint("ck_credit_cards_last_four_digits", "last_four_digits ~ '^[0-9]{4}$'");
            table.HasCheckConstraint("ck_credit_cards_status", "status IN ('ACTIVE', 'BLOCKED', 'CANCELLED')");
            table.HasCheckConstraint("ck_credit_cards_credit_limit", "credit_limit >= 0");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.CardholderName)
            .HasColumnName("cardholder_name")
            .HasMaxLength(CreditCard.CardholderNameMaxLength)
            .IsRequired();
        builder.Property(c => c.Nickname)
            .HasColumnName("nickname")
            .HasMaxLength(CreditCard.NicknameMaxLength);
        builder.Property(c => c.Brand)
            .HasColumnName("brand")
            .HasMaxLength(CreditCard.BrandMaxLength)
            .IsRequired();
        builder.Property(c => c.FirstFourDigits)
            .HasColumnName("first_four_digits")
            .HasMaxLength(4)
            .IsFixedLength()
            .IsRequired();
        builder.Property(c => c.LastFourDigits)
            .HasColumnName("last_four_digits")
            .HasMaxLength(4)
            .IsFixedLength()
            .IsRequired();
        builder.Property(c => c.ExpirationDate).HasColumnName("expiration_date").IsRequired();
        builder.Property(c => c.CreditLimit)
            .HasColumnName("credit_limit")
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(s => s.ToWireValue(), v => CardStatusExtensions.Parse(v))
            .IsRequired();
        builder.Property(c => c.PinEncrypted).HasColumnName("pin_encrypted").IsRequired();
        builder.Property(c => c.ExternalId).HasColumnName("external_id").HasMaxLength(120);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");

        builder.Ignore(c => c.MaskedNumber);
        builder.Ignore(c => c.IsDeleted);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_credit_cards_user_id");

        builder.HasIndex(c => new { c.UserId, c.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_credit_cards_user_created_at");
        builder.HasIndex(c => new { c.UserId, c.ExpirationDate })
            .HasDatabaseName("ix_credit_cards_user_expiration_date");

        // Soft delete: removed cards never show up in regular queries.
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
