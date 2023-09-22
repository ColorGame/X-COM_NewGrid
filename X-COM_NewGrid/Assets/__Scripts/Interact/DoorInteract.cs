using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;

public class DoorInteract : MonoBehaviour, IInteractable //�����-�������������� �������� ����� ����������� 
{
    public static event EventHandler OnAnyDoorOpened; // �� �������� ������� Event �����(Any) ����� �������.
                                                      // static - ���������� ��� event ����� ������������ ��� ����� ������, � �� ��� ���������� �����.
                                                      // ������� ��� ������������� ����� ������� ��������� �� ����� ������ �� ���������� ������, ��� ����� �������� ������ � �������
                                                      // ����� �����, ������� ����� ��������� ���� � �� �� ������� ��� ������� �������. 

    public static event EventHandler OnAnyDoorIsLocked; //������� ������� ����� -����� ����� �������(������ ������� �������)
                                                        // static - ���������� ��� event ����� ������������ ��� ����� ������, � �� ��� ���������� �����.

    public event EventHandler OnDoorOpened; //������� ������� ����� - ����� �������




    [SerializeField] private bool _isOpen; //������� (�����)
    [SerializeField] private bool _isInteractable = true; //����� ����������������� (�� ��������� true)

    private HashAnimationName _animBase = new HashAnimationName(); 
    private bool _isActive;
    private float _timer; // ������ ������� �� ����� ��������� ���������� ����������������� � ������
    private Transform[] _transformChildrenDoorArray;  //������ �������� �������� ����� (��� ���� �����[0] �����[1] b ������[2] �����)
    private Animator _animator; //�������� �� �����
    private SingleNodeBlocker _singleNodeBlocker; // ����������� ���� �� ����� �����

    private Action _onInteractionComplete; // ������� �������������� ���������// �������� ������� � ������������ ���� - using System;
                                           //�������� ��� ������� ��� ������������ ���������� (� ��� ����� ��������� ������ ������� �� ���������).
                                           //Action- ���������� �������. ���� ��� �������. ������� Func<>. 
                                           //�������� ��������. ����� ���������� ������� � ������� �� �������� ��������, ����� �����, � ������������ ����� ����, ����������� ����������� �������.
                                           //�������� ��������. ����� �������� �������� ������� �� ������� ������

