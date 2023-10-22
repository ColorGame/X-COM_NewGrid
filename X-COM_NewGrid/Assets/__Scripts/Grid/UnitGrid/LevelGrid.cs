using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;


// �������� ������� ���������� ������� LevelGrid, ������� � Project Settings/ Script Execution Order � �������� ���������� LevelGrid ���� Default Time, ����� LevelGrid ���������� ������ �� ���� ��� ��������� �������� ����� ���� ( � Start() �� ��������� ����� PathfindingMonkey - ��������� ������ ����)

public class LevelGrid : MonoBehaviour // �������� ������ ������� ��������� ������ ������� ������ . �������� ������ ��������� ��� �������� ������������� ����� � �������� ������� �����
{

    public static LevelGrid Instance { get; private set; }   //(������� SINGLETON) ��� �������� ������� ����� ���� ������� (SET-���������) ������ ���� �������, �� ����� ���� �������� GET ����� ������ �������
                                                             // instance - ���������, � ��� ����� ���� ��������� LevelGrid ����� ���� ��� static. Instance ����� ��� ���� ����� ������ ������, ����� ����, ����� ����������� �� Event.

    public const float FLOOR_HEIGHT = 3f; // ������ ����� � ������ - ��� ������ ������

    public event EventHandler<OnAnyUnitMovedGridPositionEventArgs> OnAnyUnitMovedGridPosition; //������� ������� ����� - ����� ���� ��������� � �������� �������  // <OnPlacedObjectOverGridPositionEventArgs>- ������� �������� ����� ������� ������ ���������

    public class OnAnyUnitMovedGridPositionEventArgs : EventArgs // �������� ����� �������, ����� � ��������� ������� �������� ����� � �������� �������
    {
        public Unit unit;
        public GridPositionXZ fromGridPosition;
        public GridPositionXZ toGridPosition;
    }


    [SerializeField] private Transform _gridDebugObjectPrefab; // ������ ������� ����� //������������ ��� ������ ��������� � ����� ��������� ������ CreateDebugObject
    [SerializeField] private GridParameters _gridParameters;
    [SerializeField] private int _floorAmount = 2;// ���������� ������

    private List<GridSystemXZ<GridObjectUnitXZ>> _gridSystemList; //������ �������� ������ .� �������� ������� ��� GridObjectUnitXZ

    private void Awake()
    {
        // ���� �� �������� � ���������� �� �������� �� �����
        if (Instance != null) // ������� �������� ��� ���� ������ ���������� � ��������� ����������
        {
            Debug.LogError("There's more than one LevelGrid!(��� ������, ��� ���� LevelGrid!) " + transform + " - " + Instance);
            Destroy(gameObject); // ��������� ���� ��������
            return; // �.�. � ��� ��� ���� ��������� LevelGrid ��������� ����������, ��� �� �� ��������� ������ ����
        }
        Instance = this;

        _gridSystemList = new List<GridSystemXZ<GridObjectUnitXZ>>(); // �������������� ������

        for (int floor = 0; floor < _floorAmount; floor++) // ��������� ����� � �� ������ �������� �������� �������
        {
            GridSystemXZ<GridObjectUnitXZ> gridSystem = new GridSystemXZ<GridObjectUnitXZ>(_gridParameters, // �������� ����� 10 �� 10 � �������� 2 ������� �� ����� floor ������� 3  � � ������ ������ �������� ������ ���� GridObjectUnitXZ
                 (GridSystemXZ<GridObjectUnitXZ> g, GridPositionXZ gridPosition) => new GridObjectUnitXZ(g, gridPosition), floor, FLOOR_HEIGHT); //� 5 ��������� ��������� ������� ������� �������� ����� ������ => new GridObjectUnitXZ(g, _gridPositioAnchor) � ��������� �� ��������. (������ ��������� ����� ������� � ��������� �����)

           // gridSystem.CreateDebugObject(_gridDebugObjectPrefab); // �������� ��� ������ � ������ ������ // �������������� �.�. PathfindingGridDebugObject ����� ��������� ��������������� ������ _gridDebugObjectPrefab

            _gridSystemList.Add(gridSystem); // ������� � ������ ��������� �����
        }
    }

    private void Start()
    {
        //  PathfindingMonkey.Instance.Setup(_gridParameters, _floorAmount); // �������� ����� ����� ������ ���� // �������� ��� ���� ����� �������� ������ �� ���� ��� ��������� �������� ����� ����
    }

    private GridSystemXZ<GridObjectUnitXZ> GetGridSystem(int floor) // �������� �������� ������� ��� ������� �����
    {
        return _gridSystemList[floor];
    }


