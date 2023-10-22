using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GrenadeProjectile;

public class GrenadeFragmentationAction : GrenadeAction // Осколочная граната 
{
    [SerializeField, Min(0)] private int _grenadeDamage = 36;
    public override void _handleAnimationEvents_OnAnimationTossGrenadeEventStarted(object sender, EventArgs e)  // Зададим абстрактную функцию - "В анимации "Бросок гранаты" стартовало событие"
    {
        if (UnitActionSystem.Instance.GetSelectedAction() == this) // Проверим наше действие активное (выбранно) // Все виды гранат подписаны на событие в АНИМАЦИИ, если не сделать проверку то Юнит создат все гранаты одновременно
        {
            Transform grenadeProjectileTransform = Instantiate(GameAssets.Instance.grenadeProjectilePrefab, _grenadeSpawnTransform.position, Quaternion.identity); // Создадим префаб гранаты 
            GrenadeProjectile grenadeProjectile = grenadeProjectileTransform.GetComponent<GrenadeProjectile>(); // Возьмем у гранаты компонент GrenadeProjectile

            grenadeProjectile.Setup(_targetGridPositin, TypeGrenade.Fragmentation, OnGrenadeBehaviorComplete, _grenadeDamage); // И вызовим функцию Setup() передав в нее целевую позицию (сеточныая позиция курсора мыши) Тип ГРАНАТЫ и передадим в делегат функцию OnGrenadeBehaviorComplete ( при взрыве гранаты будем вызывать эту функцию)
        }
    }
    public override EnemyAIAction GetEnemyAIAction(GridPositionXZ gridPosition) //Получить действие вражеского ИИ // Переопределим абстрактный базовый метод
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 45, //Поставим значение действия. Будет бросать гранату если ничего другого сделать не может, 
        };
    }
    public override string GetActionName() // Присвоить базовое действие //целиком переопределим базовую функцию
    {
        return "осколочная";
    }


    public override string GetToolTip()
    {
        return "цена - " + GetActionPointCost() + "\n" +
            "дальность - " + GetMaxActionDistance() + "\n" +
             "урон - " + GetGrenadeDamage() + "\n" +
            "дальше от центра взрыва, урон уменьшается";
    }

    public override int GetGrenadeDamage()
    {
        return _grenadeDamage;
    }
}
