using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitComboSystem : MonoBehaviour // Включает и выключает базовые действия на юнитах учавствующих в КОМБО
{
  //  public static UnitComboSystem Instance { get; private set; }   //(ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                                   // instance - экземпляр, У нас будет один экземпляр UnitComboSystem можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.


    public event EventHandler OnStartComboAction; // Началось Комбо ДЕйствие.


    private Unit _startComboUnit; // Юнит котый инициализировал Комбо действие
    private Unit _targetComboUnit; // Юнит с которым будем делать Комбо    

    private void Awake()
    {
       /* // Если ты акуратем в инспекторе то проверка не нужна
        if (Instance != null) // Сделаем проверку что этот объект существует в еденичном екземпляре
        {
            Debug.LogError("There's more than one UnitComboSystem!(Там больше, чем один UnitComboSystem!) " + transform + " - " + Instance);
            Destroy(gameObject); // Уничтожим этот дубликат
            return; // т.к. у нас уже есть экземпляр UnitComboSystem прекратим выполнение, что бы не выполнить строку ниже
        }
        Instance = this; */      
    }

    private void Start()
    {
       // ComboAction.OnAnyComboActionStarted += ComboAction_OnAnyComboActionStarted; // У любого Началось Комбо ДЕйствие 
    }

    /*private void ComboAction_OnAnyComboActionStarted(object sender, ComboAction.OnComboEventArgs e)
    {
        _startComboUnit = e.startUnit;
        _targetComboUnit = e.partnerUnit;
                
        OnStartComboAction?.Invoke(this, e);
    }*/
}
