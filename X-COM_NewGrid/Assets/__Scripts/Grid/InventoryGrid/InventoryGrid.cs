using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class InventoryGrid : MonoBehaviour // Сетка инверторя
{
    public static InventoryGrid Instance { get; private set; }   

    [SerializeField] private GridParameters[] _gridParametersArray; // Массив параметров сеток ЗАДАТЬ в ИНСПЕКТОРЕ
    [SerializeField] private Transform _gridDebugObjectPrefab; // Префаб отладки сетки 


    private List<GridSystemTiltedXY<GridObjectInventoryXY>> _gridSystemTiltedXYList; //Список сеточнах систем .В дженерик предаем тип GridObjectInventoryXY    

    private void Awake()
    {
        // Если ты акуратем в инспекторе то проверка не нужна
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one InventoryGrid!(Там больше, чем один InventoryGrid!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр InventoryGrid прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this;

        _gridSystemTiltedXYList = new List<GridSystemTiltedXY<GridObjectInventoryXY>>(); // Инициализируем список              

        foreach (GridParameters gridParameters in _gridParametersArray)
        {
            GridSystemTiltedXY<GridObjectInventoryXY> gridSystem = new GridSystemTiltedXY<GridObjectInventoryXY>(gridParameters,   // ПОСТРОИМ СЕТКУ  и в каждой ячейки создадим объект типа GridObjectInventoryXY
                (GridSystemXY<GridObjectInventoryXY> g, Vector2Int gridPosition) => new GridObjectInventoryXY(g, gridPosition)); //в четвертом параметре аргумента зададим функцию ананимно через лямбду => new GridObjectUnitXZ(g, _gridPositioAnchor) И ПЕРЕДАДИМ ЕЕ ДЕЛЕГАТУ. (лямбда выражение можно вынести в отдельный метод)

            //gridSystem.CreateDebugObject(_gridDebugObjectPrefab); // Создадим наш префаб в каждой ячейки
            _gridSystemTiltedXYList.Add(gridSystem); // Добавим в список созданную сетку
        }
    }

    private void Start()
    {
        // Задодим размер и тайлинг материала заднего фона

        //_background.localPosition = new Vector3(_widthBag / 2f, _heightBag / 2f, 0) * _cellSize; // разместим задний фон посередине
        // _background.localScale = new Vector3(_widthBag, _heightBag, 0) * _cellSize;
        //_background.GetComponent<Material>().mainTextureScale = new Vector2(_widthBag, _heightBag);// Сдеалем количество тайлов равным количеству ячеек по высоте и ширине   

        /* foreach (GridParameters gridParameters in _gridParametersArray)
         {
             RectTransform bagBackground = (RectTransform)gridParameters.anchorGridTransform.GetChild(0); //Получим задний фон сетки
             bagBackground.sizeDelta = new Vector2(gridParameters.width, gridParameters.height);
             bagBackground.localScale = Vector3.one * gridParameters.cellSize;
             bagBackground.GetComponent<RawImage>().uvRect = new Rect(0, 0, gridParameters.width, gridParameters.height);   // Сдеалем количество тайлов равным количеству ячеек по высоте и ширине   
         }*/
    }


    public bool TryGetGridSystemGridPosition(Vector3 worldPositionMouse, out GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, out Vector2Int gridPositionMouse) //Попробуем Получим сеточную систему для заданной позиции Мыши и в случае удачи вернем ее и сеточную позицию
    {
        gridSystemXY = null;
        gridPositionMouse = new Vector2Int(0, 0);

        foreach (GridSystemTiltedXY<GridObjectInventoryXY> localGridSystem in _gridSystemTiltedXYList) // перберем список сеток
        {
            Vector2Int testGridPositionMouse = localGridSystem.GetGridPosition(worldPositionMouse); //для данной localGridSystem получим сеточную позицию мышки

            if (localGridSystem.IsValidGridPosition(testGridPositionMouse)) // Если тестируемая сеточная позиция допустима, вернем ее и саму сеточную систему
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

    public bool TryAddPlacedObjectAtGridPosition(Vector2Int gridPositionMouse, PlacedObject placedObject, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY)//Попробую Добавить Размещаемый объект в Позицию Сетки
    {
        List<Vector2Int> gridPositionList = placedObject.GetTryOccupiesGridPositionList(gridPositionMouse); // Получим список сеточных позиций которые хочет занять объект
        bool canPlace = true;
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            if (!gridSystemXY.IsValidGridPosition(gridPosition)) // Если есть хоть одна НЕ допустимая позиция то 
            {
                canPlace = false; // Разместить нельзя
                break;
            }
            if (gridSystemXY.GetGridObject(gridPosition).HasPlacedObject()) // Если хоть на одной позиции уже есть размещенный объект то
            {
                canPlace = false; // Разместить нельзя
                break;
            }
        }

        if (canPlace)//Если Можем разместить на сетке
        {
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                GridObjectInventoryXY gridObject = gridSystemXY.GetGridObject(gridPosition); // Получим GridObjectInventoryXY который находится в gridPosition
                gridObject.AddPlacedObject(placedObject); // Добавить Размещаемый объект 
            }    
            return true;
        }
        else
        {
            return false;
        }
    }

    public PlacedObject GetPlacedObjectAtGridPosition(Vector2Int gridPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) // Получить Размещаемый объект в этой сеточной позиции
    {
        GridObjectInventoryXY gridObject = gridSystemXY.GetGridObject(gridPosition); // Получим GridObjectInventoryXY который находится в gridPositionMouse
        return gridObject.GetPlacedObject();
    }

    public void RemovePlacedObjectAtGrid(PlacedObject placedObject) // Удаление Размещаемый объект из сетки (предпологается что он уже размещен в сетке)
    {
        List<Vector2Int> gridPositionList = placedObject.GetOccupiesGridPositionList(); // Получим список сеточных позиций которые занимает объект
        GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY = placedObject.GetGridSystemXY(); // Получим сетку на которой он размещен 
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            GridObjectInventoryXY gridObject = gridSystemXY.GetGridObject(gridPosition); // Получим GridObjectInventoryXY для сетки в которой он находится
            gridObject.RemovePlacedObject(placedObject); // удалим Размещаемый объект 
        }       
    }

    public List<GridSystemTiltedXY<GridObjectInventoryXY>> GetGridSystemTiltedXYList()
    {
        return _gridSystemTiltedXYList;
    }

    // Получить сеточную позицию
    public Vector2Int GetGridPosition(Vector3 worldPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetGridPosition(worldPosition);

    // Получим мировые координаты центра ячейки (относительно  нашей ***GridTransform)
    public Vector3 GetWorldPositionCenterСornerCell(Vector2Int gridPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetWorldPositionCenterСornerCell(gridPosition);

    // Получим мировые координаты нижнего левого угола ячейки (относительно  нашей ***GridTransform)
    public Vector3 GetWorldPositionLowerLeftСornerCell(Vector2Int gridPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetWorldPositionLowerLeftСornerCell(gridPosition);

    public Vector3 GetRotationAnchorGrid(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetRotationAnchorGrid();
    public Transform GetAnchorGrid(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetAnchorGrid();
    public int GetWidth(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetWidth();
    public int GetHeight(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) => gridSystemXY.GetHeight();
    public float GetCellSize() => _gridSystemTiltedXYList[0].GetCellSize(); // Для всех сеток масштаб ячейки одинвковый    

    /*public GridSystemTiltedXY<GridObjectInventoryXY> GetGridSystemTiltedXY(GridName gridName) // Получить сетку по имени
    {
        foreach (GridSystemTiltedXY<GridObjectInventoryXY> localGridSystem in _gridSystemTiltedXYList) // перберем список сеток
        {
            if (localGridSystem.GetGridName() == gridName)
            {
                return localGridSystem;
            }
        }

        return null;
    }
    public GridParameters[] GetGridParametersArray() //Получить Массив параметров сеток
    {
        return _gridParametersArray;
    }*/
}
