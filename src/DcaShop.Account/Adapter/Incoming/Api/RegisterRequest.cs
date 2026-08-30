using System.ComponentModel.DataAnnotations;

namespace DcaShop.Account.Adapter.Incoming.Api;

public sealed record RegisterRequest(
    [Required] [EmailAddress] string Email,
    [Required] [MinLength(8)] string Password,
    [Required] [MaxLength(100)] string FirstName,
    [Required] [MaxLength(100)] string LastName,
    [Required] DateOnly? DateOfBirth);
