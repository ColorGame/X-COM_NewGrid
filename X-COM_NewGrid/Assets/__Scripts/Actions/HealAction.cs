using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SwordAction;

public class HealAction : BaseAction // Действие Лечение НАСЛЕДУЕТ класс BaseAction // ВЫделим в отдельный класс // Лежит на каждом юните
{    
    public event EventHandler<Unit> OnHealActionStarted;     // Действие Лечение Началось (будем убирать оружие и достовать бинт) // в событие будем передовать Юнита которого лечим (в HandleAnimationEvents будем создавать свечение)
    public event EventHandler<Unit> OnHealActionCompleted;   // Действие Лечение Закончилочь (отключать бинт и вернуть оружие)



    private enum State
    {
        HealBefore, //До лечения (подоготовка)
        HealAfter,  //После Лечение
    }

    private int _healAmount = 50;
    private State _state; // Состояние юнита
    private float _stateTimer; //Таймер состояния
    private Unit _targetUnit;// Юнит которого лечим

    private int _maxHealDistance = 1; //Максимальная дистанция лечения//НУЖНО НАСТРОИТЬ//



    private void Update()
    {
        if (!_isActive) // Если не активны то ...
        {
            return; // выходим и игнорируем код ниже
        }

        _stateTimer -= Time.deltaTime; // Запустим таймер для переключения состояний

        switch (_state) // Переключатель активурует кейс в зависимости от _state
        {
            case State.HealBefore:

                if (_targetUnit != _unit) // Если лечим другого юнита то развернемся в его сторону
                {
                    Vector3 targetDirection = (_targetUnit.GetWorldPosition() - transform.position).normalized; // Направление к целивому юниту, еденичный вектор
                    float rotateSpeed = 10f; //НУЖНО НАСТРОИТЬ//

                    transform.forward = Vector3.Slerp(transform.forward, targetDirection, Time.deltaTime * rotateSpeed); // поворт юнита.
                }

                break;

            case State.HealAfter:
                break;
        }

        if (_stateTimer <= 0) // По истечению времени вызовим NextMusic() которая в свою очередь переключит состояние. Например - у меня было TypeGrenade.Aiming: тогда в case TypeGrenade.Aiming: переключу на TypeGrenade.Shooting;
        {
            NextState(); //Следующие состояние
        }
    }

    private void NextState() //Автомат переключения состояний
    {
        switch (_state)
        {
            case State.HealBefore:
                _state = State.HealAfter;
                float afterHealStateTime = 3f; // Для избежания магических чисель введем переменную  Продолжительность Состояния после лечения //НУЖНО НАСТРОИТЬ// (должно совподать с длительностью анимации, иначе оружие активируется в неподходящий момент)
                _stateTimer = afterHealStateTime;

                Heal(); // ЛЕЧИМ

                break;

            case State.HealAfter:

                OnHealActionCompleted?.Invoke(this, _targetUnit);  // Запустим событие Действие Лечение Закончилочь (подписчик UnitAnimator, где будем ВКЛЮЧАТЬ ОРУЖИЕ)

                ActionComplete(); // Вызовим базовую функцию ДЕЙСТВИЕ ЗАВЕРШЕНО
                break;
        }

        //Debug.Log(_state);
    }

