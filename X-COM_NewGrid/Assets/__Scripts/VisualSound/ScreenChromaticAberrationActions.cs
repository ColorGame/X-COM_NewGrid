using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GrenadeProjectile;

public class ScreenChromaticAberrationActions : MonoBehaviour //Независимый класс который реализует хроматическую абберацию экрана (связующее звено) 
{
    private void Start()
    {       
        ShootAction.OnAnyShoot += ShootAction_OnAnyShoot; // Подпишемся Любой Начал стрелять
        GrenadeProjectile.OnAnyGrenadeExploded += GrenadeProjectile_OnAnyGrenadeExploded;// Подпишемся Любая граната взорвалась
        SwordAction.OnAnySwordHit += SwordAction_OnAnySwordHit;// Подпишемся Любой Начал удар мечом
        ComboAction.OnAnyUnitStunComboAction += ComboAction_OnAnyUnitStunComboAction;

    }

    private void ComboAction_OnAnyUnitStunComboAction(object sender, EventArgs e)
    {
        ScreenChromaticAberration.Instance.SetWeight(1f);
    }

    private void ShootAction_OnAnyShoot(object sender, ShootAction.OnShootEventArgs e)
    {
        ScreenChromaticAberration.Instance.SetWeight(1f);
    }
    private void GrenadeProjectile_OnAnyGrenadeExploded(object sender, TypeGrenade typeGrenade)
    {
        if (typeGrenade != TypeGrenade.Smoke) // Если не дымовая то 
        {
            ScreenChromaticAberration.Instance.SetWeight(1f, 0.5f);
        }
    }
    private void SwordAction_OnAnySwordHit(object sender, SwordAction.OnSwordEventArgs e)
    {
        ScreenChromaticAberration.Instance.SetWeight(1f);
    }




}
