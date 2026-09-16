namespace DcaShop.Account.Domain.Model;

/// <summary>
/// The business roles an account can hold. Roles are plain strings today, as they are in the Java sample; the
/// glossary records promoting them to a value object as an open item.
///
/// <para>
/// The same two names appear on <c>IIdentityProvider.IIdentity</c> in the shared kernel, where the adapters of
/// other contexts read them: the domain must not reach into an application port to borrow a constant, so the
/// literal is spelled out on both sides. Change one, change the other.
/// </para>
/// </summary>
public static class Role
{
    /// <summary>The role every account gets at registration.</summary>
    public const string Customer = "CUSTOMER";

    /// <summary>
    /// Operator role. It guards what a shopper must never reach — listing every customer's cart, creating a
    /// product — and no registration path hands it out: an account only gets it by being given it.
    /// </summary>
    public const string Staff = "STAFF";
}