    private void Heal() // Лечение
    {
        SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.Heal);
        _targetUnit.Healing(_healAmount);
    }

    public override string GetActionName() // Присвоить базовое действие //целиком переопределим базовую функцию
    {
        return "аптечка";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition) //Получить действие вражеского ИИ // Переопределим абстрактный базовый метод
    {
        float HealthNormalized = _unit.GetHealthNormalized(); // Получим нормализованное здоровье юнита

        if (HealthNormalized <= 0.3) //Если здоровье меньше или равно 30% то
        {
            return new EnemyAIAction
            {
                gridPosition = gridPosition,
                actionValue = 150, //Поставим ВЫСОКОЕ значение действия ИНАЧЕ СДОХНИМ. 
            };
        }
        else
        {
            return new EnemyAIAction
            {
                gridPosition = gridPosition,
                actionValue = 50, //Поставим среднее значение действия. Будет выполнять лечение если ничего другого сделать не может, 
            };
        }

    }

    public override List<GridPosition> GetValidActionGridPositionList()// Получить Список Допустимых Сеточных Позиция для Действий // переопределим базовую функцию
                                                                       // Допустимая сеточная позиция для Действия Лечения будет ячейка где стоит юнит 
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = _unit.GetGridPosition(); // Получим позицию в сетке юнита

        for (int x = -_maxHealDistance; x <= _maxHealDistance; x++) // Юнит это центр нашей позиции с координатами unitGridPosition, поэтому переберем допустимые значения в условном радиусе _maxComboDistance
        {
            for (int z = -_maxHealDistance; z <= _maxHealDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z, 0); // Смещенная сеточная позиция. Где началом координат(0,0, 0-этаж) является сам юнит 
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition; // Тестируемая Сеточная позиция

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) // Проверим Является ли testGridPosition Допустимой Сеточной Позицией если нет то переходим к след циклу
                {
                    continue; // continue заставляет программу переходить к следующей итерации цикла 'for' игнорируя код ниже
                }

                if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) // Исключим сеточное позицию где нет юнитов (нам нужны ячейки с юнитами мы будем их исцелять)
                {
                    // Позиция сетки пуста, нет Юнитов
                    continue;
                }

                Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);   // Получим юнита из нашей тестируемой сеточной позиции 
                                                                                                // GetUnitAtGridPosition может вернуть null но в коде выше мы исключаем нулевые позиции, так что проверка не нужна
                if (targetUnit.IsEnemy() != _unit.IsEnemy()) // Если тестируемый юнит враг а наш юнит нет (игнорируем чужаков)
                {
                    // Оба подразделения в Разных "командах"
                    continue;
                }



                validGridPositionList.Add(testGridPosition); // Добавляем в список те позиции которые прошли все тесты
                //Debug.Log(testGridPosition);
            }
        }
        return validGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete) // (onActionComplete - по завершении действия). В аргумент будем передовать делегат Action 
                                                                                        // В данном методе добавлен аргумент который мы не используем - GridPosition _gridPosition - он добавлен лишь для того чтобы соответствовать сигнатуре Базовой функции TakeAction.
                                                                                        // Есть другой способ, создать оттдельный - public class BaseParameters{} 
                                                                                        // и наследуемый в котором можно переопределить наш базовый параметр -
                                                                                        // public SpinBaseParameters : BaseParameters{}
                                                                                        // тогда запишем - public override void TakeAction(BaseParameters baseParameters ,Action onActionComplete){
                                                                                        // SpinBaseParameters spinBaseParameters = (SpinBaseParameters)baseParameters;}
    {
        _targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition); // Получим юнита которого хотим исцелить (это может быть и сам юнит)

        _state = State.HealBefore; // Активируем состояние Подготовки до лечения
        float beforeHealStateTime = 0.5f; //До Лечения.  Для избежания магических чисель введем переменную  Продолжительность Состояния подготовки перед лечением ..//НУЖНО НАСТРОИТЬ//
        _stateTimer = beforeHealStateTime;

        OnHealActionStarted?.Invoke(this, _targetUnit); // Запустим событие Действие Лечение Началось и передадим кого лечим Подписчик UnitAnimator (ВЫКЛЮЧАЕМ ОРУЖИЕ)
        ActionStart(onActionComplete); // Вызовим базовую функцию СТАРТ ДЕЙСТВИЯ // Вызываем этот метод в конце после всех настроек т.к. в этом методе есть EVENT и он должен запускаться после всех настроек
    }

    public override int GetActionPointCost() // Переопределим базовую функцию // Получить Расход Очков на Действие (Стоимость действия)
    {
        return 2;
    }

    public override int GetMaxActionDistance()
    {
        return _maxHealDistance;
    }
       
    private int GetHealAmount()
    {
        return _healAmount;
    }

    public override string GetToolTip()
    {
        return "цена - " + GetActionPointCost() + "\n" +
                "дальность - " + GetMaxActionDistance() + "\n" +
                "восстанавливает " + GetHealAmount() + " ед. жизни" + "\n" +
                "лечить можно себя, и любого юнита поблизости";
    }


}
