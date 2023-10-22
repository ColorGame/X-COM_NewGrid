using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ������� InventoryGridSystemVisual ��� ������� ����� ������� �� ���������, ��������� �� �����, ����� ���������� ������� ����������� ����� ����� ����������.
// (Project Settings/ Script Execution Order � �������� ���������� InventoryGridSystemVisual ���� Default Time)
public class InventoryGridSystemVisual : MonoBehaviour // �������� ������� ������������ ���������
{
    public static InventoryGridSystemVisual Instance { get; private set; }

    [Serializable] // ����� ��������� ��������� ����� ������������ � ����������
    public struct GridVisualTypeMaterial    //������ ����� ��� ��������� // �������� ��������� ����� � ��������� ������. ������ � �������� ��������� ������������ ��� ���� ������ �������� ����������� ����� ������ � C#
    {                                       //� ������ ��������� ��������� ��������� ����� � ����������
        public GridVisualType gridVisualType;
        public Material materialGrid;
    }

    public enum GridVisualType //���������� ��������� �����
    {
        White,
        Grey,
        Blue,
        BlueSoft,
        Red,
        RedSoft,
        Yellow,
        YellowSoft,
        Green,
        GreenSoft,
    }

    [SerializeField] private List<GridVisualTypeMaterial> _gridVisualTypeMaterialList; // ������ ��� ��������� ����������� ��������� ����� ������� (������ �� ���������� ���� ������) ����������� ��������� ����� // � ���������� ��� ������ ��������� ���������� ��������������� �������� �����

    private InventoryGridSystemVisualSingle[][,] _inventoryGridSystemVisualSingleArray; // ������ ������� [���������� �����][����� (�), ������ (�)]
    private Dictionary<GridName, int> _gridNameIndexDictionary; // ������� (GridName - ����, int(������) -��������)


    private void Awake() //��� ��������� ������ Awake ����� ������������ ������ ��� ������������� � ���������� ��������
    {
        // ���� �� �������� � ���������� �� �������� �� �����
        if (Instance != null) // ������� �������� ��� ���� ������ ���������� � ��������� ����������
        {
            Debug.LogError("There's more than one InventoryGridSystemVisual!(��� ������, ��� ���� InventoryGridSystemVisual!) " + transform + " - " + Instance);
            Destroy(gameObject); // ��������� ���� ��������
            return; // �.�. � ��� ��� ���� ��������� InventoryGridSystemVisual ��������� ����������, ��� �� �� ��������� ������ ����
        }
        Instance = this;
    }

    private void Start()
    {
        _gridNameIndexDictionary = new Dictionary<GridName, int>();

        // �������������� ������� ������ ����� ������� - ���������� �����
        List<GridSystemTiltedXY<GridObjectInventoryXY>> gridSystemTiltedXYList = InventoryGrid.Instance.GetGridSystemTiltedXYList(); // ������� ������ �����
        _inventoryGridSystemVisualSingleArray = new InventoryGridSystemVisualSingle[gridSystemTiltedXYList.Count][,];

        // ��� ������ ����� ��������� ��������� ������ ���������
        for (int i = 0; i < gridSystemTiltedXYList.Count; i++)
        {
            _inventoryGridSystemVisualSingleArray[i] = new InventoryGridSystemVisualSingle[gridSystemTiltedXYList[i].GetWidth(), gridSystemTiltedXYList[i].GetHeight()];
        }


        for (int i = 0; i < gridSystemTiltedXYList.Count; i++) // ��������� ��� �����
        {
            for (int x = 0; x < gridSystemTiltedXYList[i].GetWidth(); x++) // ��� ������ ����� ��������� �����
            {
                for (int y = 0; y < gridSystemTiltedXYList[i].GetHeight(); y++)  // � ������
                {
                    Vector2Int gridPosition = new Vector2Int(x, y); // ������� �����
                    Vector3 rotation = InventoryGrid.Instance.GetRotationAnchorGrid(gridSystemTiltedXYList[i]);
                    Transform AnchorGridTransform = InventoryGrid.Instance.GetAnchorGrid(gridSystemTiltedXYList[i]);

                    Transform gridSystemVisualSingleTransform = Instantiate(GameAssets.Instance.inventoryGridSystemVisualSinglePrefab, InventoryGrid.Instance.GetWorldPositionCenter�ornerCell(gridPosition, gridSystemTiltedXYList[i]), Quaternion.Euler(rotation), AnchorGridTransform); // �������� ��� ������ � ������ ������� �����

                    _gridNameIndexDictionary[gridSystemTiltedXYList[i].GetGridName()] = i; // �������� �����(��� ����� ��� ���� ��������) �������� (������ �������)
                    _inventoryGridSystemVisualSingleArray[i][x, y] = gridSystemVisualSingleTransform.GetComponent<InventoryGridSystemVisualSingle>(); // ��������� ��������� LevelGridSystemVisualSingle � ���������� ������ ��� x,y,y ��� ����� ������� �������.
                }
            }
        }

        PickUpDropManager.Instance.OnAddPlacedObjectAtGrid += PickUpDropManager_OnAddPlacedObjectAtGrid;
        PickUpDropManager.Instance.OnRemovePlacedObjectAtGrid += PickUpDropManager_OnRemovePlacedObjectAtGrid;
        PickUpDropManager.Instance.OnGrabbedObjectGridPositionChanged += PickUpDropManager_OnGrabbedObjectGridPositionChanged;
        PickUpDropManager.Instance.OnGrabbedObjectGridExits += PickUpDropManager_OnGrabbedObjectGridExits;
    }

