using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using static GrenadeProjectile;
using static UnityEngine.ParticleSystem;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

// � ������� ������� � TRAIL �������� ��������� ������� Autodestruct
public class GrenadeProjectile : MonoBehaviour // ��������� ������
{

    public static event EventHandler<TypeGrenade> OnAnyGrenadeExploded; // static - ���������� ��� event ����� ������������ ��� ����� ������ �� �������� �� ���� ������� � ��� �������� ������. ������� ��� ������������� ����� ������� ��������� �� ����� ������ �� �����-���� ���������� �������, ��� ����� �������� ������ � ������� ����� �����, ������� ����� ��������� ���� � �� �� ������� ��� ������ �������. 
                                                                        // �� �������� ������� Event ����� ����� ������� ����������

    public enum TypeGrenade // ��� �������
    {
        Fragmentation,  // ����������
        Smoke,          // �������
        Stun,           // ����������      
    }

    private TypeGrenade _typeGrenade; // ��� �������

    [SerializeField, Min(0.1f)] private float _moveSpeed = 15f; // �������� ����������� 
    [SerializeField, Min(0)] private int _damageRadiusInCells = 1; // ������ ����������� � ������� ����� (������������� �� ������, ���� ����� ��� �� ����� ��������������� �� ���� ������ �� ����������� �� ������ ������ = 1,5 (0,5 ��� �������� ����������� ������ halfCentralCell - ����� ���������� ��������) (���� ����� �������������� ����� �� 2 ������ �� ������ ������ �� ������ = 2,5 ������. ��� 3 ����� ������ 3,5)
    [SerializeField] private AnimationCurve _damageMultiplierAnimationCurve; //������������ ������ ��������� �����������
    [SerializeField] private TrailRenderer _trailRenderer; // � ���������� �������� ����� ������� �� ����� � ����� ���� // � TRAIL �������� ��������� ������� Autodestruct
    [SerializeField] private AnimationCurve _arcYAnimationCurve; // ������������ ������ ��� ��������� ���� ������ �������

    private bool _crateDestroy;
    private int _grenadeDamage; // �������� �����
    private Vector3 _targetPosition;//������� ����
    private float _totalDistance;   //��� ���������. ��������� �� ���� (����� �������� � �����). ��� ����������� �������� ���� ���, � � Update() ��� ���������� �������� ��������� �� ���� ����� �������� �� _totalDistance ��������� �� ���� ��� moveStep (Vector3.Distance-��������� �����)
    private float _floorHeight; // ������ �����
    private float _damageRadiusInWorldPosition; // ������ ����������� � ������� ����������� (��� ������ �����������)
    private Collider[] _colliderArray; // ������ ���� ����������� � ���� ������

    /* //����.������.�//
     private Vector3 _moveDirection; //������ ����������� �������� �������. ��� ����������� �������� ���� ��� �.�. ��� �� �������� � ����� ������������ � Update()
     private Vector3 _positionXZ;    //���������� ������� ������ ������� �� ��� X (Y-����� ������ ������������ ������)
     private int _floor;// ����
     private float _currentDistance; //������� ���������� �� ����
     //����.������.�//*/

    //�����// ��� ������ �����
    private float _timerFlightGrenadeNormalized; // ��������������� ������ ������ �������
    private float _timerFlightGrenade; // ������ ������ �������
    private float _maxTimerFlightGrenade; // ������������ ������ ������ �������
    private Vector3 _startPosition; // ��������� �������
    //�����//

    private Action _onGrenadeBehaviorComplete;  //(�� ������� �������� ���������)// �������� ������� � ������������ ���� - using System;
                                                //�������� ��� ������� ��� ������������ ���������� (� ��� ����� ��������� ������ ������� �� ���������).
                                                //Action- ���������� �������. ���� ��� �������. ������� Func<>. 
                                                //�������� ��������. ����� ���������� ������� � ������� �� �������� ��������, ����� �����, � ������������ ����� ����, ����������� ����������� �������.
                                                //�������� ��������. ����� �������� �������� ������� �� ������� ������

