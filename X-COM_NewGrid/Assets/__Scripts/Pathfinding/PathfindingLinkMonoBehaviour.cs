using Pathfinding;
using UnityEngine;


public class PathfindingLinkMonoBehaviour : MonoBehaviour // ������ ��� ������ ���� // ������������ � ����� � ������� PathfindingLink
{

    public Vector3 linkPositionA;
    public Vector3 linkPositionB;


    public PathfindingLink GetPathfindingLink() //�������� ������ ��� ������ ����
    {
        return new PathfindingLink
        {// ������� �������� ������� �� ������� �����������
            gridPositionA = LevelGrid.Instance.GetGridPosition(linkPositionA),
            gridPositionB = LevelGrid.Instance.GetGridPosition(linkPositionB)
        };
    }

    private void Start()
    {
        // ������� ���� � ����� �������� � �������� ��
        GraphNode graphNodeA = AstarPath.active.GetNearest(linkPositionA).node;
        GraphNode graphNodeB = AstarPath.active.GetNearest(linkPositionB).node;
        uint cost = (uint)Mathf.RoundToInt(((Int3)(linkPositionA - linkPositionB)).costMagnitude);

        graphNodeA.AddConnection(graphNodeB, cost);
        graphNodeB.AddConnection(graphNodeA, cost);
    }

}
