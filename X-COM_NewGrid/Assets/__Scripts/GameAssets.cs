using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GameAssets : MonoBehaviour //������� ������
{

    private static GameAssets instance; // �.�. �� ������������ � ������ �� �� ���� ��������� ��� � �����

    // ����� ���-�� ������� ������ � ����� �������� ��� �������� ��� (get) � ��� ������������� ������� ��������� ������� ������� ��� ������ ������������
    public static GameAssets Instance // //(�������������� ������� SINGLETON) ����� ���� �������� GET ����� ������ �������
                                      // instance - ���������, � ��� ����� ���� ��������� ResourceManager ����� ���� ��� static. Instance ����� ��� ���� ����� ������ ������, ����� ����, ����� ����������� �� Event.
                                      // static ��� ������ ��, ��� ������ �������� ����������� �� ����������� ������� ������, � ����� ������, ��� ���� ������. ������� �������, ���� ������� ���� ������ ����������� �������, � � ������� ����������� ������� ���� ��� �� ���� ����� ������� ����, �� ����������� ���� ���� ��� ����� ������.

    {
        get //(�������� �������� get) ����� ������������ ������� � �� ����� ����������
        {
            if (instance == null) // ���� ��������� ������� �� �������� ���
            {
                instance = Resources.Load<GameAssets>("GameAssets"); //� ���� ���������� - (instance) ��������� ������ ������������ ����, ���������� �� ������ path(����) � ����� Resources(��� ����� � ������ � ����� ScriptableObjects  � � ����� Prefab).                                                                     
            }
            return instance; //������ ���� ���������� (���� ��� �� ������� �� ������ ������������)
        }
    }

    public Transform grenadeProjectilePrefab; // ������ �������
    public Transform bulletProjectilePrefab; // ������ ����
    public Transform gridSystemVisualSinglePrefab; // ������ ������������ ���� �����
    public Transform comboPartnerFXPrefab; // ������  �������� ��������������
    public Transform grenadeExplosionFXPrefab; // ������ �������� ������ ������� //�������� ��������� ������� � TRAIL ���������������(Destroy) ����� ������������
    public Transform grenadeSmokeFXPrefab; // ������ ���� �� ������� // ���������� ��� ����� ������ ������������� � ����
    public Transform electricityWhiteFXPrefab; // ������ ���������������� ������
    public Transform bulletHitFXPrefab; // ������ ������ �� ���������� ����
    public Transform healFXPrefab; // ������ �������� ���������
    public Transform spotterFireFXPrefab; // ������ �������� ����� ��� ����������
}