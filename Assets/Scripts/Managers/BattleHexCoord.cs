using System;
using UnityEngine;

[Serializable]
public readonly struct BattleHexCoord : IEquatable<BattleHexCoord>
{
    public readonly int Q;
    public readonly int R;

    public int S => -Q - R;

    public BattleHexCoord(int q, int r)
    {
        Q = q;
        R = r;
    }

    public static BattleHexCoord operator +(BattleHexCoord a, BattleHexCoord b) => new(a.Q + b.Q, a.R + b.R);
    public static BattleHexCoord operator -(BattleHexCoord a, BattleHexCoord b) => new(a.Q - b.Q, a.R - b.R);
    public static BattleHexCoord operator *(BattleHexCoord a, int scalar) => new(a.Q * scalar, a.R * scalar);

    public static int Distance(BattleHexCoord a, BattleHexCoord b)
    {
        return Mathf.Max(Mathf.Abs(a.Q - b.Q), Mathf.Abs(a.R - b.R), Mathf.Abs(a.S - b.S));
    }

    public bool Equals(BattleHexCoord other) => Q == other.Q && R == other.R;

    public override bool Equals(object obj) => obj is BattleHexCoord other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Q, R);

    public override string ToString() => $"({Q}, {R})";

    public static readonly BattleHexCoord[] Directions =
    {
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1),
    };
}