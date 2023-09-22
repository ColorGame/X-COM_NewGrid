using System;
using Unity.VisualScripting;
using UnityEngine;


public class MouseEnterExitEventsWorld : MonoBehaviour // ����� ��������� ���� ������
{
    public event EventHandler OnMouseEnterEvent; // ������� - ��� ����� ����
    public event EventHandler OnMouseExitEvent; // ������� - ��� ������ ����
    public event EventHandler OnMouseDownEvent; // ������� - ��� ������� ����
    public event EventHandler OnMouseUpEvent; // ������� - ��� ���������� ������ ����
    public event EventHandler OnMouseOverEvent; // ������� - ����� ���� � ������� ���������


    public void OnMouseEnter()
    {
        OnMouseEnterEvent?.Invoke(this, EventArgs.Empty); // �������� �������        
    }

    public void OnMouseExit()
    {
        OnMouseExitEvent?.Invoke(this, EventArgs.Empty); // �������� �������
    }

    public void OnMouseDown()
    {
        OnMouseDownEvent?.Invoke(this, EventArgs.Empty); // �������� �������        
    }

    public void OnMouseUp()
    {
        OnMouseUpEvent?.Invoke(this, EventArgs.Empty); // �������� �������        
    }

    public void OnMouseOver()
    {
        OnMouseOverEvent?.Invoke(this, EventArgs.Empty); // �������� �������        
    }

}
