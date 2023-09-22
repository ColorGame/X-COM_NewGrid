using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class DestroyNextTurn : MonoBehaviour // ДЕактивация объектов через несколько ходов
{
    [SerializeField] private int _turnAmountToDestroy = 2; // Количество ходов до уничтожения
    [SerializeField] private State _state; // ВЫбрать уничтожить или отключить

    private enum State
    {
        Destroy,
        SetActive
    }


    private int _startTurnNumber; // Номер очереди (хода) при старте 
    private int _currentTurnNumber; // Текущий номер очереди (хода) 

    private void Start()
    {
        _startTurnNumber = TurnSystem.Instance.GetTurnNumber(); // Получим номер хода

        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged; // Подпиш. на событие Ход Изменен
    }

    private void TurnSystem_OnTurnChanged(object sender, System.EventArgs e)
    {
        _currentTurnNumber = TurnSystem.Instance.GetTurnNumber(); // Получим ТЕКУЩИЙ номер хода;

        if (_currentTurnNumber - _startTurnNumber == _turnAmountToDestroy)
        {
            // через _turnAmountToDestroy хода уничтожим  или отключим

            switch (_state)
            {
                case State.Destroy:
                    Destroy(gameObject);
                    break;
                case State.SetActive:
                    gameObject.SetActive(false);
                    break;
            }
           
        }
    }

    private void OnDestroy()
    {
        TurnSystem.Instance.OnTurnChanged -= TurnSystem_OnTurnChanged;
    }
}
