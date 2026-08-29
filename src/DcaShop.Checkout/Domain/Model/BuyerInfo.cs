using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Contact details of the buyer, captured in the first checkout step.</summary>
public sealed record BuyerInfo : IValue
{
    public BuyerInfo(string email, string firstName, string lastName, string phone)
    {
        Email = Required(email, nameof(email));
        if (!Email.Contains('@'))
        {
            throw new ArgumentException("Email must be a valid email address", nameof(email));
        }

        FirstName = Required(firstName, nameof(firstName));
        LastName = Required(lastName, nameof(lastName));
        Phone = Required(phone, nameof(phone));
    }

    public string Email { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public string Phone { get; }

    public string FullName => FirstName + " " + LastName;

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} cannot be blank", name) : value.Trim();
}
