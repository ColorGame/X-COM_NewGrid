using System;

public struct GridPositionInventoryXY : IEquatable<GridPositionInventoryXY> //��������� Equatable - ��������������    //������ � �������� ��������� ������������ ��� ���� ������ �������� ����������� ����� ������ � C#. ����� ���� ������ ����������� ����, ��������, int, double � �.�.,
                                                          //�� ���� �������� �����������. ��� ������ � ���������� �� �������� ����� �������� � �� ������.
                                                          //�� ������� ���� ��������� �.�. �� ����� ��������������� ����������� Vector2Int. �� �������� � X Y � ��� ����� X Z (����� ���� �� ������� �������������� �� � XZ ���� � �������, �� ��� ��������� ����� ����� ���� � ���� ��������������)
{
    public int x;
    public int z;
    public GridName gridName;

    public GridPositionInventoryXY(int x, int z, GridName gridName) // ��������������� �����������
    {
        this.x = x;
        this.z = z;
        this.gridName = gridName;
    }


    public override string ToString() // ������������� ToString(). ����� ������� � ������� Debug.Log ��������� ��������� X Z � ����
    {
        return $"x: {x}; z: {z}; floor: {gridName}";
    }

    public static bool operator ==(GridPositionInventoryXY a, GridPositionInventoryXY b) // ���������� ��� ������� �������� ���������
    {
        return a.x == b.x && a.z == b.z && a.gridName == b.gridName;
    }

    public static bool operator !=(GridPositionInventoryXY a, GridPositionInventoryXY b) // ���������� ��� ������� �������� ���������
    {
        return !(a == b);
    }

    public static GridPositionInventoryXY operator +(GridPositionInventoryXY a, GridPositionInventoryXY b) // ���������� ��� �����
    {
        return new GridPositionInventoryXY(a.x + b.x, a.z + b.z, a.gridName);
    }

    public static GridPositionInventoryXY operator -(GridPositionInventoryXY a, GridPositionInventoryXY b) // ���������� ��� ��������
    {
        return new GridPositionInventoryXY(a.x - b.x, a.z - b.z, a.gridName);
    }

    public override bool Equals(object obj) // ������������� ��������������� ���������� ��������������� ���������
    {
        return obj is GridPositionInventoryXY position &&
               x == position.x &&
               z == position.z &&
               gridName == position.gridName;
    }

    public override int GetHashCode() // ������������� ��������������� ����������
    {
        return HashCode.Combine(x, z, gridName);
    }

    public bool Equals(GridPositionInventoryXY other) // ���������� ���������� ���������
    {
        return this == other;
    }
}
