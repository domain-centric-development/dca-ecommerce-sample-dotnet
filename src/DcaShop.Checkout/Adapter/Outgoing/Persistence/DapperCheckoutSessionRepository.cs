using Dapper;

using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Persistence;

namespace DcaShop.Checkout.Adapter.Outgoing.Persistence;

/// <summary>
/// Dapper (PostgreSQL) implementation of <see cref="ICheckoutSessionRepository"/>: one row per session with the
/// data of each completed step in columns of its own, and the line items in order; read back through
/// <see cref="CheckoutSession.Reconstitute"/>. "Latest" is the session saved last.
/// </summary>
/// <remarks>
/// A session read by its cart inside a transaction is read <c>FOR UPDATE</c>: confirming and replacing a cart's
/// session check its state and then change it, and the row lock holds the other back until this transaction ends —
/// across processes, where <see cref="ICheckoutSessionRepository.InCartSessionAsync{T}"/> reaches only this one.
/// </remarks>
public sealed class DapperCheckoutSessionRepository : ICheckoutSessionRepository
{
    private const string SelectSession =
        """
        SELECT id AS Id, cart_id AS CartId, customer_id AS CustomerId, current_step AS CurrentStep, status AS Status,
               subtotal_amount AS SubtotalAmount, shipping_amount AS ShippingAmount, tax_amount AS TaxAmount,
               total_amount AS TotalAmount, currency AS Currency,
               buyer_email AS BuyerEmail, buyer_first_name AS BuyerFirstName, buyer_last_name AS BuyerLastName,
               buyer_phone AS BuyerPhone,
               delivery_street AS DeliveryStreet, delivery_street_line2 AS DeliveryStreetLine2, delivery_city AS DeliveryCity,
               delivery_postal_code AS DeliveryPostalCode, delivery_country AS DeliveryCountry, delivery_state AS DeliveryState,
               shipping_option_id AS ShippingOptionId, shipping_option_name AS ShippingOptionName,
               shipping_option_estimated_delivery AS ShippingOptionEstimatedDelivery,
               shipping_option_cost_amount AS ShippingOptionCostAmount, shipping_option_cost_currency AS ShippingOptionCostCurrency,
               payment_provider_id AS PaymentProviderId, payment_provider_reference AS PaymentProviderReference,
               payment_authorized_amount AS PaymentAuthorizedAmount, payment_authorized_currency AS PaymentAuthorizedCurrency,
               order_reference AS OrderReference
        FROM checkout_sessions
        """;

    private readonly Database _database;

    public DapperCheckoutSessionRepository(Database database) => _database = database;

    private string Lock => _database.InTransaction ? " FOR UPDATE" : "";

    public Task<CheckoutSession?> FindByIdAsync(CheckoutSessionId id, CancellationToken cancellationToken = default) =>
        FindOneAsync("WHERE id = @Id", new { Id = id.Value.ToString() }, cancellationToken);

    public Task<CheckoutSession?> FindByIdForCustomerAsync(CheckoutSessionId id, CustomerId customerId, CancellationToken cancellationToken = default) =>
        FindOneAsync("WHERE id = @Id AND customer_id = @CustomerId",
            new { Id = id.Value.ToString(), CustomerId = customerId.Value }, cancellationToken);

    public Task<CheckoutSession?> FindActiveByCartIdAsync(CartId cartId, CancellationToken cancellationToken = default) =>
        FindOneAsync($"WHERE cart_id = @CartId AND status = 'ACTIVE' ORDER BY updated_at DESC LIMIT 1{Lock}",
            new { CartId = cartId.Value.ToString() }, cancellationToken);

    public Task<CheckoutSession?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        FindOneAsync("WHERE customer_id = @CustomerId AND status = 'ACTIVE' ORDER BY updated_at DESC LIMIT 1",
            new { CustomerId = customerId.Value }, cancellationToken);

    public Task<CheckoutSession?> FindConfirmedOrCompletedByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        FindOneAsync("WHERE customer_id = @CustomerId AND status IN ('CONFIRMED', 'COMPLETED') ORDER BY updated_at DESC LIMIT 1",
            new { CustomerId = customerId.Value }, cancellationToken);

