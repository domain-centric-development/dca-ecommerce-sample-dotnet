using System.ComponentModel.DataAnnotations;

namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>
/// The body of an API registration. A field its attributes refuse is answered with the attribute's message as the
/// reason (<see cref="UnacceptableValueProblemAttribute"/>).
/// </summary>
public sealed record RegisterRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    [Required][MaxLength(100)] string FirstName,
    [Required][MaxLength(100)] string LastName,
    [Required][UsableDateOfBirth] DateOnly? DateOfBirth);