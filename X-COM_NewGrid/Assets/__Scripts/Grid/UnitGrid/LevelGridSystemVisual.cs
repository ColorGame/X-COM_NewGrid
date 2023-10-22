//#define HEX_GRID_SYSTEM //������������ �������� ������� //  � C# ��������� ��� �������� �������������, ����������� ������� �� ��������public enum����� ��������� ���� ��������� ������������. 
//��� ��������� ���������� ������� ������������� ������ ��������� ����� �� ����������� � ��������� ��� � ��� �������� �����, ��� ��� ����������. 

using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnitActionSystem;


// ������� LevelGridSystemVisual ��� ������� ����� ������� �� ���������, ��������� �� �����, ����� ���������� ������� ����������� ����� ����� ����������.
// (Project Settings/ Script Execution Order � �������� ���������� LevelGridSystemVisual ���� Default Time)
public class LevelGridSystemVisual : MonoBehaviour //�������� ������� ������������  ������������ ��������� ����� �� ����� 
{
    public static LevelGridSystemVisual Instance { get; private set; }   //(������� SINGLETON) ��� �������� ������� ����� ���� ������� (SET-���������) ������ ���� �������, �� ����� ���� �������� GET ����� ������ �������
                                                                    // instance - ���������, � ��� ����� ���� ��������� UnitActionSystem ����� ���� ��� static. Instance ����� ��� ���� ����� ������ ������, ����� ����, ����� ����������� �� Event.

    [Serializable] // ����� ��������� ��������� ����� ������������ � ����������
    public struct GridVisualTypeMaterial    //������ ����� ��� ��������� // �������� ��������� ����� � ��������� ������. ������ � �������� ��������� ������������ ��� ���� ������ �������� ����������� ����� ������ � C#
    {                                       //� ������ ��������� ��������� ��������� ����� � ����������
        public GridVisualType gridVisualType;
        public Material materialGrid;
    }

    public enum GridVisualType //���������� ��������� �����
    {
        White,
        Blue,
        BlueSoft,
        Red,
        RedSoft,
        Yellow,
        YellowSoft,
        Green,
        GreenSoft,
    }
           

    [SerializeField] private List<GridVisualTypeMaterial> _gridVisualTypeMaterialListQuad; // ������ ��� ��������� ����������� ��������� ����� ������� (������ �� ���������� ���� ������) ����������� ��������� ����� // � ���������� ��� ������ ��������� ���������� ��������������� �������� �����
    [SerializeField] private List<GridVisualTypeMaterial> _gridVisualTypeMaterialListHex; // ������ ��� ��������� ����������� ��������� ����� ������������ (������ �� ���������� ���� ������) ����������� ��������� ����� // � ���������� ��� ������ ��������� ���������� ��������������� �������� �����


    private List<GridPositionXZ> _validGridPositionForGrenadeActionList; // ����� ���������� -������ ���������� �������� ������� ��� �������� ������� 
    private List<GridPositionXZ> _validGridPositionForComboActionList; // ����� ���������� -������ ���������� �������� ������� ��� �������� �����

    private LevelGridSystemVisualSingle[,,] _gridSystemVisualSingleArray; // ���������� ������    

    // ��� ������� ������������ ����� (����������� ������ ��� ������)
    // private LevelGridSystemVisualSingle _lastSelectedGridSystemVisualSingle; // ��������� ��������� �������� ������� ������������ �������

    private void Awake() //��� ��������� ������ Awake ����� ������������ ������ ��� ������������� � ���������� ��������
    {
        // ���� �� �������� � ���������� �� �������� �� �����
        if (Instance != null) // ������� �������� ��� ���� ������ ���������� � ��������� ����������
        {
            Debug.LogError("There's more than one UnitActionSystem!(��� ������, ��� ���� UnitActionSystem!) " + transform + " - " + Instance);
            Destroy(gameObject); // ��������� ���� ��������
            return; // �.�. � ��� ��� ���� ��������� UnitActionSystem ��������� ����������, ��� �� �� ��������� ������ ����
        }
        Instance = this;
    }

