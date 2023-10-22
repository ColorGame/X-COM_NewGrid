using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LevelGridSystemVisual;
// ВАЖНО // Скрипт и маску слоя Cover закидываем только на объекты ниже 1,4м. (это условие НЕраспространяется на Smoke)
// Если объект выше 1,4м (это высота точки выстрела у стоящего юнита), то он становиться OBSTACLES-препядствием, т.к. объект не простреливается (точность падает до 0%). 
// CoverFull -1,4м 
// CoverHalf -0,7м 
public class CoverSmokeObject : MonoBehaviour//  Объект Укрытия или Дым 
{
    [SerializeField] private LayerMask _coverLayerMask; // маска слоя Укрытия (появится в ИНСПЕКТОРЕ)
    [SerializeField] private LayerMask _smokeLayerMask; // маска слоя Дыма (появится в ИНСПЕКТОРЕ)
    [SerializeField] private CoverSmokeType _coverSmokeType;
    [SerializeField] private float _penaltyAccuracy;  // Штраф прицеливания сделаем Сериализованным что бы во время Игры смотреть процент Штрафа
    
    private void Start()
    {
        //Сделаем предварительные настройки.
        //Выстрелим ЛУЧ. ЛУч не может стрелять внутри колайдера(внутри стены), поэтому сместим его наружу

        float raycastOffsetDistance = 1.5f; // Дистанция смещения луча
        float raycastDistance = 0.5f; // Дистанция выстрела луча

        if (Physics.Raycast(transform.position + Vector3.up * raycastOffsetDistance, Vector3.down, raycastDistance, _coverLayerMask)) // Выстрелим лучом с высоты 1,5 вниз на 0,5м только по маске Сover 
        {
            // если мы попали то это  CoverFull-Укрытие Полное
            _coverSmokeType = CoverSmokeType.CoverFull;            
        }
        else
        {
            // в противном случае это CoverHalf-Укрытие На половину
            _coverSmokeType = CoverSmokeType.CoverHalf;           
        }

        if (Physics.Raycast(transform.position + Vector3.down * raycastOffsetDistance, Vector3.up, raycastOffsetDistance * 2, _smokeLayerMask)) // Выстрелим лучом из под низу по маске Smoke 
        {
            // если мы попали то это  SmokeFull-Дым Полный, при старте он всегда полный потом меняем через скрипт GrenadeSmokeDeactivation
            _coverSmokeType = CoverSmokeType.SmokeFull;
        }      

        _penaltyAccuracy = GetPenaltyFromEnumAccuracy(_coverSmokeType); // Установим процент Штрафа в зависимости от Установленного типа
    }

    public float GetPenaltyFromEnumAccuracy(CoverSmokeType coverSmokeType) // Вернуть Штраф прицеливания в зависимости от состояния Укрытия //НУЖНО НАСТРОИТЬ//
    {
        switch (coverSmokeType)
        {
            case CoverSmokeType.None: // 
                _penaltyAccuracy = 0;
                break;

            case CoverSmokeType.CoverHalf: //Укрытие На половину
                _penaltyAccuracy = 0.2f;
                break;

            case CoverSmokeType.CoverFull: //Укрытие Полное
                _penaltyAccuracy = 0.6f;
                break;

            case CoverSmokeType.SmokeHalf: //Дым На половину
                _penaltyAccuracy = 0.25f;
                break;

            case CoverSmokeType.SmokeFull: //Дым Полный
                _penaltyAccuracy = 0.5f;
                break;
        }
        return _penaltyAccuracy;
    }

    public float GetPenaltyAccuracy() // Вернуть Штраф прицеливания
    {
        return _penaltyAccuracy;
    }
    public CoverSmokeType GetCoverSmokeType()
    {
        return _coverSmokeType;
    }

    public void SetCoverSmokeType(CoverSmokeType coverType)
    {
        _coverSmokeType = coverType;
    }
}

public enum CoverSmokeType
{
    None,       //Нету
    CoverHalf,  //Укрытие На половину
    CoverFull,  //Укрытие Полное
    SmokeHalf,  //Дым На половину
    SmokeFull   //Дым Полный
}