    public async Task<CheckoutSession> SaveAsync(CheckoutSession aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        var id = aggregate.Id.Value.ToString();
        var totals = aggregate.Totals;
        var buyer = aggregate.BuyerInfo;
        var address = aggregate.DeliveryAddress;
        var shipping = aggregate.ShippingOption;
        var payment = aggregate.PaymentSelection;
        await _database.RunAsync(async (connection, transaction) =>
        {
            // clock_timestamp(), not the transaction's start: two saves in one transaction keep their order
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO checkout_sessions (
                  id, cart_id, customer_id, current_step, status,
                  subtotal_amount, shipping_amount, tax_amount, total_amount, currency,
                  buyer_email, buyer_first_name, buyer_last_name, buyer_phone,
                  delivery_street, delivery_street_line2, delivery_city, delivery_postal_code, delivery_country, delivery_state,
                  shipping_option_id, shipping_option_name, shipping_option_estimated_delivery,
                  shipping_option_cost_amount, shipping_option_cost_currency,
                  payment_provider_id, payment_provider_reference, payment_authorized_amount, payment_authorized_currency,
                  order_reference, updated_at)
                VALUES (
                  @Id, @CartId, @CustomerId, @CurrentStep, @Status,
                  @SubtotalAmount, @ShippingAmount, @TaxAmount, @TotalAmount, @Currency,
                  @BuyerEmail, @BuyerFirstName, @BuyerLastName, @BuyerPhone,
                  @DeliveryStreet, @DeliveryStreetLine2, @DeliveryCity, @DeliveryPostalCode, @DeliveryCountry, @DeliveryState,
                  @ShippingOptionId, @ShippingOptionName, @ShippingOptionEstimatedDelivery,
                  @ShippingOptionCostAmount, @ShippingOptionCostCurrency,
                  @PaymentProviderId, @PaymentProviderReference, @PaymentAuthorizedAmount, @PaymentAuthorizedCurrency,
                  @OrderReference, clock_timestamp())
                ON CONFLICT (id) DO UPDATE SET
                  cart_id = EXCLUDED.cart_id, customer_id = EXCLUDED.customer_id,
                  current_step = EXCLUDED.current_step, status = EXCLUDED.status,
                  subtotal_amount = EXCLUDED.subtotal_amount, shipping_amount = EXCLUDED.shipping_amount,
                  tax_amount = EXCLUDED.tax_amount, total_amount = EXCLUDED.total_amount, currency = EXCLUDED.currency,
                  buyer_email = EXCLUDED.buyer_email, buyer_first_name = EXCLUDED.buyer_first_name,
                  buyer_last_name = EXCLUDED.buyer_last_name, buyer_phone = EXCLUDED.buyer_phone,
                  delivery_street = EXCLUDED.delivery_street, delivery_street_line2 = EXCLUDED.delivery_street_line2,
                  delivery_city = EXCLUDED.delivery_city, delivery_postal_code = EXCLUDED.delivery_postal_code,
                  delivery_country = EXCLUDED.delivery_country, delivery_state = EXCLUDED.delivery_state,
                  shipping_option_id = EXCLUDED.shipping_option_id, shipping_option_name = EXCLUDED.shipping_option_name,
                  shipping_option_estimated_delivery = EXCLUDED.shipping_option_estimated_delivery,
                  shipping_option_cost_amount = EXCLUDED.shipping_option_cost_amount,
                  shipping_option_cost_currency = EXCLUDED.shipping_option_cost_currency,
                  payment_provider_id = EXCLUDED.payment_provider_id,
                  payment_provider_reference = EXCLUDED.payment_provider_reference,
                  payment_authorized_amount = EXCLUDED.payment_authorized_amount,
                  payment_authorized_currency = EXCLUDED.payment_authorized_currency,
                  order_reference = EXCLUDED.order_reference, updated_at = EXCLUDED.updated_at
                """,
                new
                {
                    Id = id,
                    CartId = aggregate.CartId.Value.ToString(),
                    CustomerId = aggregate.CustomerId.Value,
                    CurrentStep = ColumnOf(aggregate.CurrentStep),
                    Status = ColumnOf(aggregate.Status),
                    SubtotalAmount = totals.Subtotal.Amount,
                    ShippingAmount = totals.Shipping.Amount,
                    TaxAmount = totals.Tax.Amount,
                    TotalAmount = totals.Total.Amount,
                    Currency = totals.Total.Currency.Code,
                    BuyerEmail = buyer?.Email,
                    BuyerFirstName = buyer?.FirstName,
                    BuyerLastName = buyer?.LastName,
                    BuyerPhone = buyer?.Phone,
                    DeliveryStreet = address?.Street,
                    DeliveryStreetLine2 = address?.StreetLine2,
                    DeliveryCity = address?.City,
                    DeliveryPostalCode = address?.PostalCode,
                    DeliveryCountry = address?.Country,
                    DeliveryState = address?.State,
                    ShippingOptionId = shipping?.Id,
                    ShippingOptionName = shipping?.Name,
                    ShippingOptionEstimatedDelivery = shipping?.EstimatedDelivery,
                    ShippingOptionCostAmount = shipping?.Cost.Amount,
                    ShippingOptionCostCurrency = shipping?.Cost.Currency.Code,
                    PaymentProviderId = payment?.ProviderId.Value,
                    PaymentProviderReference = payment?.ProviderReference,
                    PaymentAuthorizedAmount = payment?.AuthorizedAmount?.Amount,
                    PaymentAuthorizedCurrency = payment?.AuthorizedAmount?.Currency.Code,
                    aggregate.OrderReference,
                },
                transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);

            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM checkout_line_items WHERE session_id = @Id", new { Id = id }, transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO checkout_line_items (session_id, position, id, product_id, product_name, unit_price_amount,
                                                 unit_price_currency, quantity, image_url, position_snapshot)
                VALUES (@SessionId, @Position, @Id, @ProductId, @ProductName, @Amount, @Currency, @Quantity, @ImageUrl, @Snapshot)
                """,
                aggregate.LineItems.Select((line, position) => new
                {
                    SessionId = id,
                    Position = position,
                    Id = line.Id.Value.ToString(),
                    ProductId = line.ProductId.Value.ToString(),
                    line.ProductName,
                    line.UnitPrice.Amount,
                    Currency = line.UnitPrice.Currency.Code,
                    line.Quantity,
                    line.ImageUrl,
                    Snapshot = line.PositionSnapshot,
                }),
                transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return aggregate;
    }

    public Task DeleteByIdAsync(CheckoutSessionId id, CancellationToken cancellationToken = default) =>
        _database.RunAsync((connection, transaction) => connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM checkout_sessions WHERE id = @Id", new { Id = id.Value.ToString() }, transaction,
            cancellationToken: cancellationToken)), cancellationToken);

