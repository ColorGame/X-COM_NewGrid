
using System;
using System.Collections.Generic;
using UnityEngine;




public class PickUpDropManager : MonoBehaviour // �������� �������������� � �������� ���������
{
    public static PickUpDropManager Instance { get; private set; }

    public event EventHandler<OnGrabbedObjectGridPositionChangedEventArgs> OnGrabbedObjectGridPositionChanged; // ������� ������������ ������� �� ����� ����������
    public class OnGrabbedObjectGridPositionChangedEventArgs : EventArgs // �������� ����� �������, ����� � ��������� ������� ��������
    {
        public GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY; // �������� ������� ������� ����
        public Vector2Int newMouseGridPosition;  // ����� �������� ������� ����
        public PlacedObject placedObject; // ���������� ������
    }

    public event EventHandler<PlacedObject> OnAddPlacedObjectAtGrid; // ������ �������� � ����� 
    public event EventHandler<PlacedObject> OnRemovePlacedObjectAtGrid; // ������ ������ �� �����
    public event EventHandler OnGrabbedObjectGridExits; // ���������� ������ ������� �����

    [SerializeField] private LayerMask _inventoryLayerMask; // ��� ��������� ��������� ���� ��� Inventory // �������� �� ������� ��� ���� ���������    
    [SerializeField] private Transform _canvasInventoryWorld;

    private PlacedObject _placedObject; // ����������� ������    
    private Vector3 _offset; // �������� �� �����
    private PlacedObjectTypeSO _placedObjectTypeSO;
    private PlacedObjectTypeSO.Dir _dir;
    private Camera _mainCamera;
    private Plane _plane; // ��������� �� ������� ����� ���������� ����������� �������    
    private Vector2Int _mouseGridPosition;  // �������� ������� ����
    private bool _startEventOnGrabbedObjectGridExits = false; // �������� ������� (���������� ������ ������� �����), ����� �� ��������� ������� ������ ���� ������ �������������

    private void Awake()
    {
        if (Instance != null) // ������� �������� ��� ���� ������ ���������� � ��������� ����������
        {
            Debug.LogError("There's more than one PickUpDropManager!(��� ������, ��� ���� PickUpDropManager!) " + transform + " - " + Instance);
            Destroy(gameObject); // ��������� ���� ��������
            return; // �.�. � ��� ��� ���� ��������� PickUpDropManager ��������� ����������, ��� �� �� ��������� ������ ����
        }
        Instance = this;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        _plane = new Plane(_mainCamera.transform.forward, _canvasInventoryWorld.position); // �������� ��������� ��������������� ������ � ������� canvasInventory
    }

    private void Update()
    {
        if (InputManager.Instance.IsMouseButtonDownThisFrame()) // ���� ��� ������ 
        {
            // ����� � �������� ������ ����� ������ � �����, �������� ���           
            if (InventoryGrid.Instance.TryGetGridSystemGridPosition(GetMousePositionOnPlane(), out GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, out Vector2Int gridPositionMouse)) // ���� ��� ������ �� ��������� �������� ��
            {
                if (_placedObject == null) // �� ���� ��� ���� �������� ��������, ����������� ��������
                {
                    TryGrab();
                }
                else // ���������� ������� ������ �� �����
                {
                    TryDrop(gridSystemXY, gridPositionMouse);
                }
            }
            else //���� �� ��� ������ 
            {
                if (_placedObject != null) // ���� ���� ���������� ������ 
                {
                    _placedObject.Drop();
                    _placedObject.SetMoveStartPosition(true); //�� ��������� ������ � ��������� ���������, ���������� � ������� ��� � ������ �������
                    _placedObject = null;
                }
            }
        }

        if (_placedObject != null) // ���� ���� ����������� ������ ����� ��� ���������� �� ���������� ���� �� ��������� ���������
        {
            SetTargetPosition();
        }
    }

