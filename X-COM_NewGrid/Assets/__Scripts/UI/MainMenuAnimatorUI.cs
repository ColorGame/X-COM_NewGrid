using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuAnimatorUI : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _rifleTransformHand_R; //в инспекторе закинуть Трансформ Винтовки на ПРАВОЙ РУКЕ
    [SerializeField] private Transform _rifleTransformHand_L; //в инспекторе закинуть Трансформ Винтовки на ЛЕВОЙ РУКЕ
    [SerializeField] private Transform _swordTransform; //в инспекторе закинуть Трансформ Мечя
    [SerializeField] private Transform _binocularsTransform; //в инспекторе закинуть Трансформ Бинокля
    [SerializeField] private Transform _shootPointTransform; // Точкавыстрела

    public enum State
    {
        Idle,
        SwordAction,
        SwordAfter,
        ShootAction,
        ShootAfter,
        HealAction,
        HealAfter,
        SpotterFireAction,
        SpotterFireAfter
    }

    private State _state; // Состояние юнита
    private float _stateTimer; //Таймер состояния
    private float _Timer = 1f; //Таймер состояния


    private bool _canShootBullet; // Может стрелять пулей    
    private float _timerShoot; //таймер выстрела
    private int _counterShoot; // Счетчик выстрелов
    private float _delayShoot = 0.2f; //задержка между выстрелами
    private int _numberShoot = 10; // Количество выстрелов
    private Transform _spotterFireFX;

    private void Start()
    {
        EquipRifleHand_R(); // Включим винтовку
        _state = State.Idle;
        _stateTimer = _Timer;
    }


    private void Update()
    {
        _stateTimer -= Time.deltaTime; // Запустим таймер для переключения состояний
        _timerShoot -= Time.deltaTime;// Запустим таймер для интервалов м у выстрелами

        switch (_state) // Переключатель активурует кейс в зависимости от _state
        {

            case State.ShootAction:

            if (_canShootBullet && _timerShoot <= 0) // Если могу стрелять пулей и таймер истек ...
            {
                Shoot();
                _timerShoot = _delayShoot; // Установим таймер = задержки между выстрелами
                _counterShoot += 1; // Прибавим к счетчику выстрелов 1 
            }

            if (_counterShoot >= _numberShoot) //После выпуска 3 пуль или когда враг СДОХ
            {
                _canShootBullet = false;
                _counterShoot = 0; //Обнулим счетчик пуль
            }

            break;
        }

        if (_stateTimer <= 0) // По истечению времени _musicTimer вызовим NextMusic() которая в свою очередь переключит состояние. Например - у меня было TypeGrenade.Aiming: тогда в case TypeGrenade.Aiming: переключу на TypeGrenade.Shooting;
        {
            NextState(); //Следующие состояние
        }
    }

    private void NextState() //Автомат переключения состояний
    {
        switch (_state)
        {
            case State.Idle:
                _state = State.SwordAction;
                _stateTimer =1f;
                EquipSword(); // Экипировка меч
                _animator.SetTrigger("SwordSlash");// Установить тригер
                break;

            case State.SwordAction:
                _state = State.SwordAfter;
                _stateTimer = 1;
                EquipRifleHand_R(); // Включим винтовку

                break;

            case State.SwordAfter:
                _state = State.ShootAction;                
                
                float shootingStateTime = _numberShoot * _delayShoot + 0.5f; // Для избежания магических чисель введем переменную  Продолжительность Состояния Выстрел = Количество выстрелов * время выстрела
                _stateTimer = shootingStateTime;
                _canShootBullet = true;

                break;

            case State.ShootAction:
                _state = State.ShootAfter;
                _stateTimer = 1;
                break;

            case State.ShootAfter:
                _state = State.HealAction;
                _stateTimer = 3;
                HideAllEquip(); // выключим экипировку
                _animator.SetTrigger("Heal");// 
                Instantiate(GameAssets.Instance.healFXPrefab, transform.position, Quaternion.LookRotation(transform.up));
                break;

            case State.HealAction:
                _state = State.HealAfter;
                _stateTimer = 1;
                EquipRifleHand_R(); // Включим винтовку
                break;

            case State.HealAfter:                           
                _state = State.SpotterFireAction;
                _stateTimer = 3;
                EquipBinoculars();// Экипировка Биноклем
                _animator.SetBool("IsLooking", true);
                _spotterFireFX = Instantiate(GameAssets.Instance.spotterFireFXPrefab, transform.position +transform.up*1.7f, Quaternion.identity);
                break;

            case State.SpotterFireAction:
                _state = State.SpotterFireAfter;
                _animator.SetBool("IsLooking", false);
                _stateTimer = 1;
                EquipRifleHand_R(); // Включим винтовку

                break;

            case State.SpotterFireAfter:
                _state = State.Idle;
                Destroy(_spotterFireFX.gameObject);
                _stateTimer = 0.5f;
                break;
        }
    }

    private void Shoot()
    {
        _animator.SetTrigger("Shoot");
        Transform bulletProjectilePrefabTransform = Instantiate(GameAssets.Instance.bulletProjectilePrefab, _shootPointTransform.position, Quaternion.identity); // Создадим префаб пули в точке выстрела
        BulletProjectile bulletProjectile = bulletProjectilePrefabTransform.GetComponent<BulletProjectile>(); // Вернем компонент BulletProjectile созданной пули
        Vector3 targetPosition = transform.forward * 20 + Vector3.up*2f; // позицию Прицеливания целевого юнита. 
        bulletProjectile.Setup(targetPosition, true); // В аргумент предали позицию Прицеливания целевого юнита
    }

    private void EquipSword() // Экипировка меч
    {
        HideAllEquip(); // выключим экипировку
        _swordTransform.gameObject.SetActive(true);
    }

    private void EquipRifleHand_R() // Экипировка винтовка Правой Руки
    {
        HideAllEquip(); // выключим экипировку
        _rifleTransformHand_R.gameObject.SetActive(true);
    }

    private void EquipRifleHand_L() // Экипировка винтовка Левой Руки
    {
        HideAllEquip(); // выключим экипировку
        _rifleTransformHand_L.gameObject.SetActive(true);
    }

    private void EquipBinoculars()// Экипировка Биноклем
    {
        HideAllEquip(); // выключим экипировку
        _binocularsTransform.gameObject.SetActive(true);
    }

    private void HideAllEquip() // Скрыть всю экипировку
    {
        _swordTransform.gameObject.SetActive(false);
        _rifleTransformHand_R.gameObject.SetActive(false);
        _rifleTransformHand_L.gameObject.SetActive(false);
        _binocularsTransform.gameObject.SetActive(false);
    }
}
