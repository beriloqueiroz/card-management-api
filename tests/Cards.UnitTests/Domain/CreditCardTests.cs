using Cards.Domain;
using Cards.Domain.Cards;

namespace Cards.UnitTests.Domain;

public class CreditCardTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-15T12:00:00Z");
    private static readonly byte[] EncryptedPin = [1, 2, 3];

    private static CreditCard NewCard() => CreditCard.Create(
        Guid.NewGuid(),
        "MARIANA ALVES",
        "Principal",
        "visa",
        CardNumber.Create("5321123412345336"),
        new DateOnly(2029, 12, 31),
        6500.00m,
        CardStatus.Active,
        EncryptedPin,
        Now);

    [Fact]
    public void Create_SetsMaskedNumberAndNormalizesBrand()
    {
        var card = NewCard();

        Assert.Equal("5321 **** **** 5336", card.MaskedNumber);
        Assert.Equal("VISA", card.Brand);
        Assert.Equal(Now, card.CreatedAt);
        Assert.Null(card.UpdatedAt);
        Assert.False(card.IsDeleted);
    }

    [Fact]
    public void Create_RejectsMissingCardholderName()
    {
        Assert.Throws<DomainValidationException>(() => CreditCard.Create(
            Guid.NewGuid(), "  ", null, "VISA", CardNumber.Create("5321123412345336"),
            new DateOnly(2029, 12, 31), 100m, CardStatus.Active, EncryptedPin, Now));
    }

    [Fact]
    public void Create_RejectsNegativeCreditLimit()
    {
        Assert.Throws<DomainValidationException>(() => CreditCard.Create(
            Guid.NewGuid(), "MARIANA ALVES", null, "VISA", CardNumber.Create("5321123412345336"),
            new DateOnly(2029, 12, 31), -0.01m, CardStatus.Active, EncryptedPin, Now));
    }

    [Fact]
    public void SetNickname_TreatsEmptyAsNull()
    {
        var card = NewCard();

        card.SetNickname("  ");

        Assert.Null(card.Nickname);
    }

    [Fact]
    public void ReplaceEditableData_UpdatesEverythingAndStampsUpdatedAt()
    {
        var card = NewCard();
        var later = Now.AddMinutes(10);

        card.ReplaceEditableData(
            "MARIANA A ALVES",
            "Principal Atualizado",
            "MASTERCARD",
            CardNumber.Create("5412123412341002"),
            new DateOnly(2030, 1, 31),
            14000.00m,
            CardStatus.Blocked,
            [9, 9, 9],
            later);

        Assert.Equal("MARIANA A ALVES", card.CardholderName);
        Assert.Equal("5412 **** **** 1002", card.MaskedNumber);
        Assert.Equal(CardStatus.Blocked, card.Status);
        Assert.Equal(later, card.UpdatedAt);
    }

    [Fact]
    public void ExpressiveStatusTransitions_AreUnrestricted()
    {
        var card = NewCard();

        card.Block();
        Assert.Equal(CardStatus.Blocked, card.Status);

        card.Cancel();
        Assert.Equal(CardStatus.Cancelled, card.Status);

        // The spec defines no state machine: reactivating a cancelled card is allowed.
        card.Activate();
        Assert.Equal(CardStatus.Active, card.Status);
    }

    [Fact]
    public void ApplyPartialChanges_AppliesOnlyProvidedFieldsAndStampsUpdatedAt()
    {
        var card = NewCard();
        var later = Now.AddMinutes(5);

        var changed = card.ApplyPartialChanges(
            cardholderName: null,
            nickname: "Uso diário",
            brand: null,
            cardNumber: null,
            expirationDate: null,
            creditLimit: 15500.00m,
            status: null,
            pinEncrypted: null,
            later);

        Assert.True(changed);
        Assert.Equal("Uso diário", card.Nickname);
        Assert.Equal(15500.00m, card.CreditLimit);
        Assert.Equal("MARIANA ALVES", card.CardholderName); // untouched
        Assert.Equal(later, card.UpdatedAt);
    }

    [Fact]
    public void ApplyPartialChanges_WithNothingProvided_ReportsNoChangeAndKeepsUpdatedAt()
    {
        var card = NewCard();

        var changed = card.ApplyPartialChanges(
            null, null, null, null, null, null, null, null, Now.AddMinutes(5));

        Assert.False(changed);
        Assert.Null(card.UpdatedAt);
    }

    [Fact]
    public void SoftDelete_MarksDeletedAndBlocksFurtherChanges()
    {
        var card = NewCard();

        card.SoftDelete(Now);

        Assert.True(card.IsDeleted);
        Assert.Equal(Now, card.DeletedAt);
        Assert.Throws<DomainValidationException>(() => card.SoftDelete(Now));
        Assert.Throws<DomainValidationException>(() => card.MarkUpdated(Now));
        Assert.Throws<DomainValidationException>(() =>
            card.ApplyPartialChanges(null, "x", null, null, null, null, null, null, Now));
    }
}
