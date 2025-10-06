using System;

public static class GameSignals
{
    public static Action<bool> Reported;       // true=correct, false=wrong
    public static Action<int>  ActiveChanged;  // active anomalies
    public static Action       Win;
    public static Action<string> Lose;         // optional reason
}