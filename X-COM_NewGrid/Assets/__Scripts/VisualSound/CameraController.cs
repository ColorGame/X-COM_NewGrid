using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TMPro;
using Unity.VisualScripting;

public class CameraController : MonoBehaviour // В дальнейшем можно сделать рефакторинг и добавить InputSystem
{

    public static CameraController Instance { get; private set; }   //(ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                                    // instance - экземпляр, У нас будет один экземпляр LevelGrid можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.


    private const float MIN_FOLLOW_Y_OFFSET = 2f;
    private const float MAX_FOLLOW_Y_OFFSET = 15f;

    [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;
    [SerializeField] private Collider cameraBoundsCollider; //Ссылка на коллайдер который ограничивает виртуальную камеру

    private CinemachineTransposer _cinemachineTransposer;
    private Vector3 _targetFollowOffset; // Целевое Смещение следования       
    private bool _edgeScrolling; // прокрутка по краям    
    private Unit _targetUnit;
    private bool _targetCompleted = true; // Цель достигнута 

    private float _moveSpeed = 10f; // Скорость камеры
    private float _rotationSpeed = 100f;
    private float _zoomSpeed = 5f;

    private void Awake()
    {
        // Если ты акуратем в инспекторе то проверка не нужна
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one CameraController!(Там больше, чем один CameraController!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр CameraController прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this;

        _edgeScrolling = PlayerPrefs.GetInt("edgeScrolling", 1) == 1; // Загрузим сохраненый параметр _edgeScrolling и если это 1 то будет истина если не=1 то будет ложь (из PlayerPrefs.GetInt нельзя тегать булевые параметры поэтому используем строку)
    }

    private void Start()
    {
        _cinemachineTransposer = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>(); // Получим и сохраним компонент CinemachineTransposer из виртуальной камеры, чтобы в дальнейшем изменять ее параметры для ZOOM камеры

        _targetFollowOffset = _cinemachineTransposer.m_FollowOffset; // Смещение следования

        FriendlyUnitButtonUI.OnAnyFriendlyUnitButtonPressed += FriendlyUnitButtonUI_OnAnyFriendlyUnitButtonPressed; //подпишемся Нажата любая кнопка дружественного юнита 
        EnemyUnitButtonUI.OnAnyEnemylyUnitButtonPressed += EnemyUnitButtonUI_OnAnyEnemylyUnitButtonPressed; //подпишемся Нажата любая кнопка вражественного юнита
    }

    private void EnemyUnitButtonUI_OnAnyEnemylyUnitButtonPressed(object sender, Unit targetUnit)
    {
        _targetUnit = targetUnit;
        _targetCompleted = false;
    }

    private void FriendlyUnitButtonUI_OnAnyFriendlyUnitButtonPressed(object sender, Unit targetUnit)
    {
        _targetUnit = targetUnit;
        _targetCompleted = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();

        MoveSelectedUnit();
    }

    private void HandleMovement() // Ручное движение
    {
        Vector2 inputMoveDirection = InputManager.Instance.GetCameraMoveVector(); // Направление вводимого движенияи (обнуляем перед каждой трансформащией)

        if (_edgeScrolling) // Если прокрутка по краям активированна то считаем позиции мыши
        {
            Vector2 mousePosition = InputManager.Instance.GetMouseScreenPosition(); 
            float edgeScrollingSize = 0; // (количество пикселей) Отступ от края экрана где начинается движение камеры
            if (mousePosition.x > Screen.width - edgeScrollingSize) // если мыш больше высоты экран - отступ от края
            {
                inputMoveDirection.x = +1f;
            }
            if (mousePosition.x < edgeScrollingSize)
            {
                inputMoveDirection.x = -1f;
            }
            if (mousePosition.y > Screen.height - edgeScrollingSize)
            {
                inputMoveDirection.y = +1f;
            }
            if (mousePosition.y < edgeScrollingSize)
            {
                inputMoveDirection.y = -1f;
            }
        }               

        //Чтобы Движение учитывало вращение преобразуем вектор inputMoveDirection в moveVector
        Vector3 moveVector = transform.forward * inputMoveDirection.y + transform.right * inputMoveDirection.x; // Применим локальное смещение. Локальным вектор forward(z) изменим на inputMoveDirection.y, а Локальным вектор right(x) изменим на inputMoveDirection.x
        Vector3 targetPosition = transform.position + moveVector * _moveSpeed * Time.deltaTime; //расчитаем целевую позицию в которую хотим  переместить наш объект

        //ограничим движение
        targetPosition.x = Mathf.Clamp(targetPosition.x,
            cameraBoundsCollider.bounds.min.x ,
            cameraBoundsCollider.bounds.max.x);
        targetPosition.z = Mathf.Clamp(targetPosition.z,
            cameraBoundsCollider.bounds.min.z ,
            cameraBoundsCollider.bounds.max.z );

       // Debug.Log( cameraBoundsCollider.bounds.min);
       transform.position = targetPosition; // Переместим в расчитаную позицию
    }

    private void HandleRotation() // Ручной поворот
    {
        Vector3 rotationVector = new Vector3(0, 0, 0); // Вектор вращения // Будем вращать только вокруг оси Y (обнуляем перед каждой трансформащией)

        rotationVector.y = InputManager.Instance.GetCameraRotateAmount(); //Получить величину поворота камеры по ост У

        transform.eulerAngles += rotationVector * _rotationSpeed * Time.deltaTime;
        //Еще один способ
        //transform .Rotate(rotationVector, _rotationSpeed * Time.deltaTime);
    }

    private void HandleZoom() // Ручное масштабирование
    {
        //Debug.Log(InputManager.Instance.GetCameraZoomAmount()); // Отладка чтобы вивдить вводимые данные

        float zoomIncreaseAmount = 1f; //Масштаб величины увеличение (скорость увеличения)

        _targetFollowOffset.y += InputManager.Instance.GetCameraZoomAmount() * zoomIncreaseAmount; // Получить величину увеличения камеры

        // Мы не используем Time.deltaTime т.к. фиксируем лиш изминение величины прокрутки колесика и не учитываем ее величину (тоже самое что и фиксировать лиш нажатие клавиши например Input.GetKeyDown)

        _targetFollowOffset.y = Mathf.Clamp(_targetFollowOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);// ограничим значения масштабирования
      
        _cinemachineTransposer.m_FollowOffset = Vector3.Lerp(_cinemachineTransposer.m_FollowOffset, _targetFollowOffset, Time.deltaTime * _zoomSpeed); // Загружаем наши измененые значения, Для плавности используем Lerp
    }

    private void MoveSelectedUnit()
    {
        if (!_targetCompleted) // Если цель НЕ достигнута то выполняем движенее к ней
        {
            transform.position = Vector3.Lerp(transform.position, _targetUnit.transform.position, Time.deltaTime * _moveSpeed);

            float stoppingDistance = 0.2f; // Дистанция остановки //НУЖНО НАСТРОИТЬ//
            if (Vector3.Distance(transform.position, _targetUnit.transform.position) < stoppingDistance)  // Если растояние до целевой позиции меньше чем Дистанция остановки // Мы достигли цели        
            {
                _targetCompleted = true;
            }
        }
    }

    public float GetCameraHeight() // Получить высоту камеры смещения
    {
        return _targetFollowOffset.y;
    }

    public void SetEdgeScrolling(bool edgeScrolling) // Установить булевое значение для - прокрутки по краям
    {
        this._edgeScrolling = edgeScrolling;
        PlayerPrefs.SetInt("edgeScrolling", edgeScrolling ? 1 : 0); // Сохраним полученное значение в память (если _edgeScrolling истина то установим 1 если ложь установим 0 )
    }

    public bool GetEdgeScrolling() // Вернуть булевое значение для - прокрутки по краям
    {
        return _edgeScrolling;
    }

}
