using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAction : BaseAction // Базовое действие Меч
{
    public static event EventHandler<OnSwordEventArgs> OnAnySwordHit;   // Событие - Любой Начал удар мечом (когда любой юнит начнет атаковать мечом мы запустим событие Event) // <Unit> вариант передачи целевого юнита для пули
                                                                        // static - обозначает что event будет существовать для всего класса не зависимо от того скольго у нас созданно Юнитов.
                                                                        // Поэтому для прослушивания этого события слушателю не нужна ссылка на какую-либо конкретную единицу, они могут получить доступ к событию через класс,
                                                                        // который затем запускает одно и то же событие для каждой единицы. 

    public event EventHandler OnSwordActionStarted;     // Действие Меч Началось
    public event EventHandler OnSwordActionCompleted;   // Действие Меч Закончилочь
    public class OnSwordEventArgs : EventArgs // Расширим класс событий, чтобы в аргументе события передать нужных юнитов
    {
        public Unit targetUnit; // Целевой юнит в кого ударяем
        public Unit hittingUnit; // Унит который нанносит удар мечом
    }
    private enum State
    {
        SwingingSwordBeforeHit, //Взмах мечом перед ударом
        SwingingSwordAfterHit,  //Взмах меча после удара
    }


    private State _state; // Состояние юнита
    private float _stateTimer; //Таймер состояния
    private Unit _targetUnit;// Юнит в которого стреляем целимся

    private int _swordDamage = 50; // Урон от меча
    private int _maxSwordDistance = 1; //Максимальная дальность поражения Мечом //НУЖНО НАСТРОИТЬ//



    private void Update()
    {
        if (!_isActive) // Если не активны то ...
        {
            return; // выходим и игнорируем код ниже
        }

        _stateTimer -= Time.deltaTime; // Запустим таймер для переключения состояний

        switch (_state) // Переключатель активурует кейс в зависимости от _state
        {
            case State.SwingingSwordBeforeHit:

                Vector3 aimDirection = (_targetUnit.GetWorldPosition() - transform.position).normalized; // Направление прицеливания, еденичный вектор
                float rotateSpeed = 10f; //НУЖНО НАСТРОИТЬ//

                transform.forward = Vector3.Slerp(transform.forward, aimDirection, Time.deltaTime * rotateSpeed); // поворт юнита.

                break;

            case State.SwingingSwordAfterHit:
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
            case State.SwingingSwordBeforeHit:
                _state = State.SwingingSwordAfterHit;
                float afterHitStateTime = 1f; // Для избежания магических чисель введем переменную  Продолжительность Состояния Взмах меча после удара //НУЖНО НАСТРОИТЬ//
                _stateTimer = afterHitStateTime;
                SwordHit();
                break;

            case State.SwingingSwordAfterHit:

                OnSwordActionCompleted?.Invoke(this, EventArgs.Empty);  // Запустим событие Действие Меч Закончилось

                ActionComplete(); // Вызовим базовую функцию ДЕЙСТВИЕ ЗАВЕРШЕНО
                break;
        }

        //Debug.Log(_state);
    }

    private void SwordHit() // Удар мечом
    {
        OnAnySwordHit?.Invoke(this, new OnSwordEventArgs // создаем новый экземпляр класса OnShootEventArgs
        {
            targetUnit = _targetUnit,
            hittingUnit = _unit
        }); // Запустим событие ЛЮБОЙ Начал удар мечом и в аргумент передадим в кого ударяем и кто нанносит удар (Подписчики ScreenShakeActions ДЛЯ РЕАЛИЗАЦИИ ТРЯСКИ ЭКРАНА и UnitRagdollSpawner- для определения направления поражения)

        SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.Sword);

        if (_targetUnit.GetStunned() && !_unit.GetStunned()) // Если враг оглушенный а я нет
        {
            int maxDamage = Mathf.RoundToInt(_targetUnit.GetHealthMax() * 0.8f); // Отнимим 90% его макс здоровья
            _targetUnit.Damage(maxDamage);
        }
        else
        {
            _targetUnit.Damage(_swordDamage); // Нанесем Целевому юниту БОЛЬШОЙ урон     //НУЖНО НАСТРОИТЬ// В дальнейшем будем брать этот показатель из оружия
        }
    }


    public override string GetActionName()
    {
        return "меч";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition) //Получить действие вражеского ИИ // Переопределим абстрактный базовый метод
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 150, // Поставим это действие приоритетным //НУЖНО НАСТРОИТЬ//
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()// Получить Список Допустимых Сеточных Позиция для Действий // переопределим базовую функцию                                                                       
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = _unit.GetGridPosition(); // Получим позицию в сетке юнита

        for (int x = -_maxSwordDistance; x <= _maxSwordDistance; x++) // Юнит это центр нашей позиции с координатами unitGridPosition, поэтому переберем допустимые значения в условном радиусе _maxComboDistance
        {
            for (int z = -_maxSwordDistance; z <= _maxSwordDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z, 0); // Смещенная сеточная позиция. Где началом координат(0,0) является сам юнит 
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition; // Тестируемая Сеточная позиция

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) // Проверим Является ли testGridPosition Допустимой Сеточной Позицией если нет то переходим к след циклу
                {
                    continue; // continue заставляет программу переходить к следующей итерации цикла 'for' игнорируя код ниже
                }

                if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) // Исключим сеточное позицию где нет юнитов (нам нужны ячейки с юнитами мы будем по ним шмалять)
                {
                    // Позиция сетки пуста, нет Юнитов
                    continue;
                }

                Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);   // Получим юнита из нашей тестируемой сеточной позиции 
                                                                                                // GetUnitAtGridPosition может вернуть null но в коде выше мы исключаем нулевые позиции, так что проверка не нужна
                if (targetUnit.IsEnemy() == _unit.IsEnemy()) // Если тестируемый юнит враг и наш юнит тоже враг то (если они оба в одной команде то будем игнорировать этих юнитов)
                {
                    // Оба подразделения в одной "команде"
                    continue;
                }

                validGridPositionList.Add(testGridPosition); // Добавляем в список те позиции которые прошли все тесты
                //Debug.Log(testGridPosition);
            }
        }
        return validGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)  // Переопределим TakeAction (Применить Действие (Действовать). (Делегат onActionComplete - по завершении действия). в нашем случае делегату передаем функцию ClearBusy - очистить занятость
    {
        _targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition); // Получим юнита в которого целимся и сохраним его

        _state = State.SwingingSwordBeforeHit; // Активируем состояние Прицеливания  Взмах мечом перед ударом
        float beforeHitStateTime = 0.7f; //До Удара.  Для избежания магических чисель введем переменную  Продолжительность Состояния Взмах мечом перед ударом ..//НУЖНО НАСТРОИТЬ//
        _stateTimer = beforeHitStateTime;

        OnSwordActionStarted?.Invoke(this, EventArgs.Empty); // Запустим событие Действие Меч Началось Подписчик UnitAnimator

        ActionStart(onActionComplete); // Вызовим базовую функцию СТАРТ ДЕЙСТВИЯ // Вызываем этот метод в конце после всех настроек т.к. в этом методе есть EVENT и он должен запускаться после всех настроек

    }
    public override int GetActionPointCost() // Переопределим базовую функцию // Получить Расход Очков на Действие (Стоимость действия)
    {
        return 2;
    }

    public override int GetMaxActionDistance()
    {
        return _maxSwordDistance;
    }

    public Unit GetTargetUnit() // Раскроем _unitPartner
    {
        return _targetUnit;
    }
    private int GetSwordDamage()
    {
        return _swordDamage;
    }


    public override string GetToolTip()
    {
        return "цена - " + GetActionPointCost() + "\n" +
                "дальность - " + GetMaxActionDistance() + "\n" +
                "урон - " + GetSwordDamage() + "\n" +
                "эффект комбо" + "\n" +
                "у врага в ШОКОВОМ состоянии отнимается 80%  его макс здоровья";
    }

}
