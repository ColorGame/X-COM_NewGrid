using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnitSystemUI : MonoBehaviour
{
    [SerializeField] private Transform _enemyUnitButonUIPrefab; // � ���������� ������� ������ ������
    [SerializeField] private Transform _enemyUnitButonContainerTransform; // � ���������� ���������  ��������� ��� ������( ���������� � ����� � Canvas)


    private List<EnemyUnitButtonUI> _enemyUnitButonUIList; // ������ ������ ��������� ������

    private void Awake()
    {
        _enemyUnitButonUIList = new List<EnemyUnitButtonUI>();
    }

    private void Start()
    {
        UnitManager.OnAnyUnitDeadAndRemoveList += UnitManager_OnAnyUnitDeadAndRemoveList;// ������� ����� ���� ���� � ������ �� ������
        UnitManager.OnAnyEnemyUnitSpawnedAndAddList += UnitManager_OnAnyEnemyUnitSpawnedAndAddList;// ����� ��������� ���� ������ � �������� � ������
       
        CreateEnemyUnitButtons(); // ������� ������ ���  ������
    }

    private void UnitManager_OnAnyEnemyUnitSpawnedAndAddList(object sender, EventArgs e)
    {
        CreateEnemyUnitButtons();
    }   

    private void UnitManager_OnAnyUnitDeadAndRemoveList(object sender, EventArgs e)
    {
        CreateEnemyUnitButtons();
    }

    private void CreateEnemyUnitButtons() // ������� ������ ��� ������ ������
    {
        foreach (Transform buttonTransform in _enemyUnitButonContainerTransform) // ������� ��������� � ��������
        {
            Destroy(buttonTransform.gameObject); // ������ ������� ������ ������������� � Transform
        }

        _enemyUnitButonUIList.Clear(); // ������� ����� ������

        foreach (Unit unit in UnitManager.Instance.GetEnemyUnitList())// ��������� ��������� ������
        {
            if (unit.gameObject.activeSelf) // ���� ���� �������� �� 
            {
                Transform actionButtonTransform = Instantiate(_enemyUnitButonUIPrefab, _enemyUnitButonContainerTransform); // ��� ������� ����� �������� ������ ������ � �������� �������� - ��������� ��� ������
                EnemyUnitButtonUI enemyUnitButonUI = actionButtonTransform.GetComponent<EnemyUnitButtonUI>();// � ������ ������ ��������� EnemyUnitButtonUI
                enemyUnitButonUI.SetUnit(unit);//������� � ���������

                _enemyUnitButonUIList.Add(enemyUnitButonUI);// ������� � ������ ���� ������
            }
        }
    }
}
