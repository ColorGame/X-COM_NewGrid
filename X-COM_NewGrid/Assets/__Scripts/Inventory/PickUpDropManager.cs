
using System;
using System.Collections.Generic;
using UnityEngine;




public class PickUpDropManager : MonoBehaviour // Поднятие Перетаскивание и Бросание объектовЫ
{
    public static PickUpDropManager Instance { get; private set; }

    public event EventHandler<OnGrabbedObjectGridPositionChangedEventArgs> OnGrabbedObjectGridPositionChanged; // позиция захваченного объекта на сетке изменилась
    public class OnGrabbedObjectGridPositionChangedEventArgs : EventArgs // Расширим класс событий, чтобы в аргументе события передать
    {
        public GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY; // Сеточная система позиции мыши
        public Vector2Int newMouseGridPosition;  // Новая сеточная позиция мыши
        public PlacedObject placedObject; // размещаемы объект
    }

    public event EventHandler<PlacedObject> OnAddPlacedObjectAtGrid; // Объект добавлен в сетку 
    public event EventHandler<PlacedObject> OnRemovePlacedObjectAtGrid; // Объект удален из сетки
    public event EventHandler OnGrabbedObjectGridExits; // Захваченый объект покинул сетку

    [SerializeField] private LayerMask _inventoryLayerMask; // Для инвенторя настроить слой как Inventory // Настроим на объекте где есть коллайдер    
    [SerializeField] private Transform _canvasInventoryWorld;

    private PlacedObject _placedObject; // Размещенный объект    
    private Vector3 _offset; // Смещение от мышки
    private PlacedObjectTypeSO _placedObjectTypeSO;
    private PlacedObjectTypeSO.Dir _dir;
    private Camera _mainCamera;
    private Plane _plane; // плоскость по которой будем перемещять захваченные объекты    
    private Vector2Int _mouseGridPosition;  // сеточная позиция мыши
    private bool _startEventOnGrabbedObjectGridExits = false; // Запущено событие (Захваченый объект покинул сетку), чтобы не запускать событие каждый кадр сделал переключатель

