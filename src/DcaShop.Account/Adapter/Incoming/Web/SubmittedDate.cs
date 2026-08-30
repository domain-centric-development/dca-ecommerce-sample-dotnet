using System.Globalization;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// Reads the date a form submitted. A value the browser sends is text until proven otherwise, so parsing it is
/// an adapter concern and its failure is a form error, not a domain rule.
/// </summary>
internal static class SubmittedDate
{
    public const string NotADate = "Please enter a date like 1990-05-17";

    public static DateOnly? Parse(string? submitted) =>
        !string.IsNullOrWhiteSpace(submitted)
        && DateOnly.TryParse(
            submitted.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
}
