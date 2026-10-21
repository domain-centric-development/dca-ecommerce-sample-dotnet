using System.ComponentModel.DataAnnotations;

using DcaShop.Account.Domain.Specification;

namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>
/// Refuses a date of birth the model would refuse, so the request is answered as an unacceptable value before it
/// reaches the use case. The rule is <see cref="UsableDateOfBirth"/>'s; this attribute only asks it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class UsableDateOfBirthAttribute : ValidationAttribute
{
    public UsableDateOfBirthAttribute()
        : base("The date of birth must lie in the past.")
    {
    }

    /// <summary>A missing date is <see cref="RequiredAttribute"/>'s to refuse, not this one's.</summary>
    public override bool IsValid(object? value) =>
        value is not DateOnly date || UsableDateOfBirth.Rule.IsSatisfiedBy(date);
}