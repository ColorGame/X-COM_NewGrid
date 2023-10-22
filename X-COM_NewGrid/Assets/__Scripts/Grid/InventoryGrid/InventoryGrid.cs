using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class InventoryGrid : MonoBehaviour // ����� ���������
{
    public static InventoryGrid Instance { get; private set; }   

    [SerializeField] private GridParameters[] _gridParametersArray; // ������ ���������� ����� ������ � ����������
    [SerializeField] private Transform _gridDebugObjectPrefab; // ������ ������� ����� 


    private List<GridSystemTiltedXY<GridObjectInventoryXY>> _gridSystemTiltedXYList; //������ �������� ������ .� �������� ������� ��� GridObjectInventoryXY    

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

        _gridSystemTiltedXYList = new List<GridSystemTiltedXY<GridObjectInventoryXY>>(); // �������������� ������              

        foreach (GridParameters gridParameters in _gridParametersArray)
        {
            GridSystemTiltedXY<GridObjectInventoryXY> gridSystem = new GridSystemTiltedXY<GridObjectInventoryXY>(gridParameters,   // �������� �����  � � ������ ������ �������� ������ ���� GridObjectInventoryXY
                (GridSystemXY<GridObjectInventoryXY> g, Vector2Int gridPosition) => new GridObjectInventoryXY(g, gridPosition)); //� ��������� ��������� ��������� ������� ������� �������� ����� ������ => new GridObjectUnitXZ(g, _gridPositioAnchor) � ��������� �� ��������. (������ ��������� ����� ������� � ��������� �����)

            //gridSystem.CreateDebugObject(_gridDebugObjectPrefab); // �������� ��� ������ � ������ ������
            _gridSystemTiltedXYList.Add(gridSystem); // ������� � ������ ��������� �����
        }
    }

    private void Start()
    {
        // ������� ������ � ������� ��������� ������� ����

        //_background.localPosition = new Vector3(_widthBag / 2f, _heightBag / 2f, 0) * _cellSize; // ��������� ������ ��� ����������
        // _background.localScale = new Vector3(_widthBag, _heightBag, 0) * _cellSize;
        //_background.GetComponent<Material>().mainTextureScale = new Vector2(_widthBag, _heightBag);// ������� ���������� ������ ������ ���������� ����� �� ������ � ������   

        /* foreach (GridParameters gridParameters in _gridParametersArray)
         {
             RectTransform bagBackground = (RectTransform)gridParameters.anchorGridTransform.GetChild(0); //������� ������ ��� �����
             bagBackground.sizeDelta = new Vector2(gridParameters.width, gridParameters.height);
             bagBackground.localScale = Vector3.one * gridParameters.cellSize;
             bagBackground.GetComponent<RawImage>().uvRect = new Rect(0, 0, gridParameters.width, gridParameters.height);   // ������� ���������� ������ ������ ���������� ����� �� ������ � ������   
         }*/
    }


    public bool TryGetGridSystemGridPosition(Vector3 worldPositionMouse, out GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, out Vector2Int gridPositionMouse) //��������� ������� �������� ������� ��� �������� ������� ���� � � ������ ����� ������ �� � �������� �������
    {
        gridSystemXY = null;
        gridPositionMouse = new Vector2Int(0, 0);

        foreach (GridSystemTiltedXY<GridObjectInventoryXY> localGridSystem in _gridSystemTiltedXYList) // �������� ������ �����
        {
            Vector2Int testGridPositionMouse = localGridSystem.GetGridPosition(worldPositionMouse); //��� ������ localGridSystem ������� �������� ������� �����

            if (localGridSystem.IsValidGridPosition(testGridPositionMouse)) // ���� ����������� �������� ������� ���������, ������ �� � ���� �������� �������
            {
                gridSystemXY = localGridSystem;
                gridPositionMouse = testGridPositionMouse;
                break;
            }
        }

        if (gridSystemXY == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public bool TryAddPlacedObjectAtGridPosition(Vector2Int gridPositionMouse, PlacedObject placedObject, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY)//�������� �������� ����������� ������ � ������� �����
    {
        List<Vector2Int> gridPositionList = placedObject.GetTryOccupiesGridPositionList(gridPositionMouse); // ������� ������ �������� ������� ������� ����� ������ ������
        bool canPlace = true;
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            if (!gridSystemXY.IsValidGridPosition(gridPosition)) // ���� ���� ���� ���� �� ���������� ������� �� 
            {
                canPlace = false; // ���������� ������
                break;
            }
            if (gridSystemXY.GetGridObject(gridPosition).HasPlacedObject()) // ���� ���� �� ����� ������� ��� ���� ����������� ������ ��
            {
                canPlace = false; // ���������� ������
                break;
            }
        }

        if (canPlace)//���� ����� ���������� �� �����
        {
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                GridObjectInventoryXY gridObject = gridSystemXY.GetGridObject(gridPosition); // ������� GridObjectInventoryXY ������� ��������� � gridPosition
                gridObject.AddPlacedObject(placedObject); // �������� ����������� ������ 
            }    
            return true;
        }
        else
        {
            return false;
        }
    }

    public PlacedObject GetPlacedObjectAtGridPosition(Vector2Int gridPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) // �������� ����������� ������ � ���� �������� �������
    {
        GridObjectInventoryXY gridObject = gridSystemXY.GetGridObject(gridPosition); // ������� GridObjectInventoryXY ������� ��������� � gridPositionMouse
        return gridObject.GetPlacedObject();
    }

    public void RemovePlacedObjectAtGrid(PlacedObject placedObject) // �������� ����������� ������ �� ����� (�������������� ��� �� ��� �������� � �����)
    {
        List<Vector2Int> gridPositionList = placedObject.GetOccupiesGridPositionList(); // ������� ������ �������� ������� ������� �������� ������
        GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY = placedObject.GetGridSystemXY(); // ������� ����� �� ������� �� �������� 
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            GridObjectInventoryXY gridObject = gridSystemXY.GetGridObject(gridPosition); // ������� GridObjectInventoryXY ��� ����� � ������� �� ���������
            gridObject.RemovePlacedObject(placedObject); // ������ ����������� ������ 
        }       
    }

    public List<GridSystemTiltedXY<GridObjectInventoryXY>> GetGridSystemTiltedXYList()
    {
        return _gridSystemTiltedXYList;
    }

    // �������� �������� �������
    public Vector2Int GetGridPosition(Vector3 worldPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetGridPosition(worldPosition);

    // ������� ������� ���������� ������ ������ (������������  ����� ***GridTransform)
    public Vector3 GetWorldPositionCenter�ornerCell(Vector2Int gridPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetWorldPositionCenter�ornerCell(gridPosition);

    // ������� ������� ���������� ������� ������ ����� ������ (������������  ����� ***GridTransform)
    public Vector3 GetWorldPositionLowerLeft�ornerCell(Vector2Int gridPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetWorldPositionLowerLeft�ornerCell(gridPosition);

    public Vector3 GetRotationAnchorGrid(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetRotationAnchorGrid();
    public Transform GetAnchorGrid(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetAnchorGrid();
    public int GetWidth(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetWidth();
    public int GetHeight(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetHeight();
    public float GetCellSize() => _gridSystemTiltedXYList[0].GetCellSize(); // ��� ���� ����� ������� ������ ����������    

    /*public GridSystemTiltedXY<GridObjectInventoryXY> GetGridSystemTiltedXY(GridName gridName) // �������� ����� �� �����
    {
        foreach (GridSystemTiltedXY<GridObjectInventoryXY> localGridSystem in _gridSystemTiltedXYList) // �������� ������ �����
        {
            if (localGridSystem.GetGridName() == gridName)
            {
                return localGridSystem;
            }
        }

        return null;
    }
    public GridParameters[] GetGridParametersArray() //�������� ������ ���������� �����
    {
        return _gridParametersArray;
    }*/
}
