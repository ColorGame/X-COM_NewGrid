using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;


// НАСТРОИМ ПОРЯДОК ВЫПОЛНЕНИЯ СКРИПТА LevelGrid, добавим в Project Settings/ Script Execution Order и поместим выполнение LevelGrid выше Default Time, чтобы LevelGrid запустился РАНЬШЕ до того как ктонибудь совершит поиск пути ( В Start() мы запускаем класс PathfindingMonkey - настроику поиска пути)

public class LevelGrid : MonoBehaviour // Основной скрипт который управляет СЕТКОЙ данного УРОВНЯ . Оснавная задача Присвоить или Получить определенного Юнита К заданной Позиции Сетки
{

    public static LevelGrid Instance { get; private set; }   //(ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                             // instance - экземпляр, У нас будет один экземпляр LevelGrid можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.

    public const float FLOOR_HEIGHT = 3f; // Высота этажа в уровне - это высота стенок

    public event EventHandler<OnAnyUnitMovedGridPositionEventArgs> OnAnyUnitMovedGridPosition; //Запутим событие когда - Любой Юнит Перемещен в Сеточной позиции  // <OnPlacedObjectOverGridPositionEventArgs>- вариант передачи через событие нужные параметры

    public class OnAnyUnitMovedGridPositionEventArgs : EventArgs // Расширим класс событий, чтобы в аргументе события передать юнита и сеточные позиции
    {
        public Unit unit;
        public GridPositionXZ fromGridPosition;
        public GridPositionXZ toGridPosition;
    }


    [SerializeField] private Transform _gridDebugObjectPrefab; // Префаб отладки сетки //Передоваемый тип должен совподать с типом аргумента метода CreateDebugObject
    [SerializeField] private GridParameters _gridParameters;
    [SerializeField] private int _floorAmount = 2;// Количество Этажей

    private List<GridSystemXZ<GridObjectUnitXZ>> _gridSystemList; //Список сеточнах систем .В дженерик предаем тип GridObjectUnitXZ

    private void Awake()
    {
        // Если ты акуратем в инспекторе то проверка не нужна
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one LevelGrid!(Там больше, чем один LevelGrid!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр LevelGrid прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this;

        _gridSystemList = new List<GridSystemXZ<GridObjectUnitXZ>>(); // Инициализируем список

        for (int floor = 0; floor < _floorAmount; floor++) // Переберем этажи и на каждом построим сеточную систему
        {
            GridSystemXZ<GridObjectUnitXZ> gridSystem = new GridSystemXZ<GridObjectUnitXZ>(_gridParameters, // ПОСТРОИМ СЕТКУ 10 на 10 и размером 2 еденицы на этаже floor высотой 3  и в каждой ячейки создадим объект типа GridObjectUnitXZ
                 (GridSystemXZ<GridObjectUnitXZ> g, GridPositionXZ gridPosition) => new GridObjectUnitXZ(g, gridPosition), floor, FLOOR_HEIGHT); //в 5 параметре аргумента зададим функцию ананимно через лямбду => new GridObjectUnitXZ(g, _gridPositioAnchor) И ПЕРЕДАДИМ ЕЕ ДЕЛЕГАТУ. (лямбда выражение можно вынести в отдельный метод)

           // gridSystem.CreateDebugObject(_gridDebugObjectPrefab); // Создадим наш префаб в каждой ячейки // Закоментировал т.к. PathfindingGridDebugObject будет выполнять базовыедействия вместо _gridDebugObjectPrefab

            _gridSystemList.Add(gridSystem); // Добавим в список созданную сетку
        }
    }

    private void Start()
    {
        //  PathfindingMonkey.Instance.Setup(_gridParameters, _floorAmount); // ПОСТРОИМ СЕТКУ УЗЛОВ ПОИСКА ПУТИ // УБЕДИМСЯ ЧТО ЭТОТ МЕТОД СТАРТУЕТ РАНЬШЕ до того как ктонибудь совершит поиск пути
    }

    private GridSystemXZ<GridObjectUnitXZ> GetGridSystem(int floor) // Получить Сеточную систему для данного этажа
    {
        return _gridSystemList[floor];
    }


    public void AddUnitAtGridPosition(GridPositionXZ gridPosition, Unit unit) // Добавить определенного Юнита К заданной Позиции Сетки
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // Получим GridObjectUnitXZ который находится в _gridPositioAnchor
        gridObject.AddUnit(unit); // Добавить юнита 
    }

