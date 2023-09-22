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

    public event EventHandler<OnAnyUnitMovedGridPositionEventArgs> OnAnyUnitMovedGridPosition; //������� ������� ����� - ����� ���� ��������� � �������� �������  // <OnAnyInventoryMovedGridPositionEventArgs>- ������� �������� ����� ������� ������ ���������

    public class OnAnyUnitMovedGridPositionEventArgs : EventArgs // �������� ����� �������, ����� � ��������� ������� �������� ����� � �������� �������
    {
        public Unit unit;
        public GridPosition fromGridPosition;
        public GridPosition toGridPosition;
    }


    [SerializeField] private Transform _gridDebugObjectPrefab; // ������ ������� ����� //������������ ��� ������ ��������� � ����� ��������� ������ CreateDebugObject

    [SerializeField] private int _width = 10;     // ������
    [SerializeField] private int _height = 10;    // ������
    [SerializeField] private float _cellSize = 2f;// ������ ������
    [SerializeField] private int _floorAmount = 2;// ���������� ������
    [SerializeField] private Vector3 _globalOffset = Vector3.zero; // �������� ����� � ������� �����������

    private List<GridSystem<GridObject>> _gridSystemList; //������ �������� ������ .� �������� ������� ��� GridObject

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

        _gridSystemList = new List<GridSystem<GridObject>>(); // �������������� ������

        for (int floor = 0; floor < _floorAmount; floor++) // ��������� ����� � �� ������ �������� �������� �������
        {
            GridSystem<GridObject> gridSystem = new GridSystem<GridObject>(_width, _height, _cellSize, _globalOffset, floor, FLOOR_HEIGHT, // �������� ����� 10 �� 10 � �������� 2 ������� �� ����� floor ������� 3  � � ������ ������ �������� ������ ���� GridObject
                 (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition)); //� ��������� ��������� ��������� ������� ������� �������� ����� ������ => new GridObject(g, _gridPosition) � ��������� �� ��������. (������ ��������� ����� ������� � ��������� �����)

            //gridSystem.CreateDebugObject(_gridDebugObjectPrefab); // �������� ��� ������ � ������ ������ // �������������� �.�. PathfindingGridDebugObject ����� ��������� ��������������� ������ _gridDebugObjectPrefab

            _gridSystemList.Add(gridSystem); // ������� � ������ ��������� �����
        }
    }

    private void Start()
    {
      //  PathfindingMonkey.Instance.Setup(_width, _height, _cellSize, _floorAmount); // �������� ����� ����� ������ ���� // �������� ��� ���� ����� �������� ������ �� ���� ��� ��������� �������� ����� ����
    }

    private GridSystem<GridObject> GetGridSystem(int floor) // �������� �������� ������� ��� ������� �����
    {
        return _gridSystemList[floor];
    }


    public void AddUnitAtGridPosition(GridPosition gridPosition, Unit unit) // �������� ������������� ����� � �������� ������� �����
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObject ������� ��������� � _gridPosition
        gridObject.AddUnit(unit); // �������� ����� 
    }

    public List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition) // �������� ������ ������ � �������� ������� �����
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObject ������� ��������� � _gridPosition
        return gridObject.GetUnitList();// ������� �����
    }

    public void RemoveUnitAtGridPosition(GridPosition gridPosition, Unit unit) // �������� ����� �� �������� ������� �����
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObject ������� ��������� � _gridPosition
        gridObject.RemoveUnit(unit); // ������ �����
    }

    public void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition) // ���� ��������� � �������� ������� �� ������� fromGridPosition � ������� toGridPosition
    {
        RemoveUnitAtGridPosition(fromGridPosition, unit); // ������ ����� �� ������� ������� �����

        AddUnitAtGridPosition(toGridPosition, unit);  // ������� ����� � ��������� ������� �����

        OnAnyUnitMovedGridPosition?.Invoke(this, new OnAnyUnitMovedGridPositionEventArgs // ������� ����� ��������� ������ OnAnyInventoryMovedGridPositionEventArgs
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

    // ��� �� �� ���������� ��������� ���������� LevelGrid (� �� ������ ��������� ����_gridSystem) �� ������������ ������ � GridPosition ������� �������� ������� ��� ������� � GridPosition
    public GridPosition GetGridPosition(Vector3 worldPosition) // ������� �������� ������� ��� ������� ���������
    {
        int floor = GetFloor(worldPosition); // ������ ����
        return GetGridSystem(floor).GetGridPosition(worldPosition); // ��� ����� ����� ������ �������� �������
    }

    /// <summary>
    /// �������� ���� ����� - ���� �����, ��� ���������� ��������� ������ �����, � ����� ��������������� ������������������, �� 
    /// ����� �������� ������ ���� �� ���������� �������, ��� ������� �������.
    /// </summary>
    public LevelGridNode GetGridNode(GridPosition gridPosition) // �������� ���� ����� (A* Pathfinding Project4.2.18) ��� ����� �������� ������� ()
    {
        int width = AstarPath.active.data.layerGridGraph.width;
        int depth = AstarPath.active.data.layerGridGraph.depth;
        /*int layerCount = AstarPath.active.data.layerGridGraph.LayerCount;
        float nodeSize = AstarPath.active.data.layerGridGraph.nodeSize;     */

        LevelGridNode gridNode = (LevelGridNode)AstarPath.active.data.layerGridGraph.nodes[gridPosition.x + gridPosition.z * width + gridPosition.floor * width * depth];

        /*// �������� ���������� GraphNode � GridPosition  
        Debug.Log("x " + _gridPosition.x + "z " + _gridPosition.z +"Floor " + _gridPosition.floor + 
            " node" + " x " + gridNode.XCoordinateInGrid + "z " + gridNode.ZCoordinateInGrid +"Layer " + gridNode.LayerCoordinateInGrid );*/
        return gridNode;
    }

    /// <summary>
    /// True, ���� ���� �������� ��� �������.
    /// </summary>
    public bool WalkableNode(GridPosition gridPosition)
    {
       return GetGridNode(gridPosition).Walkable;
    }

    public Vector3 GetWorldPosition(GridPosition gridPosition) => GetGridSystem(gridPosition.floor).GetWorldPosition(gridPosition); // �������� �������

    public bool IsValidGridPosition(GridPosition gridPosition) // �������� �� ���������� �������� ��������
    {
        if (gridPosition.floor < 0 || gridPosition.floor >= _floorAmount) // ������� �� ������� ����� ������
        {
            return false;
        }
        else
        {
            return GetGridSystem(gridPosition.floor).IsValidGridPosition(gridPosition); // �������� ������� ��� ��������� ������� � IsValidGridPosition �� _gridSystemList
        }

    }
    public int GetWidth() => GetGridSystem(0).GetWidth(); // ��� ����� ����� ���������� ����� ����� �� ����� 0 ����
    public int GetHeight() => GetGridSystem(0).GetHeight();
    public float GetCellSize() => GetGridSystem(0).GetCellSize();
    public int GetFloorAmount() => _floorAmount;

    public bool HasAnyUnitOnGridPosition(GridPosition gridPosition) // ���� �� ����� ������ ���� �� ���� �������� �������
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObject ������� ��������� � _gridPosition
        return gridObject.HasAnyUnit();
    }
    public Unit GetUnitAtGridPosition(GridPosition gridPosition) // �������� ����� � ���� �������� �������
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObject ������� ��������� � _gridPosition
        return gridObject.GetUnit();
    }


    // IInteractable ��������� �������������� - ��������� � ������ InteractAction ����������������� � ����� �������� (�����, �����, ������...) - ������� ��������� ���� ���������
    public IInteractable GetInteractableAtGridPosition(GridPosition gridPosition) // �������� ��������� �������������� � ���� �������� �������
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObject ������� ��������� � _gridPosition
        return gridObject.GetInteractable();
    }
    public void SetInteractableAtGridPosition(GridPosition gridPosition, IInteractable interactable) // ���������� ���������� ��������� �������������� � ���� �������� �������
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObject ������� ��������� � _gridPosition
        gridObject.SetInteractable(interactable);
    }
    public void ClearInteractableAtGridPosition(GridPosition gridPosition) // �������� ��������� �������������� � ���� �������� �������
    {
        GridObject gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // ������� GridObject ������� ��������� � _gridPosition
        gridObject.ClearInteractable(); // �������� ��������� �������������� � ���� �������� �������
    }

}
