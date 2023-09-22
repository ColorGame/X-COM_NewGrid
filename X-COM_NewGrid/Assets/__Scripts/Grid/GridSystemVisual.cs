//#define HEX_GRID_SYSTEM //ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА //  В C# определен ряд директив препроцессора, оказывающих влияние на интерпреpublic enumтацию исходного кода программы компилятором. 
//Эти директивы определяют порядок интерпретации текста программы перед ее трансляцией в объектный код в том исходном файле, где они появляются. 

using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnitActionSystem;


// Добавим GridSystemVisual для запуска после времени по умолчанию, поскольку мы хотим, чтобы визуальные эффекты запускались после всего остального.
// (Project Settings/ Script Execution Order и поместим выполнение GridSystemVisual НИЖЕ Default Time)
public class GridSystemVisual : MonoBehaviour //Сеточная система визуализации  Визуализация возможных ходов по сетке 
{
    public static GridSystemVisual Instance { get; private set; }   //(ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                                    // instance - экземпляр, У нас будет один экземпляр UnitActionSystem можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.

    [Serializable] // Чтобы созданная структура могла отображаться в инспекторе
    public struct GridVisualTypeMaterial    //Визуал сетки Тип Материала // Создадим структуру можно в отдельном классе. Наряду с классами структуры представляют еще один способ создания собственных типов данных в C#
    {                                       //В данной структуре объединям состояние сетки с материалом
        public GridVisualType gridVisualType;
        public Material materialGrid;
    }

    public enum GridVisualType //Визуальные состояния сетки
    {
        White,
        Blue,
        BlueSoft,
        Red,
        RedSoft,
        Yellow,
        YellowSoft,
        Green,
        GreenSoft,
    }
           

    [SerializeField] private List<GridVisualTypeMaterial> _gridVisualTypeMaterialListQuad; // Список тип материала визуального состояния сетки Квадрат (Список из кастомного типа данных) визуального состояния сетки // В инспекторе под каждое состояние перетащить соответствующий материал сетки
    [SerializeField] private List<GridVisualTypeMaterial> _gridVisualTypeMaterialListHex; // Список тип материала визуального состояния сетки Шестиугльник (Список из кастомного типа данных) визуального состояния сетки // В инспекторе под каждое состояние перетащить соответствующий материал сетки


    private List<GridPosition> _validGridPositionForGrenadeActionList; // Будем кэшировоть -Список Допустимых сеточных позиций для действия Гранаты 
    private List<GridPosition> _validGridPositionForComboActionList; // Будем кэшировоть -Список Допустимых сеточных позиций для действия комбо

    private GridSystemVisualSingle[,,] _gridSystemVisualSingleArray; // Трехмерный массив    

    // Для отладки шестигранной сетки (отображение ячейки под мышкой)
    // private GridSystemVisualSingle _lastSelectedGridSystemVisualSingle; // Последний Выбранная Сеточная Позиция Визуализация Еденицы

    private void Awake() //Для избежания ошибок Awake Лучше использовать только для инициализации и настроийки объектов
    {
        // Если ты акуратем в инспекторе то проверка не нужна
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one UnitActionSystem!(Там больше, чем один UnitActionSystem!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр UnitActionSystem прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this;
    }

    private void Start()
    {
        _gridSystemVisualSingleArray = new GridSystemVisualSingle[ // создаем массив определенного размером widthX на heightY  и FloorAmount
            LevelGrid.Instance.GetWidth(),
            LevelGrid.Instance.GetHeight(),
            LevelGrid.Instance.GetFloorAmount()
        ];

        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                for (int floor = 0; floor < LevelGrid.Instance.GetFloorAmount(); floor++)  // переберем все этажи
                {
                    GridPosition gridPosition = new GridPosition(x, z, floor);                    

                    Transform gridSystemVisualSingleTransform = Instantiate(GameAssets.Instance.gridSystemVisualSinglePrefab, LevelGrid.Instance.GetWorldPosition(gridPosition), Quaternion.identity); // Создадим наш префаб в каждой позиции сетки

                    _gridSystemVisualSingleArray[x, z, floor] = gridSystemVisualSingleTransform.GetComponent<GridSystemVisualSingle>(); // Сохраняем компонент GridSystemVisualSingle в трехмерный массив где x,z,floor это будут индексы массива.
                }
            }
        }

