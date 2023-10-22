using System;
using UnityEngine;



public class GridSystemXY<TGridObject>  // Сеточная система // Стандартный класс C#// Будем использовать конструктор для создания нашей сетки поэтому он не наследует MonoBehaviour/
                                      //<TGridObject> - Generic, для того чтобы GridSystemXY могла работать не только с GridSystemXY но и с др. передоваемыми ей типами Объектов Сетки
                                      // Generic - позволит исользовать часть кода GridSystemXY для ПОИСКА пути (при этом нам не придется дублировать код и делать похожий класс)

{   
    protected int _width;     // Ширина
    protected int _height;    // Высота
    protected float _cellSize;// Размер ячейки
    protected Transform _anchorGridTransform; // Якорь сетки
    protected TGridObject[,] _gridObjectArray; // Двумерный массив объектов сетки
    protected Vector3 _offsetСenterCell;// Сделаем смещение что бы центр ячейки не совподал  с (0.0) transform.position родителя 
    protected GridName _gridName;

    public GridSystemXY(GridParameters gridParameters, Func<GridSystemXY<TGridObject>, Vector2Int, TGridObject> createGridObject)  // Конструктор // Func - это встроенный ДЕЛЕГАТ (третий параметр в аргументе это тип<TGridObject> который возвращает наш делегат и назавем его createGridObject)
    {
        _width = gridParameters.width;
        _height = gridParameters.height;
        _cellSize = gridParameters.cellSize;
        _anchorGridTransform = gridParameters.anchorGridTransform;
        _gridName = gridParameters.gridName;
        _offsetСenterCell = new Vector3(0.5f, 0.5f, 0) * _cellSize; // Расчитаем смещение для нашей сетки , чтобы начало сетки (0,0) было в центре нулевой ячейки

        _gridObjectArray = new TGridObject[_width, _height]; // создаем массив сетки определенного размером width на height
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                _gridObjectArray[x, y] = createGridObject(this, gridPosition); // Вызовим делегат createGridObject и в аргумент передадим нашу GridSystemXY и позиции сетки. Сохраняем его в каждой ячейким сетки в двумерном массив где x,y это будут индексы массива.

                //Debug.DrawLine(GetWorldPositionCenterСornerCell(gridPosition), GetWorldPositionCenterСornerCell(gridPosition) + Vector3.right* .2f, Color.white, 1000); // для теста нарисуем маленькие линии в центре каждой ячейки сетки
            }
        }       
    }

    public GridName GetGridName() { return _gridName; } // получит Имя сетки

    public virtual Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - _anchorGridTransform.position - _offsetСenterCell; // переведите обратно в 0 (удалим смещение якоря относительно начала координат и смещение центра нулевой ячекйки) 
        return new Vector2Int
            (
            Mathf.RoundToInt(localPosition.x / _cellSize),  // Применяем Mathf.RoundToInt для преоброзования float в int
            Mathf.RoundToInt(localPosition.y / _cellSize)
            );
    }

    public virtual Vector3 GetWorldPositionCenterСornerCell(Vector2Int gridPosition) // Получим мировые координаты центра ячейки (относительно  _anchorGridTransform)
    {
        return new Vector3(gridPosition.x, gridPosition.y, 0) * _cellSize + _anchorGridTransform.position + _offsetСenterCell;   // Получим координаты нашей ячеки с учетом ее масштаба, добавим смещение самой сетки _anchorGridTransform и смещения нулевой ячейки
                                                                                                                    // мы хотим что бы центр ячейки был смещен относительного нашего _anchorGridTransform  и левый угол сетки совподал с ***Grid.transform.position                                                                                                              
    }
    public virtual Vector3 GetWorldPositionLowerLeftСornerCell(Vector2Int gridPosition) // Получим мировые координаты нижнего левого угола ячейки (относительно  _anchorGridTransform)
    {
        return new Vector3(gridPosition.x, gridPosition.y, 0) * _cellSize + _anchorGridTransform.position;   // Получим координаты нашей ячеки с учетом ее масштаба, добавим смещение самой сетки _anchorGridTransform                                                                                        
    }


    public void CreateDebugObject(Transform debugPrefab) // Создать объект отладки ( public что бы вызвать из класса Testing и создать отладку сетки)   // Тип Transform и GameObject взаимозаменяемы т.к. у любого GameObject есть Transform и у каждого Transform есть прикрипленный GameObject
                                                         // В основном для работы нам нужен Transform игрового объекта. Если в аргументе указать тип GameObject, тогда в методе, если бы мы хотели после создани GameObject изменить его масштаб, нам придется делать дополнительный шаг "debugGameObject.Transform.LocalScale..."
                                                         // Поэтому для краткости кода в аргументе указываем тип Transform.
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y); // Позиция сетке

                Transform debugTransform = GameObject.Instantiate(debugPrefab, GetWorldPositionCenterСornerCell(gridPosition), Quaternion.identity);  // Созданим экземпляр отладочного префаба(debugPrefab) в каждой ячейки сетки // Т.к. нет расширения MonoBehaviour мы не можем напрямую использовать Instantiate только через GameObject.Instantiate
                GridDebugObject gridDebugObject = debugTransform.GetComponent<GridDebugObject>(); // У созданного объкта возьмем компонент GridDebugObject
                gridDebugObject.SetGridObject(GetGridObject(gridPosition)); // Вызываем медот SetGridObject() и передаем туда объекты сетки находящийся в позиции _gridPositioAnchor // GetGridObject(_gridPositioAnchor) as GridObjectUnitXZ - временно определим <TGridObject> как GridObjectUnitXZ
            }
        }
    }

    public TGridObject GetGridObject(Vector2Int gridPosition) // Вернет объекты которые находятся в данной позиции сетки .Сделаем публичной т.к. будем вдальнейшем вызывать из вне.
    {
        return _gridObjectArray[gridPosition.x, gridPosition.y]; // x,y это индексы массива по которым можем вернуть данные массива
    }

    public bool IsValidGridPosition(Vector2Int gridPosition) // Является ли Допустимой Сеточной Позицией  // Проверяем что переданные нам значения больше 0 и меньше ширины и высоты нашей сетки
    {
        return gridPosition.x >= 0 &&
                gridPosition.y >= 0 &&
                gridPosition.x < _width &&
                gridPosition.y < _height;
    }

    public Vector3 GetRotationAnchorGrid()
    {
        return _anchorGridTransform.eulerAngles;
    }

    public Transform GetAnchorGrid()
    {
        return _anchorGridTransform;
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
