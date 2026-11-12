namespace Volt;

static class TabCloseSelection
{
    public static T? Next<T>(IList<T> order,IEnumerable<T> closing,T? active) where T:class
    {
        var removed=closing.ToHashSet();
        if(active is not null && order.Contains(active) && !removed.Contains(active))return active;
        int index=active is null?-1:order.IndexOf(active);
        return order.Skip(index+1).FirstOrDefault(p=>!removed.Contains(p))
            ?? order.Take(Math.Max(0,index)).Reverse().FirstOrDefault(p=>!removed.Contains(p));
    }
}
