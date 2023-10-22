#define PATHFINDING

using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using static Pathfinding.BlockManager;

// Обратите внимание на эту строку, если она опущена, скрипт не будет знать, что класс 'Path' существует, и он выдаст ошибки компилятора
// Эта строка всегда должна присутствовать в верхней части скриптов, использующих поиск пути



public class MoveAction : BaseAction // Действие перемещения НАСЛЕДУЕТ класс BaseAction // ВЫделим в отдельный класс // Лежит на каждом юните
{
    public static event EventHandler <Unit> OnAnyUnitPathComplete; // У любого Юнита Вычислен Путь // static - обозначает что event будет существовать для всего класса не зависимо от того скольго у нас созданно Юнитов.
                                                            // Поэтому для прослушивания этого события слушателю не нужна ссылка на какую-либо конкретную единицу, они могут получить доступ к событию через класс, который затем запускает одно и то же событие для каждой единицы. 

    public event EventHandler OnStartMoving; // Начал двигаться (когда юнит начнет движение мы запустим событие Event)
    public event EventHandler OnStopMoving; // Прекратил движение (когда юнит законсит движение мы запустим событие Event)
    public event EventHandler<OnChangeFloorsStartedEventArgs> OnChangedFloorsStarted; // Начали менять этажи 
    public class OnChangeFloorsStartedEventArgs : EventArgs // Расширим класс событий, чтобы в аргументе события передать Сеточную позицию Юнита и Целевой позиции
    {
        public GridPositionXZ unitGridPosition; // Откуда прыгаем
        public GridPositionXZ targetGridPosition; // КУда прыгаем
    }

    [SerializeField] private int maxMoveDistance = 5; // Максимальная дистанция движения в сетке

    private List<Vector3> _positionList; // Позиции которые должен преодолеть Юнит (в определенном порядке)
    private int _currentPositionIndex; // Текущая Позиция Индекс
    private bool _isChangingFloors; // это смена этажей
    private float _differentFloorsTeleportTimer; // Таймер телепортации на разные этажи
    private float _differentFloorsTeleportTimerMax = .5f; // Максимальный таймер телепортации на разные этажи (это время воспроизведения анимации прыжка или падения)
    private List<GridPositionXZ> _validGridPositionList = new List<GridPositionXZ>(); // Список Допустимых Сеточных Позиция для Действий

#if PATHFINDING
    private BlockManager.TraversalProvider _traversalProvider; // Поставщик(провайдер) обхода
    private ABPath _path; // Путь 
    private ConstantPath _constantPath;  // Диапазон путей вокруг игрока
    private List<SingleNodeBlocker> _obstaclesIgnoreList; // Список препядсвтий которые будем игнорировать. Закинем туда самого ЮНИТА
    private SingleNodeBlocker _singleNodeBlocker; // Блокирующий узел на самом юните
    private Seeker _seeker; 



    protected override void Awake()
    {
        base.Awake();        
        _seeker = GetComponent<Seeker>();
        
    }

    private void Start()
    {
        // Установите провайдера обхода так, чтобы он блокировал все узлы, которые заблокированы SingleNodeBlocker
        // за исключением SingleNodeBlocker, принадлежащего этому ЮНИТУ (мы не хотим, блокировать самих себя).
        _singleNodeBlocker = _unit.GetSingleNodeBlocker();
        _singleNodeBlocker.BlockAtCurrentPosition(); // Заблокирую узел где стою
        _obstaclesIgnoreList = new List<SingleNodeBlocker>() { _singleNodeBlocker };
        _traversalProvider = new TraversalProvider(BlockManager.Instance, BlockMode.AllExceptSelector, _obstaclesIgnoreList);

        _seeker.pathCallback += Seeker_OnPathComplete; // Подпишемся путь вычислен
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;  //подпишемся Выбранный Юнит Изменен
        UnitActionSystem.Instance.OnBusyChanged += Instance_OnBusyChanged; //подпишемся на событие Занятость Изменена 

        GeneratePossibleMoves(_unit); // Сгенерируем возможные ходы с помощью (A* Pathfinding Project4.2.18) для нашего юнита
    }   
#endif

