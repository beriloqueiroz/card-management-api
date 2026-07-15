using System.Text;
using Cards.Application.Abstractions;
using Cards.Domain.Cards;

namespace Cards.IntegrationTests.Support;

/// <summary>Repository tests don't exercise crypto; the API tests use the real AES-GCM cipher.</summary>
public sealed class FakePinCipher : IPinCipher
{
    public byte[] Encrypt(Pin pin) => Encoding.UTF8.GetBytes($"enc:{pin.Value}");

    public Pin Decrypt(byte[] payload) => Pin.Create(Encoding.UTF8.GetString(payload)["enc:".Length..]);
}
