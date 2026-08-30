using DomainCentric.BuildingBlocks.Ddd.Strategic;

namespace DcaShop.Account;

/// <summary>
/// Account bounded context: owns registered accounts, their credentials and their profile, and it is the only
/// context that establishes or ends an authenticated session. It links a cross-context <see cref="SharedKernel.Domain.Model.UserId"/>
/// to a context-local <see cref="Domain.Model.AccountId"/>, which is what lets a guest keep their cart when they
/// register. It depends on no other context: after a login it hands control back to the browser, and the Cart
/// context decides for itself whether anything has to be merged.
/// </summary>
[BoundedContext("Account", Description = "Registered accounts, credentials, profile and authenticated sessions")]
public static class AccountContext
{
}