    public void TryGrab()
    {
        Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); //���������� ���, ������ �� ������ ����� ����� ������ ��� ���������� ������ ���� 
        if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, _inventoryLayerMask)) // ������ true ���� ������� � ���������.
        {
            _placedObject = raycastHit.transform.GetComponentInParent<PlacedObject>();
            if (_placedObject != null) // ���� � �������� ������� � ������� ������ ���� PlacedObject �� ����� ��� �������� (�� ������ ����� ������ ������ � ��� ��� ��� �������)
            {
                _placedObject.Grab(); // ������� ���
                InventoryGrid.Instance.RemovePlacedObjectAtGrid(_placedObject);// ������ �� ������� �������� �������
                _placedObjectTypeSO = _placedObject.GetPlacedObjectTypeSO();

                OnRemovePlacedObjectAtGrid?.Invoke(this, _placedObject); // �������� �������
            }
        }
    }

    public void TryDrop(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, Vector2Int gridPositionMouse)
    {
        // ��������� �������� � ���������� �� �����
        GridName gridName = gridSystemXY.GetGridName(); // ������� ��� �����
        switch (gridName)
        {
            case GridName.BagGrid1:
                if (InventoryGrid.Instance.TryAddPlacedObjectAtGridPosition(gridPositionMouse, _placedObject, gridSystemXY))
                {
                    _placedObject.Drop();  // �������
                    _placedObject.SetGridPositionAnchor(gridPositionMouse); // ��������� ����� �������� ������� �����
                    _placedObject.SetGridSystemXY(gridSystemXY); //��������� ����� �� ������� �������� ��� �������

                    OnAddPlacedObjectAtGrid?.Invoke(this, _placedObject); // �������� �������
                                                                          // ���� �������� ����������

                    _placedObject = null;
                }
                else
                {
                    TooltipUI.Instance.Show("�� ������� ����������", new TooltipUI.TooltipTimer { timer = 2f }); // ������� ��������� � ������� ����� ������ ����������� ���������
                                                                                                                 // ���� �������
                }
                break;

            // ��� ����� ��������� � ���. ������ ��������� newMouseGridPosition (0,0)
            case GridName.MainWeaponGrid2:
            case GridName.OtherWeaponGrid3:
                if (InventoryGrid.Instance.TryAddPlacedObjectAtGridPosition(new Vector2Int(0, 0), _placedObject, gridSystemXY))
                {
                    _placedObject.Drop();  // �������
                    _placedObject.SetGridPositionAnchor(new Vector2Int(0, 0)); // ��������� ����� �������� ������� �����
                    _placedObject.SetGridSystemXY(gridSystemXY); //��������� ����� �� ������� �������� ��� �������

                    OnAddPlacedObjectAtGrid?.Invoke(this, _placedObject); // �������� �������
                                                                          // ���� �������� ����������

                    _placedObject = null;
                }
                else
                {
                    TooltipUI.Instance.Show("�� ������� ����������", new TooltipUI.TooltipTimer { timer = 2f }); // ������� ��������� � ������� ����� ������ ����������� ���������

                    // ���� �������
                }
                break;
        }
    }

    public void SetTargetPosition()
    {
        Vector3 mousePositionOnPlane = GetMousePositionOnPlane();
        Vector2Int zeroGridPosition = new Vector2Int(0, 0); // ������� ������� �����
        if (InventoryGrid.Instance.TryGetGridSystemGridPosition(mousePositionOnPlane, out GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, out Vector2Int newMouseGridPosition)) // ���� ��� ������ �� ��������� �������� ��
        {
            GridName gridName = gridSystemXY.GetGridName(); // ������� ��� �����
            switch (gridName)
            {
                case GridName.BagGrid1:
                    _placedObject.SetTargetPosition(InventoryGrid.Instance.GetWorldPositionLowerLeft�ornerCell(newMouseGridPosition, gridSystemXY));
                    _placedObject.SetTargetRotation(InventoryGrid.Instance.GetRotationAnchorGrid(gridSystemXY));

                    if (_mouseGridPosition != newMouseGridPosition || _mouseGridPosition == zeroGridPosition) // ���� �������� ������� �� ����� ���������� ��� ����� ������� ������� �� ...
                    {
                        OnGrabbedObjectGridPositionChanged?.Invoke(this, new OnGrabbedObjectGridPositionChangedEventArgs //�������� - ������� ������� ���� �� ����� ���������� � ��������� ���������� � ����� �������� �������
                        {
                            gridSystemXY = gridSystemXY,
                            newMouseGridPosition = newMouseGridPosition,
                            placedObject = _placedObject
                        }); // �������� ������� � ���������

                        _mouseGridPosition = newMouseGridPosition; // ��������� ���������� ������� �� �����
                    }
                    break;

                // ��� ����� ��������� � ���. ������ ��������� TargetPosition � �������  gridPosition (0,0)
                case GridName.MainWeaponGrid2:
                case GridName.OtherWeaponGrid3:
                    
                    _placedObject.SetTargetPosition(InventoryGrid.Instance.GetWorldPositionLowerLeft�ornerCell(zeroGridPosition, gridSystemXY));
                    _placedObject.SetTargetRotation(InventoryGrid.Instance.GetRotationAnchorGrid(gridSystemXY));

                    OnGrabbedObjectGridPositionChanged?.Invoke(this, new OnGrabbedObjectGridPositionChangedEventArgs //�������� - ������� ������� ���� �� ����� ���������� � ��������� ���������� � ����� �������� �������
                    {
                        gridSystemXY = gridSystemXY,
                        newMouseGridPosition = zeroGridPosition,
                        placedObject = _placedObject
                    }); // �������� ������� � ���������                       
                    break;
            }
            _startEventOnGrabbedObjectGridExits = false; // ������� ��������
        }
        else // ���� �� ��� ������ �� ������ ������� �� �����
        {
            _placedObject.SetTargetPosition(mousePositionOnPlane + _offset);
            _placedObject.SetTargetRotation(Vector3.zero);

            _mouseGridPosition = zeroGridPosition; //������� �������� ������� - ����� ������ �������� ����� (����� ������ ����������� ���������, ��� ����� ���� ,� �� �� ������� ����� � ������� �����))

            if (!_startEventOnGrabbedObjectGridExits) // ���� �� �������� ������� �� �������� ���
            {
                OnGrabbedObjectGridExits?.Invoke(this, EventArgs.Empty);
                _startEventOnGrabbedObjectGridExits= true;
            }

        }
    }

    public void CreatePlacedObject(Vector3 worldPosition, PlacedObjectTypeSO placedObjectTypeSO) //�������� ����������� ������ ��� ������� �� ������(� ��������� �������� ������� � ��� �������) 
    {
        // ����� �������� ������� ��������
        // Vector2Int mouseGridPosition = InventoryGrid.Instance.GetGridPosition(GetMousePositionOnPlane());
        if (_placedObject != null) // ���� ���� ���������� ������ 
        {
            //������� ����������� ������
            _placedObject.Drop();
            _placedObject.SetMoveStartPosition(true);
            _placedObject = null;
        }
        else
        {
            _placedObject = PlacedObject.CreateInWorld(worldPosition, PlacedObjectTypeSO.Dir.Down, placedObjectTypeSO, _canvasInventoryWorld); // �������� ������
            _placedObject.Grab(); // ������� ���
            _offset = -_placedObject.GetOffsetVisualFromParent(); // ����� ������ ��� �� ������ ����� ���� ������� �������� ������� ������������ ��������
            _placedObjectTypeSO = _placedObject.GetPlacedObjectTypeSO();

        }
    }

    public Vector3 GetMousePositionOnPlane() // �������� ������� ���� �� ���������
    {
        Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());//���������� ���, ������ �� ������ ����� ����� ������ ��� ���������� ������ ���� 
        _plane.Raycast(ray, out float planeDistance); // ��������� ��� � ��������� � ������� ���������� ����� ����, ��� �� ���������� ���������.
        return ray.GetPoint(planeDistance); // ������� ����� �� ���� ��� ��� ��������� ���������
    }

    /*  public Vector3 CalculateOffsetGrab() // �������� �������� �������
      {
          Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); //���������� ���, ������ �� ������ ����� ����� ������ ��� ���������� ������ ���� 
          _plane.Raycast(ray, out float planeDistance); // ��������� ��� � ��������� � ������� ���������� ����� ����, ��� �� ���������� ���������.
          return _placedObject.transform.position - ray.GetPoint(planeDistance); // �������� �������� �� ������ ������� � ������  pivot �� �������.        
      }

      public Vector3 GetMousePosition(LayerMask layerMask) // �������� ������� ���� (static ���������� ��� ����� ����������� ������ � �� ������ ������ ����������) // ��� ����������� ����
      {
          Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); // ��� �� ������ � ����� �� ������ ��� ���������� ������ ����
          Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, layerMask);
          return raycastHit.point; // ���� ��� ������� � �������� �� Physics.Raycast ����� true, � raycastHit.point ������ "����� ����� � ������� ������������, ��� ��� ����� � ���������", � ���� false �� ����� ������� ����������� ������ ������ ��������(� ����� ������ ������ ������� ������).
      }

      public Vector3 GetMouseWorldSnappedPosition(Vector2Int mouseGridPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) // �������� ��������������� ������� ��������� ���� (��� ������)
      {
          Vector2Int rotationOffset = _placedObjectTypeSO.GetRotationOffset(_dir); // �������� ������� ���� �� ��������
          Vector3 placedObjectWorldPosition = InventoryGrid.Instance.GetWorldPositionCenter�ornerCell(mouseGridPosition, gridSystemXY) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * InventoryGrid.Instance.GetCellSize();
          return placedObjectWorldPosition; // ������ ��������������� ��������� � ����� �����
      }

      public Quaternion GetPlacedObjectRotation() // ������� �������� ������������ �������
      {
          if (_placedObjectTypeSO != null)
          {
              return Quaternion.Euler(0, 0, _placedObjectTypeSO.GetRotationAngle(_dir));
          }
          else
          {
              return Quaternion.identity;
          }
      }*/

}