    private void Start()
    {
        _gridSystemVisualSingleArray = new LevelGridSystemVisualSingle[ // ������� ������ ������������� �������� widthX �� heightY  � FloorAmount
            LevelGrid.Instance.GetWidth(),
            LevelGrid.Instance.GetHeight(),
            LevelGrid.Instance.GetFloorAmount()
        ];

        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                for (int floor = 0; floor < LevelGrid.Instance.GetFloorAmount(); floor++)  // ��������� ��� �����
                {
                    GridPositionXZ gridPosition = new GridPositionXZ(x, z, floor);                    

                    Transform gridSystemVisualSingleTransform = Instantiate(GameAssets.Instance.levelGridSystemVisualSinglePrefab, LevelGrid.Instance.GetWorldPosition(gridPosition), Quaternion.identity); // �������� ��� ������ � ������ ������� �����

                    _gridSystemVisualSingleArray[x, z, floor] = gridSystemVisualSingleTransform.GetComponent<LevelGridSystemVisualSingle>(); // ��������� ��������� LevelGridSystemVisualSingle � ���������� ������ ��� x,y,floor ��� ����� ������� �������.
                }
            }
        }

        UnitActionSystem.Instance.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged; // ���������� �� ������� ��������� �������� �������� (����� �������� �������� �������� � ����� ������ �� �������� ������� Event)
        UnitActionSystem.Instance.OnBusyChanged += Instance_OnBusyChanged; //���������� �� ������� ��������� �������� 
        MouseWorld.OnMouseGridPositionChanged += MouseWorld_OnMouseGridPositionChanged;// ���������� �� ������� �������� ������� ���� �������� ��� ��������� � ���������� ����� �����. �������� ��������� �������
        MoveAction.OnAnyUnitPathComplete += MoveAction_OnAnyUnitPathComplete; //���������� � ������ ����� �������� ����
        //  LevelGrid.Instance.OnAnyUnitMovedGridPosition += LevelGrid_OnAnyUnitMovedGridPosition; // ���������� �� ������� ����� ���� ��������� � �������� �������

        UpdateGridVisual();


        // ��������� ��� ����� ��� ������� HEX(�������������� ������ � ������ MouseWorld_OnMouseGridPositionChanged)
        /* for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
         {
             for (int y = 0; y < LevelGrid.Instance.GetHeight(); y++)
             {
                 _gridSystemVisualSingleArray[x, y].
                     Show(GetGridVisualTypeMaterial(GridVisualType.White));
             }
         }*/

    }

    private void MoveAction_OnAnyUnitPathComplete(object sender, Unit unit)
    {
        if (UnitActionSystem.Instance.GetSelectedUnit() == unit) // ���� ���� �������� � ����������� ����� �� ������� ������������ (����� �� ��������� �� ����� ������ ������)
        {
            UpdateGridVisual();
        }
    }
    private void Instance_OnBusyChanged(object sender, OnUnitSystemEventArgs e)
    {
        if (e.selectedAction is not MoveAction) // ���� ��������� �������� �� �� ����� �������� �� �������. MoveAction ��� �������� �� OnBusyChanged � � ��� ����������� ����.
                                                // ����� ������� ���������� ������� OnAnyUnitPathComplete ��� � ����� ���������
        {
            UpdateGridVisual();
        }
    }
    private void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }

    /*private void LevelGrid_OnAnyUnitMovedGridPosition(object sender, LevelGrid.OnPlacedObjectOverGridPositionEventArgs e)
    {
        UpdateGridVisual();

        //�������� ���������, ������� �� ������ �������, ��� ��������� ��������� ���������� ������ ���, ����� ���� ���������� �������,
        //������ ����� ��������� ���������� ������ �����, ����� ���� ��������� �������� �����.
        //��� �������� �������� ��� �������� ����������, � ��� ����� ��������� ������� �������.
    }*/

    // ��� ������� ������������ ����� (����������� ������ ��� ������)
    /*private void Update()
    {

        if (_lastSelectedGridSystemVisualSingle != null)
        {
            _lastSelectedGridSystemVisualSingle.HideSelected(); // ������� ��������� ��������� LevelGridSystemVisualSingle
        }

        Vector3 mouseWorldPosition = MouseWorld.GetPosition(); //������� ������� ����
        GridPositionXZ _gridPositioAnchor = LevelGrid.Instance.GetGridPosition(mouseWorldPosition); // ������� �������� ������� ����
        if (LevelGrid.Instance.IsValidGridPosition(_gridPositioAnchor)) // ���� ��� ���������� �������� ������� ��
        {
            _lastSelectedGridSystemVisualSingle = _gridSystemVisualSingleArray[_gridPositioAnchor.x, _gridPositioAnchor.y]; // �������� ��� ���������� LevelGridSystemVisualSingle
        }

        if (_lastSelectedGridSystemVisualSingle != null)
        {
            _lastSelectedGridSystemVisualSingle.ShowSelected();// ������� ��������� ��������� LevelGridSystemVisualSingle
        }
    }*/

    private void MouseWorld_OnMouseGridPositionChanged(object sender, MouseWorld.OnMouseGridPositionChangedEventArgs e)
    {
        // ��� ��������� ��������� ���� ����� ��������� ����������� ����� ��� ��������, ������� ���������� ������ �������� �������

        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction(); // ������� ��������� ��������

        switch (selectedAction) // ������������� ��������� ������� � ����������� �� ���������� ��������
        {
            case GrenadeFragmentationAction grenadeFragmentationAction:// �� ����� ������� �������

                UpdateVisualDamageCircleGrenade(grenadeFragmentationAction, e);
                break;

            case GrenadeStunAction grenadeStunAction:// �� ����� ������� �������

                UpdateVisualDamageCircleGrenade(grenadeStunAction, e);
                break;

            case GrenadeSmokeAction grenadeSmokeAction:

                UpdateVisualDamageQuadGrenade(grenadeSmokeAction, e);
                break;

            case ComboAction comboAction:
                if (comboAction.GetState() == ComboAction.State.ComboStart)
                {
                    UpdateVisualSelectedQuadComboAction(comboAction, e);
                }
                break;
        }
    }

    private void UpdateVisualSelectedQuadComboAction(ComboAction comboAction, MouseWorld.OnMouseGridPositionChangedEventArgs e)
    {
        _gridSystemVisualSingleArray[e.lastMouseGridPosition.x, e.lastMouseGridPosition.z, e.lastMouseGridPosition.floor].HideQuadGrenade(); // ������ ������� �� ���������� ������
        GridPositionXZ mouseGridPosition = e.newMouseGridPosition; // �������� ������� ����

        if (_validGridPositionForComboActionList.Contains(mouseGridPosition)) // ���� �������� ������� ���� ������ � ���������� �������� �� ...
        {
            _gridSystemVisualSingleArray[mouseGridPosition.x, mouseGridPosition.z, mouseGridPosition.floor].ShowQuadGrenade(GetGridVisualTypeMaterial(GridVisualType.Red)); // ������� ��� ����� �������� ������� ������� �������� ����� (���� ���� ����������� ������������ �����)
        }
    }

    private void UpdateVisualDamageQuadGrenade(GrenadeAction grenadeAction, MouseWorld.OnMouseGridPositionChangedEventArgs e)
    {
        _gridSystemVisualSingleArray[e.lastMouseGridPosition.x, e.lastMouseGridPosition.z, e.lastMouseGridPosition.floor].HideQuadGrenade(); // ������ ������� �� ���������� ������
        GridPositionXZ mouseGridPosition = e.newMouseGridPosition; // �������� ������� ����

        if (_validGridPositionForGrenadeActionList.Contains(mouseGridPosition)) // ���� �������� ������� ���� ������ � ���������� �������� �� ...
        {
            float damageRadiusInWorldPosition = grenadeAction.GetDamageRadiusInWorldPosition();
            _gridSystemVisualSingleArray[mouseGridPosition.x, mouseGridPosition.z, mouseGridPosition.floor].ShowQuadGrenade(GetGridVisualTypeMaterial(GridVisualType.RedSoft), damageRadiusInWorldPosition); // ������� ��� ����� �������� ������� ������� ��������� �� ������� � ��������� ������  � ���� ���������
        }
    }

    private void UpdateVisualDamageCircleGrenade(GrenadeAction grenadeAction, MouseWorld.OnMouseGridPositionChangedEventArgs e)
    {
        _gridSystemVisualSingleArray[e.lastMouseGridPosition.x, e.lastMouseGridPosition.z, e.lastMouseGridPosition.floor].Hide�ircleGrenade(); // ������ ���� �� ���������� ������
        GridPositionXZ mouseGridPosition = e.newMouseGridPosition; // �������� ������� ����

        if (_validGridPositionForGrenadeActionList.Contains(mouseGridPosition)) // ���� �������� ������� ���� ������ � ���������� �������� �� ...
        {
            float damageRadiusInWorldPosition = grenadeAction.GetDamageRadiusInWorldPosition();
            _gridSystemVisualSingleArray[mouseGridPosition.x, mouseGridPosition.z, mouseGridPosition.floor].Show�ircleGrenade(damageRadiusInWorldPosition); // ������� ��� ����� �������� ������� ���� ��������� �� ������� � ��������� ������ �����
        }
    }

    private void HideAllGridPosition() // ������ ��� ������� �����
    {
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                for (int floor = 0; floor < LevelGrid.Instance.GetFloorAmount(); floor++)  // ��������� ��� �����
                {
                    _gridSystemVisualSingleArray[x, z, floor].Hide();
                }
            }
        }
    }

    private void ShowGridPositionRange(GridPositionXZ gridPosition, int range, GridVisualType gridVisualType, bool showFigureRhombus) // �������� ��������� �������� �������� ������� ��� �������� (� ��������� �������� �������� �������, ������ ��������, ��� ��������� ������� �����, ������� ���������� ���� ���� ���������� � ���� ����� �� �������� true, ���� � ���� �������� �� - false )
    {
        // �� �������� ��� � ShootAction � ������ "public override List<GridPositionXZ> GetValidActionGridPositionList()"

        List<GridPositionXZ> gridPositionList = new List<GridPositionXZ>();

        for (int x = -range; x <= range; x++)  // ���� ��� ����� ����� ������� � ������������ unitGridPosition, ������� ��������� ���������� �������� � �������� ������� range
        {
            for (int z = -range; z <= range; z++)
            {
                GridPositionXZ offsetGridPosition = new GridPositionXZ(x, z, 0); // ��������� �������� �������. ��� ������� ���������(0,0) �������� ��� ���� 
                GridPositionXZ testGridPosition = gridPosition + offsetGridPosition; // ����������� �������� �������

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) // �������� �������� �� testGridPosition ���������� �������� �������� ���� ��� �� ��������� � ���� �����
                {
                    continue; // continue ���������� ��������� ���������� � ��������� �������� ����� 'for' ��������� ��� ����
                }

                LevelGridNode levelGridNode = LevelGrid.Instance.GetGridNode(testGridPosition);

                //���� � ���� ������� ��� ���� ���� ������ ��� GridPositionXZ ����� � �������  
                if (levelGridNode == null)
                {
                    continue; // ��������� ��� �������
                }

                
                /*//�������� �������� ������� ������� ����� � �������
                if (PathfindingMonkey.Instance.GetGridPositionInAirList().Contains(testGridPosition))
                {
                    continue;
                }*/

                if (showFigureRhombus)
                {
                    // ��� ��������� �������� ������� ���� � �� �������
                    int testDistance = Mathf.Abs(x) + Mathf.Abs(z); // ����� ���� ������������� ��������� �������� �������
                    if (testDistance > range) //������� ������ �� ����� � ���� ����� // ���� ���� � (0,0) �� ������ � ������������ (5,4) ��� �� ������� �������� 5+4>7
                    {
                        continue;
                    }
                }
                gridPositionList.Add(testGridPosition);
            }
        }

        ShowGridPositionList(gridPositionList, gridVisualType); // ������� ��������� �������� ��������
    }

    public void ShowGridPositionList(List<GridPositionXZ> gridPositionlist, GridVisualType gridVisualType)  //������� ������ GridPositionXZ (� ��������� ���������� ������ GridPositionXZ � ��������� ������������ ����� gridVisualType)
    {
        foreach (GridPositionXZ gridPosition in gridPositionlist) // � ����� ��������� ������ � �������(�������) ������ �� ������� ������� ��� ��������
        {
            _gridSystemVisualSingleArray[gridPosition.x, gridPosition.z, gridPosition.floor].
                Show(GetGridVisualTypeMaterial(gridVisualType)); // � �������� Show �������� �������� � ����������� �� ����������� ��� �������
        }
    }

    public void UpdateGridVisual() // ���������� ������� �����
    {
        HideAllGridPosition(); // ������ ��� ������� �����

        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit(); //������� ���������� �����

        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction(); // ������� ��������� ��������

        GridVisualType gridVisualType;  // �������� ����� ���� GridVisualType

        switch (selectedAction) // ������������� ��������� ������� ����� � ����������� �� ���������� ��������
        {
            default: // ���� ���� ����� ����������� �� ��������� ���� ��� ��������������� selectedAction
            case MoveAction moveAction: // �� ����� ������ -�����
                gridVisualType = GridVisualType.White;
                break;

            case SpinAction spinAction: // �� ����� �������� -�������
                gridVisualType = GridVisualType.Blue;
                break;

            case HealAction healAction: // �� ����� ������� -�������
                gridVisualType = GridVisualType.Green;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), healAction.GetMaxActionDistance(), GridVisualType.GreenSoft, false); // ������� �������� 
                break;

            case ShootAction shootAction: // �� ����� �������� -�������
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), shootAction.GetMaxActionDistance(), GridVisualType.RedSoft, true); // ������� �������� �������� ����-true
                break;

            case GrenadeAction grenadeAction:// �� ����� ������� ������� -������
                gridVisualType = GridVisualType.Yellow;
                _validGridPositionForGrenadeActionList = grenadeAction.GetValidActionGridPositionList(); //�������� -������ ���������� �������� ������� ��� �������� �������                                 
                break;

            case SwordAction swordAction: // �� ����� ����� ����� -�������
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), swordAction.GetMaxActionDistance(), GridVisualType.RedSoft, false); // ������� �������� �����
                break;

            case InteractAction interactAction: // �� ����� �������������� -�������
                gridVisualType = GridVisualType.Blue;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), interactAction.GetMaxActionDistance(), GridVisualType.BlueSoft, false); // ������� �������� 
                break;

            case ComboAction comboAction: // �� ����� ������ ����� �������� -�������    

                ComboAction.State statecomboAction = comboAction.GetState(); // ������� ��������� ����� 
                switch (statecomboAction)
                {
                    default:
                    case ComboAction.State.ComboSearchPartner: // ���� ���� �������� ��� �����
                        gridVisualType = GridVisualType.Green;
                        ShowGridPositionRange(selectedUnit.GetGridPosition(), comboAction.GetMaxActionDistance(), GridVisualType.GreenSoft, false); // ������� �������� 
                        break;

                    case ComboAction.State.ComboSearchEnemy: // ���� ���� ����� ��
                        gridVisualType = GridVisualType.Red;
                        ShowGridPositionRange(selectedUnit.GetGridPosition(), comboAction.GetMaxActionDistance(), GridVisualType.RedSoft, true); // ������� ��������  ����-true
                        break;

                    case ComboAction.State.ComboStart: // ������ ���� ���� ����������
                        gridVisualType = GridVisualType.RedSoft;
                        _validGridPositionForComboActionList = selectedAction.GetValidActionGridPositionList();
                        break;

                }
                break;

            case SpotterFireAction spotterFireAction: // ������������� ���� -���������
                gridVisualType = GridVisualType.Green;
                ShowGridPositionRange(selectedUnit.GetGridPosition(), spotterFireAction.GetMaxActionDistance(), GridVisualType.GreenSoft, false); // ������� �������� 
                break;
        }

        ShowGridPositionList(selectedAction.GetValidActionGridPositionList(), gridVisualType); // �������(�������) ������ �� ������� ������� ��� �������� (� �������� �������� ������ ���������� ������� ����� ���������� ��������, � ��� ��������� ������������ ������� ��� ����� switch)
    }



