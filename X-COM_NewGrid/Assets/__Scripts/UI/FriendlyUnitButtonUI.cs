using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class FriendlyUnitButtonUI : MonoBehaviour
{
    public static event EventHandler<Unit> OnAnyFriendlyUnitButtonPressed; // Нажата любая кнопка дружественного юнита // static - обозначает что event будет существовать для всего класса не зависимо от того скольго у нас созданно Юнитов.
                                                                           // Поэтому для прослушивания этого события слушателю не нужна ссылка на какую-либо конкретную единицу, они могут получить доступ к событию через класс, который затем запускает одно и то же событие для каждой единицы. 

    [SerializeField] private TextMeshProUGUI _nameUnitText; // Имя юнита
    [SerializeField] private TextMeshProUGUI _actionPointsText; // Очки действия
    [SerializeField] private Button _button; // Сама кнопка
    [SerializeField] private GameObject _selectedButtonVisualUI; // Будем включать и выкл. GameObject что бы скрыть или показать рамку кнопки // В инспекторе надо закинуть рамку
    [SerializeField] private Image _healthBarImage; // в инспекторе закинуть шкалу здоровья "Bar"
    [SerializeField] private Image _backgroundImage; // в инспекторе закинуть шкалу здоровья "Bar"
    [SerializeField] private Image _actionImage; // в инспекторе закинуть шкалу здоровья "Bar"

    private Unit _unit;
    private Color _nameUnitTextColor;
    private Color _actionPointsTextColor;
    private Color _healthBarImageColor;
    private Color _backgroundImageColor;
    private Color _actionImageColor;

   
    public void SetUnit(Unit unit)
    {
        _unit = unit;
        _nameUnitText.text = _unit.gameObject.name.ToUpper(); // Зададим имя с ЗАГЛАВНОЙ БУКВЫ
        _actionPointsText.text = _unit.GetActionPoints().ToString();
        _healthBarImage.fillAmount = _unit.GetHealthNormalized();

        // Сохраним цвета элементов кнопки
        _nameUnitTextColor = _nameUnitText.color;
        _actionPointsTextColor = _actionPointsText.color;
        _healthBarImageColor = _healthBarImage.color;
        _backgroundImageColor = _backgroundImage.color;
        _actionImageColor = _actionImage.color;

        // т.к. кнопки создаются динамически то и события настраиваем в скрипте а не в инспекторе
        //Добавим событие при нажатии на нашу кнопку// AddListener() в аргумент должен получить делегат- ссылку на функцию. Функцию будем объявлять АНАНИМНО через лямбду () => {...} 
        _button.onClick.AddListener(() =>
        {           
            UnitActionSystem.Instance.SetSelectedUnit(_unit, _unit.GetAction<MoveAction>()); // Сделаем юнита выделенным и активируем движение
            OnAnyFriendlyUnitButtonPressed?.Invoke(this, _unit); // Запустим событие и передадим выделенного юнита            
        });
    }

    public void UpdateSelectedVisual() // (Обновление визуала) Включение и выключение визуализации выбора.(вызывается событием при выборе кнопки базового действия)
    {
        Unit unit = UnitActionSystem.Instance.GetSelectedUnit(); // Выделенный Югит
        _selectedButtonVisualUI.SetActive(unit == _unit);   // Включить рамку если это наш юнит // Если не совподает то получим false и рамка отключиться       
    }
    public void UpdateActionPoints()
    {
        _actionPointsText.text = _unit.GetActionPoints().ToString();
    }
    public void UpdateHealthBar()
    {
        _healthBarImage.fillAmount = _unit.GetHealthNormalized();
    }

    //3//{ Третий способ скрыть кнопки когда занят действием
    private void InteractableEnable() // Включить взаимодействие
    {
        _button.interactable = true;
        // Восстановим оригинальные цвета
        _nameUnitText.color = _nameUnitTextColor;
        _actionPointsText.color = _actionPointsTextColor;
        _healthBarImage.color = _healthBarImageColor;
        _backgroundImage.color = _backgroundImageColor;
        _actionImage.color = _actionImageColor;

        UpdateSelectedVisual(); // Обновим отображение рамки кнопки в зависимости от выбранного юнита
    }

    private void InteractableDesabled() // Отключить взаимодействие // Кнопка становиться не активная и меняет цвет(Настраивается в инспекторе color  Desabled)
    {
        _button.interactable = false;

        Color nameUnitTextColor = _nameUnitTextColor; // Сохраним в локальную переменную цвет текста
        Color actionPointsTextColor = _actionPointsTextColor;
        Color healthBarImageColor = _healthBarImageColor;
        Color backgroundImageColor = _backgroundImageColor;
        Color actionImageColor = _actionImageColor;


        nameUnitTextColor.a = 0.1f; // Изменим значение альфа канала
        actionPointsTextColor.a = 0.1f;
        healthBarImageColor.a = 0.1f;
        backgroundImageColor.a = 0.1f;
        actionImageColor.a = 0.1f;

        _nameUnitText.color = nameUnitTextColor; // Изменим текущий цвет текса (сдел прозрачным)
        _actionPointsText.color = actionPointsTextColor;
        _healthBarImage.color = healthBarImageColor;
        _backgroundImage.color = backgroundImageColor;
        _actionImage.color = actionImageColor;

        _selectedButtonVisualUI.SetActive(false); //Отключим рамку
    }

    public void HandleStateButton(bool isBusy) // Обработать состояние кнопки
    {
        if (isBusy) // Если занят
        {
            InteractableDesabled(); // Отключить взаимодействие
        }
        else
        {
            InteractableEnable(); // Включить взаимодействие
        }
    }//3//}
}
