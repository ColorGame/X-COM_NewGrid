
using System;
using UnityEngine;

[Serializable] // ����� ��������� ��������� ����� ������������ � ����������
public struct GridParameters   //������ ����� ��� ��������� // �������� ��������� ����� � ��������� ������. ������ � �������� ��������� ������������ ��� ���� ������ �������� ����������� ����� ������ � C#
{                                       //� ������ ��������� ��������� ��������� ����� � ����������
    public GridName gridName;
    public int width;       //������
    public int height;      //������
    public float cellSize;  // ������ ������
    public Transform anchorGridTransform; //����� ���������� �����

    public GridParameters(GridName gridName, int width, int height, float cellSize, Transform anchorGridTransform)
    {
        this.gridName = gridName;
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.anchorGridTransform = anchorGridTransform;
    }
}
