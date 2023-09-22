using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlacedObjectTypeSO;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class InventoryGrid : MonoBehaviour // Сетка инверторя
{
    public static InventoryGrid Instance { get; private set; }

    public event EventHandler<OnAnyInventoryMovedGridPositionEventArgs> OnAnyInventoryMovedGridPosition; //Любой Инвентарь Перемещен в Сеточной позиции  // <OnAnyInventoryMovedGridPositionEventArgs>- вариант передачи через событие нужные параметры

    public class OnAnyInventoryMovedGridPositionEventArgs : EventArgs // Расширим класс событий, чтобы в аргументе события передать инвентарь и сеточные позиции
    {
        public PlacedObject placedObject; // размещенный объект типа SO List
        public GridPosition fromGridPosition;
        public GridPosition toGridPosition;
    }


    [SerializeField] private Transform _gridDebugObjectPrefab; // Префаб отладки сетки //Передоваемый тип должен совподать с типом аргумента метода CreateDebugObject
    [SerializeField] private int _width = 10;     // Ширина
    [SerializeField] private int _height = 10;    // Высота
    [SerializeField] private float _cellSize = 1f;// Размер ячейки
    [SerializeField] private Transform _background;

    private MouseEnterExitEventsUI _mouseEnterExitEvents; // Лежит на заднем фоне
    private Vector3 _globalOffset = Vector3.zero; // Смещение сетки в мировых координатах
    private GridSystem<GridObject> _gridSystem;
    private PlacedObject _placedObject;
    

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

        _globalOffset = transform.position + new Vector3(_cellSize / 2, 0, _cellSize / 2); // Сделаем чтобы нижиний левый угол сетки совподал с InventoryGrid.transform.position (для удобства создания сетки, достаточно переместить объект InventoryGrid в нужное место)

        _gridSystem = new GridSystem<GridObject>(_width, _height, _cellSize, _globalOffset,  // ПОСТРОИМ СЕТКУ 10 на 10 и размером 2 еденицы и в каждой ячейки создадим объект типа GridObject
             (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition)); //в четвертом параметре аргумента зададим функцию ананимно через лямбду => new GridObject(g, _gridPosition) И ПЕРЕДАДИМ ЕЕ ДЕЛЕГАТУ. (лямбда выражение можно вынести в отдельный метод)
        _placedObject = null;
        //gridSystem.CreateDebugObject(_gridDebugObjectPrefab); // Создадим наш префаб в каждой ячейки // Закоментировал т.к. PathfindingGridDebugObject будет выполнять базовыедействия вместо _gridDebugObjectPrefab

        // Задодим размер и тайлинг материала заднего фона
        _background.localPosition = new Vector3(_width / 2f, 0, _height / 2f);
        _background.localScale = new Vector3(_width, _height, 1);
        _background.GetComponent<Renderer>().material.mainTextureScale = new Vector2(_width, _height);

        
    }    


    public Transform GetGridBackground() // Получить фон сетки
    {
        return _background;
    }

    public GridPosition GetGridPosition(Vector3 worldPosition) => _gridSystem.GetGridPosition(worldPosition);   
    public Vector3 GetWorldPosition(GridPosition gridPosition) => _gridSystem.GetWorldPosition(gridPosition); // Сквозная функция

    public int GetWidth() => _gridSystem.GetWidth(); // Все этажи имеют одинаковую форму поэто му берем 0 этаж
    public int GetHeight() => _gridSystem.GetHeight();
    public float GetCellSize() => _gridSystem.GetCellSize();
}
