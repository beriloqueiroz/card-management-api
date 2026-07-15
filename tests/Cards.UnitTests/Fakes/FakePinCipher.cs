using System.Text;
using Cards.Application.Abstractions;
using Cards.Domain.Cards;

namespace Cards.UnitTests.Fakes;

/// <summary>Reversible without crypto so tests can assert the round trip.</summary>
public sealed class FakePinCipher : IPinCipher
{
    public byte[] Encrypt(Pin pin) => Encoding.UTF8.GetBytes($"enc:{pin.Value}");

    public Pin Decrypt(byte[] payload) => Pin.Create(Encoding.UTF8.GetString(payload)["enc:".Length..]);
}
