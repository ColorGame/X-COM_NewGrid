using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleAnimationEvents : MonoBehaviour // Обработчик Анимационных событий. 
{
    public event EventHandler OnAnimationTossGrenadeEventStarted;     // Действие В анимации "Бросок гранаты" стартовало событие  (в этот момент будем сосздавать гранату) // Это промежуточное событие между AnimationEvent и GrenadyAction в нем будем создовать гранату

    [SerializeField] private Unit _unit;

    private Unit _targetUnit;
    private HealAction _healAction;


    private void Start()
    {
        //Unit unit = GetComponentInParent<Unit>(); // Получим компонент Unit на родителе 
        if (_unit != null) // Если юнит существует
        {
            _unit.TryGetComponent<HealAction>(out HealAction healAction);// Попробуем на Юните получить компонент HealAction и если получиться сохраним в healAction
            _healAction = healAction;

            _healAction.OnHealActionStarted += HealAction_OnHealActionStarted; // Подпишемся на событие           
        }
    }

    private void HealAction_OnHealActionStarted(object sender, Unit unit)
    {
        _targetUnit = unit;
    }

    private void InstantiateHealFXPrefab() // Вызываю в AnimationEvent на анимации молитвы StendUp
    {       
        Instantiate(GameAssets.Instance.healFXPrefab, _targetUnit.GetWorldPosition(), Quaternion.LookRotation(Vector3.up)); // Создадим префаб частиц для юнита которого исцеляем (Не забудь в инспекторе включить у частиц Stop Action - Destroy)
    }

    private void StartIntermediateEvent() // Старт промежуточного события
    {
        OnAnimationTossGrenadeEventStarted?.Invoke(this, EventArgs.Empty); // Запустим событие В анимации "Бросок гранаты" стартовало событие (подписчик GrenadyAction)           
    }

    private void OnDestroy()
    {
        if (_healAction != null)
        {
            _healAction.OnHealActionStarted -= HealAction_OnHealActionStarted;// Отпишемя от события чтобы не вызывались функции в удаленных объектах.
        }
    }


}
