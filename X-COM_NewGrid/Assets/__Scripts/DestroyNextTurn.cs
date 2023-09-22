using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class DestroyNextTurn : MonoBehaviour // ����������� �������� ����� ��������� �����
{
    [SerializeField] private int _turnAmountToDestroy = 2; // ���������� ����� �� �����������
    [SerializeField] private State _state; // ������� ���������� ��� ���������

    private enum State
    {
        Destroy,
        SetActive
    }


    private int _startTurnNumber; // ����� ������� (����) ��� ������ 
    private int _currentTurnNumber; // ������� ����� ������� (����) 

    private void Start()
    {
        _startTurnNumber = TurnSystem.Instance.GetTurnNumber(); // ������� ����� ����

        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged; // ������. �� ������� ��� �������
    }

    private void TurnSystem_OnTurnChanged(object sender, System.EventArgs e)
    {
        _currentTurnNumber = TurnSystem.Instance.GetTurnNumber(); // ������� ������� ����� ����;

        if (_currentTurnNumber - _startTurnNumber == _turnAmountToDestroy)
        {
            // ����� _turnAmountToDestroy ���� ���������  ��� ��������

            switch (_state)
            {
                case State.Destroy:
                    Destroy(gameObject);
                    break;
                case State.SetActive:
                    gameObject.SetActive(false);
                    break;
            }
           
        }
    }

    private void OnDestroy()
    {
        TurnSystem.Instance.OnTurnChanged -= TurnSystem_OnTurnChanged;
    }
}
