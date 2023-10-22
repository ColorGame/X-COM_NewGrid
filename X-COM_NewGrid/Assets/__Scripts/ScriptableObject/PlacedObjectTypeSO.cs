using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/PlacedObjectType")]
public class PlacedObjectTypeSO : ScriptableObject  // ����������� ������ ���� SO List
{ 

    public static Dir GetNextDir(Dir dir) // ������� ��������� �����������
    {
        switch (dir)
        {
            default:
            case Dir.Down: return Dir.Left;
            case Dir.Left: return Dir.Up;
            case Dir.Up: return Dir.Right;
            case Dir.Right: return Dir.Down;
        }
    }

    public static Vector2Int GetDirForwardVector(Dir dir) // �������� ����������� Forward(y) ������� � ����������� �� ����������� ���������
    {
        switch (dir)
        {
            default:
            case Dir.Down: return new Vector2Int(0, -1);
            case Dir.Left: return new Vector2Int(-1, 0);
            case Dir.Up: return new Vector2Int(0, +1);
            case Dir.Right: return new Vector2Int(+1, 0);
        }
    }

    public static Dir GetDir(Vector2Int from, Vector2Int to) // �������� ����������� �� from � to
    {
        if (from.x < to.x)
        {
            return Dir.Right;
        }
        else
        {
            if (from.x > to.x)
            {
                return Dir.Left;
            }
            else
            {
                if (from.y < to.y)
                {
                    return Dir.Up;
                }
                else
                {
                    return Dir.Down;
                }
            }
        }
    }

    public enum Dir // ����������� �������
    {
        Down,
        Left,
        Up,
        Right,
    }

    public string nameString;
    public Transform prefab;
    public Transform visual;
    public int widthX; // ������� �������� ������ � ������ �
    public int heightY; // � ������ �


    public int GetRotationAngle(Dir dir) // �������� ���� �������� � ����������� �� �����������
    {
        switch (dir)
        {
            default:
            case Dir.Down: return 0;
            case Dir.Left: return 90;
            case Dir.Up: return 180;
            case Dir.Right: return 270;
        }
    }

    public Vector2Int GetRotationOffset(Dir dir) // �������� �������� ������� � ����������� �� ���� ��� �� ��� ��������� (������ ������ ���. ��� ���� � ���� ���� ������ ������ ��������� � ������))
    {
        switch (dir)
        {
            default:
            case Dir.Down: return new Vector2Int(0, 0);
            case Dir.Left: return new Vector2Int(0, widthX);
            case Dir.Up: return new Vector2Int(widthX, heightY);
            case Dir.Right: return new Vector2Int(heightY, 0);
        }
    }

    public Vector3 GetOffsetVisualFromParent()
    {
        float x = InventoryGrid.Instance.GetCellSize() * widthX / 2; // ������ ������ ������� �� ���������� �����, ������� �������� ��� ������ �� � � ����� �������
        float y = InventoryGrid.Instance.GetCellSize() * heightY / 2;

        return new Vector3(x, y, 0);
    }

    public List<Vector2Int> GetGridPositionList(Vector2Int gridPosition, Dir dir) // ������ �������� ������� ������� �������� ������ ������������ ���������� �������� ������� � ����������� �������
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();
        switch (dir)
        {
            default:
            case Dir.Down: // ��������� �� ���������
            case Dir.Up: 
                for (int x = 0; x < widthX; x++)
                {
                    for (int y = 0; y < heightY; y++)
                    {
                        gridPositionList.Add(gridPosition + new Vector2Int(x, y));
                    }
                }
                break;
            case Dir.Left:
            case Dir.Right:
                for (int x = 0; x < heightY; x++)
                {
                    for (int y = 0; y < widthX; y++)
                    {
                        gridPositionList.Add(gridPosition + new Vector2Int(x, y));
                    }
                }
                break;
        }
        return gridPositionList;
    }

}
