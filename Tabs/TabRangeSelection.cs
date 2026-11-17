namespace Volt;

sealed class TabRangeSelection<T> where T : class
{
    internal readonly HashSet<T> Items = new();
    T? anchor;
    public void Click(IList<T> order,T item,bool extend)
    {
        int end=order.IndexOf(item);if(end<0)return;
        int start=anchor is null?-1:order.IndexOf(anchor);
        Items.Clear();
        if(!extend || start<0){anchor=item;Items.Add(item);return;}
        for(int i=Math.Min(start,end);i<=Math.Max(start,end);i++)Items.Add(order[i]);
    }
    public void Prune(IList<T> order){Items.RemoveWhere(x=>!order.Contains(x));if(anchor is not null&&!order.Contains(anchor))anchor=null;}
}