    private List<GridPosition> _doorGridPositionList = new List<GridPosition>();



    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _transformChildrenDoorArray = GetComponentsInChildren<Transform>();
        _singleNodeBlocker = GetComponent<SingleNodeBlocker>();
    }


    private void Start()
    {
        foreach (GridPosition gridPosition in GetDoorGridPositionList()) // ��������� ������ �������� ������� ������� �������� �����
        {
            LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this); // � ���������� �������� ������� ��������� ��� �������� ������ ����� � ����������� Interactable(��������������)            
        }        
        UpdateStateDoor(_isOpen);
    }

    private void Update()
    {
        if (!_isActive) // ���� ���������� �������������� � ������ �� ��� ������� � ����������� ��� ����
        {
            return;// ������� � ���������� ��� ����
        }

        _timer -= Time.deltaTime; // �������� ������

        if (_timer <= 0f)
        {
            _isActive = false; // ��������� �������� ���������
            _onInteractionComplete(); // ������� ����������� ������� ������� ��� �������� ������� Interact(). � ����� ������ ��� ActionComplete() �� ������� ��������� � ������ UI
        }
    }

    public void UpdateStateDoor(bool isOpen) //������� ��������� ����� � ����������� �� ���������� ������� ����������
    {
        if (isOpen) // ���������� ������������� �� ��������� ��������� ����� 
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }



    public void Interact(Action onInteractionComplete) // ��������������. � �������� ������� ������� �������������� ���������
    {
        _onInteractionComplete = onInteractionComplete; // �������� ���������� �������
        _isActive = true; // ����������� ����� � �������� ���������
        _timer = 1.5f; // ������ ����� ��������� ���������  //����� ���������//

        if (_isInteractable) // ���� � ������ ����� ����������������� ��
        {
            if (_isOpen) // ��� ��������������, ���� ����� ������� ����� �� ��������� � ��������
            {
                CloseDoor();
            }
            else
            {
                OpenDoor();
            }
        }
        else
        {
            // ����� ����������� ���� ���������� ���������� ��� ��������� �������
            SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.DoorClosed);
            OnAnyDoorIsLocked?.Invoke(this, EventArgs.Empty); // �������� ������� ����� ����� ������� (��� ���������� �������)
        }
    }

    private void OpenDoor() // ������� �����
    {
        _isOpen = true;        
        _animator.CrossFade(_animBase.DoorOpen, 0.2f);
        //_animator.SetBool("IsOpen", _isOpen); // �������� ������� ���������� "GetIsOpen". ��������� �� �������� _isOpen        

        foreach (GridPosition gridPosition in _doorGridPositionList) // ��������� ������ �������� ������� ������� �������� �����
        {
            // PathfindingMonkey.Instance.SetIsWalkableGridPosition(_gridPosition, true); // ��������� ��� ����� ������ �� ���� �������� �������
            GraphNode graphNode = LevelGrid.Instance.GetGridNode(gridPosition); // ������� ����������� ����
            BlockManager.Instance.InternalUnblock(graphNode, _singleNodeBlocker); // ������������ ����
        }
        SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.DoorOpen);

        // �������� �������
        OnDoorOpened?.Invoke(this, EventArgs.Empty);
        OnAnyDoorOpened?.Invoke(this, EventArgs.Empty);
    }

    private void CloseDoor() // ������� �����
    {        
        var selectorList = new List<SingleNodeBlocker>() { _singleNodeBlocker };   // ������ ����������� ������� ����� ������������. ������� ���� ���� �����

        // �������� ����� ������ ����� ��� ����
        foreach (GridPosition gridPosition in _doorGridPositionList) // ��������� ������ �������� ������� ������� �������� �����
        {           
            GraphNode graphNode = LevelGrid.Instance.GetGridNode(gridPosition); // ������� ����������� ����
            if (BlockManager.Instance.NodeContainsAnyExcept(graphNode, selectorList)) // ���� ��� ���� �������������� ������. ���� ������ (SingleNodeBlocker), ���������� ��� �� ����, ��� � �����
            {                
                _animator.CrossFade(_animBase.DoorBlocked, 0.2f); // ����� �������������
                return; // ������� � ���������� ��� ����
            }           
        }
               
        foreach (GridPosition gridPosition in _doorGridPositionList) // ��������� ������ �������� ������� ������� �������� �����
        {
            //  PathfindingMonkey.Instance.SetIsWalkableGridPosition(_gridPosition, false); // ��������� ��� ������ ������ �� ���� �������� �������
            GraphNode graphNode = LevelGrid.Instance.GetGridNode(gridPosition); // ������� ����������� ����
            BlockManager.Instance.InternalBlock(graphNode, _singleNodeBlocker); // ����������� ����
        }

        _isOpen = false;
        _animator.CrossFade(_animBase.DoorClose, 0.2f);
        // _animator.SetBool("IsOpen", _isOpen); // �������� ������� ���������� "GetIsOpen". ��������� �� �������� _isOpen
        SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.DoorOpen);
    }

    private List<GridPosition> GetDoorGridPositionList() //�������� ������ �������� ������� �����  
    {
        //������� ����� � ��, ����� ������� ����� �������� https://community.gamedev.tv/t/larger-doors-and-doors-that-can-be-shot-through/220723*/

        float offsetFromEdgeGrid = 0.01f; // �������� �� ���� ����� (������) //����� ���������//

        GridPosition childreGridPositionLeft = LevelGrid.Instance.GetGridPosition(_transformChildrenDoorArray[1].position + _transformChildrenDoorArray[1].right * offsetFromEdgeGrid); // ��������� �������� ������� ��������� ������� ����� ����� �������� ����� (��������� �� ������� ������ ��� �� �� ������� �� �������� �����)
        GridPosition childreGridPositionRight = LevelGrid.Instance.GetGridPosition(_transformChildrenDoorArray[2].position - _transformChildrenDoorArray[2].right * offsetFromEdgeGrid); // ��������� �������� ������� ��������� ������� ����� ������ �������� ����� (��������� �� ������� ������ ��� �� �� ������� �� �������� ������)

        //_doorGridPositionList = PathfindingMonkey.Instance.FindPath(childreGridPositionRight, childreGridPositionLeft, out int pathLength); // ����������� �� ������ ����

        // ������� ������ ���� ����� ������� ����� ������������ ��� ����, �� �� �������� ��� ������ ������������� �� ���������
        // ��������������� �����
        if (childreGridPositionLeft.x == childreGridPositionRight.x)
        {
            if (childreGridPositionLeft.z <= childreGridPositionRight.z) //��� ������� ��� ����� ������� ���� ������
            {
                for (int z = childreGridPositionLeft.z; z <= childreGridPositionRight.z; z++)  // ��������� �������� ������� �� ��� Z (�������� ������ �� ����� ������� �� ������).
                {
                    GridPosition testGridPosition = new GridPosition(childreGridPositionLeft.x, z, childreGridPositionLeft.floor); // ����������� �������� ������� �� ��� Z. �������� ������� �� X ����� ����� ����� �������

                    _doorGridPositionList.Add(testGridPosition); // ������� � ������ ����������� ������    
                }
            }

            if (childreGridPositionRight.z <= childreGridPositionLeft.z) //��� ������� ��� ������ ������� ���� �����
            {
                for (int z = childreGridPositionRight.z; z <= childreGridPositionLeft.z; z++)  // ��������� �������� ������� �� ��� Z (�������� ������ �� ������ ������� �� �����).
                {
                    GridPosition testGridPosition = new GridPosition(childreGridPositionLeft.x, z, childreGridPositionLeft.floor); // ����������� �������� ������� �� ��� Z. �������� ������� �� X ����� ����� ����� �������

                    _doorGridPositionList.Add(testGridPosition); // ������� � ������ ����������� ������    
                }
            }
        }

        // �������������� �����
        if (childreGridPositionLeft.z == childreGridPositionRight.z)
        {
            if (childreGridPositionLeft.x <= childreGridPositionRight.x) //��� ������� ��� ����� ������� ����� �� ������
            {
                for (int x = childreGridPositionLeft.x; x <= childreGridPositionRight.x; x++)  // ��������� �������� ������� �� ��� � (�������� ������ �� ����� ������� �� ������).
                {
                    GridPosition testGridPosition = new GridPosition(x, childreGridPositionLeft.z, childreGridPositionLeft.floor); // ����������� �������� ������� �� ��� �. �������� ������� �� Z ����� ����� ����� �������

                    _doorGridPositionList.Add(testGridPosition); // ������� � ������ ����������� ������    
                }
            }

            if (childreGridPositionLeft.x >= childreGridPositionRight.x) //��� ������� ��� ����� ������� ������ �� ������
            {
                for (int x = childreGridPositionRight.x; x <= childreGridPositionLeft.x; x++)  // ��������� �������� ������� �� ��� � (�������� ������ �� ������ ������� �� �����).
                {
                    GridPosition testGridPosition = new GridPosition(x, childreGridPositionLeft.z, childreGridPositionLeft.floor); // ����������� �������� ������� �� ��� �. �������� ������� �� Z ����� ����� ����� �������

                    _doorGridPositionList.Add(testGridPosition); // ������� � ������ ����������� ������    
                }
            }
        }

        return _doorGridPositionList;
    }

    public void SetIsInteractable(bool isInteractable) // ���������� ����� �����������������
    {
        _isInteractable = isInteractable;
    }
}

