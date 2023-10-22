using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelInteract : MonoBehaviour, IInteractable // Бочка Взаимодействия
{


    public event EventHandler OnBarrelInteractlActivated; //Создадим событие когда - Бочка Взаимодействия Активорованна

    [SerializeField] private Transform _barrel; // Целая Бочка
    [SerializeField] private Transform _barrelDestroyed; //Разрушенная бочка


    private GridPositionXZ _gridPosition;
    private Action _onInteractionComplete; // Делегат Взаимодействие Завершено// Объявляю делегат в пространстве имен - using System;
                                           //Сохраним наш делегат как обыкновенную переменную (в ней будет храниться функия которую мы передадим).
                                           //Action- встроенный делегат. Есть еще встроен. делегат Func<>. 
                                           //СВОЙСТВО Делегата. После выполнения функции в которую мы передали делегата, можно ПОЗЖЕ, в определенном месте кода, выполниться сохраненный делегат.
                                           //СВОЙСТВО Делегата. Может вызывать закрытую функцию из другого класса
    private bool _isActive;
    private float _timer;// Таймер что бы взаимодействие было не мгновенным и кномки экшена были отключены некоторое время
    private SingleNodeBlocker _singleNodeBlocker; // блокировщик пути
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _singleNodeBlocker = GetComponent<SingleNodeBlocker>();
    }


    private void Start()
    {       
        _gridPosition = LevelGrid.Instance.GetGridPosition(transform.position); // Получим сеточную позицию бочки
        LevelGrid.Instance.SetInteractableAtGridPosition(_gridPosition, this); // Установить полученный Интерфейс Взаимодействия в этой сеточной позиции
        _singleNodeBlocker.BlockAtCurrentPosition();// Заблокирую узел

        // PathfindingMonkey.Instance.SetIsWalkableGridPosition(_gridPositioAnchor, false); // Установить что Можно или Нельзя (в зависимости от isWalkable)  ходить по переданной в аргумент Сеточной Позиции
    }

    private void Update()
    {
        if (!_isActive) // Если происходит взаимодействие с бочкой то она активна и выполняется код ниже
        {
            return; // выходим и игнорируем код ниже
        }

        _timer -= Time.deltaTime; // Запустим таймер

        if (_timer <= 0f)
        {
            _isActive = false;// Выключаем Активное состояние                    
            Destroy(gameObject); // Уничтожим бочку
            _onInteractionComplete(); // Вызовим сохраненный делегат который нам передала функция Interact(). В нашем случае это ActionComplete() он снимает занятость с кнопок UI
        }
    }

    public void Interact(Action onInteractionComplete) // Взаимодействие. В аргумент предаем Делегат Взаимодействие Завершено
    {
        _onInteractionComplete = onInteractionComplete;// Сохраним полученный делегат
        _isActive = true; //Переключаем Бочку в Активное состояние
        _timer = 0.5f;// Задаем время активного состояния  //НУЖНО НАСТРОИТЬ//

        
        _barrelDestroyed.gameObject.SetActive(true); // Активируем разрушенную бочку
        _barrelDestroyed.parent = null; // Отсоеденим Разрушенную бочку от родителя    
        _barrel.gameObject.SetActive(false); // Отключим целую бочку
        _collider.enabled = false; // Отключим коллайдер что бы не мешал взрыву

        ApplyExplosionToChildren(_barrelDestroyed, 150f, transform.position, 10f); // Применим силу к разрушенной бочки

        LevelGrid.Instance.ClearInteractableAtGridPosition(_gridPosition); // Удалить Интерфейс Взаимодействия в этой сеточной позиции

        GraphNode graphNode = AstarPath.active.GetNearest(transform.position).node; // Получим проверяемый узел
        BlockManager.Instance.InternalUnblock(graphNode, _singleNodeBlocker); // Разблокируем узел
        //PathfindingMonkey.Instance.SetIsWalkableGridPosition(_gridPositioAnchor, true); // Установить что Можно ходить по этой ячейки

        OnBarrelInteractlActivated?.Invoke(this, EventArgs.Empty); // Запустим событие - Бочка Взаимодействия Активированна
    }

    private void ApplyExplosionToChildren(Transform root, float explosionForce, Vector3 explosionPosition, float explosionRange) // Применить Взрыв к Детям (explosionRange Диапазон взрыва)
    {
        foreach (Transform child in root)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childrigidbody)) // Попробуем получить риджибоди дочерних объектов 
            {
                childrigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRange);
            }

            ApplyExplosionToChildren(child, explosionForce, explosionPosition, explosionRange);  // Рекурсивная функция
        }
    }  

}
