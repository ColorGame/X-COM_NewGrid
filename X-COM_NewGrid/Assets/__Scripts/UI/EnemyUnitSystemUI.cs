using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnitSystemUI : MonoBehaviour
{
    [SerializeField] private Transform _enemyUnitButonUIPrefab; // В инспекторе закинем префаб Кнопки
    [SerializeField] private Transform _enemyUnitButonContainerTransform; // В инспекторе назначить  Контейнер для кнопок( находиться в сцене в Canvas)


    private List<EnemyUnitButtonUI> _enemyUnitButonUIList; // Список кнопок Вражеских Юнитов

    private void Awake()
    {
        _enemyUnitButonUIList = new List<EnemyUnitButtonUI>();
    }

    private void Start()
    {
        UnitManager.OnAnyUnitDeadAndRemoveList += UnitManager_OnAnyUnitDeadAndRemoveList;// Событие Любой Юнит Умер И Удален из Списка
        UnitManager.OnAnyEnemyUnitSpawnedAndAddList += UnitManager_OnAnyEnemyUnitSpawnedAndAddList;// Любой вражеский юнит ражден и добавлен в Списка
       
        CreateEnemyUnitButtons(); // Создать Кнопки для  Юнитов
    }

    private void UnitManager_OnAnyEnemyUnitSpawnedAndAddList(object sender, EventArgs e)
    {
        CreateEnemyUnitButtons();
    }   

    private void UnitManager_OnAnyUnitDeadAndRemoveList(object sender, EventArgs e)
    {
        CreateEnemyUnitButtons();
    }

    private void CreateEnemyUnitButtons() // Создать Кнопки для Врагов Юнитов
    {
        foreach (Transform buttonTransform in _enemyUnitButonContainerTransform) // Очистим контейнер с кнопками
        {
            Destroy(buttonTransform.gameObject); // Удалим игровой объект прикрипленный к Transform
        }

        _enemyUnitButonUIList.Clear(); // Очистим сисок кнопок

        foreach (Unit unit in UnitManager.Instance.GetEnemyUnitList())// Переберем вражеских юнитов
        {
            if (unit.gameObject.activeSelf) // Если юнит активный то 
            {
                Transform actionButtonTransform = Instantiate(_enemyUnitButonUIPrefab, _enemyUnitButonContainerTransform); // Для каждого ЮНИТА создадим префаб кнопки и назначим родителя - Контейнер для кнопок
                EnemyUnitButtonUI enemyUnitButonUI = actionButtonTransform.GetComponent<EnemyUnitButtonUI>();// У кнопки найдем компонент EnemyUnitButtonUI
                enemyUnitButonUI.SetUnit(unit);//Назвать и Присвоить

                _enemyUnitButonUIList.Add(enemyUnitButonUI);// Добавим в список нашу кнопку
            }
        }
    }
}