        UnitActionSystem.Instance.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged; // подпишемся на событие Выбранное Действие Изменено (когда меняется активное действие в блоке кнопок мы запустим событие Event)
        UnitActionSystem.Instance.OnBusyChanged += Instance_OnBusyChanged; //подпишемся на событие Занятость Изменена 
        MouseWorld.OnMouseGridPositionChanged += MouseWorld_OnMouseGridPositionChanged;// подпишемся на событие Сеточная Позиция Мыши Изменена для включения и выключения круга показ. диапазон поражения гранаты
        MoveAction.OnAnyUnitPathComplete += MoveAction_OnAnyUnitPathComplete; //подпишемся У любого Юнита Вычислен Путь
        //  LevelGrid.Instance.OnAnyUnitMovedGridPosition += LevelGrid_OnAnyUnitMovedGridPosition; // подпишемся на событие Любой Юнит Перемещен в Сеточной позиции

        UpdateGridVisual();


        // Отабразим всю сетку для отладки HEX(ЗАКОМЕНТИРОВАЛ СТРОКУ в методе MouseWorld_OnMouseGridPositionChanged)
        /* for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
         {
             for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
             {
                 _gridSystemVisualSingleArray[x, z].
                     Show(GetGridVisualTypeMaterial(GridVisualType.White));
             }
         }*/

    }

    private void MoveAction_OnAnyUnitPathComplete(object sender, Unit unit)
    {
        if (UnitActionSystem.Instance.GetSelectedUnit() == unit) // Если путь вычислен у выделенного юнита то обновим визуализацию (чтобы не обновлять во время хотьбы врагов)
        {
            UpdateGridVisual();
        }
    }
    private void Instance_OnBusyChanged(object sender, OnUnitSystemEventArgs e)
    {
        if (e.selectedAction is not MoveAction) // Если Занятость Изменена НЕ во время движения то обновим. MoveAction сам подписан на OnBusyChanged и в нем расчитывает путь.
                                                // После расчета вызывается событие OnAnyUnitPathComplete там и будем обновлять
        {
            UpdateGridVisual();
        }
    }
    private void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }

    /*private void LevelGrid_OnAnyUnitMovedGridPosition(object sender, LevelGrid.OnAnyInventoryMovedGridPositionEventArgs e)
    {
        UpdateGridVisual();

        //Основное изменение, которое вы можете сделать, это перестать обновлять траекторию всякий раз, когда юнит перемещает позицию,
        //вместо этого вычислять траекторию только тогда, когда Юнит достигает конечной точки.
        //Это устранит заикание при движении устройства, и оно будет ощущаться намного плавнее.
    }*/

    // Для отладки шестигранной сетки (отображение ячейки под мышкой)
    /*private void Update()
    {

        if (_lastSelectedGridSystemVisualSingle != null)
        {
            _lastSelectedGridSystemVisualSingle.HideSelected(); // Спрячим последний выбранный GridSystemVisualSingle
        }

        Vector3 mouseWorldPosition = MouseWorld.GetPosition(); //Мировая позиция мыши
        GridPosition _gridPosition = LevelGrid.Instance.GetGridPosition(mouseWorldPosition); // Получим сеточную позицию мыши
        if (LevelGrid.Instance.IsValidGridPosition(_gridPosition)) // Если это допустимая сеточная позиция то
        {
            _lastSelectedGridSystemVisualSingle = _gridSystemVisualSingleArray[_gridPosition.x, _gridPosition.z]; // Сохраним наш захваченый GridSystemVisualSingle
        }

        if (_lastSelectedGridSystemVisualSingle != null)
        {
            _lastSelectedGridSystemVisualSingle.ShowSelected();// Покажем последний выбранный GridSystemVisualSingle
        }
    }*/

    private void MouseWorld_OnMouseGridPositionChanged(object sender, MouseWorld.OnMouseGridPositionChangedEventArgs e)
    {
        // При изминении положении мыши будем обновлять отображение круга или квадрата, который показывает радиус действия гранаты

        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction(); // Получим Выбранное Действие

        switch (selectedAction) // Переключатель состояний визуала в зависимости от Выбранного Действия
        {
            case GrenadeFragmentationAction grenadeFragmentationAction:// Во время кидания ГРАНАТЫ

                UpdateVisualDamageCircleGrenade(grenadeFragmentationAction, e);
                break;

            case GrenadeStunAction grenadeStunAction:// Во время кидания ГРАНАТЫ

                UpdateVisualDamageCircleGrenade(grenadeStunAction, e);
                break;

            case GrenadeSmokeAction grenadeSmokeAction:

                UpdateVisualDamageQuadGrenade(grenadeSmokeAction, e);
                break;

            case ComboAction comboAction:
                if (comboAction.GetState() == ComboAction.State.ComboStart)
                {
                    UpdateVisualSelectedQuadComboAction(comboAction, e);
                }
                break;
        }
    }

    private void UpdateVisualSelectedQuadComboAction(ComboAction comboAction, MouseWorld.OnMouseGridPositionChangedEventArgs e)
    {
        _gridSystemVisualSingleArray[e.lastMouseGridPosition.x, e.lastMouseGridPosition.z, e.lastMouseGridPosition.floor].HideQuad(); // Скроем квадрат на предыдущей ячейки
        GridPosition mouseGridPosition = e.newMouseGridPosition; // Сеточная позиция мыши

        if (_validGridPositionForComboActionList.Contains(mouseGridPosition)) // Если Сеточная позиция мыши входит в Допустимый диапазон то ...
        {
            _gridSystemVisualSingleArray[mouseGridPosition.x, mouseGridPosition.z, mouseGridPosition.floor].ShowQuad(GetGridVisualTypeMaterial(GridVisualType.Red)); // Покажем для нашей сеточной позиции квадрат красного цвета (куда надо переместить захваченного врага)
        }
    }

    private void UpdateVisualDamageQuadGrenade(GrenadeAction grenadeAction, MouseWorld.OnMouseGridPositionChangedEventArgs e)
    {
        _gridSystemVisualSingleArray[e.lastMouseGridPosition.x, e.lastMouseGridPosition.z, e.lastMouseGridPosition.floor].HideQuad(); // Скроем квадрат на предыдущей ячейки
        GridPosition mouseGridPosition = e.newMouseGridPosition; // Сеточная позиция мыши

        if (_validGridPositionForGrenadeActionList.Contains(mouseGridPosition)) // Если Сеточная позиция мыши входит в Допустимый диапазон то ...
        {
            float damageRadiusInWorldPosition = grenadeAction.GetDamageRadiusInWorldPosition();
            _gridSystemVisualSingleArray[mouseGridPosition.x, mouseGridPosition.z, mouseGridPosition.floor].ShowQuad(GetGridVisualTypeMaterial(GridVisualType.RedSoft), damageRadiusInWorldPosition); // Покажем для нашей сеточной позиции квадрат поражения от гранаты и передадим размер  и цвет материала
        }
    }

    private void UpdateVisualDamageCircleGrenade(GrenadeAction grenadeAction, MouseWorld.OnMouseGridPositionChangedEventArgs e)
    {
        _gridSystemVisualSingleArray[e.lastMouseGridPosition.x, e.lastMouseGridPosition.z, e.lastMouseGridPosition.floor].HideСircle(); // Скроем круг на предыдущей ячейки
        GridPosition mouseGridPosition = e.newMouseGridPosition; // Сеточная позиция мыши

        if (_validGridPositionForGrenadeActionList.Contains(mouseGridPosition)) // Если Сеточная позиция мыши входит в Допустимый диапазон то ...
        {
            float damageRadiusInWorldPosition = grenadeAction.GetDamageRadiusInWorldPosition();
            _gridSystemVisualSingleArray[mouseGridPosition.x, mouseGridPosition.z, mouseGridPosition.floor].ShowСircle(damageRadiusInWorldPosition); // Покажем для нашей сеточной позиции круг поражения от гранаты и передадим радиус круга
        }
    }

    private void HideAllGridPosition() // Скрыть все позиции сетки
    {
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                for (int floor = 0; floor < LevelGrid.Instance.GetFloorAmount(); floor++)  // переберем все этажи
                {
                    _gridSystemVisualSingleArray[x, z, floor].Hide();
                }
            }
        }
    }

    private void ShowGridPositionRange(GridPosition gridPosition, int range, GridVisualType gridVisualType, bool showFigureRhombus) // Показать возможный Диапазон Сеточных Позиций для стрельбы (в аргументе передаем Сеточную позицию, Радиус стрельбы, Тип состояния Визуала Сетки, Булевая переменная если надо отобразить в виде Ромба то передаем true, если в виде КВАДРАТА то - false )
    {
        // По аналогии как в ShootAction в методе "public override List<GridPosition> GetValidActionGridPositionList()"

        List<GridPosition> gridPositionList = new List<GridPosition>();

        for (int x = -range; x <= range; x++)  // Юнит это центр нашей позиции с координатами unitGridPosition, поэтому переберем допустимые значения в условном радиусе range
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z, 0); // Смещенная сеточная позиция. Где началом координат(0,0) является сам юнит 
                GridPosition testGridPosition = gridPosition + offsetGridPosition; // Тестируемая Сеточная позиция

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) // Проверим Является ли testGridPosition Допустимой Сеточной Позицией если нет то переходим к след циклу
                {
                    continue; // continue заставляет программу переходить к следующей итерации цикла 'for' игнорируя код ниже
                }

                LevelGridNode levelGridNode = LevelGrid.Instance.GetGridNode(testGridPosition);

                //если в этой позиции нет узла пути значит эта GridPosition висит в воздухе  
                if (levelGridNode == null)
                {
                    continue; // Пропустим эту позицию
                }

                
                /*//Исключим сеточные позиции которые висят в воздухе
                if (PathfindingMonkey.Instance.GetGridPositionInAirList().Contains(testGridPosition))
                {
                    continue;
                }*/

                if (showFigureRhombus)
                {
                    // Для диапазона действия сделаем ромб а не квадрат
                    int testDistance = Mathf.Abs(x) + Mathf.Abs(z); // Сумма двух положительных координат сеточной позиции
                    if (testDistance > range) //Получим фигуру из ячеек в виде ромба // Если юнит в (0,0) то ячейка с координатами (5,4) уже не пройдет проверку 5+4>7
                    {
                        continue;
                    }
                }
                gridPositionList.Add(testGridPosition);
            }
        }

        ShowGridPositionList(gridPositionList, gridVisualType); // Покажем возможный Диапазон стрельбы
    }

    public void ShowGridPositionList(List<GridPosition> gridPositionlist, GridVisualType gridVisualType)  //Покажем Список GridPosition (в аргументе передается список GridPosition и состояние визуализации сетки gridVisualType)
    {
        foreach (GridPosition gridPosition in gridPositionlist) // в цикле перебереи список и Покажем(включим) только те позиции которые нам передали
        {
            _gridSystemVisualSingleArray[gridPosition.x, gridPosition.z, gridPosition.floor].
                Show(GetGridVisualTypeMaterial(gridVisualType)); // В аргумент Show предадим материал в зависимости от переданного нам события
        }
    }

    public void UpdateGridVisual() // Обнавление визуала сетки
    {
        HideAllGridPosition(); // Скрыть все позиции сетки

        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit(); //Получим Выбранного Юнита

        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction(); // Получим Выбранное Действие

        GridVisualType gridVisualType;  // Создадим перем типа GridVisualType

        switch (selectedAction) // Переключатель состояний визуала сетки в зависимости от Выбранного Действия
        {
            default: // Этот кейс будет выполняться по умолчанию если нет соответствующих selectedAction
            case MoveAction moveAction: // Во время ХОТЬБЫ -БЕЛЫЙ
                gridVisualType = GridVisualType.White;
                break;

            case SpinAction spinAction: // Во время ПОВОРОТА -ГОЛУБОЙ
                gridVisualType = GridVisualType.Blue;
                break;

            case HealAction healAction: // Во время ЛЕЧЕНИЯ -ЗЕЛЕНЫЙ
                gridVisualType = GridVisualType.Green;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), healAction.GetMaxActionDistance(), GridVisualType.GreenSoft, false); // Покажем диапазон 
                break;

            case ShootAction shootAction: // Во время СТРЕЛЬБЫ -КРАСНЫЙ
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), shootAction.GetMaxActionDistance(), GridVisualType.RedSoft, true); // Покажем диапазон стрельбы РОМБ-true
                break;

            case GrenadeAction grenadeAction:// Во время кидания ГРАНАТЫ -ЖЕЛТЫЙ
                gridVisualType = GridVisualType.Yellow;
                _validGridPositionForGrenadeActionList = grenadeAction.GetValidActionGridPositionList(); //Сохраним -Список Допустимых сеточных позиций для действия Гранаты                                 
                break;

            case SwordAction swordAction: // Во время удара МЕЧОМ -красный
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), swordAction.GetMaxActionDistance(), GridVisualType.RedSoft, false); // Покажем диапазон удара
                break;

            case InteractAction interactAction: // Во время ВЗАИМОДЕЙСТВИЯ -ГОЛУБОЙ
                gridVisualType = GridVisualType.Blue;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), interactAction.GetMaxActionDistance(), GridVisualType.BlueSoft, false); // Покажем диапазон 
                break;

            case ComboAction comboAction: // Во время Выбора комбо союзника -ЗЕЛЕНЫЙ    

                ComboAction.State statecomboAction = comboAction.GetState(); // Получим состояние КОМБО 
                switch (statecomboAction)
                {
                    default:
                    case ComboAction.State.ComboSearchPartner: // Если ищем Партнера для комбо
                        gridVisualType = GridVisualType.Green;
                        ShowGridPositionRange(selectedUnit.GetGridPosition(), comboAction.GetMaxActionDistance(), GridVisualType.GreenSoft, false); // Покажем диапазон 
                        break;

                    case ComboAction.State.ComboSearchEnemy: // Если Ищем врага то
                        gridVisualType = GridVisualType.Red;
                        ShowGridPositionRange(selectedUnit.GetGridPosition(), comboAction.GetMaxActionDistance(), GridVisualType.RedSoft, true); // Покажем диапазон  РОМБ-true
                        break;

                    case ComboAction.State.ComboStart: // Ячейки куда надо перетащить
                        gridVisualType = GridVisualType.RedSoft;
                        _validGridPositionForComboActionList = selectedAction.GetValidActionGridPositionList();
                        break;

                }
                break;

            case SpotterFireAction spotterFireAction: // Корректировка огня -ЗЕЛЕННЫЙЙ
                gridVisualType = GridVisualType.Green;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), spotterFireAction.GetMaxActionDistance(), GridVisualType.GreenSoft, false); // Покажем диапазон 
                break;
        }

        ShowGridPositionList(selectedAction.GetValidActionGridPositionList(), gridVisualType); // Покажем(включим) только те позиции которые нам передали (в аргумент передаем список допустимых позиций сетки выбранного действия, и Тип состояния визуализации который нам выдал switch)
    }



