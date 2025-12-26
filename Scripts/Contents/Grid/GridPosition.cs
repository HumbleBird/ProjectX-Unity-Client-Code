


using System;
using static Define;

[System.Serializable]
public struct GridPosition : IEquatable<GridPosition>
{
    public int x;
    public int z;
    public int floor;

    public GridPosition(int x, int z, int floor)
    {
        this.x = x;
        this.z = z;
        this.floor = floor;
    }

    // ✅ 혹은 프로퍼티로도 가능 (이 버전이 더 Unity 스타일)
    public static GridPosition zero => new GridPosition(0, 0, 0);

    public override bool Equals(object obj)
    {
        return obj is GridPosition position &&
               x == position.x &&
               z == position.z &&
               floor == position.floor;
    }

    public bool Equals(GridPosition other)
    {
        return this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, z, floor);
    }

    public override string ToString()
    {
        return $"x: {x}; z: {z}; floor: {floor}";
    }

    public static bool operator ==(GridPosition a, GridPosition b)
    {
        return a.x == b.x && a.z == b.z && a.floor == b.floor;
    }

    public static bool operator !=(GridPosition a, GridPosition b)
    {
        return !(a == b);
    } 

    public static GridPosition operator +(GridPosition a, GridPosition b)
    {
        return new GridPosition(a.x + b.x, a.z + b.z, a.floor + b.floor);
    }

    public static GridPosition operator -(GridPosition a, GridPosition b)
    {
        return new GridPosition(a.x - b.x, a.z - b.z, a.floor - b.floor);
    }

    public static GridPosition operator *(GridPosition a, int b)
    {
        return new GridPosition(a.x * b, a.z * b, a.floor * b);
    }

    public GridPosition ReverseSign()
    {
        return new GridPosition(x, z, floor) * -1;
    }


}
