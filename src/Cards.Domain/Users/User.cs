namespace Cards.Domain.Users;

/// <summary>
/// Read-only in this API: users come from the provided seed and are matched
/// to the authenticated principal by e-mail. There is no user CRUD.
/// </summary>
public sealed class User
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    public User(Guid id, string fullName, string email, DateTimeOffset createdAt)
    {
        Id = id;
        FullName = fullName;
        Email = email;
        CreatedAt = createdAt;
    }
}
