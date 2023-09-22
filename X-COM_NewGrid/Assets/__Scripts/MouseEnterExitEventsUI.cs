using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseEnterExitEventsUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler // События входа и выхода мышью (ИНТЕРФЕЙС -Обработчик входа указателя и Обработчик выхода указателя)
                                                                                                          // Обрабатывает когда мыш проходит по объекту и выходит из объета
                                                                                                          // Будем применять при наведении на кнопки выбора зданий
                                                                                                          // У background подсказки надо убрать галочку Raycast target
{

    public event EventHandler OnMouseEnter; // Событие - при входе мыши
    public event EventHandler OnMouseExit; // Событие - при выходе мыши

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnMouseEnter?.Invoke(this, EventArgs.Empty); // Запустим событие

        Debug.Log("вошли");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnMouseExit?.Invoke(this, EventArgs.Empty); // Запустим событие
    }

}
