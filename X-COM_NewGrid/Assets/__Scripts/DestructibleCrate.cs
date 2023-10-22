using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Pathfinding;
using Unity.VisualScripting;

public class DestructibleCrate : MonoBehaviour // ����������� ����
{
    public static event EventHandler OnAnyDestroyed; // static - ���������� ��� event ����� ������������ ��� ����� ������, � �� ��� ����������� �����. ������� ��� ������������� ����� ������� ��������� �� ����� ������ �� ���������� ������, ��� ����� �������� ������ � ������� ����� �����, ������� ����� ��������� ���� � �� �� ������� ��� ������� �������. 
                                                     // �� �������� ������� Event ����(Any) ������ ��������.

    [SerializeField] private Transform _crate; // ����� ���� //��������� ������ �� �������� �.�. ��� ����� ����� �������, ������ ����������� � ���������� ������
    [SerializeField] private Transform _crateDestroyed; // ����������� ����     

    private GridPositionXZ _gridPosition; // ������� ����� ������ �����
    private SingleNodeBlocker _singleNodeBlocker;

    private void Awake()
    {      
        _singleNodeBlocker = GetComponent<SingleNodeBlocker>();
    }

    private void Start()
    {
        _crateDestroyed.gameObject.SetActive(false); // ������ ����������� ���� (�� ������ ������ ���� ������ ������ � ����������)
        _gridPosition = LevelGrid.Instance.GetGridPosition(transform.position); //������� �������� ������� �����
        _singleNodeBlocker.BlockAtCurrentPosition();// ���������� ����
    }
    public void Damage(Vector3 explosionPosition)
    {
        _crateDestroyed.gameObject.SetActive(true); // ���������� ����������� ����
        _crateDestroyed.parent = null; // ���������� ����������� ���� �� �������� 
        Destroy(gameObject); // ��������� ����

        GraphNode graphNode = AstarPath.active.GetNearest(transform.position).node; // ������� ����������� ����
        BlockManager.Instance.InternalUnblock(graphNode, _singleNodeBlocker); // ������������ ����

        ApplyExplosionToChildren(_crateDestroyed, 150f, explosionPosition, 10f); // �������� ����� � ������������ �����, � ����� 150, � ��� �� �������, � ������ �������� 10
               
        OnAnyDestroyed?.Invoke(this, EventArgs.Empty);
    }

    public GridPositionXZ GetGridPosition()
    {
        return _gridPosition;
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
