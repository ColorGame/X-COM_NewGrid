using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Добавим InventoryGridSystemVisual для запуска после времени по умолчанию, поскольку мы хотим, чтобы визуальные эффекты запускались после всего остального.
// (Project Settings/ Script Execution Order и поместим выполнение InventoryGridSystemVisual НИЖЕ Default Time)
public class InventoryGridSystemVisual : MonoBehaviour // Сеточная система визуализации инвенторя
{
    public static InventoryGridSystemVisual Instance { get; private set; }

    [Serializable] // Чтобы созданная структура могла отображаться в инспекторе
    public struct GridVisualTypeMaterial    //Визуал сетки Тип Материала // Создадим структуру можно в отдельном классе. Наряду с классами структуры представляют еще один способ создания собственных типов данных в C#
    {                                       //В данной структуре объединям состояние сетки с материалом
        public GridVisualType gridVisualType;
        public Material materialGrid;
    }

    public enum GridVisualType //Визуальные состояния сетки
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

    [SerializeField] private List<GridVisualTypeMaterial> _gridVisualTypeMaterialList; // Список тип материала визуального состояния сетки Квадрат (Список из кастомного типа данных) визуального состояния сетки // В инспекторе под каждое состояние перетащить соответствующий материал сетки

    private InventoryGridSystemVisualSingle[][,] _inventoryGridSystemVisualSingleArray; // Массив масивов [количество сеток][длина (Х), высота (У)]
    private Dictionary<GridName, int> _gridNameIndexDictionary; // Словарь (GridName - ключ, int(индекс) -значение)


