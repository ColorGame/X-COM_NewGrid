//#define HEX_GRID_SYSTEM //ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА //  В C# определен ряд директив препроцессора, оказывающих влияние на интерпретацию исходного кода программы компилятором. 
//Эти директивы определяют порядок интерпретации текста программы перед ее трансляцией в объектный код в том исходном файле, где они появляются. 

using System.Collections.Generic;
using UnityEngine;

// НАСТРОИМ ПОРЯДОК ВЫПОЛНЕНИЯ СКРИПТА PathfindingMonkey, добавим в Project Settings/ Script Execution Order и поместим выполнение PathfindingMonkey выше Default Time, чтобы PathfindingMonkey запустился РАНЬШЕ до того как ктонибудь совершит поиск пути 

public class PathfindingMonkey : MonoBehaviour // Поиск пути // Логика которая будет обрабатывать данные для поиска пути (похож на класс LevelGrid)
{

    public static PathfindingMonkey Instance { get; private set; }   //(ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                                     // instance - экземпляр, У нас будет один экземпляр PathfindingMonkey можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.



    private const int MOVE_STRAIGHT_COST = 10; // Стоимость движения прямо (для удобства взяли 10 а не 1 что бы не использовать float)
    private const int MOVE_DIAGONAL_COST = 14; // Стоимость движения по диоганали ( расчитывается по теореме пифагора корень квадратный из суммы квадратов катетов прямоугольного треугольника. И опять для удобства взяли 14 а не 1,4 что бы не использовать float)

    //переменные для ДРУГОЙ РЕАЛИЗАЦИИ МЕТОДА GetNeighbourList() БОЛЕЕ КОРОТКАЯ
    /*// Координаты сеточных позиций для поиска соседних ячеек
    private GridPositionXZ UP = new(0, 1);
    private GridPositionXZ DOWN = new(0, -1);
    private GridPositionXZ RIGHT = new(1, 0);
    private GridPositionXZ LEFT = new(-1, 0);*/

    [SerializeField] private Transform _pathfindingGridDebugObject; // Префаб отладки сетки //Передоваемый тип должен совподать с типом аргумента метода CreateDebugObject
    [SerializeField] private LayerMask _obstaclesCoverLayerMask; // маска слоя препятствия (появится в ИНСПЕКТОРЕ) НАДО ВЫБРАТЬ Obstacles и Cover// ВАЖНО НА ВСЕХ СТЕНАХ В ИГРЕ УСТАНОВИТЬ МАСКУ СЛОЕВ -Obstacles кроме дверей а на укрытиях Cover
    [SerializeField] private LayerMask _floorLayerMask; // маска слоя пола (появится в ИНСПЕКТОРЕ) НАДО ВЫБРАТЬ MousePlane -это пол и есть
    [SerializeField] private Transform _pathfindingLinkContainer; // контейнер ссылок для поиска пути // В инспекторе закинуть из сцены PathfindingLinkContainer
    
    private GridParameters _gridParameters;
    private int _width;     // Ширина
    private int _height;    // Высота
    private float _cellSize;// Размер ячейки
    private Vector3 _globalOffset = Vector3.zero; // Смещение сетки в мировых координатах
    private int _floorAmount; // Количество этажей
    private List<GridSystemXZ<PathNode>> _gridSystemList; //Список частных сеточных систем с типом PathNode
    private List<PathfindingLink> _pathfindingLinkList; //Список ссылок для поиска пути (т.к. ссылок на одной позиции может быть несколько - например угл здания с которого можно спуститься в 2 направления)
    private List<GridPositionXZ> _gridPositionInAirList; // Список позиций в воздухе

