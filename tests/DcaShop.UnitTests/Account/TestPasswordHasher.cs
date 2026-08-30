using DcaShop.Account.Domain.Gateway;

namespace DcaShop.UnitTests.Account;

/// <summary>
/// A hasher that is reversible on purpose, so the tests can tell a hash from its plaintext without paying for
/// BCrypt in every case. The real one is an adapter and is not what these tests are about.
/// </summary>
internal sealed class TestPasswordHasher : IPasswordHasher
{
    public string Hash(string plaintext) => $"hashed:{plaintext}";

    public bool Matches(string plaintext, string hash) => hash == Hash(plaintext);
}
