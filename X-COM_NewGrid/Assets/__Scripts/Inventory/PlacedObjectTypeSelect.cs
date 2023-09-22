using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlacedObjectTypeSelect : MonoBehaviour // ��������� ��� ������������ ������� // ������� ������ ��� ������ ���������
{
    [SerializeField] private Transform _selectPlacedObjectButtonPrefab; // ������ ������ ����������� ������������ �������
    [SerializeField] private Transform _placedObjectTypeSelectContainer; // ��������� ��� ������     
    [SerializeField] private List<PlacedObjectTypeSO> _ignorePlacedObjectTypeList; // ������ �������� ������� ���� ������������ ��� �������� ������ ���������

    private Dictionary<PlacedObjectTypeSO, Transform> _buttonTransformDictionary; // ������� (��� ������������ ������� - ����, Transform- -��������)
    private PlacedObjectTypeListSO _placedObjectTypeList; // ������ ����� ������������ �������


    private void Awake()
    {
        _placedObjectTypeList = Resources.Load<PlacedObjectTypeListSO>(typeof(PlacedObjectTypeListSO).Name);    // ��������� ������ ������������ ����, ���������� �� ������ path(����) � ����� Resources(��� ����� � ������ � ����� ScriptableObjects).
                                                                                                                // ��� �� �� ��������� � ����� ������ ������ �����. �������� ��������� BuildingTypeListSO (������ ����� ����) � ������� ����� ��� � �����, ����� ��� ������ SO ����� ��������� ��� ������ ������� ��������� � ������ ����������
        _buttonTransformDictionary = new Dictionary<PlacedObjectTypeSO, Transform>(); // �������������� ����� �������
    }

    private void Start()
    {
       CreatePlacedObjectTypeButton(); // ������� ������ ����� ����������� ��������
    }


    private void CreatePlacedObjectTypeButton() // ������� ������ ����� ����������� ��������
    {
        foreach (Transform selectPlacedButton in _placedObjectTypeSelectContainer) // ������� ��������� 
        {
            Destroy(selectPlacedButton.gameObject); // ������ ������� ������ ������������� � Transform
        }

        foreach (PlacedObjectTypeSO placedType in _placedObjectTypeList.list) // ��������� ������ ����� ����������� ��������
        {
            if (_ignorePlacedObjectTypeList.Contains(placedType)) continue; // ��������� ������� ��� ������� �� ���� ��������� ������

            Transform buttonTransform = Instantiate(_selectPlacedObjectButtonPrefab, transform); // �������� ������ � ������� �������� � ����� �������

            Transform visualButton = Instantiate(placedType.visual, buttonTransform); // �������� ������ ������ � ����������� �� ���� ������������ ������� � ������� �������� � ������ 

            buttonTransform.GetComponent<Button>().onClick.AddListener(() => //������� ������� ��� ������� �� ���� ������// AddListener() � �������� ������ �������� �������- ������ �� �������. ������� ����� ��������� �������� ����� ������ () => {...} 
            {
                PlacedObject.CreateInWorld(buttonTransform.position + new Vector3 (-1,0,0), PlacedObjectTypeSO.Dir.Right , placedType); // �������� ������ ������              
            });

            MouseEnterExitEventsUI mouseEnterExitEventsUI = buttonTransform.GetComponent<MouseEnterExitEventsUI>(); // ������ �� ������ ��������� - ������� ����� � ������ ����� 
            mouseEnterExitEventsUI.OnMouseEnter += (object sender, EventArgs e) => // ���������� �� �������
            {
                TooltipUI.Instance.Show(placedType.nameString + "\n" + "��������������"); // ��� ��������� �� ������ ������� ��������� � ��������� �����
            };
            mouseEnterExitEventsUI.OnMouseExit += (object sender, EventArgs e) =>
            {
                TooltipUI.Instance.Hide(); // ��� ��������� ���� ������ ���������
            };

            _buttonTransformDictionary[placedType] = buttonTransform; // �������� ������� ����� �������� (������� ���� ������ ���� ��������� ������ ���������� �� �������)
        }
    }



}
