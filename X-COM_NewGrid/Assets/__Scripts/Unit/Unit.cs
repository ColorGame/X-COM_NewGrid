using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Unit : MonoBehaviour // Этот клас будет отвечать за позицию на сетке и очки действий, получение урона
{

    private const int ACTION_POINTS_MAX = 3; //НАДО НАСТРОИТЬ//

    // для // РЕШЕНИЕ 2 //в UnitActionSystemUI
    // static - обозначает что event будет существовать для всего класса не зависимо от того скольго у нас созданно Юнитов.
    // Поэтому для прослушивания этого события слушателю не нужна ссылка на какую-либо конкретную единицу,
    // они могут получить доступ к событию через класс, который затем запускает одно и то же событие для каждой единицы. 
    public static event EventHandler OnAnyActionPointsChanged;  // изменении очков действий у ЛЮБОГО(Any) юнита а не только у выбранного.                                                           
    public static event EventHandler OnAnyFriendlyUnitDamage; //Любой дружественный Юнит получил урон
    public static event EventHandler OnAnyFriendlyUnitHealing; //Любой дружественный Юнит получил ИСЦИЛЕНИЕ
    public static event EventHandler OnAnyUnitSpawned; // Событие Любой Рожденный(созданный) Юнит
    public static event EventHandler OnAnyUnitDead; // Событие Любой Юнит Умер


    [SerializeField] private bool _isEnemy; //В инспекторе у префаба Врага поставить галочку

    // Частные случаи
    private GridPosition _gridPosition;
    private HealthSystem _healthSystem;
    private BaseAction[] _baseActionsArray; // Массив базовых действий // Будем использовать при создании кнопок   
    private Rope _unitRope;
    private int _actionPoints = ACTION_POINTS_MAX; // Очки действия
    private float _penaltyStunPercent;  // Штрафной процент оглушения (будем применять в след ход)
    private bool _stunned = false; // Оглушенный(по умолчанию ложь)
    private SingleNodeBlocker _singleNodeBlocker; // Блокирующий узел на самом юните
    /*private int _startStunTurnNumber; //Номер очереди (хода) при старте события оглушения
    private int _durationStunEffectTurnNumber; // Продолжительность оглушающего эффекта Количество ходов*/

    private void Awake()
    {
        _healthSystem = GetComponent<HealthSystem>();

        if (TryGetComponent<Rope>(out Rope unitRope))
        {
            _unitRope = unitRope;
        }

        _singleNodeBlocker = GetComponent<SingleNodeBlocker>();

        _baseActionsArray = GetComponents<BaseAction>(); // _moveAction и _spinAction также будут храниться внутри этого массива
    }

    private void Start()
    {
        // Когда Unit запускается он вычисляет свое положение в сетке и добовляет себя к GridObject(объектам сетки) в данной ячейки
        _gridPosition = LevelGrid.Instance.GetGridPosition(transform.position); //Получим позицию юнита на сетке. Для этого преобразуем мировую позицию ЮНИТА в позицию на СЕТКЕ
        LevelGrid.Instance.AddUnitAtGridPosition(_gridPosition, this); // Зайдем в LevelGrid получим доступ к статическому экземпляру и вызовим AddUnitAtGridPosition

        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged; // Подпиш. на событие Ход Изменен        
        _healthSystem.OnDead += HealthSystem_OnDead; // подписываемся на Event. Будет выполняться при смерти юнита

        OnAnyUnitSpawned?.Invoke(this, EventArgs.Empty); // Запустим событие Любой Рожденный(созданный) Юнит. Событие статичное поэтому будет выполняться для всех созданных Юнитов
    }


    private void Update()
    {
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position); //Получим новую позицию юнита на сетке.
        if (newGridPosition != _gridPosition) // Если новая позиция на сетке отличается от последней то ...
        {
            // Изменем положение юнита на сетке
            GridPosition oldGridPosition = _gridPosition; // Сохраним старую позицию что бы передать в event
            _gridPosition = newGridPosition; //Обновим позицию - Новая позиция становиться текущей

            LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition); //в UnitMovedGridPosition запускаем Событие. Поэтому эту строку поместим в КОНЦЕЦ . Иначе мы запускаем событие сетка обнавляется а юнит еще не перемещен
        }
    }

    public T GetAction<T>() where T : BaseAction //Фунцкция для получения любого типа базового действия // Создадим метод с GENERICS и ограничим типами в  BaseAction
    {
        foreach (BaseAction baseAction in _baseActionsArray) // Переберем Массив базовых действий
        {
            if (baseAction is T) // если T совподает с каким нибудь baseAction то ...
            {
                return baseAction as T; // Вернем это базовое действие КАК Т // (T)baseAction; - еще один метод записи
            }
        }
        return null; // Если нет совпадений то вернем ноль
    }

    public GridPosition GetGridPosition() // Получить сеточную позицию
    {
        return _gridPosition;
    }


    public Vector3 GetWorldPosition() // Получить мировую позицию
    {
        return transform.position;
    }

    public BaseAction[] GetBaseActionsArray() // Получить Массив базовых действий
    {
        return _baseActionsArray;
    }

    public bool TrySpendActionPointsToTakeAction(BaseAction baseAction) // ПОПРОБУЕМ Потратить Очки Действия, Чтобы Выполнить Действие // Этот метод выполняет вместе два нижних метода
    {
        if (CanSpendActionPointsToTakeAction(baseAction))
        {
            SpendActionPoints(baseAction.GetActionPointCost());
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CanSpendActionPointsToTakeAction(BaseAction baseAction) //мы МОЖЕМ Потратить Очки Действия, Чтобы Выполнить Действие ? 
    {
        if (_actionPoints >= baseAction.GetActionPointCost()) // Если очков действия хватает то...
        {
            return true; // Можем выполнить действие
        }
        else
        {
            return false; // Увы очков не хватает
        }

        /*// Альтернативная запись кода выше
        return _actionPoints >= baseAction.GetActionPointCost();*/
    }

    public void SpendActionPoints(int amount) //Потратить очки действий (amount- количество которое надо потратить)
    {
        _actionPoints -= amount;

        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty); // запускаем событие ПОСЛЕ обнавления очков действий.(для // РЕШЕНИЕ // 2 //в UnitActionSystemUI)
    }

    public int GetActionPoints() // Получить очки действия
    {
        return _actionPoints;
    }


    public void TurnSystem_OnTurnChanged(object sender, EventArgs empty) //Ход изменен Сбросим очки действий до максимальных
    {
        if ((IsEnemy() && !TurnSystem.Instance.IsPlayerTurn()) || // Если это враг И его очередь (НЕ очередь игрока) ИЛИ это НЕ враг(игрок) и очередь игрока то...
            (!IsEnemy() && TurnSystem.Instance.IsPlayerTurn()))
        {
            _actionPoints = ACTION_POINTS_MAX;

            if (_penaltyStunPercent != 0)
            {
                _actionPoints -= Mathf.RoundToInt(_actionPoints * _penaltyStunPercent); // Применим штраф
                _penaltyStunPercent = 0;
                SetStunned(false); // Отключим оглушение
            }

            //  БОлее сложная распространяется на след ход
            /*int passedTurnNumber = TurnSystem.Instance.GetTurnNumber() - _startStunTurnNumber;// прошло ходов от начала Оглушения
            if (passedTurnNumber <= _durationStunEffectTurnNumber) // Если ходов прошло меньше или равно длительности ОГЛУШЕНИЯ (Значит оглушение еще действует)
            {
                _actionPoints -= Mathf.RoundToInt(_actionPoints * _penaltyStunPercent); // Применим штраф
                _penaltyStunPercent = _penaltyStunPercent *0.3f; // Уменьшим штраф оставим 30% от изначального (это надо Если оглушение длиться несколько ходов)
            }
            if (passedTurnNumber > _durationStunEffectTurnNumber) //Если ходов прошло больше продолжительности ОГЛУШЕНИЯ
            {
                SetStunned(false); // Отключим оглушение
                _penaltyStunPercent = 0; // И обнулим штраф
            }  */

            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty); // запускаем событие ПОСЛЕ обнавления очков действий.(для // РЕШЕНИЕ // 2 //в UnitActionSystemUI)
        }
    }

    public bool IsEnemy() // Раскроем поле
    {
        return _isEnemy;
    }

    public void Healing(int healingAmount) // Исцеление (в аргумент передаем величину восстановившегося здоровья)
    {
        _healthSystem.Healing(healingAmount);
        if (!_isEnemy)// Если НЕ ВРАГ то
        {
            OnAnyFriendlyUnitHealing?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Damage(int damageAmount) // Урон (в аргумент передаем величину повреждения)
    {
        _healthSystem.Damage(damageAmount);
        if (!_isEnemy)// Если НЕ ВРАГ то
        {
            OnAnyFriendlyUnitDamage?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Stun(float stunPercent) // Оглушить на stunPercent(процент оглушения)
    {
        SetStunned(true);
        _penaltyStunPercent = stunPercent; // Установим Процента Оглушения

        /*// БОлее сложная распространяется на след ход
        _startStunTurnNumber = TurnSystem.Instance.GetTurnNumber(); // Получим стартовый номер хода              
        if (_actionPoints > 0) // Если очков хода больше нуля
        {
            _durationStunEffectTurnNumber = 1; //Нужно НАСТРОИТЬ// Оглушение будет длиться весь следующий ход
        }
        if(_actionPoints<=0) // Если очков хода нету
        {
            _durationStunEffectTurnNumber = 3; //Нужно НАСТРОИТЬ// Оглушение будет длиться следующие 3 хода (через ход врага)
        }*/       
        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty); // запускаем событие ПОСЛЕ обнавления очков действий.
    }


    private void HealthSystem_OnDead(object sender, EventArgs e) // Будет выполняться при смерти юнита

    {
        LevelGrid.Instance.RemoveUnitAtGridPosition(_gridPosition, this); // Удалим из сеточной позиции умершего юнита

        Destroy(gameObject); // Уничтожим игровой объект к которому прикриплен данный скрипт

        // Вслучае смерти активного Юнита надо предать ход следующему юниту        

        OnAnyUnitDead?.Invoke(this, EventArgs.Empty); // Запустим событие Любой Мертвый Юнит. Событие статичное поэтому будет выполняться для любого мертвого Юнита      
    }

    public float GetHealthNormalized() // Раскроем для чтения
    {
        return _healthSystem.GetHealthNormalized();
    }
    public int GetHealth() // Раскроем для чтения
    {
        return _healthSystem.GetHealth();
    }
    public int GetHealthMax() // Раскроем для чтения
    {
        return _healthSystem.GetHealthMax();
    }
    public bool IsDead()
    {
        return _healthSystem.IsDead();
    }
    public HealthSystem GetHealthSystem()
    {
        return _healthSystem;
    }

    public Rope GetUnitRope()
    {
        return _unitRope;
    }

    public bool GetStunned()
    {
        return _stunned;
    }
    private void SetStunned(bool stunned)
    {
        _stunned = stunned;
    }
    public SingleNodeBlocker GetSingleNodeBlocker()
    {
        return _singleNodeBlocker;
    }


}
