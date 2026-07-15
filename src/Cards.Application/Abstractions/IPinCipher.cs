using Cards.Domain.Cards;

namespace Cards.Application.Abstractions;

/// <summary>
/// Reversible encryption for the card PIN. The challenge requires returning
/// the original PIN from a dedicated endpoint, so hashing is not an option;
/// the PIN is stored encrypted and only ever decrypted on that flow.
/// </summary>
public interface IPinCipher
{
    byte[] Encrypt(Pin pin);

    Pin Decrypt(byte[] payload);
}