#if HEX_GRID_SYSTEM // ���� ������������ �������� �������

    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType) //(������� �������� � ����������� �� ���������) �������� ��� ��������� ��� �������� ������������ � ����������� �� ����������� � �������� ��������� �������� ������������
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in _gridVisualTypeMaterialListHex) // � ����� ��������� ������ ��� ��������� ����������� ��������� ����� 
        {
            if (gridVisualTypeMaterial.gridVisualType == gridVisualType) // ����  ��������� �����(gridVisualType) ��������� � ���������� ��� ��������� �� ..
            {
                return gridVisualTypeMaterial.materialGrid; // ������ �������� ��������������� ������� ��������� �����
            }
        }

        Debug.LogError("�� ���� ����� GridVisualTypeMaterial ��� GridVisualType " + gridVisualType); // ���� �� ������ ����������� ������ ������
        return null;
    }


#else//� ��������� ������ �������������

    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType) //(������� �������� � ����������� �� ���������) �������� ��� ��������� ��� �������� ������������ � ����������� �� ����������� � �������� ��������� �������� ������������
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in _gridVisualTypeMaterialListQuad) // � ����� ��������� ������ ��� ��������� ����������� ��������� ����� 
        {
            if (gridVisualTypeMaterial.gridVisualType == gridVisualType) // ����  ��������� �����(gridVisualType) ��������� � ���������� ��� ��������� �� ..
            {
                return gridVisualTypeMaterial.materialGrid; // ������ �������� ��������������� ������� ��������� �����
            }
        }

        Debug.LogError("�� ���� ����� GridVisualTypeMaterial ��� GridVisualType " + gridVisualType); // ���� �� ������ ����������� ������ ������
        return null;
    }
#endif
}
