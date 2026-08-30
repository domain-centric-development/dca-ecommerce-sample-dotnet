using DcaShop.Account.Domain.Gateway;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// BCrypt implementation of the password-hashing domain gateway. The algorithm is an adapter concern; the domain
/// only knows that a plaintext can be hashed and compared.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plaintext) => BCrypt.Net.BCrypt.HashPassword(plaintext);

    public bool Matches(string plaintext, string hash) => BCrypt.Net.BCrypt.Verify(plaintext, hash);
}
