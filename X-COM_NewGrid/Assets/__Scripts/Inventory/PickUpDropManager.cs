
using UnityEngine;
using static PlacedObjectTypeSO;


public class PickUpDropManager : MonoBehaviour // �������� �������������� � �������� ���������
{
    public static PickUpDropManager Instance { get; private set; }

    [SerializeField] private LayerMask _inventoryLayerMask; // ��� ��������� ��������� ���� ��� Inventory
    [SerializeField] private LayerMask _mousePlaneLayerMask; // ��������� ���� ���� ����� � ����� ��� ������ ��� MousePlane

    private Camera _mainCamera;
    private PlacedObject _placedObject; // ����������� ������
    private Plane _plane; // ��������� �� ������� ����� ���������� ����������� �������
    private Vector3 _offset; // ���������� ����� ������ ������� � ������  pivot �� �������.
    private PlacedObjectTypeSO _placedObjectTypeSO;
    private GridPosition _gridPosition;
    private PlacedObjectTypeSO.Dir _dir;

    private void Awake()
    {
        // ���� �� �������� � ���������� �� �������� �� �����
        if (Instance != null) // ������� �������� ��� ���� ������ ���������� � ��������� ����������
        {
            Debug.LogError("There's more than one PickUpDropManager!(��� ������, ��� ���� PickUpDropManager!) " + transform + " - " + Instance);
            Destroy(gameObject); // ��������� ���� ��������
            return; // �.�. � ��� ��� ���� ��������� PickUpDropManager ��������� ����������, ��� �� �� ��������� ������ ����
        }
        Instance = this;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        _plane = new Plane(Vector3.forward, new Vector3(0, 1, 0)); // �������� ��������� � �������� �� ��� Z(��� ����������� ����������� ���������� ����� ����), � ������ ��������� � ������� �� � �� 1 �.�. ��� �������� ��������� ����������� �=1
    }

    private void Update()
    {
        if (InputManager.Instance.IsMouseButtonDownThisFrame()) // ���� ������ ������ ���� � ���� ����
        {
            if (_placedObject == null) // �� ���� ��� ���� �������� ��������, ����������� ��������
            {

                Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); //���������� ���, ������ �� ������ ����� ����� ������ ��� ���������� ������ ���� 
                if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, _inventoryLayerMask)) // ������ true ���� ������� � ���������.
                {
                    _placedObject = raycastHit.transform.GetComponentInParent<PlacedObject>();
                    if (_placedObject != null) // ��������� �� ������� � ������� ������ ������� PlacedObject � �������� ���
                    {
                        _placedObject.Grab(); // ������� ���

                        // �������� �.�. ������ ���� ������ ������ ����� ����� �� ������
                       _placedObjectTypeSO = _placedObject.GetPlacedObjectTypeSO();


                        // �������� �������� �������
                        _plane.Raycast(ray, out float planeDistance); // ��������� ��� � ��������� � ������� ���������� ����� ����, ��� �� ���������� ���������.
                        Vector3 placedObjectPosition = _placedObject.transform.position;
                        _offset = placedObjectPosition - ray.GetPoint(planeDistance); // �������� �������� �� ������ ������� � ������  pivot �� �������.
                    }
                }
            }
            else // ������ ������ ���� ����� ������
            {
                _placedObject.Drop();
               
                _placedObject = null;
            }
        }

        if (_placedObject != null) // ���� ���� ����������� ������ ����� ��� ���������� �� ���������� ���� �� ��������� ���������
        {
            Ray ray = _mainCamera.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());//���������� ���, ������ �� ������ ����� ����� ������ ��� ���������� ������ ���� 
            _plane.Raycast(ray, out float planeDistance); // ��������� ��� � ��������� � ������� ���������� ����� ����, ��� �� ���������� ���������.
            Vector3 targetPosition = ray.GetPoint(planeDistance); // ������� ����� �� ���� ��� ��� ��������� ���������

           

            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, _mousePlaneLayerMask)) // ���� ������ � ������ ��������� ��� � ����� ��
            {
                _placedObject.SetTargetPosition(GetMouseWorldSnappedPosition() + _offset);
                _placedObject.SetOverGrid(true);
                Debug.Log("� �����");
            }
            else
            {
                _placedObject.SetTargetPosition(targetPosition + _offset);
                _placedObject.SetOverGrid(false);
            }
        }
    }


    public static Vector3 GetMousePosition() // �������� ������� ���� (static ���������� ��� ����� ����������� ������ � �� ������ ������ ����������) // ��� ����������� ����
    {
        Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition()); // ��� �� ������ � ����� �� ������ ��� ���������� ������ ����
        Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, Instance._mousePlaneLayerMask); // Instance._coverLayerMask - ����� ������ ��� �������� ����� ����� 1<<6  �.�. mousePlane ��� 6 �������
        return raycastHit.point; // ���� ��� ������� � �������� �� Physics.Raycast ����� true, � raycastHit.point ������ "����� ����� � ������� ������������, ��� ��� ����� � ���������", � ���� false �� ����� ������� ����������� ������ ������ ��������(� ����� ������ ������ ������� ������).
    }


    public Vector3 GetMouseWorldSnappedPosition() // ������������ ��������� ���� � ���� 
    {
        Vector3 mousePosition = GetMousePosition(); // �������� ������� ��� ���������
        GridPosition mouseGridPosition = InventoryGrid.Instance.GetGridPosition(mousePosition);

        if (_placedObjectTypeSO != null)
        {
            Vector2Int rotationOffset = _placedObjectTypeSO.GetRotationOffset(_dir); // �������� ������� ���� �� ��������
            Vector3 placedObjectWorldPosition = InventoryGrid.Instance.GetWorldPosition(mouseGridPosition) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * InventoryGrid.Instance.GetCellSize();
            return placedObjectWorldPosition; // ������ ��������������� ��������� � ����� �����
        }
        else
        {
            return mousePosition;
        }
    }

    public Quaternion GetPlacedObjectRotation() // ������� �������� ������������ �������
    {
        if (_placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, _placedObjectTypeSO.GetRotationAngle(_dir), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }



}
