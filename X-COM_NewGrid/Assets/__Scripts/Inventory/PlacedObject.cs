using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static PlacedObjectTypeSO;

public class PlacedObject : MonoBehaviour // Размещенный объект (создаем и размещаем объект на сетке)
{
    public static PlacedObject CreateInGrid(Vector3 worldPosition, Vector2Int gridPosition, PlacedObjectTypeSO.Dir dir, PlacedObjectTypeSO placedObjectTypeSO, Transform parent) // (static обозначает что метод принадлежит классу а не кокому нибудь экземпляру)
    {
        PlacedObject placedObject = CreateInWorld(worldPosition, dir, placedObjectTypeSO, parent);
        placedObject._gridPositioAnchor = gridPosition;

        return placedObject;
    }

    public static PlacedObject CreateInWorld(Vector3 worldPosition, PlacedObjectTypeSO.Dir dir, PlacedObjectTypeSO placedObjectTypeSO, Transform parent)
    {
        Vector3 offset = placedObjectTypeSO.GetOffsetVisualFromParent(); // вычислим смещение чтобы создать объект в центре worldPosition
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, worldPosition - offset, Quaternion.Euler(parent.rotation.eulerAngles.x, 0, placedObjectTypeSO.GetRotationAngle(dir)), parent); //parent.rotation.eulerAngles.x- что бы был повернут как родитель

        PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();
        placedObject._placedObjectTypeSO = placedObjectTypeSO;
        placedObject._dir = dir;
        placedObject._offsetVisualFromParent = offset;
        placedObject.Setup();

