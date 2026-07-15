using System.Security.Cryptography;
using System.Text;
using Cards.Application.Abstractions;
using Cards.Domain.Cards;
using Microsoft.Extensions.Options;

namespace Cards.Infrastructure.Security;

/// <summary>
/// AES-256-GCM (authenticated encryption). Payload layout: nonce (12) + tag (16) + ciphertext.
/// A fresh random nonce per encryption means equal PINs never produce equal payloads.
/// </summary>
internal sealed class AesGcmPinCipher : IPinCipher
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmPinCipher(IOptions<PinEncryptionOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.Key);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("PinEncryption:Key must be a base64-encoded 32-byte key.");
        }
    }

    public byte[] Encrypt(Pin pin)
    {
        var plaintext = Encoding.UTF8.GetBytes(pin.Value);
        var payload = new byte[NonceSize + TagSize + plaintext.Length];
        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return payload;
    }

    public Pin Decrypt(byte[] payload)
    {
        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Pin.Create(Encoding.UTF8.GetString(plaintext));
    }
}