    private void Awake()
    {
        // Если ты акуратем в инспекторе то проверка не нужна
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one Pathfinding!(Там больше, чем один Pathfinding!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр PathfindingMonkey прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this;
    }

    public void Setup(GridParameters gridParameters, int floorAmount) // Настроим Узлы поиска путей
    {
        _width = gridParameters.width;
        _height = gridParameters.height;
        _cellSize = gridParameters.cellSize;
        _floorAmount = floorAmount;

        _gridSystemList = new List<GridSystemXZ<PathNode>>();
        _gridPositionInAirList = new List<GridPositionXZ>();

        for (int floor = 0; floor < floorAmount; floor++)
        {
            GridSystemXZ<PathNode> gridSystem = new GridSystemXZ<PathNode>(_gridParameters, // ПОСТРОИМ СЕТКУ 10 на 10 и размером 2 еденицы на этаже floor c заданной высотою этажа и в каждой ячейки создадим объект типа PathNode
                    (GridSystemXZ<PathNode> g, GridPositionXZ gridPosition) => new PathNode(gridPosition), floor, LevelGrid.FLOOR_HEIGHT);   //в 5 параметре аргумента зададим функцию ананимно через лямбду => new PathNode(_gridPositioAnchor) И ПЕРЕДАДИМ ЕЕ ДЕЛЕГАТУ. (лямбда выражение можно вынести в отдельный метод)

           // gridSystem.CreateDebugObject(_pathfindingGridDebugObject); // Создадим наш префаб в каждой ячейки// Отладочный объект можно убирать т.к. настроика завершена

            _gridSystemList.Add(gridSystem);
        }

        // В цикле настроим все ячейки на возможность проходимости, будем стрелять лучом из каждой позиции и для начала Сделаем все узлы непроходимыми и если 1ый луч попал в пол то сделаем проходимым а 2рым луч проверим на препядствие _obstaclesDoorMousePlaneCoverLayerMask, если они есть установим эту ячейку опять не проходимой
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                for (int flooor = 0; flooor < floorAmount; flooor++)
                {
                    GridPositionXZ gridPosition = new GridPositionXZ(x, z, flooor); // Позиция сетке
                    Vector3 worldPosition = LevelGrid.Instance.GetWorldPosition(gridPosition); // Получим мировые координаты
                    float raycastOffsetDistance = 1f; // Дистанция смещения луча

                    GetNode(x, z, flooor).SetIsWalkable(false); //для начала Сделаем все узлы непроходимыми

                    //Выстрелим ЛУЧ. // Для данного коллайдера что бы все правильно работало нужно стрелять СВЕРХУ ВНИЗ,  поэтому сместим его вверх, и будем стрелять вниз, лучом размер которого в два раза больше чем смещение вниз, луч будет взаимодействовать с выбранной маской слоя
                    if (Physics.Raycast(
                         worldPosition + Vector3.up * raycastOffsetDistance,
                         Vector3.down,
                         raycastOffsetDistance * 2,
                         _floorLayerMask)) // Если луч попал в пол то сделаем его ПРОХОДИМЫМ (Raycast -вернет bool переменную)
                    {
                        GetNode(x, z, flooor).SetIsWalkable(true);
                    }
                    else // Если под ячейкой нет пола то добавим ее в воздушный список
                    {
                        _gridPositionInAirList.Add(gridPosition);
                    }

                    //Выстрелим ЛУЧ. ЛУч не может стрелять внутри колайдера(внутри стены), поэтому сместим его вниз, и будем стрелять вверх, лучом размер которого в два раза больше чем смещение вниз, луч будет взаимодействовать с выбранной маской слоя
                    // МОЖНО СДЕЛАТЬ НАСТРОЙКИ в UNITY и не смещать выстрел луча. Project Settings/Physics/Queries Hit Backfaces - поставить галочку, и тогда можно стрелять из нутри колайдера
                    if (Physics.Raycast(
                         worldPosition + Vector3.down * raycastOffsetDistance,
                         Vector3.up,
                         raycastOffsetDistance * 2,
                         _obstaclesCoverLayerMask)) // Если луч попал в препятствие или в укрытие то установим ячейку НЕ ПРОХОДИМОЙ (Raycast -вернет bool переменную)
                    {
                        GetNode(x, z, flooor).SetIsWalkable(false);
                    }
                }
            }
        }

        _pathfindingLinkList = new List<PathfindingLink>();

