using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GrenadeProjectile;

// � ������ ������� ActionVirtualCamera ����� ���� ����������� ������ ��� ����� ��������, �������� �� ��� � �������� CinemachineVirtualCamera
// �� ������� Add Extension �������� Cinemachine Impulse Listener � ������ �� ����������� ��� � � �������� (����� ����������� ��������� � �������� � �������� � ������ ��� ����� ������)

public class ScreenShakeActions : MonoBehaviour //����������� ����� ������� ��������� ������ ������ (��������� �����) 
{


    private void Start()
    {
        ShootAction.OnAnyShoot += ShootAction_OnAnyShoot; // ���������� �� ������� ����� ����� ��������
        GrenadeProjectile.OnAnyGrenadeExploded += GrenadeProjectile_OnAnyGrenadeExploded;// ���������� �� ������� ����� ������� ����������
        SwordAction.OnAnySwordHit += SwordAction_OnAnySwordHit;// ���������� �� �������  ����� ����� ���� �����
    }

    private void SwordAction_OnAnySwordHit(object sender, SwordAction.OnSwordEventArgs e)
    {
        ScreenShake.Instance.Shake(2); // �� ��������� ������������� ������ = 1
    }

    private void GrenadeProjectile_OnAnyGrenadeExploded(object sender, TypeGrenade typeGrenade)
    {
        if (typeGrenade != TypeGrenade.Smoke) // ���� �� ������� �� ������ �����
        {
            ScreenShake.Instance.Shake(5); // ������������� ������ ��� ������ ������� ��������� 2 //����� ���������//
        }
    }

    private void ShootAction_OnAnyShoot(object sender, ShootAction.OnShootEventArgs e)
    {
        ScreenShake.Instance.Shake(); // �� ��������� ������������� ������ = 1
    }

}