#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА

    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType) //(Вернуть Материал в зависимости от Состояния) Получить Тип Материала для Сеточной Визуализации в зависимости от переданного в аргумент Состояния Сеточной Визуализации
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in _gridVisualTypeMaterialListHex) // в цикле переберем Список тип материала визуального состояния сетки 
        {
            if (gridVisualTypeMaterial.gridVisualType == gridVisualType) // Если  Состояние сетки(gridVisualType) совподает с переданным нам состояние то ..
            {
                return gridVisualTypeMaterial.materialGrid; // Вернем материал соответствующий данному состоянию сетки
            }
        }

        Debug.LogError("Не смог найти GridVisualTypeMaterial для GridVisualType " + gridVisualType); // Если не найдет соответсвий выдаст ошибку
        return null;
    }


#else//в противном случае компилировать

    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType) //(Вернуть Материал в зависимости от Состояния) Получить Тип Материала для Сеточной Визуализации в зависимости от переданного в аргумент Состояния Сеточной Визуализации
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in _gridVisualTypeMaterialListQuad) // в цикле переберем Список тип материала визуального состояния сетки 
        {
            if (gridVisualTypeMaterial.gridVisualType == gridVisualType) // Если  Состояние сетки(gridVisualType) совподает с переданным нам состояние то ..
            {
                return gridVisualTypeMaterial.materialGrid; // Вернем материал соответствующий данному состоянию сетки
            }
        }

        Debug.LogError("Не смог найти GridVisualTypeMaterial для GridVisualType " + gridVisualType); // Если не найдет соответсвий выдаст ошибку
        return null;
    }
#endif
}