      //  if (_pathfindingLinkContainer.childCount !=0) // если контейнер содержит дочерние объекты то
       // {
            foreach (Transform pathfindingLinkTransform in _pathfindingLinkContainer) // Переберем дочерние элементы контейнера ссылок для поиска пути
            {
                if (pathfindingLinkTransform.TryGetComponent(out PathfindingLinkMonoBehaviour pathfindingLinkMonoBehaviour)) // У дочернего объекта попробуем вернуть PathfindingLinkMonoBehaviour
                {
                    _pathfindingLinkList.Add(pathfindingLinkMonoBehaviour.GetPathfindingLink()); // // получим сеточные позиции и добавим в Список ссылок для поиска пути
                }
            }
       // }

    }

    public List<GridPositionXZ> FindPath(GridPositionXZ startGridPosition, GridPositionXZ endGridPosition, out int pathLength) // Найти путь // Если мы используете ключевое слово "out", то функция должна установить значение для этой переменной pathLength-длина пути, то есть она должна вывести значение.
    {
        List<PathNode> openList = new List<PathNode>();     // Создадим список "Открытый Список" - ВСЕ УЗЛЫ КОТОРЫЕ ПРЕДСТОИТ НАЙТИ 
        List<PathNode> closedList = new List<PathNode>();   // Создадим список "Закрытый Список" - ВСЕ УЗЛЫ, В КОТОРЫХ УЖЕ БЫЛ ПРОИЗВЕДЕН ПОИСК

        PathNode startNode = GetGridSystem(startGridPosition.floor).GetGridObject(startGridPosition); // Получим стартовый узел, вернем GridObjectUnitXZ типа PathNode в переданной нам startGridPosition
        PathNode endNode = GetGridSystem(endGridPosition.floor).GetGridObject(endGridPosition); // Получим конечный узел, вернем GridObjectUnitXZ типа PathNode в переданной нам endGridPosition

        openList.Add(startNode); //Добавим в список начальный узел

        for (int x = 0; x < _width; x++) // В цикле пройдем все узлы сеточной системы и сбросим параметры
        {
            for (int z = 0; z < _height; z++)
            {
                for (int flooor = 0; flooor < _floorAmount; flooor++)
                {
                    GridPositionXZ gridPosition = new GridPositionXZ(x, z, flooor); // Позиция сетке

                    PathNode pathNode = GetGridSystem(flooor).GetGridObject(gridPosition); // Получим объект сетки типа PathNode

                    pathNode.SetGCost(int.MaxValue); // Установим параметр G максимальным числом
                    pathNode.SetHCost(0);            // Установим параметр H =0
                    pathNode.CalculateFCost();       // Расчитаем параметр F
                    pathNode.ResetCameFromPathNode(); // Сбросим ссылку на предыдущий Узел Пути 
                }
            }
        }

        startNode.SetGCost(0); // G -это стоимость перехода от предыдущего узла к настоящему который тестируем. Мы еще не откуда не перемещались поэтому G=0
        startNode.SetHCost(CalculateHeuristicDistance(startGridPosition, endGridPosition)); // Установим вычисленный H компонент
        startNode.CalculateFCost();

        while (openList.Count > 0) // если в Открытом списке есть элементы то это означает что есть узлы для поиска. Цикл будет работать пока не переберет все ячейки
        {
            PathNode currentNode = GetLowestFCostPathNode(openList); // Получим узел пути с наименьшей стоимостью F из openList и зделаем его Текущим Узлом

            if (currentNode == endNode) // Проверяем равен ли наш текущий узел конечному узлу
            {
                // Достигли конечного узла
                pathLength = endNode.GetFCost(); // Вернем стоимость длины пути
                return CalculatePath(endNode); // Вернем вычисленый путь
            }

            openList.Remove(currentNode); // Удалим текущий узел из открытого списка
            closedList.Add(currentNode);  // И добавим в закрытый список // ЭТО ОЗНАЧАЕТ ЧТО МЫ ИСКАЛИ ПО ЭТОМУ УЗЛУ

            foreach (PathNode neighbourNode in GetNeighbourList(currentNode)) // Переберем всех соседние узлы
            {
                if (closedList.Contains(neighbourNode)) // Проверяем что бы соседний узел небыл в "Закрытом списке"
                {
                    //МЫ УЖЕ ИСКАЛИ ПО ЭТОМУ УЗЛУ
                    continue;
                }

                //  ПРОПУСТИМ УЗЛЫ НЕ ДОСТУПНЫЕ ДЛЯ ХОТЬБЫ
                if (!neighbourNode.GetIsWalkable()) // Проверим, соседний узел - доступен для хотьбы // Есди Не доступен то добавим в "Закрытый Список" и перейдем к следующему узлу
                {
                    closedList.Add(neighbourNode);
                    continue;
                }
#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА

                int tentativeGCost =currentNode.GetGCost() + MOVE_STRAIGHT_COST;// Предварительная стоимость G = текущая G + Стоимость движения прямо

#else//в противном случае компилировать 
                int tentativeGCost = currentNode.GetGCost() + CalculateHeuristicDistance(currentNode.GetGridPosition(), neighbourNode.GetGridPosition()); // Предварительная стоимость G = текущая G + стоимость перемещения от текущего к соседнему узлу
#endif

                if (tentativeGCost < neighbourNode.GetGCost()) // Если Предварительная стоимость G меньше стоимости G соседнего узла(по умолчанию она имеет значение MaxValue ) (Мы нашли луший путь что бы попасть в этот соседний узел)
                {
                    neighbourNode.SetCameFromPathNode(currentNode); // Установить - на соседний узел пришел - С текущего Узла Пути
                    neighbourNode.SetGCost(tentativeGCost); // Установим на соседний узем расчитанный параметр G
                    neighbourNode.SetHCost(CalculateHeuristicDistance(neighbourNode.GetGridPosition(), endGridPosition)); // Установим параметр H от текущего до конечного
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode)) // Если открытый список не содержит этого соседнего узла то добавим его
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }

        // ПУТЬ НЕ НАЙДЕН
        pathLength = 0;
        return null;
    }

    public int CalculateHeuristicDistance(GridPositionXZ gridPositionA, GridPositionXZ gridPositionB) // Вычислить эвристическое(приблизительное) расстояние между любыми 2мя сеточными позициями ЭТО БУДЕТ ПАРАМЕТР "H"
    {
#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА

        return Mathf.RoundToInt(MOVE_STRAIGHT_COST *Vector3.Distance(GetGridSystem(gridPositionA.floor).GetWorldPosition(gridPositionA), GetGridSystem(gridPositionB.floor).GetWorldPosition(gridPositionB)));


#else//в противном случае компилировать 
        GridPositionXZ gridPositionDistance = gridPositionA - gridPositionB; //Может быть отрицательным (вычислить приращение координат между первой и второй точкой - на сколько надо сместить относительно первой точки)

        // для движение только по прямой (МОЖНО ИСПОЛЬЗОВАТЬ ДЛЯ ПОИСКА СЕТОК ДВЕРЕЙ)
        //int totalDistance = Mathf.Abs(gridPositionDistance.x) + Mathf.Abs(gridPositionDistance.y); // получим сумму координат по модулю (не учитывает движение по диогонали только по прямой. Например из точки (0,0) в точку (3,2) двигался три раза в право и два раза вверх итого 5)

        int xDistsnce = Mathf.Abs(gridPositionDistance.x); //перемещение с (0,0) на (1,1) можем заменить 1 диоганалью, вместо ДВУХ перемещений по прямой // Если с (0,0) на (2,1) то диогональ будет все равно ОДНА. Поэтому для расчета количества диоганалей надо взять минимальное число из полученных (x,y)
        int zDistsnce = Mathf.Abs(gridPositionDistance.z);
        int remaining = Mathf.Abs(xDistsnce - zDistsnce); // Оставшееся растояние будет перемещением по прямой, берем по модулю
        int CalculateDistance = MOVE_DIAGONAL_COST * Mathf.Min(xDistsnce, zDistsnce) + MOVE_STRAIGHT_COST * remaining; // вернем вычесленное расстояние в системе GridPositionXZ как сумму смещений по диогонали и смещений по прямой
        return CalculateDistance;
#endif
    }

    private PathNode GetLowestFCostPathNode(List<PathNode> pathNodeList) // Получить узел пути с наименьшей стоимостью F  в аргумент передадим список узлов пути// для оптимизации поиска чтобы не искать по всем подрят
                                                                         // Если несколько ячеек имеют одинаковое наименьшее F то вернется первый попавшийся в списке
                                                                         // В нашем случае мы перебераем ячейки против часовой (зависит от формирования списка в методе GetNeighbourList())
    {
        PathNode lowestFCostPathNode = pathNodeList[0]; // Возьмем первый элемент в списке
        for (int i = 0; i < pathNodeList.Count; i++) // переберем в цикле другие элементы
        {
            if (pathNodeList[i].GetFCost() < lowestFCostPathNode.GetFCost()) // Если параметр F нового элемента меньше текущего то сделаем его lowestFCostPathNode
            {
                lowestFCostPathNode = pathNodeList[i];
            }
        }
        return lowestFCostPathNode;
    }

    private GridSystemXZ<PathNode> GetGridSystem(int floor) // Получить Сеточную систему для данного этажа
    {
        return _gridSystemList[floor];
    }

    private PathNode GetNode(int x, int z, int floor) // Получить Узел с координатами по GridPositionXZ(x,y)
    {
        return GetGridSystem(floor).GetGridObject(new GridPositionXZ(x, z, floor));
    }

    private List<PathNode> GetNeighbourList(PathNode currentNode) // Получить список соседей для currentNode (для ШЕСТИГРАННОЙ 6 сосседей , для КВАДРАТНОЙ  8 соседей
    {
        List<PathNode> neighbourList = new List<PathNode>(); // Инициализируем новый список соседей

        GridPositionXZ gridPosition = currentNode.GetGridPosition();

        // Сделаем ПРОВЕРКИ чтобы не уйти за границы сетки иначе получем ошибку ссылку на нулевой объект

        if (gridPosition.x - 1 >= 0)
        {
            //Left
            neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z + 0, gridPosition.floor)); // Добавим узел слева

