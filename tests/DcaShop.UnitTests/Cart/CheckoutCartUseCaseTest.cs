using DcaShop.Cart.Application.CheckoutCart;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.UnitTests.Cart;

/// <summary>Why a checkout is refused — and that the refusal says which of the two reasons applies.</summary>
public sealed class CheckoutCartUseCaseTest
{
    private static readonly CustomerId Customer = CustomerId.Of("customer-1");
    private static readonly Price Ten = Price.Of(Money.Euro(10m));

    private readonly StubShoppingCartRepository _repository = new();
    private readonly StubArticleDataPort _articles = new();
    private readonly CheckoutCartUseCase _useCase;

    public CheckoutCartUseCaseTest() =>
        _useCase = new CheckoutCartUseCase(
            _repository, _articles, new EnrichedCartFactory(), new ClearingDomainEventPublisher(), new ImmediateTransactionBoundary());

    [Fact]
    public async Task AnEmptyCartIsRefusedByTheAggregateInItsOwnWords()
    {
        var cart = await _repository.SaveAsync(new ShoppingCart(CartId.Generate(), Customer));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(cart));

        Assert.Equal("Cannot checkout an empty cart", failure.Message);
    }

    [Fact]
    public async Task ACartThatIsAlreadyCheckedOutSaysSo()
    {
        var cart = new ShoppingCart(CartId.Generate(), Customer);
        var productId = ProductId.Generate();
        cart.AddItem(productId, Quantity.Of(1), Ten);
        cart.Checkout();
        await _repository.SaveAsync(cart);
        _articles.Available(productId, 5);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(cart));

        Assert.Equal("Cart is already checked out", failure.Message);
    }

    [Fact]
    public async Task AnArticleThatIsGoneIsReportedAsAValidationError()
    {
        var cart = new ShoppingCart(CartId.Generate(), Customer);
        var productId = ProductId.Generate();
        cart.AddItem(productId, Quantity.Of(2), Ten);
        await _repository.SaveAsync(cart);
        _articles.Unavailable(productId);

        var failure = await Assert.ThrowsAsync<CartValidationException>(() => Execute(cart));

        var error = Assert.Single(failure.ValidationResult.Errors);
        Assert.Equal(ValidationErrorType.ProductUnavailable, error.Type);
        Assert.Equal(productId, error.ProductId);
    }

    [Fact]
    public async Task StockThatDoesNotCoverTheQuantityIsReportedAsAValidationError()
    {
        var cart = new ShoppingCart(CartId.Generate(), Customer);
        var productId = ProductId.Generate();
        cart.AddItem(productId, Quantity.Of(5), Ten);
        await _repository.SaveAsync(cart);
        _articles.Available(productId, 3);

        var failure = await Assert.ThrowsAsync<CartValidationException>(() => Execute(cart));

        Assert.Equal(ValidationErrorType.InsufficientStock, Assert.Single(failure.ValidationResult.Errors).Type);
    }

    [Fact]
    public async Task ACartWithAvailableArticlesIsCheckedOut()
    {
        var cart = new ShoppingCart(CartId.Generate(), Customer);
        var productId = ProductId.Generate();
        cart.AddItem(productId, Quantity.Of(2), Ten);
        await _repository.SaveAsync(cart);
        _articles.Available(productId, 10);

        var result = await Execute(cart);

        Assert.Equal(cart.Id.Value, result.CartId);
        Assert.Equal(CartStatus.CheckedOut, cart.Status);
    }

    private Task<CheckoutCartResult> Execute(ShoppingCart cart) =>
        _useCase.ExecuteAsync(new CheckoutCartCommand(cart.Id.Value, Customer.Value));

    /// <summary>Runs the work right here — a test needs the boundary, not a transaction.</summary>
    private sealed class ImmediateTransactionBoundary : ITransactionBoundary
    {
        public Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default) =>
            work(cancellationToken);
    }

    /// <summary>Publishing is not what these tests are about; clearing keeps the aggregate honest.</summary>
    private sealed class ClearingDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default)
        {
            aggregate.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    private sealed class StubShoppingCartRepository : IShoppingCartRepository
    {
        private readonly Dictionary<CartId, ShoppingCart> _carts = new();

        public Task<ShoppingCart?> FindByIdAsync(CartId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_carts.TryGetValue(id, out var cart) ? cart : null);

        public Task<ShoppingCart> SaveAsync(ShoppingCart aggregate, CancellationToken cancellationToken = default)
        {
            _carts[aggregate.Id] = aggregate;
            return Task.FromResult(aggregate);
        }

        public Task DeleteByIdAsync(CartId id, CancellationToken cancellationToken = default)
        {
            _carts.Remove(id);
            return Task.CompletedTask;
        }

        public Task<ShoppingCart?> FindByIdForCustomerAsync(CartId id, CustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_carts.TryGetValue(id, out var cart) && cart.CustomerId == customerId ? cart : null);

        public Task<ShoppingCart?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_carts.Values.FirstOrDefault(cart => cart.CustomerId == customerId && cart.IsActive));

        public Task<IReadOnlyList<ShoppingCart>> FindByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingCart>>(_carts.Values.Where(cart => cart.CustomerId == customerId).ToList());

        public Task<IReadOnlyList<ShoppingCart>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingCart>>(_carts.Values.ToList());
    }

    private sealed class StubArticleDataPort : IArticleDataPort
    {
        private readonly Dictionary<ProductId, CartArticle> _articles = new();

        internal void Available(ProductId productId, int stock) => _articles[productId] = Article(productId, stock, true);

        internal void Unavailable(ProductId productId) => _articles[productId] = Article(productId, 0, false);

        public Task<IReadOnlyDictionary<ProductId, CartArticle>> GetArticleDataAsync(
            IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, CartArticle>>(
                productIds.Where(_articles.ContainsKey).ToDictionary(id => id, id => _articles[id]));

        public Task<CartArticle?> GetArticleDataAsync(ProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_articles.GetValueOrDefault(productId));

        private static CartArticle Article(ProductId productId, int stock, bool isAvailable) =>
            new(productId, "Article", Money.Euro(10m), stock, isAvailable, string.Empty);
    }
}
