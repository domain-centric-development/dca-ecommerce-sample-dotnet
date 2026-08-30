namespace DcaShop.Account.Application.ChangeProfile;

/// <summary>The editable half of a profile: the login address and the owner's date of birth.</summary>
public sealed record ChangeProfileCommand(string UserId, string Email, DateOnly? DateOfBirth);
