using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MouseWorld : MonoBehaviour // Класс отвечающий за положение курсора мыши
{

    public static MouseWorld Instance { get; private set; }   //(ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                              // instance - экземпляр, У нас будет один экземпляр MouseWorld можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.

    public static event EventHandler<OnMouseGridPositionChangedEventArgs> OnMouseGridPositionChanged; // Событие позиция мыши на сетке изменилось
    public class OnMouseGridPositionChangedEventArgs : EventArgs // Расширим класс событий, чтобы в аргументе события передать
    {
        public GridPosition lastMouseGridPosition; // Прошлая сеточная позиция мыши
        public GridPosition newMouseGridPosition;  // Новая сеточная позиция мыши
    }

    [SerializeField] private LayerMask _mousePlaneLayerMask; // маска слоя плоскости мыши (появится в ИНСПЕКТОРЕ)

   private GridPosition _mouseGridPosition;  // сеточная позиция мыши


    private void Awake()
    {
        // Если ты акуратем в инспекторе то проверка не нужна
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one MouseWorld!(Там больше, чем один MouseWorld!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр MouseWorld прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this;
    }

    private void Start()
    {
        _mouseGridPosition = LevelGrid.Instance.GetGridPosition(GetPositionOnlyHitVisible());  // Установим при старте сеточную позицию мышм // НЕЛЬЗЯ ВЫЗЫВАТЬ в Awake() т.к. в результате гонки возникает нулевая ошибка (кто проснется раньше InputManager или MouseWorld неизвестно)
    }

    // Для теста, светящий шар следует за курсором мыши.
    /*private void Update()
    {
        transform.position = MouseWorld.GetPosition(); // Так можно вызывать из ЛЮБОГО МЕСТА
    }*/

    private void Update()
    {
        GridPosition newMouseGridPosition = LevelGrid.Instance.GetGridPosition(GetPositionOnlyHitVisible()); // Получим новую сеточную позицию мыши
        if (LevelGrid.Instance.IsValidGridPosition(newMouseGridPosition) && _mouseGridPosition != newMouseGridPosition) // Если это ДОПУСТИМАЯ сеточная позиция и она не равна предыдущей то ...
        {
            OnMouseGridPositionChanged?.Invoke(this, new OnMouseGridPositionChangedEventArgs //запустим - Событие позиция мыши на сетке изменилось и передадим предыдущую и новою сеточную позицию
            {
                lastMouseGridPosition = _mouseGridPosition,
                newMouseGridPosition = newMouseGridPosition,

            }); // Создадим событие и передадим

            _mouseGridPosition = newMouseGridPosition; // Перепишем предыдущую позицию на новую
        }
    }

    public static Vector3 GetPosition() // Получить позицию (static обозначает что метод принадлежит классу а не кокому нибудь экземпляру) // При одноэтажной игре
    {
        Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); // Луч от камеры в точку на экране где находиться курсор мыши
        Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, Instance._mousePlaneLayerMask); // Instance._coverLayerMask - можно задать как смещение битов слоев 1<<6  т.к. mousePlane под 6 номером
        return raycastHit.point; // Если луч попадет в колайдер то Physics.Raycast будет true, и raycastHit.point вернет "Точку удара в мировом пространстве, где луч попал в коллайдер", а если false то можно вернуть какоенибудь другое нужное значение(в нашем случае вернет нулевой вектор).
    }

    public static Vector3 GetPositionOnlyHitVisible() // Получить позицию при попадании, только для видимых объектов (static обозначает что метод принадлежит классу а не кокому нибудь экземпляру) // В некоторых отдельных случаях при отключении видимости этажа пол становиться не видимым но колайдер остается активным, и тогда мы не можем кликнуть по позиции или юниту т.к. нашему лучу мешает коллайдер
    {
        Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); // Луч от камеры в точку на экране где находиться курсор мыши
        RaycastHit[] raycastHitArray = Physics.RaycastAll(ray, float.MaxValue, Instance._mousePlaneLayerMask); // Сохраним массив всех попаданий луча
        System.Array.Sort(raycastHitArray, (RaycastHit raycastHitA, RaycastHit raycastHitB) => // Отсортируем элементы в нашем одномерном массиве по растоянию от точки выстрела луча (т.к. они сохраняются рандомно)
        {
            return Mathf.RoundToInt(raycastHitA.distance - raycastHitB.distance); // Метод сортировки // Интерфейс IComparer Предоставляет метод, который сравнивает два объекта.
        });

        foreach (RaycastHit raycastHit in raycastHitArray) // Переберем наш полученный массив
        {
            if (raycastHit.transform.TryGetComponent(out Renderer renderer)) //Попробуем получить на объекте в который попал луч компонент Renderer
            {
                if (renderer.enabled) // и если он виден
                {
                    return raycastHit.point;// вернем "Точку удара в мировом пространстве, где луч попал в коллайдер"
                }
                // Если он НЕ виден просто игнорируем попадание в него
            }
        }

        // Если ни во что не попадет вернем ноль
        return Vector3.zero;
    }
}
