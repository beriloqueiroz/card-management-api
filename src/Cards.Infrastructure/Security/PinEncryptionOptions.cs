namespace Cards.Infrastructure.Security;

public sealed class PinEncryptionOptions
{
    public const string SectionName = "PinEncryption";

    /// <summary>Base64-encoded 256-bit key. Comes from configuration/environment, never from source.</summary>
    public string Key { get; set; } = string.Empty;
}
