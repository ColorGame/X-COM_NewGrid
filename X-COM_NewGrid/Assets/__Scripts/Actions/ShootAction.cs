using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ShootAction : BaseAction
{

    public static event EventHandler<OnShootEventArgs> OnAnyShoot;  // Событие - Любой Начал стрелять (когда любой юнит начнет стрелять мы запустим событие Event) // <Unit> вариант передачи целевого юнита для пули
                                                                    // static - обозначает что event будет существовать для всего класса не зависимо от того скольго у нас созданно Юнитов.
                                                                    // Поэтому для прослушивания этого события слушателю не нужна ссылка на какую-либо конкретную единицу, они могут получить доступ к событию через класс,
                                                                    // который затем запускает одно и то же событие для каждой единицы. 

    public event EventHandler<OnShootEventArgs> OnShoot; // Начал стрелять (когда юнит начнет стрелять мы запустим событие Event) // <OnShootEventArgs> вариант передачи целевого юнита для пули

    public class OnShootEventArgs : EventArgs // Расширим класс событий, чтобы в аргументе события передать нужных юнитов
    {
        public Unit targetUnit; // Целевой юнит в кого стреляем
        public Unit shootingUnit; // Стереляющий юнит это кто стреляет
    }

    private enum State
    {
        Aiming,     // Прицеливание
        Shooting,   // Стрельба
        Cooloff,    // Остывание (небольшая задержка прежде чем мы закончим действие)
    }

    [SerializeField] private LayerMask _obstaclesDoorMousePlaneCoverLayerMask; //маска слоя препятствия НАДО ВЫБРАТЬ Obstacles и DoorInteract и MousePlane(пол) Cover// ВАЖНО НА ВСЕХ СТЕНАХ В ИГРЕ УСТАНОВИТЬ МАСКУ СЛОЕВ -Obstacles, а на дверях -DoorInteract //для полов верхних этажей поменять колайдер на Box collider иначе снизу можно будет простреливать верхний этаж // Cover нельзя простреливать если точка выстрела ниже укрытия (надо проверять все объекты Cover)
    [SerializeField] private LayerMask _smokeCoverLayerMask; //маска слоя ДЫМА и ПРИКРЫТИЯ (появится в ИНСПЕКТОРЕ) НАДО ВЫБРАТЬ Smoke и Cover // ВАЖНО НА ВСЕХ ПРИКРЫТИЯ и ДЫМЕ от гранаты,  УСТАНОВИТЬ СООТВЕТСТВУЮЩУЮ МАСКУ СЛОЕВt 
    [SerializeField] private LayerMask _coverLayerMask; // маска слоя Укрытие
    [SerializeField] private LayerMask _smokeLayerMask; // маска слоя Дым   
    [SerializeField] private int _numberShoot = 3; // Количество выстрелов
    [SerializeField] private float _delayShoot = 0.2f; //задержка между выстрелами
    [SerializeField] private Transform _shootPointTransform; // в инспекторе закинуть точку выстрела лежит на автомате
    [SerializeField] private Transform _aimPointTransform; // в инспекторе закинуть точку прицеливания лежит на голове (нужна если враг присел или это маленький персонаж)
    [SerializeField] private Transform _targetAnimationAim; //Цель для анимации прицеливания 
    [SerializeField] private Rig _aimRig; // Анимация прицеливания взлом

    private int _maxShootDistance = 7;
    private int _shootDamage = 6; // Величина уронв
    private State _state; // Состояние юнита
    private float _stateTimer; //Таймер состояния
    private Unit _targetUnit; // Юнит в которого стреляем целимся
    private Vector3 _targetUnitAimPointPosition; // точка прицеливания у Партнера
    private bool _canShootBullet; // Может стрелять пулей    
    private float _timerShoot; //таймер выстрела
    private int _counterShoot; // Счетчик выстрелов
    private bool _hit; // Попал или промазал
    private float _hitPercent; // Процент попадания
    private float _cellSize;// Размер ячейки
    private float _aimRigWeight;// Вес Анимации прицеливания взлом

    private bool _haveSpotterFire = false; // Есть корректировщик огня (по умолчанию нет)
    private Unit _spotterFireUnit; // Юнит корректировщик огня    

    private void Start()
    {
        _hitPercent = 1f; //Установим Процент попадания МАКСИМАЛЬНЫМ 100%
        _cellSize = LevelGrid.Instance.GetCellSize();
        _targetAnimationAim.position = _shootPointTransform.position; // при старте Цель для анимации прицеливания  будет точка прицеливания на самом юните, что бы не было лишних поворотов направлений
    }
    private void Update()
    {
        if (!_isActive) // Если не активны то ...
        {
            return; // выходим и игнорируем код ниже
        }

        _stateTimer -= Time.deltaTime; // Запустим таймер для переключения состояний
        _timerShoot -= Time.deltaTime;// Запустим таймер для интервалов м у выстрелами

        switch (_state) // Переключатель активурует кейс в зависимости от _state
        {
            case State.Aiming:

                Vector3 aimDirection = (_targetUnit.transform.position - transform.position).normalized; // Направление прицеливания, еденичный вектор
                aimDirection.y = 0; // Чтобы юнит не наклонялся пли стрельбе (т.к. вектор будет поворачиваться только по плоскости x,z)

                float rotateSpeed = 10f; //НУЖНО НАСТРОИТЬ// чем больше тем быстрее
                transform.forward = Vector3.Slerp(transform.forward, aimDirection, Time.deltaTime * rotateSpeed); // поворт юнита.                               

                _aimRig.weight = Mathf.Lerp(_aimRig.weight, _aimRigWeight, Time.deltaTime * 20f); // Плавно изменим вес для анимации прицеливания

                if (_haveSpotterFire) // Если есть корректировщик то развернем и его
                {
                    Vector3 SpotterUnitEnemyDirection = (_targetUnit.transform.position - _spotterFireUnit.transform.position).normalized; // Направление От корректировщика к врагу
                    SpotterUnitEnemyDirection.y = 0;

                    _spotterFireUnit.transform.forward = Vector3.Slerp(_spotterFireUnit.transform.forward, SpotterUnitEnemyDirection, Time.deltaTime * rotateSpeed); // поворт корректировщика.    
                }               
                    break;

            case State.Shooting:               

                if (_canShootBullet && _timerShoot <= 0) // Если могу стрелять пулей и таймер истек ...
                {
                    Shoot();
                    _timerShoot = _delayShoot; // Установим таймер = задержки между выстрелами
                    _counterShoot += 1; // Прибавим к счетчику выстрелов 1 
                }

                if (_counterShoot >= _numberShoot || _targetUnit.IsDead()) //После выпуска 3 пуль или когда враг СДОХ
                {
                    _canShootBullet = false;
                    _counterShoot = 0; //Обнулим счетчик пуль
                }

                break;

            case State.Cooloff: // Блок пустой но можно добавить анимацию попадания или промоха
                _aimRig.weight = Mathf.Lerp(_aimRig.weight, _aimRigWeight, Time.deltaTime * 20f); // Плавно изменим вес для анимации прицеливания
                break;
        }

        if (_stateTimer <= 0) // По истечению времени _stateTimer вызовим NextMusic() которая в свою очередь переключит состояние. Например - у меня было TypeGrenade.Aiming: тогда в case TypeGrenade.Aiming: переключу на TypeGrenade.Shooting;
        {
            NextState(); //Следующие состояние
        }
    }

    private void NextState() //Автомат переключения состояний
    {
        switch (_state)
        {
            case State.Aiming:
                _state = State.Shooting;
                float shootingStateTime = _numberShoot * _delayShoot + 0.5f; // Для избежания магических чисель введем переменную  Продолжительность Состояния Выстрел = Количество выстрелов * время выстрела
                _stateTimer = shootingStateTime;
                _targetAnimationAim.position = _targetUnitAimPointPosition; //Цель для анимации прицеливания  будет точка прицеливания на враге
                break;
            case State.Shooting:
                _state = State.Cooloff;
                float cooloffStateTime = 0.5f; // Для избежания магических чисель введем переменную  Продолжительность Состояния Охлаждения //НАДО НАСТРОИТЬ// Продолжительность анимации выстрела(наведение камеры))
                _stateTimer = cooloffStateTime;
                _aimRigWeight = 0; // Уменьшим вес для анимации прицеливания
                break;
            case State.Cooloff:
                _targetAnimationAim.position = _shootPointTransform.position; // Цель для анимации прицеливания  будет точка прицеливания на самом юните, что бы при старте след выстрела юнит смотрел в нужное направление
                ActionComplete(); // Вызовим базовую функцию ДЕЙСТВИЕ ЗАВЕРШЕНО
                break;
        }

        //Debug.Log(_state);
    }

    public override void TakeAction(GridPosition targetGridPosition, Action onActionComplete) // Выполнение действий
    {
        _targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition); // Получим юнита в которого целимся и сохраним его                               
        _targetUnitAimPointPosition = _targetUnit.GetAction<ShootAction>().GetAimPoinTransform().position; // позицию Прицеливания целевого юнита. 

        _state = State.Aiming; // Активируем состояние Прицеливания 
        float aimingStateTime = 0.5f; //Продолжительность Состояния Прицеливания //НУЖНО НАСТРОИТЬ//
        _stateTimer = aimingStateTime;

        _canShootBullet = true;                
              
        _aimRigWeight = 1; // Увеличим вес для анимации прицеливания           

        ActionStart(onActionComplete); // Вызовим базовую функцию СТАРТ ДЕЙСТВИЯ и передадим делегат // Вызываем этот метод в конце после всех настроек т.к. в этом методе есть EVENT и он должен запускаться после всех настроек
    }

    private void Shoot() // ВЫстрел
    {
        //Вариант использования кода для тряски эрана. Но это делает код зависимым от наличия ScreenShake. Поэтому реализуем по другому
        //ScreenShake.Instance.Shake(5);
        OnAnyShoot?.Invoke(this, new OnShootEventArgs // создаем новый экземпляр класса OnShootEventArgs
        {
            targetUnit = _targetUnit,
            shootingUnit = _unit
        }); // Запустим событие ЛЮБОЙ Начал стрелять и в аргумент передадим в кого стреляют и кто стреляет (Подписчики ScreenShakeActions ДЛЯ РЕАЛИЗАЦИИ ТРЯСКИ ЭКРАНА и UnitRagdollSpawner- для определения направления поражения)

        OnShoot?.Invoke(this, new OnShootEventArgs // создаем новый экземпляр класса OnShootEventArgs
        {
            targetUnit = _targetUnit,
            shootingUnit = _unit
        }); // Запустим событие Начал стрелять и в аргумент передадим в кого стреляют и кто стреляет (UnitAnimator-подписчик)               

        // Вычислить попадание или промах
        _hit = UnityEngine.Random.Range(0, 1f) < GetHitPercent(_targetUnit);

        Transform bulletProjectilePrefabTransform = Instantiate(GameAssets.Instance.bulletProjectilePrefab, _shootPointTransform.position, Quaternion.identity); // Создадим префаб пули в точке выстрела
        BulletProjectile bulletProjectile = bulletProjectilePrefabTransform.GetComponent<BulletProjectile>(); // Вернем компонент BulletProjectile созданной пули
        
        SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.Shoot); // Воспроизведем звук 
        if (_hit) // Если попали то
        {
            bulletProjectile.Setup(_targetUnitAimPointPosition, _hit); // В аргумент предали позицию Прицеливания целевого юнита
            _targetUnit.Damage(GetShootDamage()); // ДЛЯ ТЕСТА УЩЕРБ БУДЕТ 5. В дальнейшем будем брать этот показатель из оружия //НАДО НАСТРОИТЬ//
        }
        else // Если промах
        {
            //Рандомно сместим по X Y Z
           _targetUnitAimPointPosition += Vector3.one * UnityEngine.Random.Range(-0.8f, 0.8f);
            bulletProjectile.Setup(_targetUnitAimPointPosition, _hit); // В аргумент предали Мировую позицию целевого юнита. 
        }
    }

    public float GetHitPercent(Unit enemyUnit) // Получите процент попадания по врагу (в аргумент передаем врага)
    {
        _hitPercent = 1f; //Установим Процент попадания МАКСИМАЛЬНЫМ 100%

        if (_haveSpotterFire) // Если есть корректировщик то ПОМЕХ НЕТУ
        {

            return _hitPercent;
        }
        // ПРОВЕРИМ ВСЕ CoverSmokeObject НА ПУТИ ВЫСТРЕЛА

        Vector3 unitWorldPosition = _unit.GetWorldPosition(); // Получим мировые координаты Юнита
        Vector3 enemyUnitWorldPosition = enemyUnit.GetWorldPosition(); //Получим мировые координаты ЮнитаВРАГА
        Vector3 shototDirection = (enemyUnitWorldPosition - unitWorldPosition).normalized; //Нормализованный Вектор Направления стрельбы
        float heightRaycast = 0.5f; // Высота выстрела луча (сделал низким что бы попасть в Half половинчатый коллайдер)              
        float maxPenaltyAccuracy = 0; // максимальный штраф прицеливания
        Collider ignoreCoverSmokeCollider = null; // Игнорировать CoverSmokeObject

        // Выстрелим ЛУЧ в сорону врага на растояние 1.5 КЛЕТКИ и выясню Это УКРЫТИЕ близко. Если БЛИЗКО то игнорируем штраф от этого укрытия(ДЫМ будем игнорировать т.к. он не защищает от пуль а только мешает прицелиться). Если БЛИЗКО то игнорируем штраф от этого укрытия
        if (Physics.Raycast(
                 unitWorldPosition + Vector3.up * heightRaycast,
                 shototDirection,
                 out RaycastHit hitCoverInfo,
                 _cellSize * 1.5f,
                 _coverLayerMask))
        {
            ignoreCoverSmokeCollider = hitCoverInfo.collider;
        }
        /*Debug.DrawRay(unitWorldPosition + Vector3.up * heightRaycast,
                 shototDirection * (_cellSize *1.5f),
                 Color.white,
                 100);*/

        // Пороверим все CoverSmokeObject и получим maxPenaltyAccuracy
        RaycastHit[] raycastHitArray = Physics.RaycastAll(
                unitWorldPosition + Vector3.up * heightRaycast,
                shototDirection,
                Vector3.Distance(unitWorldPosition, enemyUnitWorldPosition),
                _smokeCoverLayerMask); // Сохраним массив всех попаданий луча. Obstacles слой Игнорирую т.к. через него НЕЛЬЗЯ СТРЕЛЯТЬ

        /*Debug.DrawRay(unitWorldPosition + Vector3.up * heightRaycast,
                shototDirection * Vector3.Distance(unitWorldPosition, enemyUnitWorldPosition),
                 Color.green,
                 999);*/

        foreach (RaycastHit raycastHit in raycastHitArray) // Переберем наш полученный массив И получим максимальный штраф прицеливания maxPenaltyAccuracy
        {
            Collider coverSmokeCollider = raycastHit.collider; // Получим колайдер, в который попали.

            if (coverSmokeCollider == ignoreCoverSmokeCollider)
            {
                //Пропустим коллайдер который надо игнорировать(это укрытие за него нет штрафа)
                continue;
            }
            CoverSmokeObject coverSmokeObject = coverSmokeCollider.GetComponent<CoverSmokeObject>(); // Получим на колайдере, в который попали, компонент CoverSmokeObject - Объект укрытия или Дым
            float penaltyAccuracy = coverSmokeObject.GetPenaltyAccuracy(); // Вернуть Штраф прицеливания
            if (penaltyAccuracy > maxPenaltyAccuracy)
            {
                maxPenaltyAccuracy = penaltyAccuracy;
            }
        }

        // Выстрелим ЛУЧ со стороны врага что бы проверить юнит в ДЫМУ (ЛУч не может стрелять внутри колайдера(внутри стены))
        if (Physics.Raycast(
                 enemyUnitWorldPosition + Vector3.up * heightRaycast,
                 shototDirection * (-1),
                 out RaycastHit hitSmokeInfo,
                 Vector3.Distance(unitWorldPosition, enemyUnitWorldPosition),
                 _smokeLayerMask)) // проверяем только дым
        {
            CoverSmokeObject coverSmokeObject = hitSmokeInfo.collider.GetComponent<CoverSmokeObject>(); // Получим на колайдере, в который попали, компонент CoverSmokeObject - Объект укрытия
            float penaltyAccuracy = coverSmokeObject.GetPenaltyAccuracy(); // Вернуть Штраф прицеливания
            if (penaltyAccuracy > maxPenaltyAccuracy)
            {
                maxPenaltyAccuracy = penaltyAccuracy;
            }
        }
        return _hitPercent -= maxPenaltyAccuracy;
    }

    public override string GetActionName() // Получим имя для кнопки
    {
        return "автомат";
    }

    public override List<GridPosition> GetValidActionGridPositionList() //Получить Список Допустимых Сеточных Позиция для Действий // переопределим базовую функцию
    {
        GridPosition unitGridPosition = _unit.GetGridPosition(); // Получим позицию в сетке юнита
        return GetValidActionGridPositionList(unitGridPosition);
    }

    //Отличается от метода выше сигнатурой.
    public List<GridPosition> GetValidActionGridPositionList(GridPosition unitGridPosition) //Получить Список Допустимых Сеточных Позиция для Действий.
                                                                                            //Получить Список Допустимых целей вокруг позиции Юнита
                                                                                            //В аргумент передаем сеточную позицию Юнита                                                                                            
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        int maxShootDistance = GetMaxActionDistance();
        for (int x = -maxShootDistance; x <= maxShootDistance; x++) // Юнит это центр нашей позиции с координатами unitGridPosition, поэтому переберем допустимые значения в условном радиусе maxShootDistance
        {
            for (int z = -maxShootDistance; z <= maxShootDistance; z++)
            {
                for (int floor = -maxShootDistance; floor <= maxShootDistance; floor++)
                {

                    GridPosition offsetGridPosition = new GridPosition(x, z, floor); // Смещенная сеточная позиция. Где началом координат(0,0, 0-этаж) является сам юнит 
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition; // Тестируемая Сеточная позиция

                    if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) // Проверим Является ли testGridPosition Допустимой Сеточной Позицией если нет то переходим к след циклу
                    {
                        continue; // continue заставляет программу переходить к следующей итерации цикла 'for' игнорируя код ниже
                    }
                    // Для области выстрела сделаем ромб а не квадрат
                    int testDistance = Mathf.Abs(x) + Mathf.Abs(z); // Сумма двух положительных координат сеточной позиции
                    if (testDistance > maxShootDistance) //Получим фигуру из ячеек в виде ромба // Если юнит в (0,0) то ячейка с координатами (5,4) уже не пройдет проверку 5+4>7
                    {
                        continue;
                    }

                    if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) // Исключим сеточное позицию где нет юнитов (нам нужны ячейки с юнитами мы будем по ним шмалять)
                    {
                        // Позиция сетки пуста, нет Юнитов
                        continue;
                    }

                    Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);   // Получим юнита из нашей тестируемой сеточной позиции 
                                                                                                    // GetUnitAtGridPosition может вернуть null но в коде выше мы исключаем нулевые позиции, так что проверка не нужна
                    if (targetUnit.IsEnemy() == _unit.IsEnemy()) // Если тестируемый юнит враг и наш юнит тоже враг то (если они оба в одной команде то будем игнорировать этих юнитов)
                    {
                        // Оба подразделения в одной "команде"
                        continue;
                    }

                    // ПРОВЕРИМ НА ПРОСТРЕЛИВАЕМОСТЬ до цели , Cover нельзя простреливать если точка выстрела ниже укрытия (надо проверять все объекты Cover)
                    Vector3 unitWorldPosition = LevelGrid.Instance.GetWorldPosition(unitGridPosition); // Переведем в мировые координаты переданную нам сеточную позицию Юнита  
                    Vector3 shototDirection = (targetUnit.GetWorldPosition() - unitWorldPosition).normalized; //Нормализованный Вектор Направления стрельбы
                    Vector3 shootPoint = _shootPointTransform.position - unitWorldPosition; // Получим расстояние от точки выстрела до основания юнита (актуально для 2 этажа)

                    float reserveHeight = 0.15f; // Резерв по высоте чуть выше точки выстрела ()
                    if (Physics.Raycast(
                            unitWorldPosition + Vector3.up * (shootPoint.y + reserveHeight),
                            shototDirection,
                            Vector3.Distance(unitWorldPosition, targetUnit.GetWorldPosition()),
                            _obstaclesDoorMousePlaneCoverLayerMask)) // Если луч попал в препятствие то (Raycast -вернет bool переменную)
                    {
                        // Мы заблоктрованны препятствием
                        continue;
                    }

                    /*Debug.DrawRay(unitWorldPosition + Vector3.up * (shootPoint.y + reserveHeight),
                        shototDirection * Vector3.Distance(unitWorldPosition, startUnit.GetWorldPosition()),
                        Color.red,
                        999);*/

                    validGridPositionList.Add(testGridPosition); // Добавляем в список те позиции которые прошли все тесты
                                                                 //Debug.Log(testGridPosition);
                }
            }
        }

        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition) //Получить действие вражеского ИИ  для переданной нам сеточной позиции// Переопределим абстрактный базовый метод //EnemyAIAction создан в каждой Допустимой Сеточнй Позиции, наша задача - настроить каждую ячейку в зависимости от состоянии юнита который там стоит
    {
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition); // Получим юнита для этой позиции это наша цель

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            //actionValue = 100 +Mathf.RoundToInt(1- startUnit.GetHealthNormalized()) *100,  // Реализуем логику для стрельбы по самому поврежденному игроку .
            // Например если юнит полностью здоров то GetHealthNormalized() вернет 1  тогда (1-1)*100 = 0 в итоге actionValue останеться прежним 100
            // но если осталось половину жизни то GetHealthNormalized() вернет 0,5 тогда (1-0,5)*100 = 50 и actionValue станет равным 150 более высокая значимость действия 
            // ЛОГИКА НЕ РАБОТАЕТ КОГДА У ЮНИТОВ РАЗНОЕ МАКСИМАЛЬНОЕ ЗДОРОВЬЕ
            // Реализую другую логику
            actionValue = 100 + Mathf.RoundToInt(AttackScore(targetUnit))
        };
    }

    public float AttackScore(Unit unit) // Оценка атаки // В первую очередь Бьем слабых, но потом при равных значениях здоровья, добиваем тех кто был самым сильным
    {
        int health = unit.GetHealth();
        int healthMax = unit.GetHealthMax();

        float unitPerHealthPoint = 100 / healthMax;  //чем выше здоровье, тем ниже будет этот балл
        return (healthMax - health) * unitPerHealthPoint + unitPerHealthPoint; // Итак… если у всех целей максимальное здоровье (health=healthMax), то БОЛЬШЕ очков получит юнит с меньшим максимальным здоровьем (самому хилому наваляют в первую очередь).
                                                                               // Если после повреждений у двух юнитов одинаковое здоровье например 20 но у первого healthMax=100 а у второго 120 то наваляют второму т.к. у него больше максимальное здоровье и он получит больше очков значения
    }

    public void SetSpotterFireUnit(Unit spotterFireUnit) // Установить корректировщика огня
    {
        _spotterFireUnit = spotterFireUnit;
        _haveSpotterFire = true;

    }

    public void СlearSpotterFireUnit()// Очистить поле корректировщика огня
    {
        _spotterFireUnit = null;
        _haveSpotterFire = false;
    }

    public int GetTargetCountAtPosition(GridPosition gridPosition) // Получить Количество Целей На Позиции
    {
        return GetValidActionGridPositionList(gridPosition).Count; // Получим количество целей из списка Допустимых целей
    }
    public Transform GetAimPoinTransform() // Получить точку прицеливания
    {
        return _aimPointTransform;
    }
    public Transform GetShootPoinTransform() // Получить точку выстрела
    {
        return _shootPointTransform;
    }
    public Unit GetTargetUnit() // Раскроем _targetUnit
    {
        return _targetUnit;
    }
    public override int GetMaxActionDistance() // Раскроем maxShootDistance
    {
        if (_haveSpotterFire)
        {
            float percentageShootDistanceIncrease = 0.5f;// Процент увеличения дальности выстрела //НУЖНО НАСТРОИТЬ//
            return _maxShootDistance + Mathf.RoundToInt(_maxShootDistance * percentageShootDistanceIncrease);
        }
        else
        {
            return _maxShootDistance;
        }

    }
    private int GetShootDamage()
    {
        if (_haveSpotterFire)
        {
            float percentageShootDamageIncrease = 0.5f;// Процент увеличения ehjyf выстрела //НУЖНО НАСТРОИТЬ//
            return _shootDamage + Mathf.RoundToInt(_shootDamage * percentageShootDamageIncrease);
        }
        else
        {
            return _shootDamage;
        }
    }

    public override string GetToolTip()
    {
        return "цена - " + GetActionPointCost() + " (за 3 выстрела очередью)" + "\n" +
            "дальность - " + GetMaxActionDistance() + "\n" +
            "урон - " + GetShootDamage() + "\n" +
            "препятствия и дым дают штраф точности" + "\n" +
           "наводчик увеличивает дальность на  50%";
    }
}
