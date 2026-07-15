namespace Cards.Domain.Cards;

public sealed class CreditCard
{
    public const int CardholderNameMaxLength = 120;
    public const int NicknameMaxLength = 80;
    public const int BrandMaxLength = 40;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CardholderName { get; private set; } = null!;
    public string? Nickname { get; private set; }
    public string Brand { get; private set; } = null!;
    public string FirstFourDigits { get; private set; } = null!;
    public string LastFourDigits { get; private set; } = null!;
    public DateOnly ExpirationDate { get; private set; }
    public decimal CreditLimit { get; private set; }
    public CardStatus Status { get; private set; }
    public byte[] PinEncrypted { get; private set; } = null!;

    /// <summary>Reference to a tokenized card at an external acquirer/gateway (future one-click journeys).</summary>
    public string? ExternalId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public string MaskedNumber => $"{FirstFourDigits} **** **** {LastFourDigits}";
    public bool IsDeleted => DeletedAt is not null;

    private CreditCard()
    {
    }

    public static CreditCard Create(
        Guid userId,
        string? cardholderName,
        string? nickname,
        string? brand,
        CardNumber cardNumber,
        DateOnly expirationDate,
        decimal creditLimit,
        CardStatus status,
        byte[] pinEncrypted,
        DateTimeOffset now)
    {
        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = now,
        };
        card.SetCardholderName(cardholderName);
        card.SetNickname(nickname);
        card.SetBrand(brand);
        card.SetCardNumber(cardNumber);
        card.SetExpirationDate(expirationDate);
        card.SetCreditLimit(creditLimit);
        card.ChangeStatus(status);
        card.SetPin(pinEncrypted);
        return card;
    }

    /// <summary>Rehydrates a card with known values. Used by the database seeder and tests only.</summary>
    public static CreditCard Restore(
        Guid id,
        Guid userId,
        string cardholderName,
        string? nickname,
        string brand,
        string firstFourDigits,
        string lastFourDigits,
        DateOnly expirationDate,
        decimal creditLimit,
        CardStatus status,
        byte[] pinEncrypted,
        string? externalId,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        DateTimeOffset? deletedAt) => new()
    {
        Id = id,
        UserId = userId,
        CardholderName = cardholderName,
        Nickname = nickname,
        Brand = brand,
        FirstFourDigits = firstFourDigits,
        LastFourDigits = lastFourDigits,
        ExpirationDate = expirationDate,
        CreditLimit = creditLimit,
        Status = status,
        PinEncrypted = pinEncrypted,
        ExternalId = externalId,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt,
        DeletedAt = deletedAt,
    };

    /// <summary>Full replacement of the editable fields (PUT semantics).</summary>
    public void ReplaceEditableData(
        string? cardholderName,
        string? nickname,
        string? brand,
        CardNumber cardNumber,
        DateOnly expirationDate,
        decimal creditLimit,
        CardStatus status,
        byte[] pinEncrypted,
        DateTimeOffset now)
    {
        EnsureNotDeleted();
        SetCardholderName(cardholderName);
        SetNickname(nickname);
        SetBrand(brand);
        SetCardNumber(cardNumber);
        SetExpirationDate(expirationDate);
        SetCreditLimit(creditLimit);
        ChangeStatus(status);
        SetPin(pinEncrypted);
        MarkUpdated(now);
    }

    public void SetCardholderName(string? cardholderName)
    {
        var value = cardholderName?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > CardholderNameMaxLength)
        {
            throw new DomainValidationException(
                $"cardholderName is required and must have at most {CardholderNameMaxLength} characters.");
        }

        CardholderName = value;
    }

    public void SetNickname(string? nickname)
    {
        var value = nickname?.Trim();
        if (value?.Length > NicknameMaxLength)
        {
            throw new DomainValidationException($"nickname must have at most {NicknameMaxLength} characters.");
        }

        Nickname = string.IsNullOrEmpty(value) ? null : value;
    }

    public void SetBrand(string? brand)
    {
        var value = brand?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > BrandMaxLength)
        {
            throw new DomainValidationException(
                $"brand is required and must have at most {BrandMaxLength} characters.");
        }

        Brand = value.ToUpperInvariant();
    }

    public void SetCardNumber(CardNumber cardNumber)
    {
        FirstFourDigits = cardNumber.FirstFour;
        LastFourDigits = cardNumber.LastFour;
    }

    public void SetExpirationDate(DateOnly expirationDate) => ExpirationDate = expirationDate;

    public void SetCreditLimit(decimal creditLimit)
    {
        if (creditLimit < 0)
        {
            throw new DomainValidationException("creditLimit must be greater than or equal to zero.");
        }

        CreditLimit = creditLimit;
    }

    public void Activate() => Status = CardStatus.Active;

    public void Block() => Status = CardStatus.Blocked;

    public void Cancel() => Status = CardStatus.Cancelled;

    /// <summary>
    /// Dispatches API payload statuses to the expressive transitions above.
    /// Transitions are deliberately unrestricted: the challenge spec defines
    /// no state machine (e.g. PUT may reactivate a cancelled card).
    /// </summary>
    public void ChangeStatus(CardStatus status)
    {
        switch (status)
        {
            case CardStatus.Active:
                Activate();
                break;
            case CardStatus.Blocked:
                Block();
                break;
            case CardStatus.Cancelled:
                Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    /// <summary>
    /// Merge-style partial update (PATCH semantics): null keeps the current
    /// value. Stamps UpdatedAt and reports whether anything was applied.
    /// </summary>
    public bool ApplyPartialChanges(
        string? cardholderName,
        string? nickname,
        string? brand,
        CardNumber? cardNumber,
        DateOnly? expirationDate,
        decimal? creditLimit,
        CardStatus? status,
        byte[]? pinEncrypted,
        DateTimeOffset now)
    {
        EnsureNotDeleted();

        var changed = false;
        if (cardholderName is not null)
        {
            SetCardholderName(cardholderName);
            changed = true;
        }

        if (nickname is not null)
        {
            SetNickname(nickname);
            changed = true;
        }

        if (brand is not null)
        {
            SetBrand(brand);
            changed = true;
        }

        if (cardNumber is not null)
        {
            SetCardNumber(cardNumber);
            changed = true;
        }

        if (expirationDate is { } newExpirationDate)
        {
            SetExpirationDate(newExpirationDate);
            changed = true;
        }

        if (creditLimit is { } newCreditLimit)
        {
            SetCreditLimit(newCreditLimit);
            changed = true;
        }

        if (status is { } newStatus)
        {
            ChangeStatus(newStatus);
            changed = true;
        }

        if (pinEncrypted is not null)
        {
            SetPin(pinEncrypted);
            changed = true;
        }

        if (changed)
        {
            MarkUpdated(now);
        }

        return changed;
    }

    public void SetPin(byte[] pinEncrypted)
    {
        if (pinEncrypted.Length == 0)
        {
            throw new DomainValidationException("pin is required.");
        }

        PinEncrypted = pinEncrypted;
    }

    public void MarkUpdated(DateTimeOffset now)
    {
        EnsureNotDeleted();
        UpdatedAt = now;
    }

    /// <summary>Soft delete: the card disappears from regular queries but the row is kept for traceability.</summary>
    public void SoftDelete(DateTimeOffset now)
    {
        EnsureNotDeleted();
        DeletedAt = now;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainValidationException("card has been removed.");
        }
    }
}
