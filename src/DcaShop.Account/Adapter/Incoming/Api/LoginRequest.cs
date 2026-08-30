using System.ComponentModel.DataAnnotations;

namespace DcaShop.Account.Adapter.Incoming.Api;

public sealed record LoginRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Password);
