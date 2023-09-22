using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlacedObject : MonoBehaviour // ����������� ������ (������� � ��������� ������ �� �����)
{
    public static PlacedObject CreateInGrid(Vector3 worldPosition, GridPosition gridPosition, PlacedObjectTypeSO.Dir dir, PlacedObjectTypeSO placedObjectTypeSO) // (static ���������� ��� ����� ����������� ������ � �� ������ ������ ����������)
    {
        PlacedObject placedObject = CreateInWorld(worldPosition, dir, placedObjectTypeSO);
        placedObject._gridPosition = gridPosition;

        return placedObject;
    }

    public static PlacedObject CreateInWorld(Vector3 worldPosition, PlacedObjectTypeSO.Dir dir, PlacedObjectTypeSO placedObjectTypeSO)
    {
        //worldPosition.y = 1; // �������� ������ ���� �������� ������
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, worldPosition, Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0));

        PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();
        placedObject._placedObjectTypeSO = placedObjectTypeSO;
        placedObject._dir = dir;

        placedObject.Setup();

        return placedObject;
    }

    public static PlacedObject CreateCanvas(Transform parent, Vector2 anchoredPosition, GridPosition gridPosition, PlacedObjectTypeSO.Dir dir, PlacedObjectTypeSO placedObjectTypeSO)
    {
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, parent);
        placedObjectTransform.rotation = Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0);
        placedObjectTransform.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;

        PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();
        placedObject._placedObjectTypeSO = placedObjectTypeSO;
        placedObject._gridPosition = gridPosition;
        placedObject._dir = dir;

        placedObject.Setup();

        return placedObject;
    }


   private PlacedObjectTypeSO _placedObjectTypeSO;
    private GridPosition _gridPosition;
    private PlacedObjectTypeSO.Dir _dir;
    private Vector3 _targetPosition;
    private bool _grabbed; // �������   
    private bool _overGrid; // ��� ������   
    private Vector3 _scaleOriginal;    

    

    private void Start()
    {       
        Setup(); // ���� ������ �� ������� ����� �� ����� ��������� ���
    }    

    private void LateUpdate()
    {
        if (_grabbed) // ���� ������ ����� ��
        {            
            float lerpSpeed = 20f;
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * lerpSpeed);

            //transform.rotation = Quaternion.Lerp(transform.rotation, PickUpDropManager.Instance.GetPlacedObjectRotation(), Time.deltaTime * 15f);// ������ �������� ������
        }        
    }

    protected virtual void Setup()
    {
        //Debug.Log("PlacedObject.Setup() " + transform);
    }

    public void Grab() // ���������
    {
        _grabbed = true;
        // �������� ������������ ������� ��������� ���
        _scaleOriginal = transform.localScale;
        float _scaleMultiplier = 1.1f; // ��������� �������� ��� ������� ���������� ��� �������
        transform.localScale = _scaleOriginal * _scaleMultiplier;
    }

    public void Drop() // �������
    {
        _grabbed = false;
        _overGrid = false;
        transform.localScale = _scaleOriginal;
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        _targetPosition = targetPosition;
    }

    public void SetOverGrid(bool overGrid)
    {
        _overGrid = overGrid;
    }

    public virtual void GridSetupDone()
    {
        //Debug.Log("PlacedObject.GridSetupDone() " + transform);
    }

    /* protected virtual void TriggerGridObjectChanged()
     {
         foreach (GridPosition _gridPosition in GetGridPositionList())
         {
             GridBuildingSystem3D.Instance.GetGridObject(_gridPosition).TriggerGridObjectChanged();
         }
     }*/

    public GridPosition GetGridPosition()
    {
        return _gridPosition;
    }

    public void SetGridPosition(GridPosition gridPosition)
    {
        _gridPosition = gridPosition;
    }

    public List<GridPosition> GetGridPositionList() // �������� ������ �������� ������� ��� ������ �������. (� �������� ��������� ��� ���������� �������� ������� � ����������)
    {
        return _placedObjectTypeSO.GetGridPositionList(_gridPosition, _dir);
    }

    public PlacedObjectTypeSO.Dir GetDir() // �������� �����������
    {
        return _dir;
    }

    public virtual void DestroySelf() // ���������� ����
    {
        Destroy(gameObject);
    }

    public override string ToString()
    {
        return _placedObjectTypeSO.nameString;
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return _placedObjectTypeSO;
    }


   


    /*public SaveObject GetSaveObject() // �������� ����������� ������
    {
        return new SaveObject
        {
            placedObjectTypeSOName = _placedObject.name,
            gridPosition = _gridPosition,
            _dir = _dir,
            floorPlacedObjectSave = (this is FloorPlacedObject) ? ((FloorPlacedObject)this).Save() : "", //
        };
    }*/

    [System.Serializable]
    public class SaveObject // ����������� ������
    {

        public string placedObjectTypeSOName;
        public GridPosition gridPosition;
        public PlacedObjectTypeSO.Dir dir;
        public string floorPlacedObjectSave; // ���������� �������, ������������ �� ����

    }

}
