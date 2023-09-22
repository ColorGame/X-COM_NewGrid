using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitWorldUI : MonoBehaviour // Мировой пользовательский интерфейс юнита //Лежит в canvas на юните
{
    [SerializeField] private TextMeshProUGUI _actionPointsText; // Закинуть текс UI
    [SerializeField] private TextMeshProUGUI _hitPercentText; // Закинуть процент попадания
    [SerializeField] private TextMeshProUGUI _healthPointsText; // Закинуть текст здоровья
    [SerializeField] private Image _aimImage; // закинуть иконку прицела
    [SerializeField] private Image _stunnedImage; // закинуть иконку ОГЛУШЕНИЯ
    [SerializeField] private Image _healthBarImage; // в инспекторе закинуть шкалу здоровья "Bar"
    [SerializeField] private Unit _unit; // в инспекторе закинуть юнита
    [SerializeField] private HealthSystem _healthSystem; // Закинуть самого юнита тк скрипт висит на нем


    private void Start()
    {
        Unit.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged; // Подпишемся на статическое событие (любое изминение очков действий) // Небольшой недостаток - это событие вызывается когда изменяется ActionPoints у любого юнита, а это немного расточительно но незначительно
        _healthSystem.OnDamageAndHealing += HealthSystem_OnDamageAndHealing; // Подпишемся на событие Получил повреждение или Вылечился.
        UnitActionSystem.Instance.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged; // Подпишемся Выбранное Действие Изменено
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged; //Подпишемся Ход Изменен

        UpdateActionPointsText();
        UpdateHealthBar();
        HideHitPercent();
        UpdateStunnedState();
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        UpdateHitPercentText(); // Надо обновить текста Процент попадания т.к. в ЭТОМ ходе УКРЫТИЕ могло поменять свое состаяние
    }

    private void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e) // Подпишемся Выбранное Действие Изменено
    {
        // Посмотрите, нужно ли показывать процент попадания в действии
        HideHitPercent();
        UpdateHitPercentText();
    }

    private void UpdateHitPercentText() // Обнавления текста Процент попадания
    {
        BaseAction baseAction = UnitActionSystem.Instance.GetSelectedAction();

        switch (baseAction)
        {
            case ShootAction shootAction:
                // Действие выстрела активно

                Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();

                if (_unit.IsEnemy() != selectedUnit.IsEnemy())
                {
                    // Этот ЮНИТ и Активный в разных групах

                    if (shootAction.IsValidActionGridPosition(_unit.GetGridPosition())) // Если этот юнит входит в список юнитов по которым я могу стрелять
                    {
                        // Это юнит выводит нас на правильную позицию для стрельбы
                        ShowHitPercent(shootAction.GetHitPercent(_unit));
                    }
                }
                break;
        }
    }
    private void UpdateActionPointsText() // Обнавления текста Очков Действия
    {
        _actionPointsText.text = _unit.GetActionPoints().ToString(); // Вернем очки действия юнита преобразуеи в строку и передадим в текст который отображается над юнитом
    }

    private void Unit_OnAnyActionPointsChanged(object sender, EventArgs e)
    {
        UpdateActionPointsText();
        UpdateStunnedState();
    }

    /*//  Если вы хотите точно знать, какой юнит претерпел изменения в OnAnyActionPointsChanged, вам просто нужно указать отправителя как Юнита.
    private void Unit_OnAnyActionPointsChanged(object sender, EventArgs args)
    {
        Unit unit = sender as Unit;
        Debug.Log($"у {unit} очки деиствий изменились.");
    }*/
    private void UpdateHealthBar() // Обновления шкалы здоровья
    {
        _healthBarImage.fillAmount = _healthSystem.GetHealthNormalized();
        _healthPointsText.text = _healthSystem.GetHealth().ToString();

    }
    private void HealthSystem_OnDamageAndHealing(object sender, EventArgs e) // при наступления события обновим шкалу жизни
    {
        UpdateHealthBar();
    }

    private void ShowHitPercent(float hitChance)
    {
        _hitPercentText.gameObject.SetActive(true);
        _hitPercentText.text = Mathf.Round(hitChance * 100f) + "%";
        _aimImage.gameObject.SetActive(true);
    }

    private void HideHitPercent()
    {
        _hitPercentText.gameObject.SetActive(false);
        _aimImage.gameObject.SetActive(false);
    }

    private void UpdateStunnedState()
    {
        _stunnedImage.gameObject.SetActive(_unit.GetStunned());
    }
        

    private void OnDestroy()
    {
        UnitActionSystem.Instance.OnSelectedActionChanged -= UnitActionSystem_OnSelectedActionChanged;
        Unit.OnAnyActionPointsChanged -= Unit_OnAnyActionPointsChanged;
    }
}
