using System.Security.Cryptography;
using System.Text;

using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// The fingerprint of an order as the review shows it: one digest per line — its product, quantity and unit price — and
/// one over everything else the review shows: the buyer's details, the delivery address, the shipping method and its
/// price, the payment method and the total. "Place Order" carries the fingerprint the review showed; an order whose
/// fingerprint differs at that moment is not placed. Digests only, so the fingerprint says nothing the page does not.
/// </summary>
public sealed record OrderFingerprint : IValue
{
    /// <summary>Between two lines' digests.</summary>
    private const char LineSeparator = '-';

    /// <summary>Between the lines' digests and the digest of everything else.</summary>
    private const char RestSeparator = '.';

    /// <summary>Between the parts a digest is taken over, a character no part contains.</summary>
    private const char PartSeparator = '\u001f';

    /// <summary>How many hexadecimal digits of a digest the fingerprint keeps.</summary>
    private const int DigestLength = 16;

    private OrderFingerprint(string value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>The fingerprint of an order with these lines, these details and this total.</summary>
    public static OrderFingerprint Of(
        IReadOnlyList<CheckoutLineItem> lines,
        BuyerInfo? buyer,
        DeliveryAddress? address,
        ShippingOption? shipping,
        PaymentSelection? payment,
        Money total)
    {
        var lineDigests = lines.Select(line => Digest(line.ProductId, line.Quantity, line.UnitPrice));
        var rest = Digest(
            buyer?.Email, buyer?.FirstName, buyer?.LastName, buyer?.Phone,
            address?.Street, address?.StreetLine2, address?.City, address?.PostalCode, address?.Country, address?.State,
            shipping?.Id, shipping?.Name, shipping?.Cost,
            payment?.ProviderId,
            total);
        return new OrderFingerprint(string.Join(LineSeparator, lineDigests) + RestSeparator + rest);
    }

    /// <summary>The fingerprint a caller sent, as it was sent — a missing one is empty and matches no order.</summary>
    public static OrderFingerprint From(string? value) => new(value ?? string.Empty);

    /// <summary>
    /// The position of the first line whose digest differs from <paramref name="shown"/>'s; none when the lines agree,
    /// and none when <paramref name="shown"/> has another number of lines — then it is not this order's at all.
    /// </summary>
    public int? FirstChangedLine(OrderFingerprint shown)
    {
        var lines = LineDigests();
        var shownLines = shown.LineDigests();
        if (lines.Count != shownLines.Count)
        {
            return null;
        }

        for (var line = 0; line < lines.Count; line++)
        {
            if (lines[line] != shownLines[line])
            {
                return line;
            }
        }

        return null;
    }

    private IReadOnlyList<string> LineDigests()
    {
        var end = Value.LastIndexOf(RestSeparator);
        return end <= 0 ? [] : Value[..end].Split(LineSeparator);
    }

    private static string Digest(params object?[] parts)
    {
        var text = string.Join(PartSeparator, parts.Select(part => part?.ToString() ?? string.Empty));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..DigestLength];
    }
}