using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlacedObjectTypeSelect : MonoBehaviour // Выбранный Тип Размещаемого Объекта // Создает кнопки для выбора инвенторя
{
    [SerializeField] private Transform _selectPlacedObjectButtonPrefab; // Префаб кнопка выделенного Размещаемого Объекта
    [SerializeField] private Transform _placedObjectTypeSelectContainer; // Контейнер для кнопок     
    [SerializeField] private List<PlacedObjectTypeSO> _ignorePlacedObjectTypeList; // Список Объектов которые надо игнорировать при создании кнопок выделения

    private Dictionary<PlacedObjectTypeSO, Transform> _buttonTransformDictionary; // Словарь (Тип Размещаемого Объекта - ключ, Transform- -значение)
    private PlacedObjectTypeListSO _placedObjectTypeList; // Список типов Размещаемого Объекта


    private void Awake()
    {
        _placedObjectTypeList = Resources.Load<PlacedObjectTypeListSO>(typeof(PlacedObjectTypeListSO).Name);    // Загружает ресурс запрошенного типа, хранящийся по адресу path(путь) в папке Resources(эту папку я создал в папке ScriptableObjects).
                                                                                                                // Что бы не ошибиться в имени пойдем другим путем. Создадим экземпляр BuildingTypeListSO (список будет один) и назавем также как и класс, потом для поиска SO будем извлекать имя класса которое совпадает с именем экземпляра
        _buttonTransformDictionary = new Dictionary<PlacedObjectTypeSO, Transform>(); // Инициализируем новый словарь
    }

    private void Start()
    {
       CreatePlacedObjectTypeButton(); // Создать Кнопки типов Размещаемых объектов
    }


    private void CreatePlacedObjectTypeButton() // Создать Кнопки типов Размещаемых объектов
    {
        foreach (Transform selectPlacedButton in _placedObjectTypeSelectContainer) // Очистим контейнер 
        {
            Destroy(selectPlacedButton.gameObject); // Удалим игровой объект прикрипленный к Transform
        }

        foreach (PlacedObjectTypeSO placedType in _placedObjectTypeList.list) // Переберем список Типов Размещаемых объектов
        {
            if (_ignorePlacedObjectTypeList.Contains(placedType)) continue; // Пропустим объекты для которых не надо создавать кнопки

            Transform buttonTransform = Instantiate(_selectPlacedObjectButtonPrefab, transform); // Создадим кнопку и сделаем дочерним к этому объекту

            Transform visualButton = Instantiate(placedType.visual, buttonTransform); // Создадим Визуал кнопки в зависимости от типа размещаемого объекта и сделаем дочерним к кнопке 

            buttonTransform.GetComponent<Button>().onClick.AddListener(() => //Добавим событие при нажатии на нашу кнопку// AddListener() в аргумент должен получить делегат- ссылку на функцию. Функцию будем объявлять АНАНИМНО через лямбду () => {...} 
            {
                PlacedObject.CreateInWorld(buttonTransform.position + new Vector3 (-1,0,0), PlacedObjectTypeSO.Dir.Right , placedType); // Создадим нужный объект              
            });

            MouseEnterExitEventsUI mouseEnterExitEventsUI = buttonTransform.GetComponent<MouseEnterExitEventsUI>(); // Найдем на кнопке компонент - События входа и выхода мышью 
            mouseEnterExitEventsUI.OnMouseEnter += (object sender, EventArgs e) => // Подпишемся на событие
            {
                TooltipUI.Instance.Show(placedType.nameString + "\n" + "характеристики"); // При наведении на кнопку покажем подсказку и передадим текст
            };
            mouseEnterExitEventsUI.OnMouseExit += (object sender, EventArgs e) =>
            {
                TooltipUI.Instance.Hide(); // При отведении мыши скроем подсказку
            };

            _buttonTransformDictionary[placedType] = buttonTransform; // Присвоим каждому ключу значение (Каждому типу ЗДАНИЙ свой Трансформ кнопки созданного из шаблона)
        }
    }



}
