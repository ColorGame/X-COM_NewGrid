using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeDeactivation : MonoBehaviour // Деактивация дыма от гранаты после нескольких ХОДОВ
{

    private int _startTurnNumber; // Номер очереди (хода) при старте 
    private int _currentTurnNumber; // Текущий номер очереди (хода) 
    
    private ParticleSystem _particleSystem;
    private CoverSmokeObject _coverSmokeObject; // Объект укрытия
    private float _rateOverTime = 50;// Скорость, с которой излучатель порождает новые частицы с течением времени (по умолчанию 200).

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        _coverSmokeObject = GetComponent<CoverSmokeObject>();
    }

    private void Start()
    {
        _startTurnNumber = TurnSystem.Instance.GetTurnNumber(); // Получим номер хода
        _coverSmokeObject.SetCoverSmokeType(CoverSmokeType.SmokeFull); // Установим укрытие дыма полным

        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged; // Подпиш. на событие Ход Изменен
    }

    private void TurnSystem_OnTurnChanged(object sender, System.EventArgs e)
    {
        _currentTurnNumber = TurnSystem.Instance.GetTurnNumber(); // Получим ТЕКУЩИЙ номер хода;

        if (_currentTurnNumber - _startTurnNumber == 4)
        {
            // на 4 ходу эффективность и защита падает на 50%
            var emission =  _particleSystem.emission;
            emission.rateOverTime = _rateOverTime; // Уменьшим количество создаваемых частиц
            _coverSmokeObject.SetCoverSmokeType(CoverSmokeType.SmokeHalf); // Установим укрытие дыма На половину
        }

        if (_currentTurnNumber - _startTurnNumber == 6)
        {
            // на 6 ходу уничтожим дым
            Destroy(gameObject); 
        }
    }

    private void OnDestroy()
    {
        TurnSystem.Instance.OnTurnChanged -= TurnSystem_OnTurnChanged;
    }
}
