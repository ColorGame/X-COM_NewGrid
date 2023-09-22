using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TMPro;
using Unity.VisualScripting;

public class CameraController : MonoBehaviour // � ���������� ����� ������� ����������� � �������� InputSystem
{

    public static CameraController Instance { get; private set; }   //(������� SINGLETON) ��� �������� ������� ����� ���� ������� (SET-���������) ������ ���� �������, �� ����� ���� �������� GET ����� ������ �������
                                                                    // instance - ���������, � ��� ����� ���� ��������� LevelGrid ����� ���� ��� static. Instance ����� ��� ���� ����� ������ ������, ����� ����, ����� ����������� �� Event.


    private const float MIN_FOLLOW_Y_OFFSET = 2f;
    private const float MAX_FOLLOW_Y_OFFSET = 15f;

    [SerializeField] private CinemachineVirtualCamera _cinemachineVirtualCamera;
    [SerializeField] private Collider cameraBoundsCollider; //������ �� ��������� ������� ������������ ����������� ������

    private CinemachineTransposer _cinemachineTransposer;
    private Vector3 _targetFollowOffset; // ������� �������� ����������       
    private bool _edgeScrolling; // ��������� �� �����    
    private Unit _targetUnit;
    private bool _targetCompleted = true; // ���� ���������� 

    private float _moveSpeed = 10f; // �������� ������
    private float _rotationSpeed = 100f;
    private float _zoomSpeed = 5f;

    private void Awake()
    {
        // ���� �� �������� � ���������� �� �������� �� �����
        if (Instance != null) // ������� �������� ��� ���� ������ ���������� � ��������� ����������
        {
            Debug.LogError("There's more than one CameraController!(��� ������, ��� ���� CameraController!) " + transform + " - " + Instance);
            Destroy(gameObject); // ��������� ���� ��������
            return; // �.�. � ��� ��� ���� ��������� CameraController ��������� ����������, ��� �� �� ��������� ������ ����
        }
        Instance = this;

        _edgeScrolling = PlayerPrefs.GetInt("edgeScrolling", 1) == 1; // �������� ���������� �������� _edgeScrolling � ���� ��� 1 �� ����� ������ ���� ��=1 �� ����� ���� (�� PlayerPrefs.GetInt ������ ������ ������� ��������� ������� ���������� ������)
    }

    private void Start()
    {
        _cinemachineTransposer = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>(); // ������� � �������� ��������� CinemachineTransposer �� ����������� ������, ����� � ���������� �������� �� ��������� ��� ZOOM ������

        _targetFollowOffset = _cinemachineTransposer.m_FollowOffset; // �������� ����������

        FriendlyUnitButtonUI.OnAnyFriendlyUnitButtonPressed += FriendlyUnitButtonUI_OnAnyFriendlyUnitButtonPressed; //���������� ������ ����� ������ �������������� ����� 
        EnemyUnitButtonUI.OnAnyEnemylyUnitButtonPressed += EnemyUnitButtonUI_OnAnyEnemylyUnitButtonPressed; //���������� ������ ����� ������ �������������� �����
    }

    private void EnemyUnitButtonUI_OnAnyEnemylyUnitButtonPressed(object sender, Unit targetUnit)
    {
        _targetUnit = targetUnit;
        _targetCompleted = false;
    }

    private void FriendlyUnitButtonUI_OnAnyFriendlyUnitButtonPressed(object sender, Unit targetUnit)
    {
        _targetUnit = targetUnit;
        _targetCompleted = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();

        MoveSelectedUnit();
    }

