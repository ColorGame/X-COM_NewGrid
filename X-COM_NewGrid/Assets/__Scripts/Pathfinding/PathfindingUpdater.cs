using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingUpdater : MonoBehaviour //���������� ������ ����
{
    private void Start()
    {
        DestructibleCrate.OnAnyDestroyed += DestructibleCrate_OnAnyDestroyed; //���������� �� ������� ����(Any) ������ ��������.

    }

    private void DestructibleCrate_OnAnyDestroyed(object sender, System.EventArgs e)
    {
        DestructibleCrate destructibleCrate = sender as DestructibleCrate; // �������� ���������� ���� (��� ��� ����������� �������))

        PathfindingMonkey.Instance.SetIsWalkableGridPosition(destructibleCrate.GetGridPosition(), true); // �������� ������� ��� ��������� ���� ������� ��������� ��� ������
    }
}
