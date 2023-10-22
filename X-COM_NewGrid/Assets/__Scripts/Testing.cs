using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    [SerializeField] private Transform _gridDebugObjectPrefab; // Префаб отладки сетки //Передоваемый тип должен совподать с типом аргумента метода CreateDebugObject
    [SerializeField] private Unit _unit;
    private GridSystemXZ<GridObjectUnitXZ> _gridSystem;

    private void Start()
    {


        //ТЕСТ CreateDebugObject визуализация координат сетки в каждой ячейки
        /*_gridSystemTiltedXYList = new GridSystemXZ(10, 10, 2f); // построим сетку 10 на 10 и размером 2 еденицы
        _gridSystemTiltedXYList.CreateDebugObject(_gridDebugObjectPrefab); // Создадим наш префаб в каждой ячейки

        Debug.Log(new GridPositionXZ(5, 7)); // тестируем что возвращает GridPositionXZ*/
    }

    private void Update()
    {
        //ТЕСТ 
        //Debug.Log(_gridSystemTiltedXYList.GetGridPosition(MouseWorld.GetPosition())); // Получим положение сетки прямо под мышкой

        //ТЕСТ Допустимых Сеточных Позиция для Действий




        
        if(Input.GetKeyDown(KeyCode.T))
        {
            //Для отрисовки линии следования из (0,0) в место указателя мыши
            /*GridPositionXZ _mouseGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());

            GridPositionXZ startGridPosition = new GridPositionXZ(0, 0);

            List<GridPositionXZ> gridPositionList = PathfindingMonkey.Instance.FindPath(startGridPosition, _mouseGridPosition);

            for (int i = 0; i < gridPositionList.Count -1 ; i++)
            {
                Debug.DrawLine(
                    LevelGrid.Instance.GetWorldPositionCenterСornerCell(gridPositionList[i]),
                    LevelGrid.Instance.GetWorldPositionCenterСornerCell(gridPositionList[i + 1]),
                    Color.white,
                    10f
                    );
            }*/

            
        }      
    }
}

