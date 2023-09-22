
using UnityEngine;
using static PlacedObjectTypeSO;


public class PickUpDropManager : MonoBehaviour // Поднятие Перетаскивание и Бросание объектовЫ
{
    public static PickUpDropManager Instance { get; private set; }

    [SerializeField] private LayerMask _inventoryLayerMask; // Для инвенторя настроить слой как Inventory
    [SerializeField] private LayerMask _mousePlaneLayerMask; // Настроить слой фона сетки и ячеек для оружия как MousePlane

    private Camera _mainCamera;
    private PlacedObject _placedObject; // Размещенный объект
    private Plane _plane; // плоскость по которой будем перемещять захваченные объекты
    private Vector3 _offset; // расстояние между точкой захвата и точкой  pivot на объекте.
    private PlacedObjectTypeSO _placedObjectTypeSO;
    private GridPosition _gridPosition;
    private PlacedObjectTypeSO.Dir _dir;

    private void Awake()
    {
        // Если ты акуратем в инспекторе то проверка не нужна
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
        _plane = new Plane(Vector3.forward, new Vector3(0, 1, 0)); // Создадим плоскость с нормалью по оси Z(для правильного определения расстояние вдоль луча), в начале координат и смещена по У на 1 т.к. все предметы инвенторя находятсяна У=1
    }

    private void Update()
    {
        if (InputManager.Instance.IsMouseButtonDownThisFrame()) // Была Нажата кнопка мыши в этот кадр
        {
            if (_placedObject == null) // Не имея при себе никакого предмета, попытайтесь схватить
            {

                Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); //Возвращает луч, идущий от камеры через точку экрана где находиться курсор мыши 
                if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, _inventoryLayerMask)) // Вернет true если попадет в инвертарь.
                {
                    _placedObject = raycastHit.transform.GetComponentInParent<PlacedObject>();
                    if (_placedObject != null) // Попробуем на объекте в который попали вернуть PlacedObject и сохраним его
                    {
                        _placedObject.Grab(); // Схватим его

                        // Временно т.к. объект пока создан дальше будем брать из кнопки
                       _placedObjectTypeSO = _placedObject.GetPlacedObjectTypeSO();


                        // вычислим смещение захвата
                        _plane.Raycast(ray, out float planeDistance); // Пересечем луч и плоскость и получим расстояние вдоль луча, где он пересекает плоскость.
                        Vector3 placedObjectPosition = _placedObject.transform.position;
                        _offset = placedObjectPosition - ray.GetPoint(planeDistance); // Вычислим смещение от точкой захвата и точкой  pivot на объекте.
                    }
                }
            }
            else // Бросим объект если чтото держим
            {
                _placedObject.Drop();
               
                _placedObject = null;
            }
        }

        if (_placedObject != null) // Если есть захваченный объект будем его перемещать за указателем мыши по созданной плоскости
        {
            Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());//Возвращает луч, идущий от камеры через точку экрана где находиться курсор мыши 
            _plane.Raycast(ray, out float planeDistance); // Пересечем луч и плоскость и получим расстояние вдоль луча, где он пересекает плоскость.
            Vector3 targetPosition = ray.GetPoint(planeDistance); // получим точку на луче где она пересекла плоскость

           

            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, _mousePlaneLayerMask)) // Если попали в ячейку инвенторя или в сетку то
            {
                _placedObject.SetTargetPosition(GetMouseWorldSnappedPosition() + _offset);
                _placedObject.SetOverGrid(true);
                Debug.Log("в сетке");
            }
            else
            {
                _placedObject.SetTargetPosition(targetPosition + _offset);
                _placedObject.SetOverGrid(false);
            }
        }
    }


    public static Vector3 GetMousePosition() // Получить позицию мыши (static обозначает что метод принадлежит классу а не кокому нибудь экземпляру) // При одноэтажной игре
    {
        Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); // Луч от камеры в точку на экране где находиться курсор мыши
        Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, Instance._mousePlaneLayerMask); // Instance._coverLayerMask - можно задать как смещение битов слоев 1<<6  т.к. mousePlane под 6 номером
        return raycastHit.point; // Если луч попадет в колайдер то Physics.Raycast будет true, и raycastHit.point вернет "Точку удара в мировом пространстве, где луч попал в коллайдер", а если false то можно вернуть какоенибудь другое нужное значение(в нашем случае вернет нулевой вектор).
    }


    public Vector3 GetMouseWorldSnappedPosition() // Зафиксируйте положение мыши в мире 
    {
        Vector3 mousePosition = GetMousePosition(); // Получить позицию при попадании
        GridPosition mouseGridPosition = InventoryGrid.Instance.GetGridPosition(mousePosition);

        if (_placedObjectTypeSO != null)
        {
            Vector2Int rotationOffset = _placedObjectTypeSO.GetRotationOffset(_dir); // Смещение объекта если он повернут
            Vector3 placedObjectWorldPosition = InventoryGrid.Instance.GetWorldPosition(mouseGridPosition) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * InventoryGrid.Instance.GetCellSize();
            return placedObjectWorldPosition; // Вернет зафиксированное положение в узлах сетки
        }
        else
        {
            return mousePosition;
        }
    }

    public Quaternion GetPlacedObjectRotation() // Получим вращение размещенного объекта
    {
        if (_placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, _placedObjectTypeSO.GetRotationAngle(_dir), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }



}
