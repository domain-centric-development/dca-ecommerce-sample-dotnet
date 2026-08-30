using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Gateway;

/// <summary>
/// Domain gateway for hashing plaintext passwords and comparing a plaintext against a stored hash in
/// timing-safe fashion. The contract belongs to the domain and is called by the <see cref="Model.Account"/>
/// aggregate and by <see cref="Model.HashedPassword"/>; the algorithm (BCrypt, Argon2, …) lives in an adapter.
/// </summary>
public interface IPasswordHasher : IDomainGateway
{
    string Hash(string plaintext);

    bool Matches(string plaintext, string hash);
}