    private void Start()
    {
        _startPosition = transform.position; // ����������� ��������� ��������� ������� ��� ������ ����� ������ �����        
    }

    private void Update()
    {
        //�����//
        _timerFlightGrenade -= Time.deltaTime; // �������� ������ ������ �������

        _timerFlightGrenadeNormalized = 1 - _timerFlightGrenade / _maxTimerFlightGrenade; // ��������  ��������������� ����� ������ ������� (� ������ ������ _timerFlightGrenade=_maxTimerFlightGrenade ������ 1-1=0 )

        // ������� ����� �� ������ ����� � ������ ������ �������
        Vector3 positionBezier = Bezier.GetPoint(
            _startPosition,
            _startPosition + Vector3.up * _floorHeight,
            _targetPosition + Vector3.up * _floorHeight,
            _targetPosition,
            _timerFlightGrenadeNormalized
            );

        transform.position = positionBezier; // ���������� ������ � ��� �����

        if (_timerFlightGrenade <= 0) // �� ��������� ������� ������ �������...
        {
            OnAnyGrenadeExploded?.Invoke(this, _typeGrenade);// ������� �������

            GrenadeExplosion(); // ����� �������

            _trailRenderer.transform.parent = null; // ���������� ����� �� �������� ��� �� �� ��� ���. � � ���������� �������� ������� Autodestruct - ����������� ����� ���������� ����������

            Destroy(gameObject);

            _onGrenadeBehaviorComplete(); // ������� ����������� ������� ������� ��� �������� ������� Setup(). � ����� ������ ��� ActionComplete() �� ������� ��������� � ������ UI

        }
        //�����//


        /*//����.������.�//
        float moveStep = _moveSpeed * Time.deltaTime; // ��� ����������� �� ����

        transform.position += _moveDirection * moveStep; // ���������� ������ �� ��� � �� ���� ���

        _currentDistance -= moveStep; // ������� ��������� �� ����. �� ����������� ��������� ������ ���� ����� �������� ��������� ���

        float currentDistanceNormalized = 1 - _currentDistance / _totalDistance;//����� ������� AnimationCurve (�������������� ���) ������� �� ��������������� ��������� �� ���� � ������� ��������(�� 1 ������� ���������� ��������). _currentDistance<=_totalDistance ������� �������� ����� �� 0 �� 1.
                                                                                //� ������ ������ ������� _currentDistance = _totalDistance, ����� currentDistanceNormalized = 1(��� �������� ��� ������� � ������������ ������), ��� 1 ��� �������� �������� positionY ����� ������� � ��� ����� � ������ �������� � ������ ������� 0 ������� ������� ��������)
        float maxHeight = _totalDistance / 4f + _floor * _floorHeight;// ������ ������ ������� ������� ��������� �� ��������� ������ � �� ����� �� ������� ������ ������� (��� �� ��� �������� ������� ����� �������� �����������) //����� ���������//
        float positionY = _arcYAnimationCurve.Evaluate(currentDistanceNormalized) * maxHeight; // ������� ������� � �� ������������ ������  � ������� �� ���� ������ ������

        transform.position = new Vector3(transform.position.x, positionY, transform.position.y); //���������� ������� � ������ ������������ ������

        float reachedTargetDistance = 0.2f; // ����������� ������� ����������
        if (_currentDistance < reachedTargetDistance) // ���� ������� ���������� ������ �� �������� ��
        {
            Collider[] colliderArray = Physics.OverlapSphere(_targetRotation, _damageRadiusInWorldPosition); //� ���� ������ - �������� � �������� ������ �� ����� ������������, ���������������� �� ������ ��� ����������� ������ ���.

            foreach (Collider collider in colliderArray)  // ��������� ������ ����������
            {
                if (collider.TryGetComponent<Unit>(out Unit startUnit))//� ������� � �������� ���������� collider ��������� �������� ��������� Unit // ���� �� ����������� �������� ����� "out", �� ������� ������ ���������� �������� ��� ���� ����������
                                                                        // TryGetComponent - ���������� true, ���� ���������< > ������.���������� ��������� ���������� ����, ���� �� ����������.
                {
                    *//*//1// ������ ���� �� ������� �� ����������
                    startUnit.Damage(_grenadeDamage);
                    //1//*//*

                    //2// ������ ���� ������� �� ����������
                    float distanceToUnit = Vector3.Distance(startUnit.GetWorldPositionCenter�ornerCell(), _targetRotation); // ��������� �� ������ ������ �� ����� ������� ����� � ������ ������
                    float distanceToUnitNormalized = distanceToUnit / _damageRadiusInWorldPosition; // ����� ������� AnimationCurve (�������������� ���) ������� �� ��������������� ��������� �� ����� (distanceToUnit<=damageRadius ������� �������� ����� �� 0 �� 1. ���� ���� ���������� ������ ������ �� distanceToUnit =0 ����� distanceToUnitNormalized ���� = 0, ����� ������������ ������ ������ �������� ������������ ��� � ������� ������ ������� ��� �������� ����� =1)
                    int damageAmountFromDistance = Mathf.RoundToInt(_grenadeDamage * _damageMultiplierAnimationCurve.Evaluate(distanceToUnitNormalized)); //�������� ����������� �� ���������. �������� �� ������ � ��������� � int �.�. Damage() ��������� ����� �����

                    startUnit.Damage(damageAmountFromDistance); // �������� ���� � ����� ��������� � ������ ������
                    //2//
                }

                if (collider.TryGetComponent<DestructibleCrate>(out DestructibleCrate destructibleCrate))   //� ������� � �������� ���������� collider ��������� �������� ��������� DestructibleCrate // ���� �� ����������� �������� ����� "out", �� ������� ������ ���������� �������� ��� ���� ����������
                                                                                                            // TryGetComponent - ���������� true, ���� ���������< > ������.���������� ��������� ���������� ����, ���� �� ����������.
                {
                    destructibleCrate.Damage(); // ���� ���� ���� �������� ��� // ����� ����� ����������� ��������� ���������� ��� �� ������� ����� ��������� ��� ������� ������� ��������� ���� ���������
                }

            }

            OnAnyGrenadeExploded?.Invoke(this, EventArgs.Empty0);// ������� �������

            _trailRenderer.transform.parent = null; // ���������� ����� �� �������� ��� �� �� ��� ���. � � ���������� �������� ������� Autodestruct - ����������� ����� ���������� ����������

            Instantiate(_grenadeExplosionFXPrefab, _targetRotation, Quaternion.LookRotation(Vector3.up)); //�������� ��������� ������ . ��������� ��� �� ��� Z �������� ����� �.�. � ��� ������� ������ ��� ���������

            Destroy(gameObject);

            _onGrenadeBehaviorComplete(); // ������� ����������� ������� ������� ��� �������� ������� Setup(). � ����� ������ ��� ActionComplete() �� ������� ��������� � ������ UI
        }
        //����.������.�//*/
    }

