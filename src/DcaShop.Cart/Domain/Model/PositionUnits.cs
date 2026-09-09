using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DcaShop.Cart.Domain.Model;

/// <summary>Compressed unit identities within one position; reductions remove the oldest units first.</summary>
public sealed record PositionUnits : IValue
{
    public sealed record Span(long First, long Last) : IValue;
    public long Allocated { get; }
    public IReadOnlyList<Span> Spans { get; }
    public PositionUnits(long allocated, IEnumerable<Span> spans)
    {
        var copy = spans.ToArray(); long previous = 0;
        foreach (var span in copy)
        {
            if (span.First < 1 || span.Last < span.First || span.First <= previous || span.Last > allocated) throw new ArgumentException("Invalid unit sequence");
            previous = span.Last;
        }
        Allocated = allocated; Spans = Array.AsReadOnly(copy);
    }
    public static PositionUnits Initial(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
        return new(quantity, new[] { new Span(1, quantity) });
    }
    public int Quantity => checked((int)Spans.Sum(s => s.Last - s.First + 1));
    public PositionUnits Resize(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");
        int delta = quantity - Quantity; if (delta == 0) return this;
        if (delta > 0) return new(checked(Allocated + delta), Spans.Append(new Span(checked(Allocated + 1), checked(Allocated + delta))));
        long remove = -(long)delta; var result = new List<Span>();
        foreach (var span in Spans)
        {
            long take = Math.Min(remove, span.Last - span.First + 1); remove -= take;
            if (span.First + take <= span.Last) result.Add(new(span.First + take, span.Last));
        }
        return new(Allocated, result);
    }
    public PositionUnits Reconcile(PositionUnits purchased)
    {
        var result = new List<Span>();
        foreach (var current in Spans)
        {
            long cursor = current.First;
            foreach (var bought in purchased.Spans)
            {
                if (bought.Last < cursor || bought.First > current.Last) continue;
                if (bought.First > cursor) result.Add(new(cursor, bought.First - 1));
                cursor = Math.Max(cursor, bought.Last + 1); if (cursor > current.Last) break;
            }
            if (cursor <= current.Last) result.Add(new(cursor, current.Last));
        }
        return new(Allocated, result);
    }
    public string Serialize() => Allocated + "|" + string.Join(",", Spans.Select(s => s.First + "-" + s.Last));
    public static PositionUnits Parse(string encoded)
    {
        var parts = encoded.Split('|'); if (parts.Length != 2) throw new ArgumentException("Invalid unit snapshot");
        var spans = parts[1].Length == 0 ? Array.Empty<Span>() : parts[1].Split(',').Select(token => { var pair = token.Split('-'); return new Span(long.Parse(pair[0]), long.Parse(pair[1])); });
        return new(long.Parse(parts[0]), spans);
    }
}