    private void Update()
    {      
        if (!_isActive) // Если не активны то ...
        {
            return; // выходим и игнорируем код ниже
        }

#if PATHFINDING
        if (!_seeker.IsDone())//Если путь не расчитан 
        {
            return; // выходим и игнорируем код ниже            
        }
        
#endif
        // Буду двигаться по списку ячеек из _positionList, каждая следующая ячейка будет targetPosition
        Vector3 targetPosition = _positionList[_currentPositionIndex]; // Целевой позицией будет позиция из листа с заданным индексом

        if (_isChangingFloors) // Если надо сменить этаж то
        {
            // Логика остановки и телепортации
            // При подходе к ячейки, с которой юнит будет телепортироваться, необходимо что бы он смотрел в сторону прыжка но только по горизонтали (Не смотрел вверх или вниз)
            Vector3 targetSameFloorPosition = targetPosition; // Целевая позиция этого же Этажа = Целевой позици
            targetSameFloorPosition.y = transform.position.y; // Изменим позицию по оси У как у игрока

            Vector3 rotateDirection = (targetSameFloorPosition - transform.position).normalized; // Направление поворота

            float rotateSpeed = 10f;
            transform.forward = Vector3.Slerp(transform.forward, rotateDirection, Time.deltaTime * rotateSpeed);

            _differentFloorsTeleportTimer -= Time.deltaTime; //ЗАПУСТИМ Таймер телепортации на разные этажи
            if (_differentFloorsTeleportTimer < 0f) // По истечению таймера // Переключим переключатель этажей и Телепортируемся в целевое положение (а также в момент отсчета таймера будет происходить анимация прыжка)
            {
                _isChangingFloors = false;
                transform.position = targetPosition;
            }
        }
        else
        {
            // Обычная логика перемещения

            Vector3 moveDirection = (targetPosition - transform.position).normalized; // Направление движения, еденичный вектор

            float rotateSpeed = 10f; //Чем больше тем быстрее
            transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed); // поворт юнита. ЗАМЕНИЛ Lerp на - Slerp Сферически интерполирует между кватернионами a и b по соотношению t. Параметр t ограничен диапазоном [0, 1]. Используйте это для создания поворота, который плавно интерполирует между первым кватернионом a и вторым кватернионом b на основе значения параметра t. Если значение параметра близко к 0, выходные данные будут близки к a, если оно близко к 1, выходные данные будут близки к b.

            float moveSpead = 4f; //НУЖНО НАСТРОИТЬ//
            transform.position += moveDirection * moveSpead * Time.deltaTime;
        }

        float stoppingDistance = 0.2f; // Дистанция остановки //НУЖНО НАСТРОИТЬ//
        if (Vector3.Distance(transform.position, targetPosition) < stoppingDistance)  // Если растояние до целевой позиции меньше чем Дистанция остановки // Мы достигли цели        
        {
            _currentPositionIndex++; // Увеличим индекс на еденицу

            if (_currentPositionIndex >= _positionList.Count) // Если мы дошли до конца списка тогда...
            {
                SoundManager.Instance.SetLoop(false);
                SoundManager.Instance.Stop();

                OnStopMoving?.Invoke(this, EventArgs.Empty); //Запустим событие Прекратил движение
                _singleNodeBlocker.BlockAtCurrentPosition(); // Заблокирую узел на новом месте и разблокирую предыдущий
                ActionComplete(); // Вызовим базовую функцию ДЕЙСТВИЕ ЗАВЕРШЕНО                
            }
            else
            {
                targetPosition = _positionList[_currentPositionIndex]; // Целевой позицией будет позиция из листа с заданным индексом

                GridPositionXZ targetGridPosition = LevelGrid.Instance.GetGridPosition(targetPosition); // Получим сеточную позицию Целевой позиции
                GridPositionXZ unitGridPosition = LevelGrid.Instance.GetGridPosition(transform.position); // Получим сеточную позицию Юнита                               
                
                if (targetGridPosition.floor != unitGridPosition.floor) // Если этаж Целевой позииции не совпадает с этажом Юнита то ...              
                {
                    // Разные этажи
                    _isChangingFloors = true;
                    _differentFloorsTeleportTimer = _differentFloorsTeleportTimerMax;

                    OnChangedFloorsStarted?.Invoke(this, new OnChangeFloorsStartedEventArgs // Запустим события и передадим сеточные позиции откуда и куда прыгаем
                    {
                        unitGridPosition = unitGridPosition,
                        targetGridPosition = targetGridPosition,
                    });
                }
            }
        }
    }


    public override void TakeAction(GridPositionXZ gridPosition, Action onActionComplete) // Движение к целевой позиции. В аргумент передаем сеточную позицию  и делегат. Вызываю ее для передачи новой целевой позиции
    {
#if PATHFINDING
        _path = ABPath.Construct(transform.position, LevelGrid.Instance.GetWorldPosition(gridPosition)); // построим
        _path.traversalProvider = _traversalProvider; // Установим объекты для обхода
        // Запланируйте путь для расчета
        _seeker.StartPath(_path);   // вычислим
#else
        List<GridPosition> pathGridPositionList = PathfindingMonkey.Instance.FindPath(_unit.GetGridPosition(), gridPosition, out int pathLength); // Получим список Пути позиций сетки от текущего сеточного положения Юнита до целевого (out int pathLength добавили что бы соответствовала сигнатуре)

       // Надо преобразовать полученный список GridPosition в МИРОВЫЕ КООРДИНАТЫ Vector3
       _positionList = new List<Vector3>(); // Иниацилизируем список Позиции

        foreach (GridPosition pathGridPosition in pathGridPositionList) // переберем компоненты списка pathGridPositionList, преобразуем их в мировые координаты и добавим в _positionList
        {
            _positionList.Add(LevelGrid.Instance.GetWorldPosition(pathGridPosition)); // преобразуем pathGridPosition в мировую и добавим в _positionList
        }
#endif
        SoundManager.Instance.SetLoop(true);
        SoundManager.Instance.Play(SoundManager.Sound.Move);
        _currentPositionIndex = 0; // По умолчанию возвращаем к нулю
        OnStartMoving?.Invoke(this, EventArgs.Empty); // Запустим событие Начал двигаться 
        ActionStart(onActionComplete); // Вызовим базовую функцию СТАРТ ДЕЙСТВИЯ // Вызываем этот метод в конце после всех настроек т.к. в этом методе есть EVENT и он должен запускаться после всех настроек
    }