    private void Awake()
    {
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one PickUpDropManager!(Там больше, чем один PickUpDropManager!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр PickUpDropManager прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        _plane = new Plane(_mainCamera.transform.forward, _canvasInventoryWorld.position); // Создадим плоскость перпендикулярно камере в позиции canvasInventory
    }

    private void Update()
    {
        if (InputManager.Instance.IsMouseButtonDownThisFrame()) // Если мыш нажата 
        {
            // Взять и положить объект можно только в сетке, проверим это           
            if (InventoryGrid.Instance.TryGetGridSystemGridPosition(GetMousePositionOnPlane(), out GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, out Vector2Int gridPositionMouse)) // Если над сеткой то попробуем получить ее
            {
                if (_placedObject == null) // Не имея при себе никакого предмета, попытайтесь схватить
                {
                    TryGrab();
                }
                else // Попытаемся сбросим объект на сетку
                {
                    TryDrop(gridSystemXY, gridPositionMouse);
                }
            }
            else //Если НЕ над сеткой 
            {
                if (_placedObject != null) // Если Есть захваченый объект 
                {
                    _placedObject.Drop();
                    _placedObject.SetMoveStartPosition(true); //то перенести объект в стартовое положение, уничтожить и вернуть его в список товаров
                    _placedObject = null;
                }
            }
        }

        if (_placedObject != null) // Если есть захваченный объект будем его перемещать за указателем мыши по созданной плоскости
        {
            SetTargetPosition();
        }
    }

    public void TryGrab()
    {
        Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); //Возвращает луч, идущий от камеры через точку экрана где находиться курсор мыши 
        if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, _inventoryLayerMask)) // Вернет true если попадет в инвертарь.
        {
            _placedObject = raycastHit.transform.GetComponentInParent<PlacedObject>();
            if (_placedObject != null) // Если у родителя объекта в который попали есть PlacedObject то можно его схватить (на кнопка висит просто визуал и там нет род объекта)
            {
                _placedObject.Grab(); // Схватим его
                InventoryGrid.Instance.RemovePlacedObjectAtGrid(_placedObject);// Удалим из текущей сеточной позиции
                _placedObjectTypeSO = _placedObject.GetPlacedObjectTypeSO();

                OnRemovePlacedObjectAtGrid?.Invoke(this, _placedObject); // Запустим событие
            }
        }
    }

    public void TryDrop(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, Vector2Int gridPositionMouse)
    {
        // Попробуем сбросить и разместить на сетке
        GridName gridName = gridSystemXY.GetGridName(); // Получим имя сетки
        switch (gridName)
        {
            case GridName.BagGrid1:
                if (InventoryGrid.Instance.TryAddPlacedObjectAtGridPosition(gridPositionMouse, _placedObject, gridSystemXY))
                {
                    _placedObject.Drop();  // Бросить
                    _placedObject.SetGridPositionAnchor(gridPositionMouse); // Установим новую сеточную позицию якоря
                    _placedObject.SetGridSystemXY(gridSystemXY); //Установим сетку на которую добавили наш оббъект

                    OnAddPlacedObjectAtGrid?.Invoke(this, _placedObject); // Запустим событие
                                                                          // Звук удачного размещения

                    _placedObject = null;
                }
                else
                {
                    TooltipUI.Instance.Show("не удалось разместить", new TooltipUI.TooltipTimer { timer = 2f }); // Покажем подсказку и зададим новый таймер отображения подсказки
                                                                                                                 // Звук неудачи
                }
                break;

            // для сетки Основного и Доп. оружия установим newMouseGridPosition (0,0)
            case GridName.MainWeaponGrid2:
            case GridName.OtherWeaponGrid3:
                if (InventoryGrid.Instance.TryAddPlacedObjectAtGridPosition(new Vector2Int(0, 0), _placedObject, gridSystemXY))
                {
                    _placedObject.Drop();  // Бросить
                    _placedObject.SetGridPositionAnchor(new Vector2Int(0, 0)); // Установим новую сеточную позицию якоря
                    _placedObject.SetGridSystemXY(gridSystemXY); //Установим сетку на которую добавили наш оббъект

                    OnAddPlacedObjectAtGrid?.Invoke(this, _placedObject); // Запустим событие
                                                                          // Звук удачного размещения

                    _placedObject = null;
                }
                else
                {
                    TooltipUI.Instance.Show("не удалось разместить", new TooltipUI.TooltipTimer { timer = 2f }); // Покажем подсказку и зададим новый таймер отображения подсказки

                    // Звук неудачи
                }
                break;
        }
    }

    public void SetTargetPosition()
    {
        Vector3 mousePositionOnPlane = GetMousePositionOnPlane();
        Vector2Int zeroGridPosition = new Vector2Int(0, 0); // Нулевая позиция сетки
        if (InventoryGrid.Instance.TryGetGridSystemGridPosition(mousePositionOnPlane, out GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY, out Vector2Int newMouseGridPosition)) // Если над сеткой то попробуем получить ее
        {
            GridName gridName = gridSystemXY.GetGridName(); // Получим имя сетки
            switch (gridName)
            {
                case GridName.BagGrid1:
                    _placedObject.SetTargetPosition(InventoryGrid.Instance.GetWorldPositionLowerLeftСornerCell(newMouseGridPosition, gridSystemXY));
                    _placedObject.SetTargetRotation(InventoryGrid.Instance.GetRotationAnchorGrid(gridSystemXY));

                    if (_mouseGridPosition != newMouseGridPosition || _mouseGridPosition == zeroGridPosition) // Если сеточная позиция не равна предыдущей или равна нулевой позиции то ...
                    {
                        OnGrabbedObjectGridPositionChanged?.Invoke(this, new OnGrabbedObjectGridPositionChangedEventArgs //запустим - Событие позиция мыши на сетке изменилось и передадим предыдущую и новою сеточную позицию
                        {
                            gridSystemXY = gridSystemXY,
                            newMouseGridPosition = newMouseGridPosition,
                            placedObject = _placedObject
                        }); // Создадим событие и передадим

                        _mouseGridPosition = newMouseGridPosition; // Перепишем предыдущую позицию на новую
                    }
                    break;

                // для сетки Основного и Доп. оружия установим TargetPosition в нулевой  gridPosition (0,0)
                case GridName.MainWeaponGrid2:
                case GridName.OtherWeaponGrid3:
                    
                    _placedObject.SetTargetPosition(InventoryGrid.Instance.GetWorldPositionLowerLeftСornerCell(zeroGridPosition, gridSystemXY));
                    _placedObject.SetTargetRotation(InventoryGrid.Instance.GetRotationAnchorGrid(gridSystemXY));

                    OnGrabbedObjectGridPositionChanged?.Invoke(this, new OnGrabbedObjectGridPositionChangedEventArgs //запустим - Событие позиция мыши на сетке изменилось и передадим предыдущую и новою сеточную позицию
                    {
                        gridSystemXY = gridSystemXY,
                        newMouseGridPosition = zeroGridPosition,
                        placedObject = _placedObject
                    }); // Создадим событие и передадим                       
                    break;
            }
            _startEventOnGrabbedObjectGridExits = false; // Сбросим параметр
        }
        else // Если не над сеткой то просто следуем за мышью
        {
            _placedObject.SetTargetPosition(mousePositionOnPlane + _offset);
            _placedObject.SetTargetRotation(Vector3.zero);

            _mouseGridPosition = zeroGridPosition; //Сбросим сеточную позицию - Когда объект покидает сетку (чтобы визуал отображался корректно, при вводе мыши ,в ту же область сетки с который вышла))

            if (!_startEventOnGrabbedObjectGridExits) // Если не запущено событие то запустим его
            {
                OnGrabbedObjectGridExits?.Invoke(this, EventArgs.Empty);
                _startEventOnGrabbedObjectGridExits= true;
            }

        }
    }

    public void CreatePlacedObject(Vector3 worldPosition, PlacedObjectTypeSO placedObjectTypeSO) //Создадим размещаемый объект при нажатии на кнопку(в аргументе получаем позицию и тип объекта) 
    {
        // Перед создание сделаем проверку
        // Vector2Int mouseGridPosition = InventoryGrid.Instance.GetGridPosition(GetMousePositionOnPlane());
        if (_placedObject != null) // Если Есть захваченый объект 
        {
            //Сбросим захваченный объект
            _placedObject.Drop();
            _placedObject.SetMoveStartPosition(true);
            _placedObject = null;
        }
        else
        {
            _placedObject = PlacedObject.CreateInWorld(worldPosition, PlacedObjectTypeSO.Dir.Down, placedObjectTypeSO, _canvasInventoryWorld); // Создадим объект
            _placedObject.Grab(); // Схватим его
            _offset = -_placedObject.GetOffsetVisualFromParent(); // Чтобы объект был по центру мышки надо вычесть смещение визуала относительно родителя
            _placedObjectTypeSO = _placedObject.GetPlacedObjectTypeSO();

        }
    }

    public Vector3 GetMousePositionOnPlane() // Получить позицию мыши на плоскости
    {
        Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());//Возвращает луч, идущий от камеры через точку экрана где находиться курсор мыши 
        _plane.Raycast(ray, out float planeDistance); // Пересечем луч и плоскость и получим расстояние вдоль луча, где он пересекает плоскость.
        return ray.GetPoint(planeDistance); // получим точку на луче где она пересекла плоскость
    }

    /*  public Vector3 CalculateOffsetGrab() // вычислим смещение захвата
      {
          Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); //Возвращает луч, идущий от камеры через точку экрана где находиться курсор мыши 
          _plane.Raycast(ray, out float planeDistance); // Пересечем луч и плоскость и получим расстояние вдоль луча, где он пересекает плоскость.
          return _placedObject.transform.position - ray.GetPoint(planeDistance); // Вычислим смещение от точкой захвата и точкой  pivot на объекте.        
      }

      public Vector3 GetMousePosition(LayerMask layerMask) // Получить позицию мыши (static обозначает что метод принадлежит классу а не кокому нибудь экземпляру) // При одноэтажной игре
      {
          Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); // Луч от камеры в точку на экране где находиться курсор мыши
          Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, layerMask);
          return raycastHit.point; // Если луч попадет в колайдер то Physics.Raycast будет true, и raycastHit.point вернет "Точку удара в мировом пространстве, где луч попал в коллайдер", а если false то можно вернуть какоенибудь другое нужное значение(в нашем случае вернет нулевой вектор).
      }

      public Vector3 GetMouseWorldSnappedPosition(Vector2Int mouseGridPosition, GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) // Получить Зафиксированное мировое положение мыши (над сеткой)
      {
          Vector2Int rotationOffset = _placedObjectTypeSO.GetRotationOffset(_dir); // Смещение объекта если он повернут
          Vector3 placedObjectWorldPosition = InventoryGrid.Instance.GetWorldPositionCenterСornerCell(mouseGridPosition, gridSystemXY) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * InventoryGrid.Instance.GetCellSize();
          return placedObjectWorldPosition; // Вернет зафиксированное положение в узлах сетки
      }

      public Quaternion GetPlacedObjectRotation() // Получим вращение размещенного объекта
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
