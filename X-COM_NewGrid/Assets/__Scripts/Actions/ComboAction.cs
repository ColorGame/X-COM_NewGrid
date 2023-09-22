using Pathfinding;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class ComboAction : BaseAction // Комбо // Действие могут выполнить толька два соседних юнита одновременно
{
    // static - обозначает что event будет существовать для всего класса не зависимо от того скольго у нас созданно Юнитов.                                                                                
    public static event EventHandler<OnComboEventArgs> OnAnyUnitComboStateChanged; // У любого юнита изменилось состояние Комбо         
    public class OnComboEventArgs : EventArgs // Расширим класс событий, чтобы в аргументе события передать нужных юнитов
    {
        public Unit partnerUnit; // Юнит партнер на котором надо изменить состояние
        public State state; // Состояние
    }

    public static event EventHandler OnAnyUnitStunComboAction; // Любой Юнит Оглушен в Комбо Действии

    public event EventHandler<Unit> OnComboActionStarted;     // Действие Комбо Началось 
    public event EventHandler<Unit> OnComboActionCompleted;   // Действие Комбо Закончилочь 

    public enum State
    {
        ComboSearchPartner, //Поиск партнера для комбо
        ComboSearchEnemy,   //Поиск Врага
        ComboStart,         //Старт комбо
        ComboAfter,         //После Комбо
    }

    [SerializeField] private LayerMask _obstaclesDoorMousePlaneCoverLayerMask; //маска слоя препятствия НАДО ВЫБРАТЬ Obstacles и DoorInteract и MousePlane(пол) Cover// ВАЖНО НА ВСЕХ СТЕНАХ В ИГРЕ УСТАНОВИТЬ МАСКУ СЛОЕВ -Obstacles, а на дверях -DoorInteract //для полов верхних этажей поменять колайдер на Box collider иначе снизу можно будет простреливать верхний этаж 
   
    private State _state; // Состояние юнита
    private float _stateTimer; //Таймер состояния
    private Unit _unitPartner; // Юнит партнер с которым будем делать Комбо
    private Unit _unitEnemy;  // Юнит ВРАГ    
    private GridPosition _targetPointEnemyGridPosition; // Точка перемещения врага
    private Transform _instantiateFXPrefab; // Созданный префаб частичек
    private RopeRanderer _ropeRandererUnit;
    private RopeRanderer _ropeRandererParten;

    private int _searchEnemyPointCost = 2; // Стоимость поиска врага
    private int _maxComboPartnerDistance = 1; //Максимальная дистанция Комбо Для Поиска Партнера и Перетаскивания Врага//НУЖНО НАСТРОИТЬ//
    private int _maxComboEnemyDistance = 5; //Максимальная дистанция Комбо Для поиска Врага//НУЖНО НАСТРОИТЬ//
    private float zOffset = 0; // 

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        _state = State.ComboSearchPartner; // Установим состояние по умолчанию т.к. используем в методе GetValidActionGridPositionList
        _ropeRandererUnit = _unit.GetUnitRope().GetRopeRanderer();       

        ComboAction.OnAnyUnitComboStateChanged += ComboAction_OnAnyUnitComboStateChanged;      
    }

    
    private void Update()
    {
        if (!_isActive) // Если не активны то ...
        {
            return; // выходим и игнорируем код ниже
        }

        _stateTimer -= Time.deltaTime; // Запустим таймер для переключения состояний

        switch (_state) // Переключатель активурует кейс в зависимости от _state
        {
            case State.ComboSearchPartner:

                // развернемся в сторону юнита с кем будет Комбо
                float rotateSpeed = 10f;
                Vector3 unitPartnerDirection = (_unitPartner.transform.position - transform.position).normalized; // Направление к целивому юниту, еденичный вектор
                transform.forward = Vector3.Slerp(transform.forward, unitPartnerDirection, Time.deltaTime * rotateSpeed); // поворт юнита.
                break;

            case State.ComboSearchEnemy:

                HookShootin();

                break;

            case State.ComboStart:

                PullEnemy();

                break;

            case State.ComboAfter:
                break;
        }

        if (_stateTimer <= 0) // По истечению времени вызовим NextMusic() которая в свою очередь переключит состояние. 
        {
            NextState(); //Следующие состояние
        }

        // Debug.Log(_state);
    }

    private void NextState() //Автомат переключения состояний (лучше использовать для настройки)
    {
        switch (_state)
        {
            case State.ComboSearchPartner:

                _instantiateFXPrefab = Instantiate(GameAssets.Instance.comboPartnerFXPrefab, transform.position + Vector3.up * 1.7f, Quaternion.identity); // Создадим частички взаимодействия
                _instantiateFXPrefab.LookAt(_unitPartner.transform.position + Vector3.up * 1.7f); // И разверну в сторону партнера

                _state = State.ComboSearchEnemy;
                OnAnyUnitComboStateChanged?.Invoke(this, new OnComboEventArgs // У юнита партнера тоже изменим состоянеи, что бы он смог правильно потратить очки действия (они GetActionPointCost() зависят от состояния)
                {
                    partnerUnit = _unitPartner,
                    state = _state
                });
                ActionComplete(); // Завершим действие выбора ПАРТНЕРА и ВЫБЕРИМ ЦЕЛЬ  (выполним делегат ClearBusy переданный из класса UnitActionSystem, а в UnitActionSystem_OnBusyChanged из класса UnitActionSystemUI сделаем доп проверку, что-бы кнопки оставались выключенными но юнитов можно выбирать)
                break;

            case State.ComboSearchEnemy:
                _state = State.ComboStart;
                ActionComplete();
                break;
            case State.ComboStart:
                _state = State.ComboAfter;
                OnAnyUnitComboStateChanged?.Invoke(this, new OnComboEventArgs // У юнита партнера тоже изменим состоянеи, что бы он смог выйти из комбо цикла
                {
                    partnerUnit = _unitPartner,
                    state = _state
                });
                float ComboAfterStateTime = 0.5f;
                _stateTimer = ComboAfterStateTime;
                break;

            case State.ComboAfter: // В этом состоянии кнопки UI появляются
                Destroy(_instantiateFXPrefab.GameObject());
                _unitPartner.GetUnitRope().HideRope();
                _unit.GetUnitRope().HideRope();

                _state = State.ComboSearchPartner;
                OnAnyUnitComboStateChanged?.Invoke(this, new OnComboEventArgs // У юнита партнера тоже изменим состоянеи, что бы он смог выйти из комбо цикла
                {
                    partnerUnit = _unitPartner,
                    state = _state
                });
                ActionComplete(); // Вызовим базовую функцию ДЕЙСТВИЕ ЗАВЕРШЕНО
                break;
        }
    }

    private void HookShootin() // Стрельба КРЮКОМ
    {
        float rotateSpeed = 10f;
        Vector3 partnerEnemyDirection = (_unitEnemy.transform.position - _unitPartner.transform.position).normalized; // Направление к целивому юниту, еденичный вектор
        Vector3 unitEnemyDirection = (_unitEnemy.transform.position - transform.position).normalized; // Направление к целивому юниту, еденичный вектор

        // развернем партнера и самого юнита
        _unitPartner.transform.forward = Vector3.Slerp(_unitPartner.transform.forward, partnerEnemyDirection, Time.deltaTime * rotateSpeed); // поворт юнита.
        transform.forward = Vector3.Slerp(transform.forward, unitEnemyDirection, Time.deltaTime * rotateSpeed); // поворт юнита.

        // Когда развернусь 
        if (Vector3.Dot(unitEnemyDirection, transform.forward) >= 0.95f) // Точка возвращает 1, если они указывают в одном и том же направлении, -1, если они указывают в совершенно противоположных направлениях, и ноль, если векторы перпендикулярны.
        {
            //Стреляем веревкой
            Vector3 enemuAimPoint = _unitEnemy.GetAction<ShootAction>().GetAimPoinTransform().position; // Точка прицеливания уврага
            // Развернем веревку в сторону врага (будем работать с локальной Z)
            _ropeRandererParten.transform.LookAt(enemuAimPoint);
            _ropeRandererUnit.transform.LookAt(enemuAimPoint);

            float speedShootRope = 10;
            zOffset += Time.deltaTime * speedShootRope;

            if (zOffset <= Vector3.Distance(_ropeRandererParten.transform.position, _unitEnemy.transform.position))  // Если веревка еще не долетела до врага то
            {
                _ropeRandererParten.RopeDraw(Vector3.forward * zOffset);// Рисуем веревку       
            }
            if (zOffset <= Vector3.Distance(_ropeRandererUnit.transform.position, _unitEnemy.transform.position))  // Если веревка еще не долетела до врага то
            {
                _ropeRandererUnit.RopeDraw(Vector3.forward * zOffset); // Рисуем веревку               
            }

            if (zOffset >= Vector3.Distance(_unitPartner.transform.position, _unitEnemy.transform.position) &&
                zOffset >= Vector3.Distance(transform.position, _unitEnemy.transform.position)) // Веревка долетела до врага
            {
                SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.HookShoot);
                NextState(); //Следующие состояние
            }
        }
    }

    private void PullEnemy() // Тяним врага
    {
        Vector3 targetPointEnemyWorldPosition = LevelGrid.Instance.GetWorldPosition(_targetPointEnemyGridPosition); // Получим позицию куда надо переместить врага                

        Vector3 moveEnemyDirection = (targetPointEnemyWorldPosition - _unitEnemy.transform.position).normalized; // Направление движения, еденичный вектор

        float moveEnemySpead = 6f; //НУЖНО НАСТРОИТЬ//
        _unitEnemy.transform.position += moveEnemyDirection * moveEnemySpead * Time.deltaTime;

        // Развернем партнера и юнита в сторону врага
        _unitPartner.transform.LookAt(_unitEnemy.transform);
        transform.LookAt(_unitEnemy.transform);

        // Расчитаем растояние от партнера до врага и от юнита до врага
        float zDistancePartner = Vector3.Distance(_unitPartner.transform.position, _unitEnemy.transform.position);
        _ropeRandererParten.RopeDraw(Vector3.forward * zDistancePartner);// Рисуем веревку 
        float zDistanceUnit = Vector3.Distance(transform.position, _unitEnemy.transform.position);
        _ropeRandererUnit.RopeDraw(Vector3.forward * zDistanceUnit); // Рисуем веревку 

        float stoppingDistance = 0.2f; // Дистанция остановки //НУЖНО НАСТРОИТЬ//
        if (Vector3.Distance(_unitEnemy.transform.position, targetPointEnemyWorldPosition) < stoppingDistance)  // Если растояние до целевой позиции меньше чем Дистанция остановки // Мы достигли цели        
        {
            float stunPercent = 0.3f; // Процент ОГЛУШЕНИЯ
            _unitEnemy.Stun(stunPercent); //НУЖНО НАСТРОИТЬ// Оглушим
            SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.HookPull);
            NextState(); //Следующие состояние
        }
    }   

    private void ComboAction_OnAnyUnitComboStateChanged(object sender, OnComboEventArgs e)
    {
        if (e.partnerUnit == _unit) // Есля Партнер для комбо - Это Я то
        {
            SetState(e.state); // Изменить мое состояние
        };
    }

    public override string GetActionName() // Присвоить базовое действие //целиком переопределим базовую функцию
    {
        return "крюк";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition) //Получить действие вражеского ИИ // Переопределим абстрактный базовый метод
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0, //Поставим среднее значение действия. Будет выполнять Комбо если ничего другого сделать не может, 
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()// Получить Список Допустимых Сеточных Позиция для Действий // переопределим базовую функцию                                                                     
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = _unit.GetGridPosition(); // Получим позицию в сетке юнита

        int maxComboDistance = GetMaxActionDistance();
        for (int x = -maxComboDistance; x <= maxComboDistance; x++) // Юнит это центр нашей позиции с координатами unitGridPosition, поэтому переберем допустимые значения в условном радиусе maxComboDistance
        {
            for (int z = -maxComboDistance; z <= maxComboDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z, 0); // Смещенная сеточная позиция. Где началом координат(0,0, 0-этаж) является сам юнит 
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition; // Тестируемая Сеточная позиция
                Unit targetUnit = null;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) // Проверим Является ли testGridPosition Допустимой Сеточной Позицией если нет то переходим к след циклу
                {
                    continue; // continue заставляет программу переходить к следующей итерации цикла 'for' игнорируя код ниже
                }

                switch (_state)
                {
                    default:
                    case State.ComboSearchPartner:

                        if (_unit.GetActionPoints() < _searchEnemyPointCost) // Если у юнита не хватает очков для дальнейшего действия то (т.к. поиск ПАРТНЕНРА ничего НЕ СТОИТ, если нет очков то и нефик начить)
                        {
                            return validGridPositionList; // Вернкм пустой список
                        }

                        if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) // Исключим сеточное позицию где нет юнитов 
                        {
                            // Позиция сетки пуста, нет Юнитов
                            continue;
                        }

                        // Если ищем Партнера то ИСКЛЮЧИМ ВРАГОВ
                        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);   // Получим юнита из нашей тестируемой сеточной позиции  // GetUnitAtGridPosition может вернуть null но в коде выше мы исключаем нулевые позиции, так что проверка не нужна                        
                        if (targetUnit.IsEnemy() != _unit.IsEnemy()) // Если тестируемый не в моей команде (игнорируем его)
                        {
                            continue;
                        }

                        // Проверим партнера на достаточность очков
                        int actionPoint = targetUnit.GetActionPoints(); // Получить очки действия у проверяемого юнита                
                        if (actionPoint < _searchEnemyPointCost)
                        {
                            // Если у него нехватает очков действий то Он нам не поможет
                            continue;
                        }

                        if (targetUnit == _unit)
                        {
                            // С самим собой КОМБО делать нельзя
                            continue;
                        }
                        break;

                    case State.ComboSearchEnemy:

                        // Для области выстрела КРЮКОМ сделаем ромб а не квадрат
                        int testDistance = Mathf.Abs(x) + Mathf.Abs(z); // Сумма двух положительных координат сеточной позиции
                        if (testDistance > maxComboDistance) //Получим фигуру из ячеек в виде ромба // Если юнит в (0,0) то ячейка с координатами (5,4) уже не пройдет проверку 5+4>7
                        {
                            continue;
                        }

                        if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) // Исключим сеточное позицию где нет юнитов 
                        {
                            // Позиция сетки пуста, нет Юнитов
                            continue;
                        }

                        // Если ищем врага то ИСКЛЮЧИМ ДРУЖЕСТВЕННЫХ ЮНИТОВ
                        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);   // Получим юнита из нашей тестируемой сеточной позиции // GetUnitAtGridPosition может вернуть null но в коде выше мы исключаем нулевые позиции, так что проверка не нужна
                        if (targetUnit.IsEnemy() == _unit.IsEnemy()) // Если тестируемый в одной команде (игнорируем его)
                        {
                            continue;
                        }

                        // ПРОВЕРИМ НА ПРОСТРЕЛИВАЕМОСТЬ КРЮКОМ до цели 
                        Vector3 unitWorldPosition = LevelGrid.Instance.GetWorldPosition(unitGridPosition); // Переведем в мировые координаты переданную нам сеточную позицию Юнита  
                        Vector3 TestPositionDirection = (targetUnit.GetWorldPosition() - unitWorldPosition).normalized; //Нормализованный Вектор Направления выстрека крюка
                        float heightRaycast = 1.7f; // Высота выстрела луча на уровне головы (есле не видим значит пропустим)
                        if (Physics.Raycast(
                                unitWorldPosition + Vector3.up * heightRaycast,
                                TestPositionDirection,
                                Vector3.Distance(unitWorldPosition, targetUnit.GetWorldPosition()),
                                _obstaclesDoorMousePlaneCoverLayerMask)) // Если луч попал в препятствие то (Raycast -вернет bool переменную)
                        {
                            // Мы заблоктрованны препятствием
                            continue;
                        }
                        break;

                    case State.ComboStart:

                        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) // Исключим сеточное позицию С ЮНИТАМИ. бедем перемещать захваченного юнита на пустую
                        {
                            // Там Юнит - надо Пропустить
                            continue;
                        }

                        /*//если в этой позиции нет узла пути GraphNode  значит эта GridPosition недоступна для хотьбы (стена, колона) или висит в воздухе)              
                        if (!LevelGrid.Instance.WalkableNode(testGridPosition))
                        {
                            continue; // Пропустим эту позицию
                        }*/


                        // ПРОВЕРИМ НА ПРОСТРЕЛИВАЕМОСТЬ КРЮКОМ до цели
                        unitWorldPosition = LevelGrid.Instance.GetWorldPosition(unitGridPosition); // Переведем в мировые координаты переданную нам сеточную позицию Юнита
                        Vector3 testWorldPosition = LevelGrid.Instance.GetWorldPosition(testGridPosition);// 
                        TestPositionDirection = (testWorldPosition - unitWorldPosition).normalized; //Нормализованный Вектор Направления выстрела крюка
                        heightRaycast = 0.2f; // Высота выстрела луча сделаем низко что бы попасть в низкие объекты. (не стал стрелять сверху т.к. объект может быть смещен относительно центра сеточной позийии)
                        if (Physics.Raycast(
                                unitWorldPosition + Vector3.up * heightRaycast,
                                TestPositionDirection,
                                Vector3.Distance(unitWorldPosition, testWorldPosition),
                                _obstaclesDoorMousePlaneCoverLayerMask)) // Если луч попал в препятствие то (Raycast -вернет bool переменную)
                        {
                            // Мы заблоктрованны препятствием
                            continue;
                        }

                        //Исключим сеточные позиции где нельзя ходить (есть препятствия стены объекты) // Проверил в начале с помощъю  if (LevelGrid.Instance.GetGridNode(testGridPosition) == null) 

                        /*if (PathfindingMonkey.Instance.GetGridPositionInAirList().Contains(testGridPosition))
                        {
                            continue;
                        }*/
                        break;
                }
                validGridPositionList.Add(testGridPosition); // Добавляем в список те позиции которые прошли все тесты

                //Debug.Log(testGridPosition);
            }
        }
        return validGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete) // Выполнение действий  (onActionComplete - по завершении действия). В аргумент будем передовать делегат Action 
    {   
        SetupTakeActionFromState(gridPosition);
        ActionStart(onActionComplete); // Вызовим базовую функцию СТАРТ ДЕЙСТВИЯ разрешает доступ к UPDATE// Вызываем этот метод в конце после всех настроек т.к. в этом методе есть EVENT и он должен запускаться после всех настроек
    }

    private void SetupTakeActionFromState(GridPosition gridPosition) //Настроить Выполнение действий в зависимости от состояния
    {
        switch (_state)
        {
            default:
            case State.ComboSearchPartner: // поиска Партнера
                _unitPartner = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition); // Получим юнита для КОМБО  
                _ropeRandererParten = _unitPartner.GetUnitRope().GetRopeRanderer(); // Получим у партнера Рендеринг Веревки
                float ComboSearchPartnerStateTime = 0.5f; //Поиск ПАРТНЕРА.  Для избежания магических чисель введем переменную  Продолжительность Состояния Поиск ПАРТНЕРА ..//НУЖНО НАСТРОИТЬ//
                _stateTimer = ComboSearchPartnerStateTime;
                break;

            case State.ComboSearchEnemy:  // Если ищем врага то                 
                _unitPartner.SpendActionPoints(GetActionPointCost()); // СПИШЕМ У ПАРТНЕРА ОЧКИ ДЕЙСТВИЯ (у меня уже списали в HandleSelectedAction() в классе UnitActionSystem)
                _unitEnemy = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition); // Сохраняем врага                
                _unitPartner.GetUnitRope().ShowRope();
                _unit.GetUnitRope().ShowRope();
                // Время задам большим т.к. растояния разные, но когда достигнута целевая точка я запущу NextStste()
                float ComboSearchEnemyStateTime = 5f;
                _stateTimer = ComboSearchEnemyStateTime;

                // Запускаем событие для // Отображать стрелку от врага до места перемещения в отдельном классе
                break;

            case State.ComboStart:
                _targetPointEnemyGridPosition = gridPosition; // Получим точку куна надо переместить врага
                // Время задам большим т.к. растояния разные,  но когда достигнута целевая точка я запущу NextStste()
                float ComboStartStateTime = 5f;
                _stateTimer = ComboStartStateTime;
                break;
        }
    }

    public override int GetActionPointCost() // Переопределим базовую функцию // Получить Расход Очков на Действие (Стоимость действия)
    {
        switch (_state)
        {
            default:
            case State.ComboSearchPartner:
            case State.ComboStart: // Очки потрачены при наведении на врага, во время выполнения тратить НЕ НАДО
                return 0; // Поиск партнера для комбо ничего не стоит              
            case State.ComboSearchEnemy:
                return _searchEnemyPointCost;
        }
    }

    public override int GetMaxActionDistance()
    {
        int maxComboDistance;
        switch (_state)
        {
            default:
            case State.ComboSearchPartner:

                maxComboDistance = _maxComboPartnerDistance;
                break;

            case State.ComboSearchEnemy:
                maxComboDistance = _maxComboEnemyDistance;
                break;

            case State.ComboStart:
                maxComboDistance = _maxComboPartnerDistance;
                break;
        }
        return maxComboDistance;
    }
    public State GetState()
    {
        return _state;
    }
    private State SetState(State state)
    {
        return _state = state;
    }

    private int GetMaxComboEnemyDistance()
    {
        return _maxComboEnemyDistance;
    }

    private int GetSearchEnemyPointCost()
    {
        return _searchEnemyPointCost;
    }

    public override string GetToolTip()
    {
        return "комбо действие нужно 2  юнита" + "\n" +
            "цена - " + GetSearchEnemyPointCost() + "  списывается сразу у ДВОИХ" + "\n" +
            "урон - 0" + "\n" +
            "2 юнита с расстоянии " + GetMaxComboEnemyDistance() + " клеток, тащат врага к себе" + "\n" +
           "дополнительный эффект – ШОК у врага";
    }
}