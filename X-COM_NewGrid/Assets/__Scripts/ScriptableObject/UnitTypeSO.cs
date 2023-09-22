using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/UnitType")] // ��� �������� ����������� ScriptableObject. � ����������� ���� �� ������� create ��������� ������� ScriptableObjects ������ ����� UnitType(��� ������)
public class UnitTypeSO : ScriptableObject //��� ������ - ��� ��������� ������, ������� ����� ������������ ��� ���������� ������� ������� ������, ���������� �� ����������� ������.
{
    public string nameString;   // ��� �����
    public int health; // ��������
    public int moveDistance; // ��������� �������� (���������� � ����� �����, �������� ���� � ������ ������)
    public int shootDistance; // ��������� ��������
}