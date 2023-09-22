using Pathfinding;
using UnityEngine;


public class PathfindingLinkMonoBehaviour : MonoBehaviour // Ссылка для поиска пути // Прикрипленна в сцене к объекту PathfindingLink
{

    public Vector3 linkPositionA;
    public Vector3 linkPositionB;


    public PathfindingLink GetPathfindingLink() //Получить Ссылку для поиска пути
    {
        return new PathfindingLink
        {// получим сеточные позиции по мировым координатам
            gridPositionA = LevelGrid.Instance.GetGridPosition(linkPositionA),
            gridPositionB = LevelGrid.Instance.GetGridPosition(linkPositionB)
        };
    }

    private void Start()
    {
        // Получим узлы в месте перехода и соеденим их
        GraphNode graphNodeA = AstarPath.active.GetNearest(linkPositionA).node;
        GraphNode graphNodeB = AstarPath.active.GetNearest(linkPositionB).node;
        uint cost = (uint)Mathf.RoundToInt(((Int3)(linkPositionA - linkPositionB)).costMagnitude);

        graphNodeA.AddConnection(graphNodeB, cost);
        graphNodeB.AddConnection(graphNodeA, cost);
    }

}