    private void GrenadeExplosion() // ����� �������
    {
        switch (_typeGrenade)
        {
            case TypeGrenade.Fragmentation:

                _colliderArray = Physics.OverlapSphere(_targetPosition, _damageRadiusInWorldPosition); //� ���� ������ - �������� � �������� ������ �� ����� ������������, ���������������� �� ������ ��� ����������� ������ ���.
                foreach (Collider collider in _colliderArray)  // ��������� ������ ����������
                {
                    if (collider.TryGetComponent<Unit>(out Unit targetUnit))//� ������� � �������� ���������� collider ��������� �������� ��������� Unit // ���� �� ����������� �������� ����� "out", �� ������� ������ ���������� �������� ��� ���� ����������
                                                                            // TryGetComponent - ���������� true, ���� ���������< > ������.���������� ��������� ���������� ����, ���� �� ����������.
                    {
                        //������ ���� ������� �� ����������
                        float distanceToUnit = Vector3.Distance(targetUnit.GetWorldPosition(), _targetPosition); // ��������� �� ������ ������ �� ����� ������� ����� � ������ ������
                        float distanceToUnitNormalized = distanceToUnit / _damageRadiusInWorldPosition; // ����� ������� AnimationCurve (�������������� ���) ������� �� ��������������� ��������� �� ����� (distanceToUnit<=damageRadius ������� �������� ����� �� 0 �� 1. ���� ���� ���������� ������ ������ �� distanceToUnit =0 ����� distanceToUnitNormalized ���� = 0, ����� ������������ ������ ������ �������� ������������ ��� � ������� ������ ������� ��� �������� ����� =1)
                        int damageAmountFromDistance = Mathf.RoundToInt(_grenadeDamage * _damageMultiplierAnimationCurve.Evaluate(distanceToUnitNormalized)); //�������� ����������� �� ���������. �������� �� ������ � ��������� � int �.�. Damage() ��������� ����� �����

                        targetUnit.Damage(damageAmountFromDistance); // �������� ���� � ����� ��������� � ������ ������                    
                    }

                    if (collider.TryGetComponent<DestructibleCrate>(out DestructibleCrate destructibleCrate))   //� ������� � �������� ���������� collider ��������� �������� ��������� DestructibleCrate // ���� �� ����������� �������� ����� "out", �� ������� ������ ���������� �������� ��� ���� ����������
                                                                                                                // TryGetComponent - ���������� true, ���� ���������< > ������.���������� ��������� ���������� ����, ���� �� ����������.
                    {
                        destructibleCrate.Damage(transform.position); // ���� ���� ���� �������� ��� // ����� ����� ����������� ��������� ���������� ��� �� ������� ����� ��������� ��� ������� ������� ��������� ���� ���������
                        _crateDestroy = true; // ���� ����������� ����
                    }
                }

                if (_crateDestroy) // ���� ���� � ������� ����������� ���� �� ...  (����� �� ��������� ��������� ��� � ����� ���� ��������� ������ )
                {
                    SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.DestructionCrate); 
                }

                SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.GrenadeExplosion);