    // ���������� ������ ������� �����
    private void PickUpDropManager_OnGrabbedObjectGridExits(object sender, EventArgs e)  // ���������� ������ ������� �����
    {
        SetDefaultState(); // ��������� ��������� ��������� ���� �����
    }

    // ������� ������������ ������� �� ����� ����������
    private void PickUpDropManager_OnGrabbedObjectGridPositionChanged(object sender, PickUpDropManager.OnGrabbedObjectGridPositionChangedEventArgs e)
    {
        SetDefaultState(); // ��������� ��������� ��������� ���� �����
        ShowPossibleGridPositions(e.gridSystemXY, e.placedObject, e.newMouseGridPosition, GridVisualType.Yellow); //�������� ��������� �������� �������
    }

    // ������ ������ �� �����
    private void PickUpDropManager_OnRemovePlacedObjectAtGrid(object sender, PlacedObject placedObject)
    {
        SetMaterialAndIsBusy(placedObject, GridVisualType.Yellow, false);
    }

    // ������ �������� � ����� 
    private void PickUpDropManager_OnAddPlacedObjectAtGrid(object sender, PlacedObject placedObject)
    {
        SetMaterialAndIsBusy(placedObject, GridVisualType.Blue, true);
    }

    private void SetDefaultState() // ���������� ��������� ��������� �����
    {
        List<GridSystemTiltedXY<GridObjectInventoryXY>> gridSystemTiltedXYList = InventoryGrid.Instance.GetGridSystemTiltedXYList(); // ������� ������ �����
        for (int i = 0; i < gridSystemTiltedXYList.Count; i++) // ��������� ��� �����
        {
            for (int x = 0; x < gridSystemTiltedXYList[i].GetWidth(); x++) // ��� ������ ����� ��������� �����
            {
                for (int y = 0; y < gridSystemTiltedXYList[i].GetHeight(); y++)  // � ������
                {
                    if (!_inventoryGridSystemVisualSingleArray[i][x, y].GetIsBusy()) // ���� ������� �� ������
                    {
                        _inventoryGridSystemVisualSingleArray[i][x, y].SetMaterial(GetGridVisualTypeMaterial(GridVisualType.White));
                    }
                }
            }
        }
    }

    private void SetMaterialAndIsBusy(PlacedObject placedObject, GridVisualType gridVisualType, bool isBusy)
    {
        GridSystemTiltedXY<GridObjectInventoryXY> gridSystemTiltedXY = placedObject.GetGridSystemXY(); // �������� ������� ������� �������� ������
        List<Vector2Int> OccupiesGridPositionList = placedObject.GetOccupiesGridPositionList(); // ������ ���������� �������� �������
        GridName gridName = gridSystemTiltedXY.GetGridName(); // ��� �����
        int index = _gridNameIndexDictionary[gridName]; //������ �� ������� ������ ����� � _inventoryGridSystemVisualSingleArray

        foreach (Vector2Int gridPosition in OccupiesGridPositionList) // ��������� ����������� �������� ������� �����
        {
            // ��� ������� ����� ��������� ������� ���� � ������� ��������
            _inventoryGridSystemVisualSingleArray[index][gridPosition.x, gridPosition.y].SetMaterial(GetGridVisualTypeMaterial(gridVisualType));
            _inventoryGridSystemVisualSingleArray[index][gridPosition.x, gridPosition.y].SetIsBusy(isBusy);
        }
    }

    // �������� ��������� �������� �������
    private void ShowPossibleGridPositions(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, PlacedObject placedObject, Vector2Int mouseGridPosition, GridVisualType gridVisualType ) 
    {
        GridName gridName = gridSystemXY.GetGridName(); // ��� ����� ��� ���������� ��� � ����������� ��������
        int index = _gridNameIndexDictionary[gridName]; //������ �� ������� ������ ����� � _inventoryGridSystemVisualSingleArray
        List<Vector2Int> TryOccupiesGridPositionList = placedObject.GetTryOccupiesGridPositionList(mouseGridPosition); // ������ �������� ������� ������� ����� ������
        foreach (Vector2Int gridPosition in TryOccupiesGridPositionList) // ��������� ������ ������� ������� ���� ������
        {
            if (gridSystemXY.IsValidGridPosition(gridPosition)) // ���� ������� ��������� ��...
            {
                if (!_inventoryGridSystemVisualSingleArray[index][gridPosition.x, gridPosition.y].GetIsBusy()) // ���� ������� �� ������
                {
                    _inventoryGridSystemVisualSingleArray[index][gridPosition.x, gridPosition.y].SetMaterial(GetGridVisualTypeMaterial(gridVisualType));
                }
            }
        }
    }

    

    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType) //(������� �������� � ����������� �� ���������) �������� ��� ��������� ��� �������� ������������ � ����������� �� ����������� � �������� ��������� �������� ������������
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in _gridVisualTypeMaterialList) // � ����� ��������� ������ ��� ��������� ����������� ��������� ����� 
        {
            if (gridVisualTypeMaterial.gridVisualType == gridVisualType) // ����  ��������� �����(gridVisualType) ��������� � ���������� ��� ��������� �� ..
            {
                return gridVisualTypeMaterial.materialGrid; // ������ �������� ��������������� ������� ��������� �����
            }
        }

        Debug.LogError("�� ���� ����� GridVisualTypeMaterial ��� GridVisualType " + gridVisualType); // ���� �� ������ ����������� ������ ������
        return null;
    }
}
