using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TooltipUI : MonoBehaviour// Всплывающая подсказка в ПОЛЬЗОВАТЕЛЬСКОМ ИНТЕРФЕЙСЕ (порядок сортировки в canvas зависит от полжения в иерархии, вкладка находящияся внизу отрисовывается поверх всех - поэтому всплывающ. подсказку держим внизу списка Canvas)
{                                       // У background и text подсказки надо убрать галочку Raycast target - что бы подсказка не мерцала

    public static TooltipUI Instance { get; private set; }//(ОДНОЭЛЕМЕНТНЫЙ ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                          // instance - экземпляр, У нас будет один экземпляр ResourceManager можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.
                                                          // static Это значит то, что данная сущность принадлежит не конкретному объекту класса, а всему классу, как типу данных. Другими словами, если обычное поле класса принадлежит объекту, и у каждого конкретного объекта есть как бы своя копия данного поля, то статическое поле одно для всего класса.

    [SerializeField] private RectTransform canvasRectTransform; // Трансформ холста 

    private RectTransform rectTransform; // Трансформ всплывающей подсказки TooltipUI // В ИНСПЕКТОРЕ НАСТРОИТЬ ЯКОРЬ НА НИЖНИЙ ЛЕВЫЙ УГОЛ, это координаты (0, 0) что бы он правильно следовал за мышью 
    private TextMeshProUGUI textMeshPro; // Текст подсказки (тип TextMeshProUGUI надо выбрать и буквами UI)
    private RectTransform backgroundRectTransform; // Трансформ заднего фона
    private TooltipTimer tooltipTimer; // Время отображения подсказки (расширяющий класс)

    private void Awake()
    {
        Instance = this; //созданим экземпляр класса

        rectTransform = GetComponent<RectTransform>();
        textMeshPro = transform.Find("text").GetComponent<TextMeshProUGUI>();
        backgroundRectTransform = transform.Find("background").GetComponent<RectTransform>();

        Hide(); // скроем подсказки
    }

    private void Update()
    {
        HandleFollowMouse(); // Обработка следования за мышью

        if (tooltipTimer != null) // Если задано время то - запустим таймер
        {
            tooltipTimer.timer -= Time.deltaTime;
            if (tooltipTimer.timer <= 0) //По истечении времени скроем подсказку
            {
                Hide();
            }
        }
    }

    private void HandleFollowMouse()// Обработка следования за мышью
    {
        // У Canvas - холста есть свое масштабирование Canvas Scale, которое мы настроили 1280*720. При изминении размеров Game сцены происходит изменение Scale(масштаб) на холсте
        // что бы подсказка следовала четко за мышью надо учитывать Scale холста
        Vector2 anchoredPosition = Input.mousePosition / canvasRectTransform.localScale.x; //Позицию мыши Поделим на масштаб холста (Будем использовать только Х компонент т.к. Y Z меняются пропорционально)

        // Позаботимся что бы подсказка всегда оставалась на экране
        if (anchoredPosition.x + backgroundRectTransform.rect.width > canvasRectTransform.rect.width) // Если фон подсказки выходит за правую сторону холста то ...
        {
            anchoredPosition.x = canvasRectTransform.rect.width - backgroundRectTransform.rect.width; // Зафиксируем на правой стороне 
        }
        if (anchoredPosition.y + backgroundRectTransform.rect.height > canvasRectTransform.rect.height)
        {
            anchoredPosition.y = canvasRectTransform.rect.height - backgroundRectTransform.rect.height;
        }

        rectTransform.anchoredPosition = anchoredPosition; // Сместим положени якоря "всплывающей подсказки" в полжение мыши
    }

    private void SetText(string tooltipText) // Установим текст в подсказке
    {
        textMeshPro.SetText(tooltipText);
        textMeshPro.ForceMeshUpdate(); // Принудительное обновление текса в этом кадре

        Vector2 textSize = textMeshPro.GetRenderedValues(false); // Получим размер текста (false чтобы учитывать все символы)
        Vector2 padding = new Vector2(8, 8); // Заполнение (что бы был отступ от текста) 
        backgroundRectTransform.sizeDelta = textSize + padding; // Изменим размер заднего фона в зависимости от длины текста
    }

    public void Show(string tooltipText, TooltipTimer tooltipTimer = null) // Отображение подсказки (текст -который надо отобразить, Время отображения подсказки)
                                                                           // Добавление  "= null" делает этот параметр не обязательным, некоторым подсказкам таймер не нужен
    {
        this.tooltipTimer = tooltipTimer;
        gameObject.SetActive(true);
        SetText(tooltipText);
        HandleFollowMouse(); // Обновим расположение подсказки
    }

    public void Hide() // Скрытие подсказки
    {
        gameObject.SetActive(false);
    }


    public class TooltipTimer // РАСШИРИМ КЛАСС
    {
        public float timer;
    }
}