        return placedObject;
    }

    public static PlacedObject CreateCanvas(Transform parent, Vector2 anchoredPosition, Vector2Int gridPosition, PlacedObjectTypeSO.Dir dir, PlacedObjectTypeSO placedObjectTypeSO)
    {
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, parent);
        placedObjectTransform.rotation = Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0);
        placedObjectTransform.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;

        PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();
        placedObject._placedObjectTypeSO = placedObjectTypeSO;
        placedObject._gridPositioAnchor = gridPosition;
        placedObject._dir = dir;
        placedObject.Setup();

        return placedObject;
    }


    private PlacedObjectTypeSO _placedObjectTypeSO;
    private GridSystemTiltedXY<GridObjectInventoryXY> _gridSystemXY; // Сетка в которой разместилься наш объект
    private Vector2Int _gridPositioAnchor; // Сеточная позиция Якоря
    private PlacedObjectTypeSO.Dir _dir;
    private Vector3 _targetRotation;
    private Vector3 _targetPosition;
    private Vector3 _startPosition;
    private bool _grabbed; // Схвачен   
    private bool _moveStartPosition = false; // Переместить в начальную позицию
    private Vector3 _scaleOriginal;
    private Transform _visual;
    private Vector3 _offsetVisualFromParent;


    private void Start()
    {
        Setup(); // Если создан до запуска сцены то нужно настроить его
    }

    private void LateUpdate()
    {
        if (_grabbed) // Если объект взяли то
        {
            float moveSpeed = 20f;
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(_targetRotation), Time.deltaTime * 15f);
            //transform.rotation = Quaternion.Lerp(transform.rotation, PickUpDropManager.Instance.GetPlacedObjectRotation(), Time.deltaTime * 15f);// Плавно повернем объект
        }

        if (_moveStartPosition) // Если надо переместить в начальную позиции и в конце уничтожим объект
        {
            float moveSpeed = 20f;
            transform.position = Vector3.Lerp(transform.position, _startPosition, Time.deltaTime * moveSpeed);

            float stoppingDistance = 0.1f; // Дистанция остановки //НУЖНО НАСТРОИТЬ//
            if (Vector3.Distance(transform.position, _startPosition) < stoppingDistance)  // Если растояние до целевой позиции меньше чем Дистанция остановки // Мы достигли цели        
            {
                _moveStartPosition = false; //прекратить движение Мы достигли _startPosition               
                Destroy(gameObject);
            }
        }
    }

    protected virtual void Setup()
    {
        _visual = transform.GetChild(0); //Получим визуальный объект   
        _visual.localPosition = _offsetVisualFromParent;    // Установим в середину занимаемых ячеек наш визуальный объект (Если забыли установить вручную в префабе инвенторя)           
        _startPosition = transform.position;  // Запомним стартовую позицию       
    }

    public Vector3 GetOffsetVisualFromParent()
    {
        return _offsetVisualFromParent;
    }

    public void Grab() // Захватить
    {
        _grabbed = true;
        // Сохраним оригинальный масштаб и поменяем его
        _scaleOriginal = transform.localScale;
        float _scaleMultiplier = 1.1f; // множитель масштаба для эффекта увеличения при захвате
        transform.localScale = _scaleOriginal * _scaleMultiplier;
    }

    public void Drop() // Бросить
    {
        _grabbed = false;
        transform.localScale = _scaleOriginal;
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        _targetPosition = targetPosition;
    }

    public void SetTargetRotation(Vector3 targetRotation)
    {
        _targetRotation = targetRotation;
    }

    public virtual void GridSetupDone()
    {
        //Debug.Log("PlacedObject.GridSetupDone() " + transform);
    }

    /* protected virtual void TriggerGridObjectChanged()
     {
         foreach (GridPositionXZ _gridPositioAnchor in GetGridPositionList())
         {
             GridBuildingSystem3D.Instance.GetGridObject(_gridPositioAnchor).TriggerGridObjectChanged();
         }
     }*/

    public Vector2Int GetGridPositionAnchor()
    {
        return _gridPositioAnchor;
    }

    public void SetGridPositionAnchor(Vector2Int gridPosition)
    {
        _gridPositioAnchor = gridPosition;
    }

    public GridSystemTiltedXY<GridObjectInventoryXY> GetGridSystemXY() //Получим сетку на которую добавили наш оббъект
    {
        return _gridSystemXY;
    }

    public void SetGridSystemXY(GridSystemTiltedXY<GridObjectInventoryXY> gridSystemXY) //Установим сетку на которую добавили наш оббъект
    {
        _gridSystemXY = gridSystemXY;
    }

    public List<Vector2Int> GetOccupiesGridPositionList() // Получить список занимаемых позиций в сетке. 
    {
        return _placedObjectTypeSO.GetGridPositionList(_gridPositioAnchor, _dir); // (в аргумент передадим его актуальную сеточную позицию и направлени)
    }


    public List<Vector2Int> GetTryOccupiesGridPositionList(Vector2Int gridPosition) //Получить список позиций в сетке, которые пытается занять. (в аргумент передадим сеточную позицию где хотим разместить)
    {
        return _placedObjectTypeSO.GetGridPositionList(gridPosition, _dir);
    }

    public PlacedObjectTypeSO.Dir GetDir() // Получить направление
    {
        return _dir;
    }

    public override string ToString()
    {
        return _placedObjectTypeSO.nameString;
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return _placedObjectTypeSO;
    }

    public Vector3 GetStartPosition()
    {
        return _startPosition;
    }

    public void SetStartPosition(Vector3 startPosition)
    {
        _startPosition = startPosition;
    }

    public void SetMoveStartPosition(bool moveStartPosition) // Установить надо перемещаться в стартовую позицию
    {
        _moveStartPosition = moveStartPosition;
    }

    /*public SaveObject GetSaveObject() // Получить сохраненный объект
    {
        return new SaveObject
        {
            placedObjectTypeSOName = _placedObject.name,
            gridPosition = _gridPositioAnchor,
            _dir = _dir,
            floorPlacedObjectSave = (this is FloorPlacedObject) ? ((FloorPlacedObject)this).Save() : "", //
        };
    }*/

    [System.Serializable]
    public class SaveObject // Сохраненный объект
    {

        public string placedObjectTypeSOName;
        public Vector2Int gridPosition;
        public PlacedObjectTypeSO.Dir dir;
        public string floorPlacedObjectSave; // сохранение объекта, размещенного на полу

    }

}
