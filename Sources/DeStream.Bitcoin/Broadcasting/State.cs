namespace DeStream.Bitcoin.Broadcasting
{
    public enum State
    {
        CantBroadcast,
        ToBroadcast,
        Broadcasted,
        Propagated
    }
}