    private void HandleMovement() // ������ ��������
    {
        Vector2 inputMoveDirection = InputManager.Instance.GetCameraMoveVector(); // ����������� ��������� ��������� (�������� ����� ������ ��������������)

        if (_edgeScrolling) // ���� ��������� �� ����� ������������� �� ������� ������� ����
        {
            Vector2 mousePosition = InputManager.Instance.GetMouseScreenPosition(); 
            float edgeScrollingSize = 0; // (���������� ��������) ������ �� ���� ������ ��� ���������� �������� ������
            if (mousePosition.x > Screen.width - edgeScrollingSize) // ���� ��� ������ ������ ����� - ������ �� ����
            {
                inputMoveDirection.x = +1f;
            }
            if (mousePosition.x < edgeScrollingSize)
            {
                inputMoveDirection.x = -1f;
            }
            if (mousePosition.y > Screen.height - edgeScrollingSize)
            {
                inputMoveDirection.y = +1f;
            }
            if (mousePosition.y < edgeScrollingSize)
            {
                inputMoveDirection.y = -1f;
            }
        }               

        //����� �������� ��������� �������� ����������� ������ inputMoveDirection � moveVector
        Vector3 moveVector = transform.forward * inputMoveDirection.y + transform.right * inputMoveDirection.x; // �������� ��������� ��������. ��������� ������ forward(z) ������� �� inputMoveDirection.y, � ��������� ������ right(x) ������� �� inputMoveDirection.x
        Vector3 targetPosition = transform.position + moveVector * _moveSpeed * Time.deltaTime; //��������� ������� ������� � ������� �����  ����������� ��� ������

        //��������� ��������
        targetPosition.x = Mathf.Clamp(targetPosition.x,
            cameraBoundsCollider.bounds.min.x ,
            cameraBoundsCollider.bounds.max.x);
        targetPosition.z = Mathf.Clamp(targetPosition.z,
            cameraBoundsCollider.bounds.min.z ,
            cameraBoundsCollider.bounds.max.z );

       // Debug.Log( cameraBoundsCollider.bounds.min);
       transform.position = targetPosition; // ���������� � ���������� �������
    }

    private void HandleRotation() // ������ �������
    {
        Vector3 rotationVector = new Vector3(0, 0, 0); // ������ �������� // ����� ������� ������ ������ ��� Y (�������� ����� ������ ��������������)

        rotationVector.y = InputManager.Instance.GetCameraRotateAmount(); //�������� �������� �������� ������ �� ��� �

        transform.eulerAngles += rotationVector * _rotationSpeed * Time.deltaTime;
        //��� ���� ������
        //transform .Rotate(rotationVector, _rotationSpeed * Time.deltaTime);
    }

    private void HandleZoom() // ������ ���������������
    {
        //Debug.Log(InputManager.Instance.GetCameraZoomAmount()); // ������� ����� ������� �������� ������

        float zoomIncreaseAmount = 1f; //������� �������� ���������� (�������� ����������)

        _targetFollowOffset.y += InputManager.Instance.GetCameraZoomAmount() * zoomIncreaseAmount; // �������� �������� ���������� ������

        // �� �� ���������� Time.deltaTime �.�. ��������� ��� ��������� �������� ��������� �������� � �� ��������� �� �������� (���� ����� ��� � ����������� ��� ������� ������� �������� Input.GetKeyDown)

        _targetFollowOffset.y = Mathf.Clamp(_targetFollowOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);// ��������� �������� ���������������
      
        _cinemachineTransposer.m_FollowOffset = Vector3.Lerp(_cinemachineTransposer.m_FollowOffset, _targetFollowOffset, Time.deltaTime * _zoomSpeed); // ��������� ���� ��������� ��������, ��� ��������� ���������� Lerp
    }

    private void MoveSelectedUnit()
    {
        if (!_targetCompleted) // ���� ���� �� ���������� �� ��������� �������� � ���
        {
            transform.position = Vector3.Lerp(transform.position, _targetUnit.transform.position, Time.deltaTime * _moveSpeed);

            float stoppingDistance = 0.2f; // ��������� ��������� //����� ���������//
            if (Vector3.Distance(transform.position, _targetUnit.transform.position) < stoppingDistance)  // ���� ��������� �� ������� ������� ������ ��� ��������� ��������� // �� �������� ����        
            {
                _targetCompleted = true;
            }
        }
    }

    public float GetCameraHeight() // �������� ������ ������ ��������
    {
        return _targetFollowOffset.y;
    }

    public void SetEdgeScrolling(bool edgeScrolling) // ���������� ������� �������� ��� - ��������� �� �����
    {
        this._edgeScrolling = edgeScrolling;
        PlayerPrefs.SetInt("edgeScrolling", edgeScrolling ? 1 : 0); // �������� ���������� �������� � ������ (���� _edgeScrolling ������ �� ��������� 1 ���� ���� ��������� 0 )
    }

    public bool GetEdgeScrolling() // ������� ������� �������� ��� - ��������� �� �����
    {
        return _edgeScrolling;
    }

}
