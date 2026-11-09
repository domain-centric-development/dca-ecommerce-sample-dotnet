namespace DcaShop.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// A moment as a <c>TIMESTAMP WITH TIME ZONE</c> column holds it: written as UTC, read back as UTC. Npgsql takes a
/// UTC <see cref="DateTime"/> for such a column and hands one back, so the adapters convert at the edge.
/// </summary>
public static class StoredTime
{
    public static DateTime Write(DateTimeOffset moment) => moment.UtcDateTime;

    public static DateTime? Write(DateTimeOffset? moment) => moment?.UtcDateTime;

    public static DateTimeOffset Read(DateTime stored) => new(DateTime.SpecifyKind(stored, DateTimeKind.Utc));

    public static DateTimeOffset? Read(DateTime? stored) => stored is { } some ? Read(some) : null;
}