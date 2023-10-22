using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryGridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRendererQuad; // Будем менять материал для визуализиции сетки инвенторя

    private bool _isBusy; //Занято 
    public void SetMaterial(Material material) // Установить переданный материал
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
