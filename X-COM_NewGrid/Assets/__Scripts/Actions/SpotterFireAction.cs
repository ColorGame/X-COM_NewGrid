using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class SpotterFireAction : BaseAction // Действие Корректировщик огня НАСЛЕДУЕТ класс BaseAction // ВЫделим в отдельный класс // Лежит на каждом юните
{

    public event EventHandler OnSpotterFireActionStarted;     // Действие Корректировщик огня  Началось // в событие будем передовать Юнита которого лечим (в HandleAnimationEvents будем создавать свечение)
    public event EventHandler OnSpotterFireActionCompleted;   // Действие Корректировщик огня  Закончилочь (отключать бинт и вернуть оружие)



    private enum State
    {
        SpotterFireBefore, //До 
        SpotterFireAfter,  //После 
    }


    [SerializeField] private Transform _spotterFireFXPrefab; // Круги наблюдения
    private List<Transform> _spotterFireFXList; // Список Созданных Кругов наблюдения
    private State _state; // Состояние юнита    
    private Unit _partnerUnit;// Юнит у котрого корректируем огонь    

    private int _maxSpotterFireDistance = 1; //Максимальная дистанция выбора юнита для корректировки огня//НУЖНО НАСТРОИТЬ//

    private void Start()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged; // Выбранный юнит изменен
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        if (_partnerUnit != null) // Если есть партнер
        {
            Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
            if (selectedUnit != _partnerUnit) // Если выделенный юнит НЕ ПАРТНЕР то
            {
                _partnerUnit.GetAction<ShootAction>().СlearSpotterFireUnit(); // Очистить у партнера поле корректировщика огня
                foreach (Transform spotterFireFX in _spotterFireFXList) // Удалим волны
                {
                    Destroy(spotterFireFX.GameObject()); // Уничтожим волны
                }
                _partnerUnit = null; // Обнулим партнера

                OnSpotterFireActionCompleted?.Invoke(this, EventArgs.Empty); // Запустим событие Действие Корректировки Закончилось Подписчик UnitAnimator (ВКЛЮЧАЕМ ОРУЖИЕ)
            }
        }
    }



    private void Update()
    {
        if (!_isActive) // Если не активны то ...
        {
            return; // выходим и игнорируем код ниже
        }

        switch (_state) // Переключатель активурует кейс в зависимости от _state
        {
            case State.SpotterFireBefore:

                NextState();
                break;

            case State.SpotterFireAfter:
                NextState();
                break;
        }
    }

    private void NextState() //Автомат переключения состояний
    {
        switch (_state)
        {
            case State.SpotterFireBefore:

                _state = State.SpotterFireAfter;

                break;

            case State.SpotterFireAfter:

                ActionComplete(); // Вызовим базовую функцию ДЕЙСТВИЕ ЗАВЕРШЕНО

                break;
        }

        //Debug.Log(_state);
    }

    public override string GetActionName() // Присвоить базовое действие //целиком переопределим базовую функцию
    {
        return "наводчик";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPositionXZ gridPosition) //Получить действие вражеского ИИ // Переопределим абстрактный базовый метод
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 40, //Поставим среднее значение действия. Будет выполнять лечение если ничего другого сделать не может, 
        };
    }

    public override List<GridPositionXZ> GetValidActionGridPositionList()// Получить Список Допустимых Сеточных Позиция для Действий // переопределим базовую функцию
                                                                       // Допустимая сеточная позиция для Действия Лечения будет ячейка где стоит юнит 
    {
        List<GridPositionXZ> validGridPositionList = new List<GridPositionXZ>();

        GridPositionXZ unitGridPosition = _unit.GetGridPosition(); // Получим позицию в сетке юнита

        for (int x = -_maxSpotterFireDistance; x <= _maxSpotterFireDistance; x++) // Юнит это центр нашей позиции с координатами unitGridPosition, поэтому переберем допустимые значения в условном радиусе _maxComboDistance
        {
            for (int z = -_maxSpotterFireDistance; z <= _maxSpotterFireDistance; z++)
            {
                GridPositionXZ offsetGridPosition = new GridPositionXZ(x, z, 0); // Смещенная сеточная позиция. Где началом координат(0,0, 0-этаж) является сам юнит 
                GridPositionXZ testGridPosition = unitGridPosition + offsetGridPosition; // Тестируемая Сеточная позиция

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

                // Проверим партнера на достаточность очков
                int actionPoint = targetUnit.GetActionPoints(); // Получить очки действия у проверяемого юнита                
                if (actionPoint == 0)
                {
                    // Он пустой нам не поможет
                    continue;
                }
                if (targetUnit == _unit)
                {
                    // С самим собой делать нельзя
                    continue;
                }

                validGridPositionList.Add(testGridPosition); // Добавляем в список те позиции которые прошли все тесты
                //Debug.Log(testGridPosition);
            }
        }
        return validGridPositionList;
    }

    public override void TakeAction(GridPositionXZ gridPosition, Action onActionComplete) // (onActionComplete - по завершении действия). В аргумент будем передовать делегат Action 
                                                                                        // В данном методе добавлен аргумент который мы не используем - GridPositionXZ _gridPositioAnchor - он добавлен лишь для того чтобы соответствовать сигнатуре Базовой функции TakeAction.
                                                                                        // Есть другой способ, создать оттдельный - public class BaseParameters{} 
                                                                                        // и наследуемый в котором можно переопределить наш базовый параметр -
                                                                                        // public SpinBaseParameters : BaseParameters{}
                                                                                        // тогда запишем - public override void TakeAction(BaseParameters baseParameters ,Action onActionComplete){
                                                                                        // SpinBaseParameters spinBaseParameters = (SpinBaseParameters)baseParameters;}
    {
        _partnerUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition); // Получим юнита у которого хотим Корректировать Огонь
        _state = State.SpotterFireBefore; // Активируем состояние Подготовки 

        // Найдем точки прицеливания у юнитов
        Transform unitAimPoinTransform = _unit.GetAction<ShootAction>().GetAimPoinTransform();
        Transform partnerAimPoinTransform = _partnerUnit.GetAction<ShootAction>().GetAimPoinTransform();
        _spotterFireFXList = new List<Transform> // Создадим волны и добавим в список
        {
            Instantiate(_spotterFireFXPrefab, unitAimPoinTransform.position, Quaternion.identity, unitAimPoinTransform),
            Instantiate(_spotterFireFXPrefab, partnerAimPoinTransform.position, Quaternion.identity ,partnerAimPoinTransform)
        };

        SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.Spotter);

        _partnerUnit.GetAction<ShootAction>().SetSpotterFireUnit(_unit); // Установим партнеру, Себя, как коррект. огня
        UnitActionSystem.Instance.SetSelectedUnit(_partnerUnit, _partnerUnit.GetAction<ShootAction>()); //Сделаем Партнера выделенным и выберем действие стрелять


        OnSpotterFireActionStarted?.Invoke(this, EventArgs.Empty); // Запустим событие Действие Корректировки Началось Подписчик UnitAnimator (ВЫКЛЮЧАЕМ ОРУЖИЕ)
        ActionStart(onActionComplete); // Вызовим базовую функцию СТАРТ ДЕЙСТВИЯ // Вызываем этот метод в конце после всех настроек т.к. в этом методе есть EVENT и он должен запускаться после всех настроек
    }

    public override int GetActionPointCost() // Переопределим базовую функцию // Получить Расход Очков на Действие (Стоимость действия)
    {
        return 2;
    }
    public override int GetMaxActionDistance()
    {
        return _maxSpotterFireDistance;
    }

    public Transform GetSpotterFireFXPrefab()
    {
        return _spotterFireFXPrefab;
    }

    private void OnDestroy()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged -= UnitActionSystem_OnSelectedUnitChanged; // Выбранный юнит изменен
    }

    public override string GetToolTip()
    {
        return "комбо действие нужно 2  юнита" + "\n" +
            "цена - " + GetActionPointCost() + "  списывается у первого юнита" + "\n" +
            "дальность - " + GetMaxActionDistance() + "\n" +
        "у напарника увеличивается урон и радиус выстрела на 50%," + "\n" +
        " и точность становиться 100%)\r\n";
    }
}