    public void AddUnitAtGridPosition(GridPositionXZ gridPosition, Unit unit) // �������� ������������� ����� � �������� ������� �����
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObjectUnitXZ ������� ��������� � _gridPositioAnchor
        gridObject.AddUnit(unit); // �������� ����� 
    }

    public List<Unit> GetUnitListAtGridPosition(GridPositionXZ gridPosition) // �������� ������ ������ � �������� ������� �����
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObjectUnitXZ ������� ��������� � _gridPositioAnchor
        return gridObject.GetUnitList();// ������� �����
    }

    public void RemoveUnitAtGridPosition(GridPositionXZ gridPosition, Unit unit) // �������� ����� �� �������� ������� �����
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObjectUnitXZ ������� ��������� � _gridPositioAnchor
        gridObject.RemoveUnit(unit); // ������ �����
    }

    public void UnitMovedGridPosition(Unit unit, GridPositionXZ fromGridPosition, GridPositionXZ toGridPosition) // ���� ��������� � �������� ������� �� ������� fromGridPosition � ������� toGridPosition
    {
        RemoveUnitAtGridPosition(fromGridPosition, unit); // ������ ����� �� ������� ������� �����

        AddUnitAtGridPosition(toGridPosition, unit);  // ������� ����� � ��������� ������� �����

        OnAnyUnitMovedGridPosition?.Invoke(this, new OnAnyUnitMovedGridPositionEventArgs // ������� ����� ��������� ������ OnPlacedObjectOverGridPositionEventArgs
        {
            unit = unit,
            fromGridPosition = fromGridPosition,
            toGridPosition = toGridPosition,

        }); // �������� ������� ����� ���� ��������� � �������� ������� ( � ��������� ��������� ����� ���� ������ � ����)
    }

    public int GetFloor(Vector3 worldPosition) // �������� ����
    {
        return Mathf.RoundToInt(worldPosition.y / FLOOR_HEIGHT); // ������� ������� �� � �� ������ ����� � �������� �� ������ ��� ����� ������� ����
    }

    // ��� �� �� ���������� ��������� ���������� LevelGrid (� �� ������ ��������� ����_gridSystem) �� ������������ ������ � GridPositionXZ ������� �������� ������� ��� ������� � GridPositionXZ
    public GridPositionXZ GetGridPosition(Vector3 worldPosition) // ������� �������� ������� ��� ������� ���������
    {
        int floor = GetFloor(worldPosition); // ������ ����
        return GetGridSystem(floor).GetGridPosition(worldPosition); // ��� ����� ����� ������ �������� �������
    }

    /// <summary>
    /// �������� ���� ����� - ���� �����, ��� ���������� ��������� ������ �����, � ����� ��������������� ������������������, �� 
    /// ����� �������� ������ ���� �� ���������� �������, ��� ������� �������.
    /// </summary>
    public LevelGridNode GetGridNode(GridPositionXZ gridPosition) // �������� ���� ����� (A* Pathfinding Project4.2.18) ��� ����� �������� ������� ()
    {
        int width = AstarPath.active.data.layerGridGraph.width;
        int depth = AstarPath.active.data.layerGridGraph.depth;
        /*int layerCount = AstarPath.active.data.layerGridGraph.LayerCount;
        float nodeSize = AstarPath.active.data.layerGridGraph.nodeSize;     */

        LevelGridNode gridNode = (LevelGridNode)AstarPath.active.data.layerGridGraph.nodes[gridPosition.x + gridPosition.z * width + gridPosition.floor * width * depth];

        /*// �������� ���������� GraphNode � GridPositionXZ  
        Debug.Log("x " + _gridPositioAnchor.x + "y " + _gridPositioAnchor.y +"Floor " + _gridPositioAnchor.floor + 
            " node" + " x " + gridNode.XCoordinateInGrid + "y " + gridNode.ZCoordinateInGrid +"Layer " + gridNode.LayerCoordinateInGrid );*/
        return gridNode;
    }

    /// <summary>
    /// True, ���� ���� �������� ��� �������.
    /// </summary>
    public bool WalkableNode(GridPositionXZ gridPosition)
    {
       return GetGridNode(gridPosition).Walkable;
    }

    public Vector3 GetWorldPosition(GridPositionXZ gridPosition) => GetGridSystem(gridPosition.floor).GetWorldPosition(gridPosition); // �������� �������

    public bool IsValidGridPosition(GridPositionXZ gridPosition) // �������� �� ���������� �������� ��������
    {
        if (gridPosition.floor < 0 || gridPosition.floor >= _floorAmount) // ������� �� ������� ����� ������
        {
            return false;
        }
        else
        {
            return GetGridSystem(gridPosition.floor).IsValidGridPosition(gridPosition); // �������� ������� ��� ��������� ������� � IsValidGridPosition �� _gridSystemTiltedXYList
        }

    }
    public int GetWidth() => GetGridSystem(0).GetWidth(); // ��� ����� ����� ���������� ����� ����� �� ����� 0 ����
    public int GetHeight() => GetGridSystem(0).GetHeight();
    public float GetCellSize() => GetGridSystem(0).GetCellSize();
    public int GetFloorAmount() => _floorAmount;

    public bool HasAnyUnitOnGridPosition(GridPositionXZ gridPosition) // ���� �� ����� ������ ���� �� ���� �������� �������
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObjectUnitXZ ������� ��������� � _gridPositioAnchor
        return gridObject.HasAnyUnit();
    }
    public Unit GetUnitAtGridPosition(GridPositionXZ gridPosition) // �������� ����� � ���� �������� �������
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObjectUnitXZ ������� ��������� � _gridPositioAnchor
        return gridObject.GetUnit();
    }


    // IInteractable ��������� �������������� - ��������� � ������ InteractAction ����������������� � ����� �������� (�����, �����, ������...) - ������� ��������� ���� ���������
    public IInteractable GetInteractableAtGridPosition(GridPositionXZ gridPosition) // �������� ��������� �������������� � ���� �������� �������
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObjectUnitXZ ������� ��������� � _gridPositioAnchor
        return gridObject.GetInteractable();
    }
    public void SetInteractableAtGridPosition(GridPositionXZ gridPosition, IInteractable interactable) // ���������� ���������� ��������� �������������� � ���� �������� �������
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObjectUnitXZ ������� ��������� � _gridPositioAnchor
        gridObject.SetInteractable(interactable);
    }
    public void ClearInteractableAtGridPosition(GridPositionXZ gridPosition) // �������� ��������� �������������� � ���� �������� �������
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObjectUnitXZ ������� ��������� � _gridPositioAnchor
        gridObject.ClearInteractable(); // �������� ��������� �������������� � ���� �������� �������
    }

}
