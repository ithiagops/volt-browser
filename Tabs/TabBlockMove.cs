namespace Volt;

static class TabBlockMove
{
    // Slot is a boundary in the original order, including the end boundary.
    public static List<T> Move<T>(IList<T> order,IEnumerable<T> chosen,int slot) where T : notnull
    {
        var selected=chosen.ToHashSet();var block=order.Where(selected.Contains).ToArray();
        slot=Math.Clamp(slot,0,order.Count);
        int destination=order.Take(slot).Count(x=>!selected.Contains(x));
        var result=order.Where(x=>!selected.Contains(x)).ToList();
        result.InsertRange(destination,block);return result;
    }
}
