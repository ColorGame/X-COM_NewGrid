using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // ��� ���������� ��������

public class SpinAction : BaseAction // ��������
{

    private float _totalSpinAmount; // ����� ����� ��������
    private int _maxSpinDistance = 1; //������������ ���������

    private void Update()
    {
        if (!_isActive) // ���� �� ������� �� ...
        {
            return; // ������� � ���������� ��� ����
        }

        float spinAddAmount = 360f * Time.deltaTime;
        transform.eulerAngles += new Vector3(0, spinAddAmount, 0); // ������� ����� ������ ��� �

        _totalSpinAmount += spinAddAmount; // ������ ���� �������� ���� �� ������� �� ���������

        if (_totalSpinAmount >= 360f) // ��� ������ �������� �� 360 ��������...
        {
            ActionComplete(); // ������� ������� ������� �������� ���������
        }
    }


    // ������������� TakeAction (��������� �������� (�����������)) // �� ������������� Spin � TakeAction � �������� � �������� GridPositionXZ
    public override void TakeAction(GridPositionXZ gridPosition, Action onActionComplete) // (onActionComplete - �� ���������� ��������). � �������� ����� ���������� ������� Action 
                                                                                        // � ������ ������ �������� �������� ������� �� �� ���������� - GridPositionXZ _gridPositioAnchor - �� �������� ���� ��� ���� ����� ��������������� ��������� ������� ������� TakeAction.
                                                                                        // ���� ������ ������, ������� ���������� -
                                                                                        // public class BaseParameters{} 
                                                                                        // � ����������� � ������� ����� �������������� ��� ������� �������� -
                                                                                        // public SpinBaseParameters : BaseParameters{}
                                                                                        // ����� ������� - public override void TakeAction(BaseParameters baseParameters ,Action onActionComplete){
                                                                                        // SpinBaseParameters spinBaseParameters = (SpinBaseParameters)baseParameters;}
    {
        _totalSpinAmount = 0f; // ��� ������ Spin() �������� �������� �������                               

        ActionStart(onActionComplete); // ������� ������� ������� ����� �������� // �������� ���� ����� � ����� ����� ���� �������� �.�. � ���� ������ ���� EVENT � �� ������ ����������� ����� ���� ��������
    }

    public override string GetActionName() // ��������� ������� �������� //������� ������������� ������� �������
    {
        return "��������";
    }

    public override List<GridPositionXZ> GetValidActionGridPositionList() // �������� ������ ���������� �������� ������� ��� �������� // ������������� ������� �������
                                                                        // ���������� �������� ������� ��� �������� �������� ����� ������ ��� ����� ���� 
    {
        GridPositionXZ unitGridPosition = _unit.GetGridPosition(); // ������� �������� ������� ����� 

        return new List<GridPositionXZ> // �������� ������ � ������� � ��� �������� ������� �����, � ����� ������ ��
        {
            unitGridPosition
        };
    }

    public override int GetActionPointCost() // ������������� ������� ������� // �������� ������ ����� �� �������� (��������� ��������)
    {
        return 1;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPositionXZ gridPosition) //�������� �������� ���������� �� // ������������� ����������� ������� �����
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0, //�������� ������ �������� ��������. ����� ��������� �������� ���� ������ ������� ������� �� �����, 
        };
    }

    public override string GetToolTip()
    {
        return "���� - " + GetActionPointCost() + "\n" +
                "��������� - " + GetMaxActionDistance();
    }

    public override int GetMaxActionDistance()
    {
        return _maxSpinDistance;
    }
}
