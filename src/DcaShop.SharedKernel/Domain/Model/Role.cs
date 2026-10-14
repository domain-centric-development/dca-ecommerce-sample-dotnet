using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>
/// What an account may do beyond shopping. A value from a fixed set: the only instances are
/// <see cref="Customer"/> and <see cref="Staff"/>, so no other role can be held, granted or asked for.
/// </summary>
/// <remarks>
/// It lives in the shared kernel because the Account context grants roles and the adapters of other contexts —
/// the product and cart APIs — guard on them. <see cref="Code"/> is the published name a token carries; the two
/// constants exist only because an attribute such as <c>[Authorize(Roles = …)]</c> needs a compile-time string.
/// </remarks>
public sealed record Role : IValue
{
    /// <summary>The published code of <see cref="Customer"/>.</summary>
    public const string CustomerCode = "CUSTOMER";

    /// <summary>The published code of <see cref="Staff"/>.</summary>
    public const string StaffCode = "STAFF";

    /// <summary>The role every account gets at registration.</summary>
    public static readonly Role Customer = new(CustomerCode);

    /// <summary>
    /// The operator role. It guards what a shopper must never reach — listing every customer's cart, creating a
    /// product — and no registration path hands it out: an account only gets it by being given it.
    /// </summary>
    public static readonly Role Staff = new(StaffCode);

    private Role(string code) => Code = code;

    /// <summary>The published name of the role, as a token carries it.</summary>
    public string Code { get; }

    /// <summary>The role a published code names, or <see langword="null"/> for a code that names none.</summary>
    public static Role? FromCode(string code) => code switch
    {
        CustomerCode => Customer,
        StaffCode => Staff,
        _ => null,
    };

    public override string ToString() => Code;
}