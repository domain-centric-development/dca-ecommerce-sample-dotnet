using System.Globalization;
using System.Text.RegularExpressions;

using DbUp;
using DbUp.Engine.Output;

using Microsoft.Extensions.Logging;

namespace DcaShop.Infrastructure.Persistence;

/// <summary>
/// Brings a PostgreSQL database to the schema the shop expects: the scripts under <c>Persistence/Migrations</c>,
/// named <c>V&lt;version&gt;__&lt;what&gt;.sql</c> like the Java twin's Flyway scripts, in version order, each once.
/// </summary>
public static partial class DatabaseMigrations
{
    private const string ScriptFolder = ".Persistence.Migrations.";

    /// <summary>Creates the database when it is missing and applies every script it has not seen yet.</summary>
    /// <exception cref="InvalidOperationException">A script failed; the message names it.</exception>
    public static void Apply(string connectionString, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var log = logger is null ? (IUpgradeLog)new NoOpUpgradeLog() : new LoggerUpgradeLog(logger);
        EnsureDatabase.For.PostgresqlDatabase(connectionString, log);
        var result = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrations).Assembly, name => name.Contains(ScriptFolder, StringComparison.Ordinal))
            .WithScriptNameComparer(new ByVersion())
            .WithTransactionPerScript()
            .LogTo(log)
            .Build()
            .PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException($"Migration {result.ErrorScript?.Name} failed", result.Error);
        }
    }

    /// <summary>Orders <c>V2__…</c> before <c>V10__…</c>, which an ordinal comparison of the names would not.</summary>
    private sealed class ByVersion : IComparer<string>
    {
        public int Compare(string? x, string? y) => (Version(x), x).CompareTo((Version(y), y));

        private static int Version(string? name)
        {
            var match = name is null ? Match.Empty : VersionPattern().Match(name);
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : int.MaxValue;
        }
    }

    [GeneratedRegex(@"\.V(\d+)__")]
    private static partial Regex VersionPattern();

    private sealed class LoggerUpgradeLog(ILogger logger) : IUpgradeLog
    {
        public void LogTrace(string format, params object[] args) => logger.LogTrace(format, args);
        public void LogDebug(string format, params object[] args) => logger.LogDebug(format, args);
        public void LogInformation(string format, params object[] args) => logger.LogInformation(format, args);
        public void LogWarning(string format, params object[] args) => logger.LogWarning(format, args);
        public void LogError(string format, params object[] args) => logger.LogError(format, args);
        public void LogError(Exception ex, string format, params object[] args) => logger.LogError(ex, format, args);
    }
}