                Instantiate(GameAssets.Instance.grenadeExplosionFXPrefab, _targetPosition, Quaternion.identity); //�������� ��������� ������. 

                break;

            case TypeGrenade.Smoke:

                SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.GrenadeSmoke);
                Instantiate(GameAssets.Instance.grenadeSmokeFXPrefab, _targetPosition, Quaternion.identity); //�������� ��� � ����� ������ �������.

                break;

            case TypeGrenade.Stun:

                _colliderArray = Physics.OverlapSphere(_targetPosition, _damageRadiusInWorldPosition); //� ���� ������ - �������� � �������� ������ �� ����� ������������, ���������������� �� ������ ��� ����������� ������ ���.
                foreach (Collider collider in _colliderArray)  // ��������� ������ ����������
                {
                    if (collider.TryGetComponent<Unit>(out Unit targetUnit))//� ������� � �������� ���������� collider ��������� �������� ��������� Unit // ���� �� ����������� �������� ����� "out", �� ������� ������ ���������� �������� ��� ���� ����������
                                                                            // TryGetComponent - ���������� true, ���� ���������< > ������.���������� ��������� ���������� ����, ���� �� ����������.
                    {
                        //������ ���� ������� �� ����������
                        float distanceToUnit = Vector3.Distance(targetUnit.GetWorldPosition(), _targetPosition); // ��������� �� ������ ������ �� ����� ������� ����� � ������ ������
                        float stunPercent; // ������� ���������

                        if (distanceToUnit <= _damageRadiusInWorldPosition / 2) // �� ������ �� �������� ������ ���������
                        {
                            stunPercent = 1; // 100%
                        }
                        else // ���� ������ �������� ����� �� ...
                        {
                            stunPercent = 0.5f; // 50%
                        }
                        targetUnit.Stun(stunPercent); //������� ����� ������ ����� � ������ ��������
                    }
                }
                SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.GrenadeStun);
                Instantiate(GameAssets.Instance.grenadeExplosionFXPrefab, _targetPosition, Quaternion.identity); //�������� �������� ������.
                Instantiate(GameAssets.Instance.electricityWhiteFXPrefab, _targetPosition, Quaternion.identity); //�������� ���������������� ������.


                break;
        }

    }

    public void Setup(GridPositionXZ targetGridPosition, TypeGrenade typeGrenade, Action onGrenadeBehaviorComplete, int grenadeDamage) // ��������� �������. � �������� �������� ������� �������, ��� ������� � �����  � �������� ����� ���������� ������� ���� Action (onGrenadeBehaviorComplete - �� ������� �������� ���������)
    {
        _grenadeDamage = grenadeDamage;
        _typeGrenade = typeGrenade;
        _onGrenadeBehaviorComplete = onGrenadeBehaviorComplete; // �������� ��������� �������
        _targetPosition = LevelGrid.Instance.GetWorldPosition(targetGridPosition); // ������� ������� ������� �� ���������� ��� ������� �����        
        _floorHeight = LevelGrid.FLOOR_HEIGHT; // ��������� ������ �����
        _damageRadiusInWorldPosition = GetDamageRadiusInWorldPosition();

        //�����// ������ ���������� ������� �� ������ �����
        _totalDistance = Vector3.Distance(transform.position, _targetPosition);  //�������� ��������� ����� �������� � ����� 
        _maxTimerFlightGrenade = _totalDistance / _moveSpeed; // �������� ����� ������ ������� = ��������� ������� �� ��������
        _timerFlightGrenade = _maxTimerFlightGrenade;
        //�����//

        /*//����.������.�// ������ ���������� ������� �� ������������ ������ - ������ �������� ����� ���� ����
        _floor = targetGridPosition.floor; // ��������� �� ����� ���� ����� ������               

        _positionXZ = transform.position; // �������� ������� ������� �� ��� � ��� ���� ������� � ������������
        _positionXZ.y = 0;

        _totalDistance = Vector3.Distance(transform.position, _targetRotation);  //�������� ��������� ����� �������� � ����� (����� �� ��������� ������ ���� � update)
        _currentDistance = _totalDistance; // ������� ���������� � ������ ����� ����� ����������

        _moveDirection = (_targetRotation - transform.position).normalized; //�������� ������ ����������� �������� ������� (����� �� ��������� ������ ���� � update �.�. ��� �� ��������)
        //����.������.�//*/
    }

    public int GetDamageRadiusInCells() //�������� _damageRadiusInCells
    {
        return _damageRadiusInCells;
    }

    public float GetDamageRadiusInWorldPosition() // �������� _damageRadiusInWorldPosition
    {
        if (_damageRadiusInWorldPosition == 0) // ���� ��� �� ��������
        {
            // ���������������  ���������� ��� ����������� (����� �� ��������� ������ ���� � update ����������� ������)
            float halfCentralCell = 0.5f; // �������� ����������� ������
            return _damageRadiusInWorldPosition = (_damageRadiusInCells + halfCentralCell) * LevelGrid.Instance.GetCellSize(); // ������ ����������� �� ������� = ������ ����������� � ������� �����(� ������ ����������� ������) * ������ ������
        }
        return _damageRadiusInWorldPosition;
    }

    public void SetTypeGrenade(TypeGrenade typeGrenade) // ���������� ��� �������
    {
        _typeGrenade = typeGrenade;
    }


    /*#if UNITY_EDITOR //��������� �� ��������� ����������. ��������� ��������� ��� ������ �� ����� ��� ���������� � ���������� ����� ���� ������������� ��� ����� �� �������������� ��������.
        private void OnDrawGizmos() // ��� ��������� ������������� �������� � �����, � ����� ������ ���� �������� �������
        {
            Handles.color = Color.red;
            Handles.DrawWireDisc(_targetRotation, Vector3.up , _damageRadiusInCells * LevelGrid.Instance.GetCellSize(), 4f);
        }
    #endif // ��� �������� ����� ���� ����� ���� �� ����� � ���� ���������� � ����� �������� ������ � EDITOR(��������)*/

}
