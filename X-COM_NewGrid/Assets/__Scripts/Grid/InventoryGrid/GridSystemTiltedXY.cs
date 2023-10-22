using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemTiltedXY<TGridObject> : GridSystemXY<TGridObject>    // ����������� �������� ������� :��������� ����������� 
                                                                            // ��������� ������ � ������� �����
{
    public GridSystemTiltedXY(GridParameters gridParameters, Func<GridSystemXY<TGridObject>, Vector2Int, TGridObject> createGridObject) : base(gridParameters, createGridObject)
    {
    }

    public override Vector2Int GetGridPosition(Vector3 worldPosition) // �������� �������� ������� ������������ ����� ����� (��� ����� ��������� ������� ��������� 0,0)
    {

        Vector3 localPosition = _anchorGridTransform.InverseTransformPoint(worldPosition) - _offset�enterCell; // ���������� ������� � 0 (������� ��������) // InverseTransformPoint -����������� position ���� �� �������� ������������ � ��������� (������������ _anchorGridTransform).  
        return new Vector2Int
            (
            Mathf.RoundToInt(localPosition.x / _cellSize),  // ��������� Mathf.RoundToInt ��� �������������� float � int
            Mathf.RoundToInt(localPosition.y / _cellSize)
            );
    }

    public override Vector3 GetWorldPositionCenter�ornerCell(Vector2Int gridPosition) // ������� ������� ���������� ������ ������ (������������  _anchorGridTransform)
    {
        Vector3 localPositionXY = new Vector3(gridPosition.x, gridPosition.y, 0) * _cellSize + _offset�enterCell;   // ������� ��������� ���������� ����� ����� � ������ �� ��������, ������������ _anchorGridTransform
                                                                                                                    // �� ����� ��� �� ����� ������ ��� ������ �������������� ������ _anchorGridTransform  � ����� ���� ����� �������� � ***Grid.transform.position (��� �������� �������� �����, ���������� ����������� ������ ***Grid � ������ �����) ������� + _offset�enterCell,
        return _anchorGridTransform.TransformPoint(localPositionXY); // transform.TransformPoint -����������� position ����� ������ �� ���������� ������������(_anchorGridTransform) � ������� ������������(�������� ������ ������� _anchorGridTransform). �.�. _anchorGridTransform ����� ���������� �� � (0,0) � �������� � ������� ������������
    }
    public override Vector3 GetWorldPositionLowerLeft�ornerCell(Vector2Int gridPosition) // ������� ������� ���������� ������� ������ ����� ������ (������������  ����� _anchorGridTransform)
    {
        Vector3 localPositionXY = new Vector3(gridPosition.x, gridPosition.y, 0) * _cellSize;   // ������� ��������� ���������� ����� ����� � ������ �� ��������, ������������ _anchorGridTransform                                                                                                  
        return _anchorGridTransform.TransformPoint(localPositionXY); // transform.TransformPoint -����������� position ����� ������ �� ���������� ������������(_anchorGridTransform) � ������� ������������(�������� ������ ������� _anchorGridTransform). �.�. _anchorGridTransform ����� ���������� �� � (0,0) � �������� � ������� ������������
    }
}
