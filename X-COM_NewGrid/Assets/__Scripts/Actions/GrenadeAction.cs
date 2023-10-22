using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GrenadeAction : BaseAction // Граната ДЕйствие. Наследует BaseAction
                                                 // abstract - НЕ позволяет создать Instance (экземпляр) этого класса.
{
    public event EventHandler OnGrenadeActionStarted;     // Действие Бросок Гранаты Началось    
    public event EventHandler OnGrenadeActionCompleted;   // Действие Бросок Гранаты Закончилочь
    protected enum State
    {
        GrenadeBefore, //До Бросока Гранаты (подоготовка)
        GrenadeInstantiate, //Создание гранаты
        GrenadeAfter,  //После Бросока Гранаты
    }

    [SerializeField] protected LayerMask _obstaclesDoorMousePlaneLayerMask; //маска слоя препятствия и двери (появится в ИНСПЕКТОРЕ) НАДО ВЫБРАТЬ Obstacles и DoorInteract MousePlane(пол для нескольких этажей) и // ВАЖНО НА ВСЕХ СТЕНАХ В ИГРЕ УСТАНОВИТЬ МАСКУ СЛОЕВ -Obstacles, а на дверях -DoorInteract
    [SerializeField] protected Transform _grenadeSpawnTransform; // Трансформ создания гранаты // В префабе юнита закинуть префаб гранаты

    protected State _state; // Состояние юнита
    protected float _stateTimer; //Таймер состояния      


    protected int _maxThrowDistance = 5; //Максимальная дальность броска //НУЖНО НАСТРОИТЬ//
    protected GridPositionXZ _targetGridPositin;
    protected GrenadeProjectile _grenadeProjectile;
    protected HandleAnimationEvents _handleAnimationEvents; // Обработчик Анимационных событий

    protected override void Awake()
    {
        base.Awake();

        _handleAnimationEvents = GetComponentInChildren<HandleAnimationEvents>();
        _grenadeProjectile = GameAssets.Instance.grenadeProjectilePrefab.GetComponent<GrenadeProjectile>();
    }

    protected void Start()
    {
        _handleAnimationEvents.OnAnimationTossGrenadeEventStarted += _handleAnimationEvents_OnAnimationTossGrenadeEventStarted; // Подпишемся на событие "В анимации "Бросок гранаты" стартовало событие"
    }

    public abstract void _handleAnimationEvents_OnAnimationTossGrenadeEventStarted(object sender, EventArgs e); // abstract - вынуждает реализовывать в каждом подклассе и в базовом должно иметь пустое тело.
    public abstract int GetGrenadeDamage(); // повреждения от гранаты
    protected void Update()
    {
        if (!_isActive) // Если не активны то ...
        {
            return; // выходим и игнорируем код ниже
        }

        //Если оставить эту строку здесь то мы сможем кидать следующую гранаты не дождавшись пока первая граната долетит до цели (как с автомата пока не кончаться очки действия)
        //Поэтому данную функцию будем вызывать через делегат на самой гранате когда она взорвется
        //ActionComplete(); // 

        _stateTimer -= Time.deltaTime; // Запустим таймер для переключения состояний

        switch (_state) // Переключатель активурует кейс в зависимости от _state
        {
            case State.GrenadeBefore:

                Vector3 targetPositin = LevelGrid.Instance.GetWorldPosition(_targetGridPositin);
                Vector3 targetDirection = (targetPositin - transform.position).normalized; // Направление к целивой ячейки, еденичный вектор
                targetDirection.y = 0; // Чтобы юнит не наклонялся пли броске (т.к. вектор будет поворачиваться только по плоскости x,y)

                float rotateSpeed = 10f; //НУЖНО НАСТРОИТЬ//
                transform.forward = Vector3.Slerp(transform.forward, targetDirection, Time.deltaTime * rotateSpeed); // поворт юнита.                

                break;

            case State.GrenadeInstantiate:// Можно ЗДЕСЬ настроить создание ГРАНАТЫ (сейчас использую AnimationEvent)
                break;

            case State.GrenadeAfter: // Блок пустой но можно добавить анимацию попадания или промоха             
                break;
        }

        if (_stateTimer <= 0) // По истечению времени вызовим NextMusic() которая в свою очередь переключит состояние. Например - у меня было TypeGrenade.Aiming: тогда в case TypeGrenade.Aiming: переключу на TypeGrenade.Shooting;
        {
            NextState(); //Следующие состояние
        }
    }

    protected void NextState() //Автомат переключения состояний
    {
        switch (_state)
        {
            case State.GrenadeBefore:

                _state = State.GrenadeInstantiate;
                SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.GrenadeThrow);
                //float grenadeInstantiateStateTime = 0.5f; // Для избежания магических чисель введем переменную  Продолжительность Состояния Создание Гранаты //НУЖНО НАСТРОИТЬ// Можно ЗДЕСЬ настроить время создания ГРАНАТЫ (сейчас использую AnimationEvent)
                //_musicTimer = grenadeInstantiateStateTime;                               

                break;

            case State.GrenadeInstantiate:
                break;

            case State.GrenadeAfter:
                break;
        }

        //Debug.Log(_state);
    }

    public override List<GridPositionXZ> GetValidActionGridPositionList()// Получить Список Допустимых Сеточных Позиция для Действий // переопределим базовую функцию                                                                       
    {
        List<GridPositionXZ> validGridPositionList = new List<GridPositionXZ>();

        GridPositionXZ unitGridPosition = _unit.GetGridPosition(); // Получим позицию в сетке юнита

        for (int x = -_maxThrowDistance; x <= _maxThrowDistance; x++) // Юнит это центр нашей позиции с координатами unitGridPosition, поэтому переберем допустимые значения в условном радиусе _maxComboDistance
        {
            for (int z = -_maxThrowDistance; z <= _maxThrowDistance; z++)
            {
                for (int floor = -_maxThrowDistance; floor <= _maxThrowDistance; floor++)
                {

                    GridPositionXZ offsetGridPosition = new GridPositionXZ(x, z, floor); // Смещенная сеточная позиция. Где началом координат(0,0, 0-этаж) является сам юнит 
                    GridPositionXZ testGridPosition = unitGridPosition + offsetGridPosition; // Тестируемая Сеточная позиция

                    if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) // Проверим Является ли testGridPosition Допустимой Сеточной Позицией если нет то переходим к след циклу
                    {
                        continue; // continue заставляет программу переходить к следующей итерации цикла 'for' игнорируя код ниже
                    }

                    // Для области метания гранаты сделаем ромб а не квадрат
                    int testDistance = Mathf.Abs(x) + Mathf.Abs(z); // Сумма двух положительных координат сеточной позиции
                    if (testDistance > _maxThrowDistance) //Получим фигуру из ячеек в виде ромба // Если юнит в (0,0) то ячейка с координатами (5,4) уже не пройдет проверку 5+4>7
                    {
                        continue;
                    }

                    LevelGridNode levelGridNode = LevelGrid.Instance.GetGridNode(testGridPosition);

                    //если в этой позиции нет узла пути значит эта GridPositionXZ висит в воздухе  
                    if (levelGridNode == null)
                    {
                        continue; // Пропустим эту позицию
                    }

                    /*if (!PathfindingMonkey.Instance.HasPath(unitGridPosition, testGridPosition)) //Исключим сеточные позиции куда нельзя пройти или на них есть объект с тегом (Obstacles -Препятствия)  (позиции между юнитом и тестируемой позицией)
                    {
                        continue;
                    }*/

                    //МНОГО ЖРЕТ РЕСУРСОВ
                    /*int pathfindingDistanceMultiplier = 10; // множитель расстояния определения пути (в классе PathfindingMonkey задаем стоимость смещения по клетке и она равна прямо 10 по диогонали 14, поэтому умножем наш множитель на количество клеток)
                    if (PathfindingMonkey.Instance.GetPathLength(unitGridPosition, testGridPosition) > _maxThrowDistance * pathfindingDistanceMultiplier) //Исключим сеточные позиции - Если растояние до тестируемой клетки больше расстояния которое Юнит может преодолеть за один ход
                    {
                        // Длина пути слишком велика
                        continue;
                    }*/

                    // ПРОВЕРИМ НА возможность броска через препятствия 
                    Vector3 worldTestGridPosition = LevelGrid.Instance.GetWorldPosition(testGridPosition);   // Получим мировые координаты тестируемой сеточной позиции 
                    Vector3 unitWorldPosition = LevelGrid.Instance.GetWorldPosition(unitGridPosition); // Переведем в мировые координаты переданную нам сеточную позицию Юнита  
                    Vector3 grenadeDirection = (worldTestGridPosition - unitWorldPosition).normalized; //Нормализованный Вектор Направления броска Гранаты

                    float heightRaycast = 2f; // Высота выстрела луча (делаю высоким что бы игнорировать низкие препядствия т.к. через них можно бросать гранатой) Через препятствия выше 2,5м бросать грнату НЕЛЬЗЯ (высота потолка 3м)
                    if (Physics.Raycast(
                            unitWorldPosition + Vector3.up * heightRaycast,
                            grenadeDirection,
                            Vector3.Distance(unitWorldPosition, worldTestGridPosition),
                            _obstaclesDoorMousePlaneLayerMask)) // Если луч попал в препятствие то (Raycast -вернет bool переменную) Cover слой игнорируем т.к. через укрытия можно бросать гранатой
                    {
                        // Мы заблоктрованны препятствием
                        continue;
                    }

                   

                    /*//Исключим сеточные позиции которые висят в воздухе
                    if (PathfindingMonkey.Instance.GetGridPositionInAirList().Contains(testGridPosition))
                    {
                        continue;
                    }
*/

                    validGridPositionList.Add(testGridPosition); // Добавляем в список те позиции которые прошли все тесты

                    //Debug.Log(testGridPosition);
                }
            }
        }
        return validGridPositionList;
    }

    public override void TakeAction(GridPositionXZ gridPosition, Action onActionComplete)  // Переопределим TakeAction Выполнение действий . (Делегат onActionComplete - по завершении действия). в нашем случае делегату передаем функцию ClearBusy - очистить занятость
    {
        _state = State.GrenadeBefore; // Активируем состояние Подготовки ГРАНАТЫ
        float beforeGrenadeStateTime = 0.5f; //До ГРАНАТЫ.  Для избежания магических чисель введем переменную  Продолжительность Состояния подготовки перед ГРАНАТОЙ ..//НУЖНО НАСТРОИТЬ//
        _stateTimer = beforeGrenadeStateTime;

        _targetGridPositin = gridPosition; // Сохраним переданную нам сеточную позицию

        OnGrenadeActionStarted?.Invoke(this, EventArgs.Empty); // Запустим событие Действие Бросок Гранаты началось Подписчик UnitAnimator (в анимации есть event, мы подпишемся на него и в этот моммент будем создовать гранату)

        ActionStart(onActionComplete); // Вызовим базовую функцию СТАРТ ДЕЙСТВИЯ // Вызываем этот метод в конце после всех настроек т.к. в этом методе есть EVENT и он должен запускаться после всех настроек
    }
    protected void OnGrenadeBehaviorComplete() // Промежуточный метод который возвращает ActionComplete() . Хотя можно использовать ActionComplete() напрямую но можно запутаться в названиях
    {
        OnGrenadeActionCompleted?.Invoke(this, EventArgs.Empty); // Запустим событие Действие Бросок Гранаты закончилось Подписчик UnitAnimator
        ActionComplete(); // эта функция выполняет - Очистить занятость или стать свободным - активировать кнопки UI
    }

    public override int GetMaxActionDistance()//Раскроем 
    {
        return _maxThrowDistance;
    }

    public float GetDamageRadiusInWorldPosition() => _grenadeProjectile.GetDamageRadiusInWorldPosition(); // Сквозная функция
    public int GetDamageRadiusInCells() => _grenadeProjectile.GetDamageRadiusInCells(); // Сквозная функция


    public override int GetActionPointCost() // Переопределим базовую функцию // Получить Расход Очков на Действие (Стоимость действия)
    {
        return 2;
    }
    protected void OnDestroy()
    {
        _handleAnimationEvents.OnAnimationTossGrenadeEventStarted -= _handleAnimationEvents_OnAnimationTossGrenadeEventStarted; // Отпишемя от события чтобы не вызывались функции в удаленных объектах.
    }
}

// https://community.gamedev.tv/t/grenade-can-be-thrown-through-wall/205331 БРОСАНИЕ ГРАНАТЫ ЧЕРЕЗ СТЕНЫ