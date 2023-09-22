//#define HEX_GRID_SYSTEM //ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА //  В C# определен ряд директив препроцессора, оказывающих влияние на интерпретацию исходного кода программы компилятором. 
//Эти директивы определяют порядок интерпретации текста программы перед ее трансляцией в объектный код в том исходном файле, где они появляются. 
// еще 3 скрипта GridSystemVisual  GridSystemVisualSingle  PathfindingMonkey
using System;
using UnityEngine;

public class GridSystem<TGridObject>  // Сеточная система ШЕСТИГРАННАЯ И КВАДРАТНАЯ // Стандартный класс C#// Будем использовать конструктор для создания нашей сетки поэтому он не наследует MonoBehaviour/
                                      //<TGridObject> - Generic, для того чтобы GridSystem могла работать не только с GridObject но и с др. передоваемыми ей типами Объектов Сетки
                                      // Generic - позволит исользовать часть кода GridSystem для ПОИСКА пути (при этом нам не придется дублировать код и делать похожий класс)

{
    private const float HEX_VERTICAL_OFFSET_MULTIPLIER = 0.75f; //ШЕСТИГРАННЫЙ МНОЖИТЕЛЬ ВЕРТИКАЛЬНОГО СМЕЩЕНИЯ

    private Vector3 _globalOffset; // Смещение сетки в мировых координатах //НУЖНО НАСТРОИТЬ//
    private int _width;     // Ширина
    private int _height;    // Высота
    private float _cellSize;// Размер ячейки
    private int _floor;// Этаж на которой располагается наша сеточная система
    private float _floorHeight;// ВЫсота этажа
    private TGridObject[,] _gridObjectArray; // Двумерный массив объектов сетки


    public GridSystem(int width, int height, float cellSize, Vector3 globalOffset, int floor, float floorHeight, Func<GridSystem<TGridObject>, GridPosition, TGridObject> createGridObject)  // Конструктор
                                                                                                                                                                                             // Func - это встроенный ДЕЛЕГАТ (третий параметр в аргументе это тип<TGridObject> который возвращает наш делегат и назавем его createGridObject)
    {
        _width = width; // если бы мы назвали не _width а widthX то писали код так // this.widthX = widthX;
        _height = height;
        _cellSize = cellSize;
        _floor = floor;
        _floorHeight = floorHeight;
        _globalOffset = globalOffset;

        _gridObjectArray = new TGridObject[width, height]; // создаем массив сетки определенного размером widthX на heightY
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z, floor);
                _gridObjectArray[x, z] = createGridObject(this, gridPosition); // Вызовим делегат createGridObject и в аргумент передадим нашу GridSystem и позиции сетки. Сохраняем его в каждой ячейким сетки в двумерном массив где x,z это будут индексы массива.

                // для теста                
                //Debug.DrawLine(GetWorldPosition(_gridPosition), GetWorldPosition(_gridPosition) + Vector3.right* .2f, Color.white, 1000); // для теста нарисуем маленькие линии в центре каждой ячейки сетки
            }
        }
    }

    public GridSystem(int width, int height, float cellSize, Vector3 globalOffset, Func<GridSystem<TGridObject>, GridPosition, TGridObject> createGridObject)  // Конструктор
                                                                                                                                                                                             // Func - это встроенный ДЕЛЕГАТ (третий параметр в аргументе это тип<TGridObject> который возвращает наш делегат и назавем его createGridObject)
    {
        _width = width; // если бы мы назвали не _width а widthX то писали код так // this.widthX = widthX;
        _height = height;
        _cellSize = cellSize;
        _floor = 0;
        _floorHeight = 0;
        _globalOffset = globalOffset;

        _gridObjectArray = new TGridObject[width, height]; // создаем массив сетки определенного размером widthX на heightY
        
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z, 0);
                _gridObjectArray[x, z] = createGridObject(this, gridPosition); // Вызовим делегат createGridObject и в аргумент передадим нашу GridSystem и позиции сетки. Сохраняем его в каждой ячейким сетки в двумерном массив где x,z это будут индексы массива.

                // для теста                
                //Debug.DrawLine(GetWorldPosition(_gridPosition), GetWorldPosition(_gridPosition) + Vector3.right* .2f, Color.white, 1000); // для теста нарисуем маленькие линии в центре каждой ячейки сетки
            }
        }
    }


    public Vector3 GetWorldPosition(GridPosition gridPosition) // Получить мировое положение
    {
#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА
        return
             new Vector3(gridPosition.x, 0, 0) * _cellSize +
             new Vector3(0, 0, gridPosition.z) * _cellSize * HEX_VERTICAL_OFFSET_MULTIPLIER + // По оси Z надо сместить на 75% от размера ячейки в отличии от квадратной сетки где смещали на весь размер
             (((gridPosition.z % 2) == 1) ? new Vector3(1, 0, 0) * _cellSize * .5f : Vector3.zero) + // Если строка НЕЧЕТНАЯ т.е. -остаток от деления по модулю на 2 равен 1- то сместим ее по оси Х вправо на половину размера ячейки (1%2==1  3%2==1 5%2==1 ...Незабываем что сетка начинается не 1,1 а 0,0  gridPosition.z % 2 - при деление столбиком, целый остаток это и есть наше искомое))
             _globalOffset + //Если хотим сместить сетку в глобальных координатах то зададим эту переменную
             new Vector3(0,gridPosition.floor,0) * _floorHeight; // учтем этаж и высоту этажа

#else //в противном случае компилировать 

        return new Vector3(gridPosition.x, 0, gridPosition.z) * _cellSize + _globalOffset +
            new Vector3(0, gridPosition.floor, 0) * _floorHeight; // учтем этаж и высоту этажа
#endif
    }

    public GridPosition GetGridPosition(Vector3 worldPosition) // Получить сеточное положение (положение относительно нашей созданной сетки)
    {
#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА

        GridPosition roughXZ = new GridPosition( // Приблизительный XZ
                Mathf.RoundToInt(worldPosition.x / _cellSize),
                Mathf.RoundToInt(worldPosition.z / _cellSize / HEX_VERTICAL_OFFSET_MULTIPLIER),
                _floor
        );

        bool oddRow = roughXZ.z % 2 == 1; // oddRow - НЕЧЕТНЫЙ РЯД . Если истина то мы находимся в нечетном ряду

        List<GridPosition> neighbourGridPositionList = new List<GridPosition> // Список Соседних Сеточных Позиций
        {
            roughXZ + new GridPosition(-1, 0, _floor), //Сместимся влево
            roughXZ + new GridPosition(+1, 0, _floor), //Сместимся вправо

            roughXZ + new GridPosition(0, +1, _floor), //Сместимся вверх
            roughXZ + new GridPosition(0, -1, _floor), //Сместимся вниз

            roughXZ + new GridPosition(oddRow ? +1 : -1, +1, _floor), // Если в нечетном ряду то по Х +1(вправо) если нет то Х - 1(влево),  по Z вверх
            roughXZ + new GridPosition(oddRow ? +1 : -1, -1, _floor), // Если в нечетном ряду то по Х +1(вправо) если нет то Х - 1(влево),  по Z вниз
        };

        GridPosition closestGridPosition = roughXZ; // Ближайшая позиция сетки пусть будет = Приьлизительной XZ

        foreach (GridPosition neighbourGridPosition in neighbourGridPositionList) // Переберем список соседних ячеек . Получим растояние от нашей мировой точки(например позиции мыши) до мировой координаты соседней ячейки и сравним с Ближайщей 
        {
            if (Vector3.Distance(worldPosition, GetWorldPosition(neighbourGridPosition)) <
                Vector3.Distance(worldPosition, GetWorldPosition(closestGridPosition)))
            {
                //Соседний Ближе, чем самый близкий
                closestGridPosition = neighbourGridPosition; // Установим этот соседний как самый близкий
            };
        }
        return closestGridPosition;



#else //в противном случае компилировать 

        //переведите обратно в 0 (удалите смещение) перед вычислением в обычном режиме
        Vector3 translated = worldPosition - _globalOffset;
        return new GridPosition
            (
            Mathf.RoundToInt(translated.x / _cellSize),  // Применяем Mathf.RoundToInt для преоброзования float в int
            Mathf.RoundToInt(translated.z / _cellSize),
            _floor
            );
#endif
    }

    public void CreateDebugObject(Transform debugPrefab) // Создать объект отладки ( public что бы вызвать из класса Testing и создать отладку сетки)   // Тип Transform и GameObject взаимозаменяемы т.к. у любого GameObject есть Transform и у каждого Transform есть прикрипленный GameObject
                                                         // В основном для работы нам нужен Transform игрового объекта. Если в аргументе указать тип GameObject, тогда в методе, если бы мы хотели после создани GameObject изменить его масштаб, нам придется делать дополнительный шаг "debugGameObject.Transform.LocalScale..."
                                                         // Поэтому для краткости кода в аргументе указываем тип Transform.
    {
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z, _floor); // Позиция сетке

                Transform debugTransform = GameObject.Instantiate(debugPrefab, GetWorldPosition(gridPosition), Quaternion.identity);  // Созданим экземпляр отладочного префаба(debugPrefab) в каждой ячейки сетки // Т.к. нет расширения MonoBehaviour мы не можем напрямую использовать Instantiate только через GameObject.Instantiate
                GridDebugObject gridDebugObject = debugTransform.GetComponent<GridDebugObject>(); // У созданного объкта возьмем компонент GridDebugObject
                gridDebugObject.SetGridObject(GetGridObject(gridPosition)); // Вызываем медот SetGridObject() и передаем туда объекты сетки находящийся в позиции _gridPosition // GetGridObject(_gridPosition) as GridObject - временно определим <TGridObject> как GridObject
            }
        }
    }

    public TGridObject GetGridObject(GridPosition gridPosition) // Вернет объекты которые находятся в данной позиции сетки .Сделаем публичной т.к. будем вдальнейшем вызывать из вне.
    {
        return _gridObjectArray[gridPosition.x, gridPosition.z]; // x,z это индексы массива по которым можем вернуть данные массива
    }

    public bool IsValidGridPosition(GridPosition gridPosition) // Является ли Допустимой Сеточной Позицией
    {
        return gridPosition.x >= 0 &&
                gridPosition.z >= 0 &&
                gridPosition.x < _width &&
                gridPosition.z < _height &&
                gridPosition.floor == _floor;
        // Проверяем что переданные нам значения больше 0 и меньше ширины и высоты нашей сетки
    }

    public int GetWidth()
    {
        return _width;
    }

    public int GetHeight()
    {
        return _height;
    }

    public float GetCellSize()
    {
        return _cellSize;
    }

}