    private void Awake() //Для избежания ошибок Awake Лучше использовать только для инициализации и настроийки объектов
    {
        // Если ты акуратем в инспекторе то проверка не нужна
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one InventoryGridSystemVisual!(Там больше, чем один InventoryGridSystemVisual!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр InventoryGridSystemVisual прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this;
    }

    private void Start()
    {
        _gridNameIndexDictionary = new Dictionary<GridName, int>();

        // Инициализируем сначала первую часть массива - Количество сеток
        List<GridSystemTiltedXY<GridObjectInventoryXY>> gridSystemTiltedXYList = InventoryGrid.Instance.GetGridSystemTiltedXYList(); // Получим список сеток
        _inventoryGridSystemVisualSingleArray = new InventoryGridSystemVisualSingle[gridSystemTiltedXYList.Count][,];

        // Для каждой сетки реализуем двумерный массив координат
        for (int i = 0; i < gridSystemTiltedXYList.Count; i++)
        {
            _inventoryGridSystemVisualSingleArray[i] = new InventoryGridSystemVisualSingle[gridSystemTiltedXYList[i].GetWidth(), gridSystemTiltedXYList[i].GetHeight()];
        }


        for (int i = 0; i < gridSystemTiltedXYList.Count; i++) // переберем все сетки
        {
            for (int x = 0; x < gridSystemTiltedXYList[i].GetWidth(); x++) // для каждой сетки переберем длину
            {
                for (int y = 0; y < gridSystemTiltedXYList[i].GetHeight(); y++)  // и высоту
                {
                    Vector2Int gridPosition = new Vector2Int(x, y); // позиция сетки
                    Vector3 rotation = InventoryGrid.Instance.GetRotationAnchorGrid(gridSystemTiltedXYList[i]);
                    Transform AnchorGridTransform = InventoryGrid.Instance.GetAnchorGrid(gridSystemTiltedXYList[i]);

                    Transform gridSystemVisualSingleTransform = Instantiate(GameAssets.Instance.inventoryGridSystemVisualSinglePrefab, InventoryGrid.Instance.GetWorldPositionCenterСornerCell(gridPosition, gridSystemTiltedXYList[i]), Quaternion.Euler(rotation), AnchorGridTransform); // Создадим наш префаб в каждой позиции сетки

                    _gridNameIndexDictionary[gridSystemTiltedXYList[i].GetGridName()] = i; // Присвоим ключу(имя Сетки под этим индексом) значение (индекс массива)
                    _inventoryGridSystemVisualSingleArray[i][x, y] = gridSystemVisualSingleTransform.GetComponent<InventoryGridSystemVisualSingle>(); // Сохраняем компонент LevelGridSystemVisualSingle в трехмерный массив где x,y,y это будут индексы массива.
                }
            }
        }

        PickUpDropManager.Instance.OnAddPlacedObjectAtGrid += PickUpDropManager_OnAddPlacedObjectAtGrid;
        PickUpDropManager.Instance.OnRemovePlacedObjectAtGrid += PickUpDropManager_OnRemovePlacedObjectAtGrid;
        PickUpDropManager.Instance.OnGrabbedObjectGridPositionChanged += PickUpDropManager_OnGrabbedObjectGridPositionChanged;
        PickUpDropManager.Instance.OnGrabbedObjectGridExits += PickUpDropManager_OnGrabbedObjectGridExits;
    }

    // Захваченый объект покинул сетку
    private void PickUpDropManager_OnGrabbedObjectGridExits(object sender, EventArgs e)  // Захваченый объект покинул сетку
    {
        SetDefaultState(); // Установим дефолтное состояние всех сеток
    }

    // позиция захваченного объекта на сетке изменилась
    private void PickUpDropManager_OnGrabbedObjectGridPositionChanged(object sender, PickUpDropManager.OnGrabbedObjectGridPositionChangedEventArgs e)
    {
        SetDefaultState(); // Установим дефолтное состояние всех сеток
        ShowPossibleGridPositions(e.gridSystemXY, e.placedObject, e.newMouseGridPosition, GridVisualType.Yellow); //показать возможные сеточные позиции
    }

    // Объект удален из сетки
    private void PickUpDropManager_OnRemovePlacedObjectAtGrid(object sender, PlacedObject placedObject)
    {
        SetMaterialAndIsBusy(placedObject, GridVisualType.Yellow, false);
    }

    // Объект добавлен в сетку 
    private void PickUpDropManager_OnAddPlacedObjectAtGrid(object sender, PlacedObject placedObject)
    {
        SetMaterialAndIsBusy(placedObject, GridVisualType.Blue, true);
    }

    private void SetDefaultState() // Установить дефолтное состояние сеток
    {
        List<GridSystemTiltedXY<GridObjectInventoryXY>> gridSystemTiltedXYList = InventoryGrid.Instance.GetGridSystemTiltedXYList(); // Получим список сеток
        for (int i = 0; i < gridSystemTiltedXYList.Count; i++) // переберем все сетки
        {
            for (int x = 0; x < gridSystemTiltedXYList[i].GetWidth(); x++) // для каждой сетки переберем длину
            {
                for (int y = 0; y < gridSystemTiltedXYList[i].GetHeight(); y++)  // и высоту
                {
                    if (!_inventoryGridSystemVisualSingleArray[i][x, y].GetIsBusy()) // Если позиция не занята
                    {
                        _inventoryGridSystemVisualSingleArray[i][x, y].SetMaterial(GetGridVisualTypeMaterial(GridVisualType.White));
                    }
                }
            }
        }
    }

    private void SetMaterialAndIsBusy(PlacedObject placedObject, GridVisualType gridVisualType, bool isBusy)
    {
        GridSystemTiltedXY<GridObjectInventoryXY> gridSystemTiltedXY = placedObject.GetGridSystemXY(); // Сеточная система которую занимает объект
        List<Vector2Int> OccupiesGridPositionList = placedObject.GetOccupiesGridPositionList(); // Список занимаемых сеточных позиций
        GridName gridName = gridSystemTiltedXY.GetGridName(); // Имя сетки
        int index = _gridNameIndexDictionary[gridName]; //получу из словоря Индекс сетки в _inventoryGridSystemVisualSingleArray

        foreach (Vector2Int gridPosition in OccupiesGridPositionList) // Переберем заниммаемые объектом позиции сетки
        {
            // для занятых ячеек установим красный цвет и сделаем занятыми
            _inventoryGridSystemVisualSingleArray[index][gridPosition.x, gridPosition.y].SetMaterial(GetGridVisualTypeMaterial(gridVisualType));
            _inventoryGridSystemVisualSingleArray[index][gridPosition.x, gridPosition.y].SetIsBusy(isBusy);
        }
    }

    // показать возможные сеточные позиции
    private void ShowPossibleGridPositions(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, PlacedObject placedObject, Vector2Int mouseGridPosition, GridVisualType gridVisualType ) 
    {
        GridName gridName = gridSystemXY.GetGridName(); // Имя сетки где находиться мыш с захваченным объектом
        int index = _gridNameIndexDictionary[gridName]; //получу из словоря Индекс сетки в _inventoryGridSystemVisualSingleArray
        List<Vector2Int> TryOccupiesGridPositionList = placedObject.GetTryOccupiesGridPositionList(mouseGridPosition); // Список сеточных позиций которые хотим занять
        foreach (Vector2Int gridPosition in TryOccupiesGridPositionList) // Переберем список позиций которые хоти занять
        {
            if (gridSystemXY.IsValidGridPosition(gridPosition)) // Если позиция допустима то...
            {
                if (!_inventoryGridSystemVisualSingleArray[index][gridPosition.x, gridPosition.y].GetIsBusy()) // Если позиция не занята
                {
                    _inventoryGridSystemVisualSingleArray[index][gridPosition.x, gridPosition.y].SetMaterial(GetGridVisualTypeMaterial(gridVisualType));
                }
            }
        }
    }

    

    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType) //(Вернуть Материал в зависимости от Состояния) Получить Тип Материала для Сеточной Визуализации в зависимости от переданного в аргумент Состояния Сеточной Визуализации
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in _gridVisualTypeMaterialList) // в цикле переберем Список тип материала визуального состояния сетки 
        {
            if (gridVisualTypeMaterial.gridVisualType == gridVisualType) // Если  Состояние сетки(gridVisualType) совподает с переданным нам состояние то ..
            {
                return gridVisualTypeMaterial.materialGrid; // Вернем материал соответствующий данному состоянию сетки
            }
        }

        Debug.LogError("Не смог найти GridVisualTypeMaterial для GridVisualType " + gridVisualType); // Если не найдет соответсвий выдаст ошибку
        return null;
    }
}