#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА
// для шестиграной код пустой
#else//в противном случае компилировать 
            if (gridPosition.z - 1 >= 0)
            {
                //Left Down
                neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z - 1, gridPosition.floor)); // Добавим узел слева в низу
            }

            if (gridPosition.z + 1 < _height)
            {
                //Left Up
                neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z + 1, gridPosition.floor)); // Добавим узел слева в верху
            }
#endif
        }

        if (gridPosition.x + 1 < _width)
        {
            //Right
            neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z + 0, gridPosition.floor)); // Добавим узел справа

#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА
// для шестиграной код пустой
#else//в противном случае компилировать 
            if (gridPosition.z - 1 >= 0)
            {
                //Right Down
                neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z - 1, gridPosition.floor)); // Добавим узел справа в низу
            }

            if (gridPosition.z + 1 < _height)
            {
                //Right Up
                neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z + 1, gridPosition.floor)); // Добавим узел справа в верху
            }
#endif
        }

        // Для Шенстигранной если НЕчетная - то Слева внизу и Слева в верху. Четная - то Справа в верху и Справа внизу. (Соседей с другой стороны добавляем через проверку oddRow)
        if (gridPosition.z - 1 >= 0)
        {
            //Down
            neighbourList.Add(GetNode(gridPosition.x + 0, gridPosition.z - 1, gridPosition.floor)); // Добавим узел снизу
        }
        if (gridPosition.z + 1 < _height)
        {
            //Up
            neighbourList.Add(GetNode(gridPosition.x + 0, gridPosition.z + 1, gridPosition.floor)); // Добавим узел сверху
        }