    private Task<CheckoutSession?> FindOneAsync(string condition, object parameters, CancellationToken cancellationToken) =>
        _database.RunAsync(async (connection, transaction) =>
        {
            var row = await connection.QuerySingleOrDefaultAsync<SessionRow>(new CommandDefinition(
                $"{SelectSession} {condition}", parameters, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
            if (row is null) return null;

            var lines = await connection.QueryAsync<LineRow>(new CommandDefinition(
                """
                SELECT id AS Id, product_id AS ProductId, product_name AS ProductName, unit_price_amount AS Amount,
                       unit_price_currency AS Currency, quantity AS Quantity, image_url AS ImageUrl, position_snapshot AS Snapshot
                FROM checkout_line_items WHERE session_id = @Id ORDER BY position
                """,
                new { row.Id }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
            return ToSession(row, lines);
        }, cancellationToken);

    private static CheckoutSession ToSession(SessionRow row, IEnumerable<LineRow> lines)
    {
        var currency = Currency.Of(row.Currency);
        return CheckoutSession.Reconstitute(
            CheckoutSessionId.Of(row.Id),
            CartId.Of(row.CartId),
            CustomerId.Of(row.CustomerId),
            lines.Select(line => new CheckoutLineItem(
                new CheckoutLineItemId(Guid.Parse(line.Id)), ProductId.Of(line.ProductId), line.ProductName,
                Money.Of(line.Amount, Currency.Of(line.Currency)), line.Quantity, line.ImageUrl, line.Snapshot)).ToList(),
            new CheckoutTotals(Money.Of(row.SubtotalAmount, currency), Money.Of(row.ShippingAmount, currency),
                Money.Of(row.TaxAmount, currency), Money.Of(row.TotalAmount, currency)),
            StepOf(row.CurrentStep),
            StatusOf(row.Status),
            row.BuyerEmail is null ? null : new BuyerInfo(row.BuyerEmail, row.BuyerFirstName!, row.BuyerLastName!, row.BuyerPhone!),
            row.DeliveryStreet is null
                ? null
                : new DeliveryAddress(row.DeliveryStreet, row.DeliveryStreetLine2, row.DeliveryCity!, row.DeliveryPostalCode!,
                    row.DeliveryCountry!, row.DeliveryState),
            row.ShippingOptionId is null
                ? null
                : new ShippingOption(row.ShippingOptionId, row.ShippingOptionName!, row.ShippingOptionEstimatedDelivery!,
                    Money.Of(row.ShippingOptionCostAmount!.Value, Currency.Of(row.ShippingOptionCostCurrency!))),
            row.PaymentProviderId is null
                ? null
                : new PaymentSelection(PaymentProviderId.Of(row.PaymentProviderId), row.PaymentProviderReference,
                    row.PaymentAuthorizedAmount is { } authorized ? Money.Of(authorized, Currency.Of(row.PaymentAuthorizedCurrency!)) : null),
            row.OrderReference);
    }

    /// <summary>The stored spelling of a status, written out per value as every stored session carries it.</summary>
    private static string ColumnOf(CheckoutSessionStatus status) => status switch
    {
        CheckoutSessionStatus.Active => "ACTIVE",
        CheckoutSessionStatus.Confirmed => "CONFIRMED",
        CheckoutSessionStatus.Completed => "COMPLETED",
        CheckoutSessionStatus.Abandoned => "ABANDONED",
        CheckoutSessionStatus.Expired => "EXPIRED",
        CheckoutSessionStatus.Superseded => "SUPERSEDED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static CheckoutSessionStatus StatusOf(string column) => column switch
    {
        "ACTIVE" => CheckoutSessionStatus.Active,
        "CONFIRMED" => CheckoutSessionStatus.Confirmed,
        "COMPLETED" => CheckoutSessionStatus.Completed,
        "ABANDONED" => CheckoutSessionStatus.Abandoned,
        "EXPIRED" => CheckoutSessionStatus.Expired,
        "SUPERSEDED" => CheckoutSessionStatus.Superseded,
        _ => throw new InvalidOperationException($"Unknown stored checkout status: {column}"),
    };

    /// <summary>The stored spelling of a step, written out per value as every stored session carries it.</summary>
    private static string ColumnOf(CheckoutStep step) => step switch
    {
        CheckoutStep.BuyerInfo => "BUYER_INFO",
        CheckoutStep.Delivery => "DELIVERY",
        CheckoutStep.Payment => "PAYMENT",
        CheckoutStep.Review => "REVIEW",
        CheckoutStep.Confirmation => "CONFIRMATION",
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, null),
    };

    private static CheckoutStep StepOf(string column) => column switch
    {
        "BUYER_INFO" => CheckoutStep.BuyerInfo,
        "DELIVERY" => CheckoutStep.Delivery,
        "PAYMENT" => CheckoutStep.Payment,
        "REVIEW" => CheckoutStep.Review,
        "CONFIRMATION" => CheckoutStep.Confirmation,
        _ => throw new InvalidOperationException($"Unknown stored checkout step: {column}"),
    };

    private sealed class SessionRow
    {
        public string Id { get; init; } = "";
        public string CartId { get; init; } = "";
        public string CustomerId { get; init; } = "";
        public string CurrentStep { get; init; } = "";
        public string Status { get; init; } = "";
        public decimal SubtotalAmount { get; init; }
        public decimal ShippingAmount { get; init; }
        public decimal TaxAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public string Currency { get; init; } = "";
        public string? BuyerEmail { get; init; }
        public string? BuyerFirstName { get; init; }
        public string? BuyerLastName { get; init; }
        public string? BuyerPhone { get; init; }
        public string? DeliveryStreet { get; init; }
        public string? DeliveryStreetLine2 { get; init; }
        public string? DeliveryCity { get; init; }
        public string? DeliveryPostalCode { get; init; }
        public string? DeliveryCountry { get; init; }
        public string? DeliveryState { get; init; }
        public string? ShippingOptionId { get; init; }
        public string? ShippingOptionName { get; init; }
        public string? ShippingOptionEstimatedDelivery { get; init; }
        public decimal? ShippingOptionCostAmount { get; init; }
        public string? ShippingOptionCostCurrency { get; init; }
        public string? PaymentProviderId { get; init; }
        public string? PaymentProviderReference { get; init; }
        public decimal? PaymentAuthorizedAmount { get; init; }
        public string? PaymentAuthorizedCurrency { get; init; }
        public string? OrderReference { get; init; }
    }

    private sealed class LineRow
    {
        public string Id { get; init; } = "";
        public string ProductId { get; init; } = "";
        public string ProductName { get; init; } = "";
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "";
        public int Quantity { get; init; }
        public string? ImageUrl { get; init; }
        public string Snapshot { get; init; } = "";
    }
}