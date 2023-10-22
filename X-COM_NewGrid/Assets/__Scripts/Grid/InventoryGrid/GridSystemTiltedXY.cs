using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemTiltedXY<TGridObject> : GridSystemXY<TGridObject>    // Наклоненная сеточная система :наследует стандартную 
                                                                            // Учитывает наклон и поворот сетки
{
    public GridSystemTiltedXY(GridParameters gridParameters, Func<GridSystemXY<TGridObject>, Vector2Int, TGridObject> createGridObject) : base(gridParameters, createGridObject)
    {
    }

    public override Vector2Int GetGridPosition(Vector3 worldPosition) // Получить сеточную позицию относительно якоря сетки (это будет локальным началом координат 0,0)
    {

        Vector3 localPosition = _anchorGridTransform.InverseTransformPoint(worldPosition) - _offsetСenterCell; // переведите обратно в 0 (удалите смещение) // InverseTransformPoint -Преобразует position мыши из мирового пространства в локальное (относительно _anchorGridTransform).  
        return new Vector2Int
            (
            Mathf.RoundToInt(localPosition.x / _cellSize),  // Применяем Mathf.RoundToInt для преоброзования float в int
            Mathf.RoundToInt(localPosition.y / _cellSize)
            );
    }

    public override Vector3 GetWorldPositionCenterСornerCell(Vector2Int gridPosition) // Получим мировые координаты центра ячейки (относительно  _anchorGridTransform)
    {
        Vector3 localPositionXY = new Vector3(gridPosition.x, gridPosition.y, 0) * _cellSize + _offsetСenterCell;   // Получим локальные координаты нашей ячеки с учетом ее масштаба, относительно _anchorGridTransform
                                                                                                                    // мы хотим что бы центр ячейки был смещен относительного нашего _anchorGridTransform  и левый угол сетки совподал с ***Grid.transform.position (для удобства создания сетки, достаточно переместить объект ***Grid в нужное место) поэтому + _offsetСenterCell,
        return _anchorGridTransform.TransformPoint(localPositionXY); // transform.TransformPoint -Преобразует position нашей ячейки из локального пространства(_anchorGridTransform) в мировое пространство(учитывая наклон поворот _anchorGridTransform). т.к. _anchorGridTransform может находиться не в (0,0) и наклонен в мировом пространмтве
    }
    public override Vector3 GetWorldPositionLowerLeftСornerCell(Vector2Int gridPosition) // Получим мировые координаты нижнего левого угола ячейки (относительно  нашей _anchorGridTransform)
    {
        Vector3 localPositionXY = new Vector3(gridPosition.x, gridPosition.y, 0) * _cellSize;   // Получим локальные координаты нашей ячеки с учетом ее масштаба, относительно _anchorGridTransform                                                                                                  
        return _anchorGridTransform.TransformPoint(localPositionXY); // transform.TransformPoint -Преобразует position нашей ячейки из локального пространства(_anchorGridTransform) в мировое пространство(учитывая наклон поворот _anchorGridTransform). т.к. _anchorGridTransform может находиться не в (0,0) и наклонен в мировом пространмтве
    }
}
