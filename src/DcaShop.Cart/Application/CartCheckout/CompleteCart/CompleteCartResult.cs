using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Cart.Application.CartCheckout.CompleteCart;

/// <summary>What completing a cart came to: completed, or no cart under that id.</summary>
[GenerateOneOf]
public sealed partial class CompleteCartResult : OneOfBase<CompleteCartResult.Completed, CompleteCartResult.CartNotFound>
{
    /// <summary>The cart after the completion; <paramref name="AlreadyCompleted"/> when the event had produced it before.</summary>
    public sealed record Completed(Guid CartId, string Status, bool AlreadyCompleted = false);

    /// <summary>There is no cart under that id.</summary>
    public sealed record CartNotFound(CartId CartId) : IRefusal;
}