#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА

        bool oddRow = gridPosition.z % 2 == 1; // oddRow - НЕЧЕТНЫЙ РЯД.Если истина то мы находимся в нечетном ряду

        if (oddRow) // Если НЕЧЕТНЫЙ
        {
            if (gridPosition.x + 1 < _width)
            {
                if (gridPosition.z - 1 >= 0)
                {
                    neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z - 1, gridPosition.floor));// Добавим узел справа внизу
                }
                if (gridPosition.z + 1 < _height)
                {
                    neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z + 1, gridPosition.floor)); // Добавим узел справа в верху
                }
            }
        }
        else // Если ЧЕТНЫЙ
        {
            if (gridPosition.x - 1 >= 0)
            {
                if (gridPosition.z - 1 >= 0)
                {
                    neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z - 1, gridPosition.floor)); // Добавим узел слева внизу
                }
                if (gridPosition.z + 1 < _height)
                {
                    neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z + 1, gridPosition.floor)); // Добавим узел слева в верху
                }
            }
        }
#endif

        List<PathNode> totalNeighbourList = new List<PathNode>(); // общий список соседей
        totalNeighbourList.AddRange(neighbourList); //AddRange- Добавляет элементы указанной коллекции в конец списка

        List<GridPositionXZ> pathfindingLinkGridPositionList = GetPathfindingLinkConnectedGridPositionList(gridPosition); // Получить список сеточных позиций которые связаны с этой сеточной позицией

        foreach (GridPositionXZ pathfindingLinkGridPosition in pathfindingLinkGridPositionList) // Переберем список и преобразуем GridPositionXZ(сеточную позицию) в PathNode(узел пути)
        {
            totalNeighbourList.Add(
                GetNode( // Получим Узел с координатами
                    pathfindingLinkGridPosition.x,
                    pathfindingLinkGridPosition.z,
                    pathfindingLinkGridPosition.floor
                )
            );
        }


        return totalNeighbourList;
    }

    private List<GridPositionXZ> GetPathfindingLinkConnectedGridPositionList(GridPositionXZ gridPosition) // Получить список сеточных позиций которые связаны с этой сеточной позицией
    {
        List<GridPositionXZ> gridPositionList = new List<GridPositionXZ>(); // Инициализируем список

        foreach (PathfindingLink pathfindingLink in _pathfindingLinkList) // Переберем сисок ссылок для поиска пути
        {
            // Если наша сеточная позиция совподает  с одной из позиции ссылок то добавим в список вторую позицию ссылки
            if (pathfindingLink.gridPositionA == gridPosition)
            {
                gridPositionList.Add(pathfindingLink.gridPositionB);
            }
            if (pathfindingLink.gridPositionB == gridPosition)
            {
                gridPositionList.Add(pathfindingLink.gridPositionA);
            }
        }

        return gridPositionList;
    }

    //2 ДРУГАЯ РЕАЛИЗАЦИЯ МЕТОДА GetNeighbourList() БОЛЕЕ КОРОТКАЯ
    /* private List<PathNode> GetNeighbourList(PathNode currentNode) // Получить список соседей для currentNode
     {
         List<PathNode> neighbourList = new List<PathNode>(); // Инициализируем новый список соседей

         GridPositionXZ _gridPositioAnchor = currentNode.GetGridPosition(); // Получим сеточную позицию центра поиска

         GridPositionXZ[] neigboursPositions = //Создадим массив сеточных позиций соседних ячеик
         {
         _gridPositioAnchor + UP,
         _gridPositioAnchor + UP + RIGHT,
         _gridPositioAnchor + RIGHT,
         _gridPositioAnchor + RIGHT + DOWN,
         _gridPositioAnchor + DOWN,
         _gridPositioAnchor + DOWN + LEFT,
         _gridPositioAnchor + LEFT,
         _gridPositioAnchor + LEFT + UP
         };

         foreach (GridPositionXZ p in neigboursPositions) // В цикле проверим на допустипость этих сеточных позиций
         {
             if (_gridSystemTiltedXYList.IsValidGridPosition(p))
             {
                 neighbourList.Add(GetNode(p.x, p.y));
             }                
         }

         return neighbourList;
     }*/

    private List<GridPositionXZ> CalculatePath(PathNode endNode) //Вычисление пути (будем разматывать клубок в обратном направлении)
    {
        List<PathNode> pathNodeList = new List<PathNode>(); // инициализируем список Узлов пути
        pathNodeList.Add(endNode);
        PathNode currentNod = endNode;

        while (currentNod.GetCameFromPathNode() != null) //В цикле добавим подключенные узлы в список. Подключенные узлы - это то с которого на него пришли (у всех узлов 1 ссылка на соседний с которого пришли). Последий узел будет стартовый и у него GetCameFromPathNode() = null - цикл прирвется
        {
            pathNodeList.Add(currentNod.GetCameFromPathNode());//Добавим в список наш подключенный узел
            currentNod = currentNod.GetCameFromPathNode(); // Подключенный узел становиться текущим
        }

        pathNodeList.Reverse(); // Т.к. мы начали с конца надо перевернуть наш список что бы получить тропинку от старта к финишу
        // Переведем наш список "PathNode-узлов" в список "GridPositionXZ - позиций сетки"
        List<GridPositionXZ> gridPositionList = new List<GridPositionXZ>();

        foreach (PathNode pathNode in pathNodeList)
        {
            gridPositionList.Add(pathNode.GetGridPosition());
        }

        return gridPositionList;
    }

    public void SetIsWalkableGridPosition(GridPositionXZ gridPosition, bool isWalkable) // Установить что Можно или Нельзя (в зависимости от isWalkable)  ходить по переданной в аргумент Сеточной Позиции
    {
        GetGridSystem(gridPosition.floor).GetGridObject(gridPosition).SetIsWalkable(isWalkable);
    }

    public bool IsWalkableGridPosition(GridPositionXZ gridPosition) // Можно ходить по переданной в аргумент Сеточной Позиции
    {
        return GetGridSystem(gridPosition.floor).GetGridObject(gridPosition).GetIsWalkable();
    }

    public bool HasPath(GridPositionXZ startGridPosition, GridPositionXZ endGridPosition) // Имеем путь ?
    {
        return FindPath(startGridPosition, endGridPosition, out int pathLength) != null; // Если есть путь от startGridPosition к endGridPosition то вернется true если пути нет вернктся false (out int pathLength добавили что бы соответствовала сигнатуре)
    }

    public int GetPathLength(GridPositionXZ startGridPosition, GridPositionXZ endGridPosition) // Получить длину пути (параметр F -endGridPosition)  
    {
        FindPath(startGridPosition, endGridPosition, out int pathLength);
        return pathLength;
    }

    public List<GridPositionXZ> GetGridPositionInAirList() //Получить Список позиций в воздухе
    {
        return _gridPositionInAirList;
    }

}
