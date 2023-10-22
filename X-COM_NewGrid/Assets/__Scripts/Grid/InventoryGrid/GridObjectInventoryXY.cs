using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class GridObjectInventoryXY // ������ ����� (� ������� ����� ��������� ������ ���� �������� ������� ��������� � ������� �����) 
                          // ����������� ����� C#// ����� ������������ ����������� ��� �������� ����� ����� ������� �� �� ��������� MonoBehaviour/
                          // GridObjectInventoryXY ��������� � ������ ������ �����. �������� ��������� ��� �������� ��������
{
    private GridSystemXY<GridObjectInventoryXY> _gridSystem; // �������� ������� .� �������� ������� ��� GridObjectUnitXZ// ������� �������� ������� ������� ������� ���� ������ (��� ���������� �������� ��� ����� 2-�� �����)
    private Vector2Int _gridPosition; // ��������� ������� � �����
    private PlacedObject _placedObject; // ����������� ������.
    private IInteractable _interactable; // IInteractable(��������� ��������������) (����� ������ GridObjectUnitXZ ��� � ����) ��������� ��������� ����������������� � ����� �������� (�����, �����, ������...) - ������� ��������� ���� ���������

    public GridObjectInventoryXY(GridSystemXY<GridObjectInventoryXY> gridSystem, Vector2Int gridPosition) // ����������� 
    {
        _gridSystem = gridSystem;
        _gridPosition = gridPosition;
        _placedObject = null;
    }

    public override string ToString() // ������������� ToString(). ����� ��� ���������� ������� � ����� � ����� � ���� ������ (����� ����� ��������� �������� ������������ ������)
    {                
        return _gridPosition.ToString() + "\n" +"!-"+ _placedObject;        
    }

    public void AddPlacedObject(PlacedObject placedObject) // ������� ����������� ������ 
    {
        _placedObject = placedObject;
    }

    public PlacedObject GetPlacedObject() // �������� ����������� ������ 
    {
        return _placedObject;
    }

    public void RemovePlacedObject(PlacedObject placedObject) // ������ ����������� ������ 
    {
        _placedObject = null;
    } 

    public bool HasPlacedObject()
    {
        return _placedObject != null;
    }


}

