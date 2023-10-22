using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelInteract : MonoBehaviour, IInteractable // ����� ��������������
{


    public event EventHandler OnBarrelInteractlActivated; //�������� ������� ����� - ����� �������������� �������������

    [SerializeField] private Transform _barrel; // ����� �����
    [SerializeField] private Transform _barrelDestroyed; //����������� �����


    private GridPositionXZ _gridPosition;
    private Action _onInteractionComplete; // ������� �������������� ���������// �������� ������� � ������������ ���� - using System;
                                           //�������� ��� ������� ��� ������������ ���������� (� ��� ����� ��������� ������ ������� �� ���������).
                                           //Action- ���������� �������. ���� ��� �������. ������� Func<>. 
                                           //�������� ��������. ����� ���������� ������� � ������� �� �������� ��������, ����� �����, � ������������ ����� ����, ����������� ����������� �������.
                                           //�������� ��������. ����� �������� �������� ������� �� ������� ������
    private bool _isActive;
    private float _timer;// ������ ��� �� �������������� ���� �� ���������� � ������ ������ ���� ��������� ��������� �����
    private SingleNodeBlocker _singleNodeBlocker; // ����������� ����
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _singleNodeBlocker = GetComponent<SingleNodeBlocker>();
    }


    private void Start()
    {       
        _gridPosition = LevelGrid.Instance.GetGridPosition(transform.position); // ������� �������� ������� �����
        LevelGrid.Instance.SetInteractableAtGridPosition(_gridPosition, this); // ���������� ���������� ��������� �������������� � ���� �������� �������
        _singleNodeBlocker.BlockAtCurrentPosition();// ���������� ����

        // PathfindingMonkey.Instance.SetIsWalkableGridPosition(_gridPositioAnchor, false); // ���������� ��� ����� ��� ������ (� ����������� �� isWalkable)  ������ �� ���������� � �������� �������� �������
    }

    private void Update()
    {
        if (!_isActive) // ���� ���������� �������������� � ������ �� ��� ������� � ����������� ��� ����
        {
            return; // ������� � ���������� ��� ����
        }

        _timer -= Time.deltaTime; // �������� ������

        if (_timer <= 0f)
        {
            _isActive = false;// ��������� �������� ���������                    
            Destroy(gameObject); // ��������� �����
            _onInteractionComplete(); // ������� ����������� ������� ������� ��� �������� ������� Interact(). � ����� ������ ��� ActionComplete() �� ������� ��������� � ������ UI
        }
    }

    public void Interact(Action onInteractionComplete) // ��������������. � �������� ������� ������� �������������� ���������
    {
        _onInteractionComplete = onInteractionComplete;// �������� ���������� �������
        _isActive = true; //����������� ����� � �������� ���������
        _timer = 0.5f;// ������ ����� ��������� ���������  //����� ���������//

        
        _barrelDestroyed.gameObject.SetActive(true); // ���������� ����������� �����
        _barrelDestroyed.parent = null; // ���������� ����������� ����� �� ��������    
        _barrel.gameObject.SetActive(false); // �������� ����� �����
        _collider.enabled = false; // �������� ��������� ��� �� �� ����� ������

        ApplyExplosionToChildren(_barrelDestroyed, 150f, transform.position, 10f); // �������� ���� � ����������� �����

        LevelGrid.Instance.ClearInteractableAtGridPosition(_gridPosition); // ������� ��������� �������������� � ���� �������� �������

        GraphNode graphNode = AstarPath.active.GetNearest(transform.position).node; // ������� ����������� ����
        BlockManager.Instance.InternalUnblock(graphNode, _singleNodeBlocker); // ������������ ����
        //PathfindingMonkey.Instance.SetIsWalkableGridPosition(_gridPositioAnchor, true); // ���������� ��� ����� ������ �� ���� ������

        OnBarrelInteractlActivated?.Invoke(this, EventArgs.Empty); // �������� ������� - ����� �������������� �������������
    }

    private void ApplyExplosionToChildren(Transform root, float explosionForce, Vector3 explosionPosition, float explosionRange) // ��������� ����� � ����� (explosionRange �������� ������)
    {
        foreach (Transform child in root)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childrigidbody)) // ��������� �������� ��������� �������� �������� 
            {
                childrigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRange);
            }

            ApplyExplosionToChildren(child, explosionForce, explosionPosition, explosionRange);  // ����������� �������
        }
    }  

}
