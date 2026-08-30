namespace DcaShop.Account.Domain.Model;

/// <summary>Lifecycle status of an account.</summary>
public enum AccountStatus
{
    /// <summary>The account can log in.</summary>
    Active,

    /// <summary>The account is temporarily blocked.</summary>
    Suspended,

    /// <summary>The account is permanently ended. Terminal.</summary>
    Closed,
}

/// <summary>Rules the lifecycle status decides on its own.</summary>
public static class AccountStatusExtensions
{
    public static bool CanLogin(this AccountStatus status) => status == AccountStatus.Active;

    public static bool IsTerminal(this AccountStatus status) => status == AccountStatus.Closed;
}
