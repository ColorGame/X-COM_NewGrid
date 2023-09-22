using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorVisibility : MonoBehaviour // Видимость этажа // Должна висеть на всех объектах которые хотим скрыть // Если изменить материал который поддерживает альфа канал то можно изменять прозрачность объектов
{
    [SerializeField] private bool dynamicFloorPosition; // Динамическая позиция этажа (для объектов которые могут перемещаться и менять этаж нахождения) // Для юнита в ИНСПЕКТОРЕ надо поставить галочку
    [SerializeField] private List<Renderer> ignoreRendererList; // Список Renderer который надо игнорировать при включении и отключении визуализации объектов // Это относиться к зеленому кругу на юните у которого своя логика отключения и включения

    private Renderer[] _rendererArray; // Массив Renderer дочерних объектов
    private Canvas _canvas;
    private int floor; // Этаж    

    private void Awake()
    {
        _rendererArray = GetComponentsInChildren<Renderer>(true); // Вернем компонент Renderer у всех дочерних объектов даже неактивных и сохраним в массив
        _canvas = GetComponentInChildren<Canvas>(true); // Если на объекте нет то вернет null, ниже делаю проверку    
        if (TryGetComponent(out MoveAction moveAction)) // Если на объекте есть этот компонент то подпишемся на событие
        {
            moveAction.OnChangedFloorsStarted += MoveAction_OnChangedFloorsStarted; 
        }
    }   

    private void Start()
    {
        floor = LevelGrid.Instance.GetFloor(transform.position); // Получим этаж для нашей позиции(объект на котором висит скрипт) 

        if (floor == 0 && !dynamicFloorPosition) // Если этаж на котором находяться объекты к которым прикриплен скрипт нулевой  и  этажность динамически НЕ изменяется (это касается юнитов) то...
        {
            Destroy(this); // Уничтожим этот скрипт что бы он просто так не занимал Update
        }
    }    

    private void Update()
    {
       /* if (dynamicFloorPosition) // Если объект Динамически меняет этажность то будем каждый кадр отслеживать его этаж // ДЛЯ ОПТИМИЗАЦИИ МОЖНО ИСПОЛЬЗОВАТЬ EVENT
        {
            floor = LevelGrid.Instance.GetFloor(transform.position);
        }*/

        float cameraHeight = CameraController.Instance.GetCameraHeight(); // Получим высоту камеры

        float floorHeightOffset = 3f; // смещение высоты этажа // Для удобства отображения камеры
        bool showObject = cameraHeight > LevelGrid.FLOOR_HEIGHT * floor + floorHeightOffset; // Показываемый объект при условии ( если Высота камеры больше Высоты этажа * на номер этажа + смещение)

        if (showObject || floor == 0) // Если можно показать объект или этаж нулевой (что бы если высота камера окажеться меньше cameraHeight, униты на нулевом этаже не отключались)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Show() // Показать
    {
        foreach (Renderer renderer in _rendererArray) // Переберем массив
        {
            if (ignoreRendererList.Contains(renderer)) continue; // Если объект в списке исключения то пропустим его
            renderer.enabled = true;
        }
       if(_canvas != null)
        {
            _canvas.gameObject.SetActive(true);
        }
    }

    private void Hide() // Скрыть
    {
        foreach (Renderer renderer in _rendererArray)
        {
            if (ignoreRendererList.Contains(renderer)) continue; // Если объект в списке исключения то пропустим его
            renderer.enabled = false;
        }
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
        }
    }

    private void MoveAction_OnChangedFloorsStarted(object sender, MoveAction.OnChangeFloorsStartedEventArgs e)
    {
        floor = e.targetGridPosition.floor; // Изменим этаж у нашего Юнита
    }
}