    public List<Unit> GetUnitListAtGridPosition(GridPositionXZ gridPosition) // Получить Список Юнитов В заданной Позиции Сетки
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // Получим GridObjectUnitXZ который находится в _gridPositioAnchor
        return gridObject.GetUnitList();// получим юнита
    }

    public void RemoveUnitAtGridPosition(GridPositionXZ gridPosition, Unit unit) // Удаление юнита из заданной позиции сетки
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // Получим GridObjectUnitXZ который находится в _gridPositioAnchor
        gridObject.RemoveUnit(unit); // удалим юнита
    }

    public void UnitMovedGridPosition(Unit unit, GridPositionXZ fromGridPosition, GridPositionXZ toGridPosition) // Юнит Перемещен в Сеточной позиции из позиции fromGridPosition в позицию toGridPosition
    {
        RemoveUnitAtGridPosition(fromGridPosition, unit); // Удалим юнита из прошлой позиции сетки

        AddUnitAtGridPosition(toGridPosition, unit);  // Добавим юнита к следующей позиции сетки

        OnAnyUnitMovedGridPosition?.Invoke(this, new OnAnyUnitMovedGridPositionEventArgs // создаем новый экземпляр класса OnPlacedObjectOverGridPositionEventArgs
        {
            unit = unit,
            fromGridPosition = fromGridPosition,
            toGridPosition = toGridPosition,

        }); // Запустим событие Любой Юнит Перемещен в Сеточной позиции ( в аргументе передадим Какой юнит Откуда и Куда)
    }

    public int GetFloor(Vector3 worldPosition) // Получить этаж
    {
        return Mathf.RoundToInt(worldPosition.y / FLOOR_HEIGHT); // Поделим позицию по у на высоту этажа и округлим до целого тем самым получим этаж
    }

    // Что бы не раскрывать внутриние компоненты LevelGrid (и не делать публичным поле_gridSystem) но предоставить доступ к GridPositionXZ сделаем СКВОЗНУЮ функцию для доступа к GridPositionXZ
    public GridPositionXZ GetGridPosition(Vector3 worldPosition) // вернуть сеточную позицию для мировых координат
    {
        int floor = GetFloor(worldPosition); // узнаем этаж
        return GetGridSystem(floor).GetGridPosition(worldPosition); // Для этого этажа вернем сеточную позицию
    }

    /// <summary>
    /// Получить узел сетки - Если знаем, что координата находится внутри сетки, и хотим максимизировать производительность, то 
    /// будем напрямую искать узел во внутреннем массиве, что немного быстрее.
    /// </summary>
    public LevelGridNode GetGridNode(GridPositionXZ gridPosition) // Получить узел сетки (A* Pathfinding Project4.2.18) для нашей сеточной позиции ()
    {
        int width = AstarPath.active.data.layerGridGraph.width;
        int depth = AstarPath.active.data.layerGridGraph.depth;
        /*int layerCount = AstarPath.active.data.layerGridGraph.LayerCount;
        float nodeSize = AstarPath.active.data.layerGridGraph.nodeSize;     */

        LevelGridNode gridNode = (LevelGridNode)AstarPath.active.data.layerGridGraph.nodes[gridPosition.x + gridPosition.z * width + gridPosition.floor * width * depth];

        /*// Проверял совпадение GraphNode и GridPositionXZ  
        Debug.Log("x " + _gridPositioAnchor.x + "y " + _gridPositioAnchor.y +"Floor " + _gridPositioAnchor.floor + 
            " node" + " x " + gridNode.XCoordinateInGrid + "y " + gridNode.ZCoordinateInGrid +"Layer " + gridNode.LayerCoordinateInGrid );*/
        return gridNode;
    }

    /// <summary>
    /// True, если узел доступен для прохода.
    /// </summary>
    public bool WalkableNode(GridPositionXZ gridPosition)
    {
       return GetGridNode(gridPosition).Walkable;
    }

    public Vector3 GetWorldPosition(GridPositionXZ gridPosition) => GetGridSystem(gridPosition.floor).GetWorldPosition(gridPosition); // Сквозная функция

    public bool IsValidGridPosition(GridPositionXZ gridPosition) // Является ли Допустимой Сеточной Позицией
    {
        if (gridPosition.floor < 0 || gridPosition.floor >= _floorAmount) // выходим за пределы наших этажей
        {
            return false;
        }
        else
        {
            return GetGridSystem(gridPosition.floor).IsValidGridPosition(gridPosition); // Сквозная функция для получения доступа к IsValidGridPosition из _gridSystemTiltedXYList
        }

    }
    public int GetWidth() => GetGridSystem(0).GetWidth(); // Все этажи имеют одинаковую форму поэто му берем 0 этаж
    public int GetHeight() => GetGridSystem(0).GetHeight();
    public float GetCellSize() => GetGridSystem(0).GetCellSize();
    public int GetFloorAmount() => _floorAmount;

    public bool HasAnyUnitOnGridPosition(GridPositionXZ gridPosition) // Есть ли какой нибудь юнит на этой сеточной позиции
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // Получим GridObjectUnitXZ который находится в _gridPositioAnchor
        return gridObject.HasAnyUnit();
    }
    public Unit GetUnitAtGridPosition(GridPositionXZ gridPosition) // Получить Юнита в этой сеточной позиции
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // Получим GridObjectUnitXZ который находится в _gridPositioAnchor
        return gridObject.GetUnit();
    }


    // IInteractable Интерфейс Взаимодействия - позволяет в классе InteractAction взаимодействовать с любым объектом (дверь, сфера, кнопка...) - который реализует этот интерфейс
    public IInteractable GetInteractableAtGridPosition(GridPositionXZ gridPosition) // Получить Интерфейс Взаимодействия в этой сеточной позиции
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // Получим GridObjectUnitXZ который находится в _gridPositioAnchor
        return gridObject.GetInteractable();
    }
    public void SetInteractableAtGridPosition(GridPositionXZ gridPosition, IInteractable interactable) // Установить полученный Интерфейс Взаимодействия в этой сеточной позиции
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // Получим GridObjectUnitXZ который находится в _gridPositioAnchor
        gridObject.SetInteractable(interactable);
    }
    public void ClearInteractableAtGridPosition(GridPositionXZ gridPosition) // Очистить Интерфейс Взаимодействия в этой сеточной позиции
    {
        GridObjectUnitXZ gridObject = GetGridSystem(gridPosition.floor).GetGridObject(gridPosition); // Получим GridObjectUnitXZ который находится в _gridPositioAnchor
        gridObject.ClearInteractable(); // Очистить Интерфейс Взаимодействия в эточ сеточном объекте
    }

}
