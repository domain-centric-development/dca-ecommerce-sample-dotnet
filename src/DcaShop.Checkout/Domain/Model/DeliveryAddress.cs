using System.Text;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Shipping address captured in the delivery step.</summary>
public sealed record DeliveryAddress : IValue
{
    public DeliveryAddress(string street, string? streetLine2, string city, string postalCode, string country, string? state)
    {
        Street = Required(street, nameof(street));
        StreetLine2 = Optional(streetLine2);
        City = Required(city, nameof(city));
        PostalCode = Required(postalCode, nameof(postalCode));
        Country = Required(country, nameof(country));
        State = Optional(state);
    }

    public DeliveryAddress(string street, string city, string postalCode, string country)
        : this(street, null, city, postalCode, country, null)
    {
    }

    public string Street { get; }

    public string? StreetLine2 { get; }

    public string City { get; }

    public string PostalCode { get; }

    public string Country { get; }

    public string? State { get; }

    public string FormattedAddress
    {
        get
        {
            var sb = new StringBuilder(Street);
            if (StreetLine2 is not null)
            {
                sb.Append(", ").Append(StreetLine2);
            }

            sb.Append(", ").Append(PostalCode).Append(' ').Append(City);
            if (State is not null)
            {
                sb.Append(", ").Append(State);
            }

            return sb.Append(", ").Append(Country).ToString();
        }
    }

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} cannot be blank", name) : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
