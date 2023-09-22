using System;
using Unity.VisualScripting;
using UnityEngine;


public class MouseEnterExitEventsWorld : MonoBehaviour // Будет выполнять роль кнопки
{
    public event EventHandler OnMouseEnterEvent; // Событие - при входе мыши
    public event EventHandler OnMouseExitEvent; // Событие - при выходе мыши
    public event EventHandler OnMouseDownEvent; // Событие - при нажатии мыши
    public event EventHandler OnMouseUpEvent; // Событие - при отпускании кнопки мыши
    public event EventHandler OnMouseOverEvent; // Событие - когда мышь в области колладера


    public void OnMouseEnter()
    {
        OnMouseEnterEvent?.Invoke(this, EventArgs.Empty); // Запустим событие        
    }

    public void OnMouseExit()
    {
        OnMouseExitEvent?.Invoke(this, EventArgs.Empty); // Запустим событие
    }

    public void OnMouseDown()
    {
        OnMouseDownEvent?.Invoke(this, EventArgs.Empty); // Запустим событие        
    }

    public void OnMouseUp()
    {
        OnMouseUpEvent?.Invoke(this, EventArgs.Empty); // Запустим событие        
    }

    public void OnMouseOver()
    {
        OnMouseOverEvent?.Invoke(this, EventArgs.Empty); // Запустим событие        
    }

}
