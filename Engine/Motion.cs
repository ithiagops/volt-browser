namespace Volt;
static class Motion
{
    public static double Ease(double progress)
    {double t=Math.Clamp(progress,0,1);return 1-(1-t)*(1-t)*(1-t);}
    public static int Position(int start,int end,double progress)
        => (int)Math.Round(start+(end-start)*Ease(progress));
}
