using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HealthSystem : MonoBehaviour // Система Здоровья // Висит на юните// Можно повесить на разрушаемый объекты в сцене например ящик, стена ...
{
    public event EventHandler OnDead; // Умер (запустим событие когда юнит умер)
    public event EventHandler OnDamageAndHealing; // Получил повреждение или Вылечился (запуск события при получении Урона или Лечении )

    [SerializeField] private int _health = 100; // Текущее здоровье
    private int _healthMax;

    private void Awake()
    {
        _healthMax = _health;
    }

    public void Damage(int damageAmount) // Урон (в аргумент передаем величина урона)
    {
        _health-=damageAmount;

        if (_health < 0)
        {
            _health = 0;
        }

        OnDamageAndHealing?.Invoke(this, EventArgs.Empty); // Вызовим событие при получении урона

        if(_health == 0)
        {
            Die();
            SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.DeathCry);
        }

        //Debug.Log(_health);
    }

    public void Healing(int healingAmount) // Исцеление (в аргумент передаем величину восстановившегося здоровья)
    {
        _health += healingAmount;

        if (_health > _healthMax)
        {
            _health = _healthMax;
        }

        OnDamageAndHealing?.Invoke(this, EventArgs.Empty); // Вызовим событие при восстановлении здоровья               
    }


    private void Die() // Запуская событие код становится более гибким и мы можем через подписку выполнить любое действие
    {
        OnDead?.Invoke(this, EventArgs.Empty); // запустим событие когда юнит умер (на нее будет подписываться Unit, UnitRagdollSpawner)        
    }

    public float GetHealthNormalized() // Вернем нормализованное значение здоровья (от 0 до 1) для настройки масштаба шкалы здоровья
    {
        return (float)_health/ _healthMax; // Преобразуем из int в float. Иначе если 10/100 вернет 0
    }

    public int GetHealth()
    {
        return _health;
    }
    
    public int GetHealthMax()
    {
        return _healthMax;
    }
    
    public bool IsDead()
    {
        return _health == 0;
    }
}
