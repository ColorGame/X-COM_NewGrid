using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class EnemyUnitButtonUI : MonoBehaviour // ������ �����
{
    public static event EventHandler<Unit> OnAnyEnemylyUnitButtonPressed; // ������ ����� ������ �������������� ����� // static - ���������� ��� event ����� ������������ ��� ����� ������ �� �������� �� ���� ������� � ��� �������� ������.
                                                                           // ������� ��� ������������� ����� ������� ��������� �� ����� ������ �� �����-���� ���������� �������, ��� ����� �������� ������ � ������� ����� �����, ������� ����� ��������� ���� � �� �� ������� ��� ������ �������. 

    [SerializeField] private Button _button; // ���� ������   
   

    private Unit _enemyUnit;   

   
    public void SetUnit(Unit unit)
    {
        _enemyUnit = unit; 

        // �.�. ������ ��������� ����������� �� � ������� ����������� � ������� � �� � ����������
        //������� ������� ��� ������� �� ���� ������// AddListener() � �������� ������ �������� �������- ������ �� �������. ������� ����� ��������� �������� ����� ������ () => {...} 
        _button.onClick.AddListener(() =>
        {
            OnAnyEnemylyUnitButtonPressed?.Invoke(this, _enemyUnit); // �������� ������� � ��������� ����������� �����    
        });
    }   

   
}