#if PATHFINDING

    private void Seeker_OnPathComplete(Path path)
    {
        if (path == _constantPath) // Вычислен Радиус для движения
        {
            // и обновить визуал
            _validGridPositionList.Clear(); // Очистим сисок путей
            foreach (GraphNode node in _constantPath.allNodes)
            {
                if (node == _constantPath.startNode)
                {
                    // Таже ячейка на которой стоит юнит :(
                    continue;
                }

                GridPositionXZ nodeGridPosition = LevelGrid.Instance.GetGridPosition((Vector3)node.position);
                _validGridPositionList.Add(nodeGridPosition); // Добавляем в список те позиции которые прошли все тесты

                OnAnyUnitPathComplete?.Invoke(this, _unit); // Запустим событие
            }
        }

        if (path == _path) // Вычислин путь от А до В
        {
            // Теперь путь вычислен
            _positionList = _path.vectorPath;
        }
            
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        if (UnitActionSystem.Instance.GetSelectedUnit() == _unit) // Если выбран этот юнит
        {
          GeneratePossibleMoves(_unit); // Сгенерируем возможные ходы с помощью (A* Pathfinding Project4.2.18) для нашего юнита
        }
    }
    private void Instance_OnBusyChanged(object sender, EventArgs e)
    {
        if (UnitActionSystem.Instance.GetSelectedUnit() == _unit) // Если выбран этот юнит
        {
            GeneratePossibleMoves(_unit); // Сгенерируем возможные ходы с помощью (A* Pathfinding Project4.2.18) для нашего юнита
        }
    }

    public override List<GridPositionXZ> GetValidActionGridPositionList() //Получить Список Допустимых Сеточных Позиция для Действий // переопределим базовую функцию
    {       
        return _validGridPositionList;
    }

    public void GeneratePossibleMoves(Unit unit) // Сгенерируем возможные ходы с помощью (A* Pathfinding Project4.2.18) для нашего юнита
    {
        _constantPath = ConstantPath.Construct(unit.transform.position, maxMoveDistance * 1000 * Mathf.RoundToInt(LevelGrid.Instance.GetCellSize()));
        _constantPath.traversalProvider = _traversalProvider; // Установим объекты для обхода
        // Запланируйте путь для расчета
        _seeker.StartPath(_constantPath);

        // Получим прямоугольник узлов вокруг Юнита
        /*GraphNode unitNode = AstarPath.active.GetNearest(unit.transform.position).node; // Получим узел Юнита
        List<GraphNode> graphNodes = PathUtilities.BFS(unitNode, maxMoveDistance);

             _validGridPositionList.Clear(); // Очистим сисок путей
        foreach (GraphNode node in graphNodes)
        {
            if (node == unitNode)
            {
                // Таже ячейка на которой стоит юнит :(
                continue;
            }

            GridPositionXZ nodeGridPosition = LevelGrid.Instance.GetGridPosition((Vector3)node.position);
            _validGridPositionList.Add(nodeGridPosition); // Добавляем в список те позиции которые прошли все тесты

            OnAnyUnitPathComplete?.Invoke(this, _unit); // Запустим событие
        }*/
    }


