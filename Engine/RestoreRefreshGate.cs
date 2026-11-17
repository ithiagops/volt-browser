namespace Volt;

// Native activation and restoration can arrive in the same message burst.
// Keep only one pending refresh, and invalidate callbacks from a minimized window.
sealed class RestoreRefreshGate
{
    long generation;
    bool pending;
    public bool TrySchedule(bool allowed,out long ticket)
    {
        ticket=0;
        if(!allowed || pending)return false;
        pending=true;ticket=++generation;return true;
    }
    public bool TryComplete(long ticket,bool allowed)
    {
        if(!pending || ticket!=generation)return false;
        pending=false;return allowed;
    }
    public void Cancel(){pending=false;generation++;}
}
