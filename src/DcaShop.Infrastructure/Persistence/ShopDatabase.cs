using Microsoft.Extensions.Configuration;

namespace DcaShop.Infrastructure.Persistence;

/// <summary>Where the shop's database is: the connection string <c>ConnectionStrings:Shop</c>.</summary>
public static class ShopDatabase
{
    public const string ConnectionStringName = "Shop";

    /// <exception cref="InvalidOperationException">No connection string is configured.</exception>
    public static string ConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionStringName) is { Length: > 0 } configured
            ? configured
            : throw new InvalidOperationException($"No connection string 'ConnectionStrings:{ConnectionStringName}' is configured");
}