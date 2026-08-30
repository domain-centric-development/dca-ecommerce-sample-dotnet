namespace DcaShop.Account.Domain.Model;

/// <summary>
/// The business roles an account can hold. Roles are plain strings today, as they are in the Java sample; the
/// glossary records promoting them to a value object as an open item.
/// </summary>
public static class Role
{
    /// <summary>The role every account gets at registration.</summary>
    public const string Customer = "CUSTOMER";
}
