namespace DcaShop.Backoffice.Adapter.Incoming.Web;

/// <summary>
/// What the backoffice login form shows. It never carries the submitted credentials back — not even the
/// username, which would tell a shoulder-surfer half the pair.
/// </summary>
public sealed record BackofficeLoginViewModel(bool ShowError, bool ShowLogout);
