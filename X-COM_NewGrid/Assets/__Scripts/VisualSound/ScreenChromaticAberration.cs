using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ScreenChromaticAberration : MonoBehaviour// ������ ������������� ��������� (���������� �� ����� ������ RGB �������) ����� �� ��������� �������������
{

    public static ScreenChromaticAberration Instance { get; private set; }  //(�������������� ������� SINGLETON) ��� �������� ������� ����� ���� ������� (SET-���������) ������ ���� �������, �� ����� ���� �������� GET ����� ������ �������
                                                                            // instance - ���������, � ��� ����� ���� ��������� ResourceManager ����� ���� ��� static. Instance ����� ��� ���� ����� ������ ������, ����� ����, ����� ����������� �� Event.
                                                                            // static ��� ������ ��, ��� ������ �������� ����������� �� ����������� ������� ������, � ����� ������, ��� ���� ������. ������� �������, ���� ������� ���� ������ ����������� �������, � � ������� ����������� ������� ���� ��� �� ���� ����� ������� ����, �� ����������� ���� ���� ��� ����� ������.

    private const float ATTENUATION_SPEED = 1.5f; // �������� ���������

    private Volume _volume; // ��������� Volume  �� �������������
    private float _attenuationSpeed; // �������� ���������
   

    private void Awake()
    {
        Instance = this;

        _volume = GetComponent<Volume>(); // ������� ������ �� ���� ���������
    }
    private void Update()
    {
        if (_volume.weight > 0) //���� ��� Volume ������ ���� �� ����� ��������� ���
        {            
            _volume.weight -= Time.deltaTime * _attenuationSpeed;
        }
    }

    public void SetWeight(float weight, float attenuationSpeed = ATTENUATION_SPEED) // ��������� ��� Volume � �������� ���������
    {
        _volume.weight = weight;
        _attenuationSpeed = attenuationSpeed;
    }

}
