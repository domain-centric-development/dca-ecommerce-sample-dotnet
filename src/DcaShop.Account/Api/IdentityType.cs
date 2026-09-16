namespace DcaShop.Account.Api;

/// <summary>Whether the caller has authenticated in the current session.</summary>
public enum IdentityType
{
    /// <summary>A visitor with an identity but no session. The normal state of a shopper who has not logged in.</summary>
    Anonymous,

    /// <summary>A visitor whose session names an existing account.</summary>
    Registered,
}