#else
    public override List<GridPosition> GetValidActionGridPositionList() //Получить Список Допустимых Сеточных Позиция для Действий // переопределим базовую функцию
    {
        _validGridPositionList.Clear(); // Очистим сисок путей

        GridPosition unitGridPosition = _unit.GetGridPosition(); // Получим позицию в сетке
        for (int x = -maxMoveDistance; x <= maxMoveDistance; x++) // Юнит это центр нашей позиции с координатами unitGridPosition, поэтому переберем допустимые значения в условном радиусе maxMoveDistance
        {
            for (int z = -maxMoveDistance; z <= maxMoveDistance; z++)
            {
                for (int floor = -maxMoveDistance; floor <= maxMoveDistance; floor++)
                {

                    GridPosition offsetGridPosition = new GridPosition(x, z, floor); // Смещенная сеточная позиция. Где началом координат(0,0, floor-этаж) является сам юнит 
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition; // Тестируемая Сеточная позиция

                    if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) // Проверим Является ли testGridPosition Допустимой Сеточной Позицией если нет то переходим к след циклу
                    {
                        continue; // continue заставляет программу переходить к следующей итерации цикла 'for' игнорируя код ниже
                    }

                    if (unitGridPosition == testGridPosition) // Исключим сеточную позицию где находиться сам юнит
                    {
                        // Таже ячейка на которой стоит юнит :(
                        continue;
                    }

                    if (!PathfindingMonkey.Instance.HasPath(unitGridPosition, testGridPosition)) //Исключим сеточные позиции куда нельзя пройти 
                    {
                        continue;
                    }

                    if (!PathfindingMonkey.Instance.IsWalkableGridPosition(testGridPosition)) //Исключим сеточные позиции где нельзя ходить (есть препятствия стены объекты)
                    {
                        continue;
                    }

                    if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) // Исключим сеточную позицию где находиться другие юниты
                    {
                        // Позиция занята другим юнитом :(
                        continue;
                    }

                     int pathfindingDistanceMultiplier = 10; // множитель расстояния определения пути (в классе PathfindingMonkey задаем стоимость смещения по клетке и она равна прямо 10 по диогонали 14, поэтому умножем наш множитель на количество клеток)
                    if (PathfindingMonkey.Instance.GetPathLength(unitGridPosition, testGridPosition) > maxMoveDistance * pathfindingDistanceMultiplier) //Исключим сеточные позиции - Если растояние до тестируемой клетки больше расстояния которое Юнит может преодолеть за один ход
                    {
                        // Длина пути слишком велика
                        continue;
                    }
                    _validGridPositionList.Add(testGridPosition); // Добавляем в список те позиции которые прошли все тесты
                                                                 //Debug.Log(testGridPosition);
                }
            }
        }                
        return _validGridPositionList;
    }
#endif

    public override string GetActionName() // Присвоить базовое действие //целиком переопределим базовую функцию
    {
        return "движение";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPositionXZ gridPosition) //Получить действие вражеского ИИ // Переопределим абстрактный базовый метод
    {
        int targetCountAtPosition = _unit.GetAction<ShootAction>().GetTargetCountAtPosition(gridPosition); // У юнита вернем скрипт ShootAction и вызовим у него "Получить Количество Целей На Позиции"
                                                                                                           //Я думаю, что самым простым было бы просто иметь метод, который будет подсчитывать врагов в пределах определенного радиуса определенной позиции сетки. Тогда вы можете указать радиус в сериализованном поле и не связывать одно действие с другим (Move и Shoot)
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = targetCountAtPosition * 10 + 40, //Ячейка с самым большим количеством стреляемых целей будет в ПРИОРИТЕТЕ. Например если у вас есть позиция сетки, в которой нет стреляемых целей, и другая позиция сетки, в которой есть одна стреляемая цель, ИИ перейдет на вторую позицию сетки, поскольку значение действия основано на количестве стреляемых целей.
        };
        // ВОЗМОЖНЫЕ ВАРИАНТЫ УСЛОЖНЕНИЯ Эта логика может легко учитывать другие факторы… например, если здоровье юнита составляет менее 20%, юнит может пожелать рассмотреть возможность перехода на плитку, на которой НЕТ стреляемых целей.
        // Вы могли бы назначить дополнительный вес плиткам со стреляемыми целями, у которых меньше здоровья, чем плиткам со стреляемыми целями с более высоким здоровьем…
        // Здесь есть много возможностей, помня, конечно, что добавление такой логики может увеличить время, необходимое врагам для расчета наилучших действий.
    }

    //Враги преследовали моих игроков более агрессивно.
    //https://community.gamedev.tv/t/more-aggressive-enemy/220615?_gl=1*ueppqc*_ga*NzQ2MDMzMjI4LjE2NzY3MTQ0MDc.*_ga_2C81L26GR9*MTY3OTE1NDA5Ni4zMS4xLjE2NzkxNTQ1MjYuMC4wLjA.

    public override string GetToolTip()
    {
        return "цена - " + GetActionPointCost() + "\n" +
            "дальность - " + GetMaxActionDistance();
    }

    public override int GetMaxActionDistance()
    {
        return maxMoveDistance;
    }


#if PATHFINDING
    private void OnDestroy()
    {
        _seeker.pathCallback -= Seeker_OnPathComplete; 
        UnitActionSystem.Instance.OnSelectedUnitChanged -= UnitActionSystem_OnSelectedUnitChanged;  
    }
#endif
}
