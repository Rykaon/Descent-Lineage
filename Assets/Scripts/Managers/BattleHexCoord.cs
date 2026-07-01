using System;
using Unity.Netcode;

public struct BattleHexCoord : IEquatable<BattleHexCoord>, INetworkSerializable
{
    public int Q { get; private set; }
    public int R { get; private set; }

    public static readonly BattleHexCoord[] Directions =
    {
        new(+1, 0),
        new(+1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, +1),
        new(0, +1),
    };

    public BattleHexCoord(int q, int r)
    {
        Q = q;
        R = r;
    }

    public static int Distance(BattleHexCoord a, BattleHexCoord b)
    {
        int dq = a.Q - b.Q;
        int dr = a.R - b.R;
        int ds = (-a.Q - a.R) - (-b.Q - b.R);

        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
    }

    public static BattleHexCoord operator +(BattleHexCoord a, BattleHexCoord b)
    {
        return new BattleHexCoord(a.Q + b.Q, a.R + b.R);
    }

    public static BattleHexCoord operator *(BattleHexCoord a, int multiplier)
    {
        return new BattleHexCoord(a.Q * multiplier, a.R * multiplier);
    }

    public bool Equals(BattleHexCoord other)
    {
        return Q == other.Q && R == other.R;
    }

    public override bool Equals(object obj)
    {
        return obj is BattleHexCoord other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Q, R);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        int q = Q;
        int r = R;

        serializer.SerializeValue(ref q);
        serializer.SerializeValue(ref r);

        if (serializer.IsReader)
        {
            Q = q;
            R = r;
        }
    }
}