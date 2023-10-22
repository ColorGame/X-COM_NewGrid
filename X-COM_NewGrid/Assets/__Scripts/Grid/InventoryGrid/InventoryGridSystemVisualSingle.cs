using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryGridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRendererQuad; // ����� ������ �������� ��� ������������ ����� ���������

    private bool _isBusy; //������ 
    public void SetMaterial(Material material) // ���������� ���������� ��������
    {        
        _meshRendererQuad.material = material;
    }

    public void SetIsBusy(bool isBusy)
    {
        _isBusy = isBusy;
    }

    public bool GetIsBusy()
    {
        return _isBusy;
    }
}
