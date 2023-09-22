using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlacedObjectTypeSO;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class InventoryGrid : MonoBehaviour // ����� ���������
{
    public static InventoryGrid Instance { get; private set; }

    public event EventHandler<OnAnyInventoryMovedGridPositionEventArgs> OnAnyInventoryMovedGridPosition; //����� ��������� ��������� � �������� �������  // <OnAnyInventoryMovedGridPositionEventArgs>- ������� �������� ����� ������� ������ ���������

    public class OnAnyInventoryMovedGridPositionEventArgs : EventArgs // �������� ����� �������, ����� � ��������� ������� �������� ��������� � �������� �������
    {
        public PlacedObject placedObject; // ����������� ������ ���� SO List
        public GridPosition fromGridPosition;
        public GridPosition toGridPosition;
    }


    [SerializeField] private Transform _gridDebugObjectPrefab; // ������ ������� ����� //������������ ��� ������ ��������� � ����� ��������� ������ CreateDebugObject
    [SerializeField] private int _width = 10;     // ������
    [SerializeField] private int _height = 10;    // ������
    [SerializeField] private float _cellSize = 1f;// ������ ������
    [SerializeField] private Transform _background;

    private MouseEnterExitEventsUI _mouseEnterExitEvents; // ����� �� ������ ����
    private Vector3 _globalOffset = Vector3.zero; // �������� ����� � ������� �����������
    private GridSystem<GridObject> _gridSystem;
    private PlacedObject _placedObject;
    

    private void Awake()
    {
        // ���� �� �������� � ���������� �� �������� �� �����
        if (Instance != null) // ������� �������� ��� ���� ������ ���������� � ��������� ����������
        {
            Debug.LogError("There's more than one InventoryGrid!(��� ������, ��� ���� InventoryGrid!) " + transform + " - " + Instance);
            Destroy(gameObject); // ��������� ���� ��������
            return; // �.�. � ��� ��� ���� ��������� InventoryGrid ��������� ����������, ��� �� �� ��������� ������ ����
        }
        Instance = this;

        _globalOffset = transform.position + new Vector3(_cellSize / 2, 0, _cellSize / 2); // ������� ����� ������� ����� ���� ����� �������� � InventoryGrid.transform.position (��� �������� �������� �����, ���������� ����������� ������ InventoryGrid � ������ �����)

        _gridSystem = new GridSystem<GridObject>(_width, _height, _cellSize, _globalOffset,  // �������� ����� 10 �� 10 � �������� 2 ������� � � ������ ������ �������� ������ ���� GridObject
             (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition)); //� ��������� ��������� ��������� ������� ������� �������� ����� ������ => new GridObject(g, _gridPosition) � ��������� �� ��������. (������ ��������� ����� ������� � ��������� �����)
        _placedObject = null;
        //gridSystem.CreateDebugObject(_gridDebugObjectPrefab); // �������� ��� ������ � ������ ������ // �������������� �.�. PathfindingGridDebugObject ����� ��������� ��������������� ������ _gridDebugObjectPrefab

        // ������� ������ � ������� ��������� ������� ����
        _background.localPosition = new Vector3(_width / 2f, 0, _height / 2f);
        _background.localScale = new Vector3(_width, _height, 1);
        _background.GetComponent<Renderer>().material.mainTextureScale = new Vector2(_width, _height);

        
    }    


    public Transform GetGridBackground() // �������� ��� �����
    {
        return _background;
    }

    public GridPosition GetGridPosition(Vector3 worldPosition) => _gridSystem.GetGridPosition(worldPosition);   
    public Vector3 GetWorldPosition(GridPosition gridPosition) => _gridSystem.GetWorldPosition(gridPosition); // �������� �������

    public int GetWidth() => _gridSystem.GetWidth(); // ��� ����� ����� ���������� ����� ����� �� ����� 0 ����
    public int GetHeight() => _gridSystem.GetHeight();
    public float GetCellSize() => _gridSystem.GetCellSize();
}
