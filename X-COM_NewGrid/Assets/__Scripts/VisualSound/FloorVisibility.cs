using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorVisibility : MonoBehaviour // ��������� ����� // ������ ������ �� ���� �������� ������� ����� ������ // ���� �������� �������� ������� ������������ ����� ����� �� ����� �������� ������������ ��������
{
    [SerializeField] private bool dynamicFloorPosition; // ������������ ������� ����� (��� �������� ������� ����� ������������ � ������ ���� ����������) // ��� ����� � ���������� ���� ��������� �������
    [SerializeField] private List<Renderer> ignoreRendererList; // ������ Renderer ������� ���� ������������ ��� ��������� � ���������� ������������ �������� // ��� ���������� � �������� ����� �� ����� � �������� ���� ������ ���������� � ���������

    private Renderer[] _rendererArray; // ������ Renderer �������� ��������
    private Canvas _canvas;
    private int floor; // ����    

    private void Awake()
    {
        _rendererArray = GetComponentsInChildren<Renderer>(true); // ������ ��������� Renderer � ���� �������� �������� ���� ���������� � �������� � ������
        _canvas = GetComponentInChildren<Canvas>(true); // ���� �� ������� ��� �� ������ null, ���� ����� ��������    
        if (TryGetComponent(out MoveAction moveAction)) // ���� �� ������� ���� ���� ��������� �� ���������� �� �������
        {
            moveAction.OnChangedFloorsStarted += MoveAction_OnChangedFloorsStarted; 
        }
    }   

    private void Start()
    {
        floor = LevelGrid.Instance.GetFloor(transform.position); // ������� ���� ��� ����� �������(������ �� ������� ����� ������) 

        if (floor == 0 && !dynamicFloorPosition) // ���� ���� �� ������� ���������� ������� � ������� ���������� ������ �������  �  ��������� ����������� �� ���������� (��� �������� ������) ��...
        {
            Destroy(this); // ��������� ���� ������ ��� �� �� ������ ��� �� ������� Update
        }
    }    

    private void Update()
    {
       /* if (dynamicFloorPosition) // ���� ������ ����������� ������ ��������� �� ����� ������ ���� ����������� ��� ���� // ��� ����������� ����� ������������ EVENT
        {
            floor = LevelGrid.Instance.GetFloor(transform.position);
        }*/

        float cameraHeight = CameraController.Instance.GetCameraHeight(); // ������� ������ ������

        float floorHeightOffset = 3f; // �������� ������ ����� // ��� �������� ����������� ������
        bool showObject = cameraHeight > LevelGrid.FLOOR_HEIGHT * floor + floorHeightOffset; // ������������ ������ ��� ������� ( ���� ������ ������ ������ ������ ����� * �� ����� ����� + ��������)

        if (showObject || floor == 0) // ���� ����� �������� ������ ��� ���� ������� (��� �� ���� ������ ������ ��������� ������ cameraHeight, ����� �� ������� ����� �� �����������)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Show() // ��������
    {
        foreach (Renderer renderer in _rendererArray) // ��������� ������
        {
            if (ignoreRendererList.Contains(renderer)) continue; // ���� ������ � ������ ���������� �� ��������� ���
            renderer.enabled = true;
        }
       if(_canvas != null)
        {
            _canvas.gameObject.SetActive(true);
        }
    }

    private void Hide() // ������
    {
        foreach (Renderer renderer in _rendererArray)
        {
            if (ignoreRendererList.Contains(renderer)) continue; // ���� ������ � ������ ���������� �� ��������� ���
            renderer.enabled = false;
        }
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
        }
    }

    private void MoveAction_OnChangedFloorsStarted(object sender, MoveAction.OnChangeFloorsStartedEventArgs e)
    {
        floor = e.targetGridPosition.floor; // ������� ���� � ������ �����
    }
}
