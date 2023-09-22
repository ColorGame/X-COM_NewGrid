using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using static GrenadeProjectile;
using static UnityEngine.ParticleSystem;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

// В префабе гранаты у TRAIL незабудь поставить галочку Autodestruct
public class GrenadeProjectile : MonoBehaviour // Гранатный снаряд
{

    public static event EventHandler<TypeGrenade> OnAnyGrenadeExploded; // static - обозначает что event будет существовать для всего класса не зависимо от того скольго у нас созданно гранат. Поэтому для прослушивания этого события слушателю не нужна ссылка на какую-либо конкретную гранату, они могут получить доступ к событию через класс, который затем запускает одно и то же событие для каждой гранаты. 
                                                                        // Мы запустим событие Event когда Любая граната взорволась

    public enum TypeGrenade // Тип гранаты
    {
        Fragmentation,  // Осколочная
        Smoke,          // Дымовая
        Stun,           // Оглушаюшая      
    }

    private TypeGrenade _typeGrenade; // Тип гранаты

    [SerializeField, Min(0.1f)] private float _moveSpeed = 15f; // Скорость перемещения 
    [SerializeField, Min(0)] private int _damageRadiusInCells = 1; // Радиус повреждения в Ячейках сетки (отсчитывается от центра, если хотим что бы взрыв распростронялся на одну ячейку от центральной то радиус должен = 1,5 (0,5 это половина центральной ячейки halfCentralCell - будем прибавлять отдельно) (если хотим распространить взрыв на 2 ячейки не считая центра то радиус = 2,5 ячейки. Для 3 ячеек радиус 3,5)
    [SerializeField] private AnimationCurve _damageMultiplierAnimationCurve; //Анимацтонная кривая множителя повреждения
    [SerializeField] private TrailRenderer _trailRenderer; // в инспекторе закинуть трэил гранаты он лежит в самой пули // у TRAIL незабудь поставить галочку Autodestruct
    [SerializeField] private AnimationCurve _arcYAnimationCurve; // Анимацтонная кривая для настройки дуги полета гранаты

    private bool _crateDestroy;
    private int _grenadeDamage; // Величина урона
    private Vector3 _targetPosition;//Позиция цели
    private float _totalDistance;   //Вся дистанция. Дистанция до цели (между гранатой и целью). Для оптимизации вычислим один раз, а в Update() для вычисления текущего растояния до цели будем отнимать от _totalDistance проиденый за кадр шаг moveStep (Vector3.Distance-затратный метод)
    private float _floorHeight; // Высота этажа
    private float _damageRadiusInWorldPosition; // Радиус повреждения в мировых координатах (для физики повреждения)
    private Collider[] _colliderArray; // Массив всех коллайдеров в зоне взрыва

    /* //АНИМ.КРИВАЯ.У//
     private Vector3 _moveDirection; //Вектор направление движения гранаты. Для оптимизации вычислим один раз т.к. она не меняется и будем использовать в Update()
     private Vector3 _positionXZ;    //Переменная котороя хранит позицию по оси X (Y-будем менять анимационной кривой)
     private int _floor;// Этаж
     private float _currentDistance; //Текущее расстояние до цели
     //АНИМ.КРИВАЯ.У//*/

    //БЕЗЬЕ// Для кривой БИЗЬЕ
    private float _timerFlightGrenadeNormalized; // Нормализованное Таймер полета гранаты
    private float _timerFlightGrenade; // Таймер полета гранаты
    private float _maxTimerFlightGrenade; // Максимальное Таймер полета гранаты
    private Vector3 _startPosition; // Стартовая позиция
    //БЕЗЬЕ//

    private Action _onGrenadeBehaviorComplete;  //(На Гранате Действие ЗАвершено)// Объявляю делегат в пространстве имен - using System;
                                                //Сохраним наш делегат как обыкновенную переменную (в ней будет храниться функия которую мы передадим).
                                                //Action- встроенный делегат. Есть еще встроен. делегат Func<>. 
                                                //СВОЙСТВО Делегата. После выполнения функции в которую мы передали делегата, можно ПОЗЖЕ, в определенном месте кода, выполниться сохраненный делегат.
                                                //СВОЙСТВО Делегата. Может вызывать закрытую функцию из другого класса

    private void Start()
    {
        _startPosition = transform.position; // Зафиксируем начальное положение гранаты для первой точки кривой Бизье        
    }

    private void Update()
    {
        //БЕЗЬЕ//
        _timerFlightGrenade -= Time.deltaTime; // запустим таймер полета гранаты

        _timerFlightGrenadeNormalized = 1 - _timerFlightGrenade / _maxTimerFlightGrenade; // Вычислим  Нормализованное Время полета гранаты (в начале броска _timerFlightGrenade=_maxTimerFlightGrenade значит 1-1=0 )

        // Получим точку на кривой Безье в данный момент времени
        Vector3 positionBezier = Bezier.GetPoint(
            _startPosition,
            _startPosition + Vector3.up * _floorHeight,
            _targetPosition + Vector3.up * _floorHeight,
            _targetPosition,
            _timerFlightGrenadeNormalized
            );

        transform.position = positionBezier; // Переместим снаряд в эту точку

        if (_timerFlightGrenade <= 0) // по истечении таймера полета гранаты...
        {
            OnAnyGrenadeExploded?.Invoke(this, _typeGrenade);// Вызовим событие

            GrenadeExplosion(); // Взрыв гранаты

            _trailRenderer.transform.parent = null; // Отсоеденим трэйл от родителя что бы он еще жил. А в инсепкторе поставим галочку Autodestruct - уничтожение после завершения ортрисовки

            Destroy(gameObject);

            _onGrenadeBehaviorComplete(); // Вызовим сохраненный делегат который нам передала функция Setup(). В нашем случае это ActionComplete() он снимает занятость с кнопок UI

        }
        //БЕЗЬЕ//


        /*//АНИМ.КРИВАЯ.У//
        float moveStep = _moveSpeed * Time.deltaTime; // Шаг перемещения за кадр

        transform.position += _moveDirection * moveStep; // Переместим снаряд по оси Х на один шаг

        _currentDistance -= moveStep; // Текущее растояние до цели. От изначальной дистанции каждый кадр будем отнимать пройденый шаг

        float currentDistanceNormalized = 1 - _currentDistance / _totalDistance;//Шкалу времени AnimationCurve (горизонтальная ось) заменим на нормализованное растояние до цели и сделаем инверсию(от 1 отнимем полученное значение). _currentDistance<=_totalDistance поэтому значение будет от 0 до 1.
                                                                                //В начале полета гранаты _currentDistance = _totalDistance, тогда currentDistanceNormalized = 1(это значение оси времени в анимационной кривой), при 1 нам вернется значение positionY конца графика а нам нужно в начале значение в момент времени 0 поэтому сделаем ИНВЕРСИЮ)
        float maxHeight = _totalDistance / 4f + _floor * _floorHeight;// Высота полета гранаты сделаем зависимой от дальности полета и от этажа на который кидаем гранату (что бы при коротких бросках полет выглядел естественее) //НУЖНО НАСТРОИТЬ//
        float positionY = _arcYAnimationCurve.Evaluate(currentDistanceNormalized) * maxHeight; // Получим позицию У из анимационной кривой  и умножим на макс высоту полета

        transform.position = new Vector3(transform.position.x, positionY, transform.position.z); //Переместим гранату с учетом анимационной кривой

        float reachedTargetDistance = 0.2f; // достигнутое целевое расстояние
        if (_currentDistance < reachedTargetDistance) // Если граната достаточно близко то ШАРАХНЕМ ЕЮ
        {
            Collider[] colliderArray = Physics.OverlapSphere(_targetPosition, _damageRadiusInWorldPosition); //В зоне взрыва - вычислим и сохраним массив со всеми коллайдерами, соприкасающимися со сферой или находящиеся внутри нее.

            foreach (Collider collider in colliderArray)  // переберем массив колайдеров
            {
                if (collider.TryGetComponent<Unit>(out Unit startUnit))//У объекта к которому прикриплен collider ПОПРОБУЕМ получить компонент Unit // Если мы используете ключевое слово "out", то функция должна установить значение для этой переменной
                                                                        // TryGetComponent - возвращает true, если компонент< > найден.Возвращает компонент указанного типа, если он существует.
                {
                    *//*//1// СПОСОБ УРОН НЕ ЗАВИСИТ ОТ РАССТОЯНИЯ
                    startUnit.Damage(_grenadeDamage);
                    //1//*//*

                    //2// СПОСОБ УРОН ЗАВИСИТ ОТ РАССТОЯНИЯ
                    float distanceToUnit = Vector3.Distance(startUnit.GetWorldPosition(), _targetPosition); // Растояние от центра взрыва до юнита который попал в радиус взрыва
                    float distanceToUnitNormalized = distanceToUnit / _damageRadiusInWorldPosition; // Шкалу времени AnimationCurve (горизонтальная ось) заменим на нормализованное растояние до юнита (distanceToUnit<=damageRadius поэтому значение будет от 0 до 1. Если Юнит находиться центре взрыва то distanceToUnit =0 тогда distanceToUnitNormalized тоже = 0, тогда анимационный график вернет значение вертикальной оси в нулевой момент времени это значение будет =1)
                    int damageAmountFromDistance = Mathf.RoundToInt(_grenadeDamage * _damageMultiplierAnimationCurve.Evaluate(distanceToUnitNormalized)); //Величина повреждения от растояния. Округлим до целого и переведем в int т.к. Damage() принимает целые числа

                    startUnit.Damage(damageAmountFromDistance); // применим урон к юниту попавшему в радиус взрыва
                    //2//
                }

                if (collider.TryGetComponent<DestructibleCrate>(out DestructibleCrate destructibleCrate))   //У объекта к которому прикриплен collider ПОПРОБУЕМ получить компонент DestructibleCrate // Если мы используете ключевое слово "out", то функция должна установить значение для этой переменной
                                                                                                            // TryGetComponent - возвращает true, если компонент< > найден.Возвращает компонент указанного типа, если он существует.
                {
                    destructibleCrate.Damage(); // Если есть ящик разрушим его // ЗДЕСЬ МОЖНО РЕАЛИЗОВАТЬ ИНТЕРФЕЙС РАЗРУШЕНИЯ что бы граната могла разрушать все объекты которые реализуют этот интерфейс
                }

            }

            OnAnyGrenadeExploded?.Invoke(this, EventArgs.Empty);// Вызовим событие

            _trailRenderer.transform.parent = null; // Отсоеденим трэйл от родителя что бы он еще жил. А в инсепкторе поставим галочку Autodestruct - уничтожение после завершения ортрисовки

            Instantiate(_grenadeExplosionFXPrefab, _targetPosition, Quaternion.LookRotation(Vector3.up)); //Создадим частьички взрыва . Развернем что бы ось Z смотрела вверх т.к. у нас область взрыва это полусфера

            Destroy(gameObject);

            _onGrenadeBehaviorComplete(); // Вызовим сохраненный делегат который нам передала функция Setup(). В нашем случае это ActionComplete() он снимает занятость с кнопок UI
        }
        //АНИМ.КРИВАЯ.У//*/
    }

    private void GrenadeExplosion() // Взрыв гранаты
    {
        switch (_typeGrenade)
        {
            case TypeGrenade.Fragmentation:

                _colliderArray = Physics.OverlapSphere(_targetPosition, _damageRadiusInWorldPosition); //В зоне взрыва - вычислим и сохраним массив со всеми коллайдерами, соприкасающимися со сферой или находящиеся внутри нее.
                foreach (Collider collider in _colliderArray)  // переберем массив колайдеров
                {
                    if (collider.TryGetComponent<Unit>(out Unit targetUnit))//У объекта к которому прикриплен collider ПОПРОБУЕМ получить компонент Unit // Если мы используете ключевое слово "out", то функция должна установить значение для этой переменной
                                                                            // TryGetComponent - возвращает true, если компонент< > найден.Возвращает компонент указанного типа, если он существует.
                    {
                        //СПОСОБ УРОН ЗАВИСИТ ОТ РАССТОЯНИЯ
                        float distanceToUnit = Vector3.Distance(targetUnit.GetWorldPosition(), _targetPosition); // Растояние от центра взрыва до юнита который попал в радиус взрыва
                        float distanceToUnitNormalized = distanceToUnit / _damageRadiusInWorldPosition; // Шкалу времени AnimationCurve (горизонтальная ось) заменим на нормализованное растояние до юнита (distanceToUnit<=damageRadius поэтому значение будет от 0 до 1. Если Юнит находиться центре взрыва то distanceToUnit =0 тогда distanceToUnitNormalized тоже = 0, тогда анимационный график вернет значение вертикальной оси в нулевой момент времени это значение будет =1)
                        int damageAmountFromDistance = Mathf.RoundToInt(_grenadeDamage * _damageMultiplierAnimationCurve.Evaluate(distanceToUnitNormalized)); //Величина повреждения от растояния. Округлим до целого и переведем в int т.к. Damage() принимает целые числа

                        targetUnit.Damage(damageAmountFromDistance); // применим урон к юниту попавшему в радиус взрыва                    
                    }

                    if (collider.TryGetComponent<DestructibleCrate>(out DestructibleCrate destructibleCrate))   //У объекта к которому прикриплен collider ПОПРОБУЕМ получить компонент DestructibleCrate // Если мы используете ключевое слово "out", то функция должна установить значение для этой переменной
                                                                                                                // TryGetComponent - возвращает true, если компонент< > найден.Возвращает компонент указанного типа, если он существует.
                    {
                        destructibleCrate.Damage(transform.position); // Если есть ящик разрушим его // ЗДЕСЬ МОЖНО РЕАЛИЗОВАТЬ ИНТЕРФЕЙС РАЗРУШЕНИЯ что бы граната могла разрушать все объекты которые реализуют этот интерфейс
                        _crateDestroy = true; // Есть разрушаемый ящик
                    }
                }

                if (_crateDestroy) // Если есть в радиусе разрушаемый ящик то ...  (чтобы не запускать несколько раз в цикле если несколько ящиков )
                {
                    SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.DestructionCrate); 
                }

                SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.GrenadeExplosion);

                Instantiate(GameAssets.Instance.grenadeExplosionFXPrefab, _targetPosition, Quaternion.identity); //Создадим частьички взрыва. 

                break;

            case TypeGrenade.Smoke:

                SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.GrenadeSmoke);
                Instantiate(GameAssets.Instance.grenadeSmokeFXPrefab, _targetPosition, Quaternion.identity); //Создадим Дым в месте взрыва гранаты.

                break;

            case TypeGrenade.Stun:

                _colliderArray = Physics.OverlapSphere(_targetPosition, _damageRadiusInWorldPosition); //В зоне взрыва - вычислим и сохраним массив со всеми коллайдерами, соприкасающимися со сферой или находящиеся внутри нее.
                foreach (Collider collider in _colliderArray)  // переберем массив колайдеров
                {
                    if (collider.TryGetComponent<Unit>(out Unit targetUnit))//У объекта к которому прикриплен collider ПОПРОБУЕМ получить компонент Unit // Если мы используете ключевое слово "out", то функция должна установить значение для этой переменной
                                                                            // TryGetComponent - возвращает true, если компонент< > найден.Возвращает компонент указанного типа, если он существует.
                    {
                        //СПОСОБ УРОН ЗАВИСИТ ОТ РАССТОЯНИЯ
                        float distanceToUnit = Vector3.Distance(targetUnit.GetWorldPosition(), _targetPosition); // Растояние от центра взрыва до юнита который попал в радиус взрыва
                        float stunPercent; // Процент ОГЛУШЕНИЯ

                        if (distanceToUnit <= _damageRadiusInWorldPosition / 2) // От центра до середины ПОЛНОЕ ОГЛУШЕНИЕ
                        {
                            stunPercent = 1; // 100%
                        }
                        else // Если дальше половины круга то ...
                        {
                            stunPercent = 0.5f; // 50%
                        }
                        targetUnit.Stun(stunPercent); //Оглушим юнита которы попал в радиус действия
                    }
                }
                SoundManager.Instance.PlaySoundOneShot(SoundManager.Sound.GrenadeStun);
                Instantiate(GameAssets.Instance.grenadeExplosionFXPrefab, _targetPosition, Quaternion.identity); //Создадим частички взрыва.
                Instantiate(GameAssets.Instance.electricityWhiteFXPrefab, _targetPosition, Quaternion.identity); //Создадим электромагнитное облако.


                break;
        }

    }

    public void Setup(GridPosition targetGridPosition, TypeGrenade typeGrenade, Action onGrenadeBehaviorComplete, int grenadeDamage) // Настройка гранаты. В аргумент передаем целевую позицию, тип гранаты и также  В аргумент будем передовать делегат типа Action (onGrenadeBehaviorComplete - На Гранате Действие ЗАвершено)
    {
        _grenadeDamage = grenadeDamage;
        _typeGrenade = typeGrenade;
        _onGrenadeBehaviorComplete = onGrenadeBehaviorComplete; // Сохраним полученый делегат
        _targetPosition = LevelGrid.Instance.GetWorldPosition(targetGridPosition); // Получим целевую позицию из переданной нам позиции сетки        
        _floorHeight = LevelGrid.FLOOR_HEIGHT; // Установим высоту этажа
        _damageRadiusInWorldPosition = GetDamageRadiusInWorldPosition();

        //БЕЗЬЕ// расчет траектории гранаты по кривой БЕзье
        _totalDistance = Vector3.Distance(transform.position, _targetPosition);  //Вычислим дистанцию между гранатой и целью 
        _maxTimerFlightGrenade = _totalDistance / _moveSpeed; // Вычислим время полета гранаты = растояние поделим на скорость
        _timerFlightGrenade = _maxTimerFlightGrenade;
        //БЕЗЬЕ//

        /*//АНИМ.КРИВАЯ.У// расчет траектории гранаты по анимационной кривой - хорошо работает когда один этаж
        _floor = targetGridPosition.floor; // Установим на какой этаж будем кидать               

        _positionXZ = transform.position; // Сохраним текущую позиции по оси Х при этом обнулим У состовляющию
        _positionXZ.y = 0;

        _totalDistance = Vector3.Distance(transform.position, _targetPosition);  //Вычислим дистанцию между гранатой и целью (чтобы не вычеслять каждый кадр в update)
        _currentDistance = _totalDistance; // Текцщие расстояние в начале равно всему расстоянию

        _moveDirection = (_targetPosition - transform.position).normalized; //Вычислим вектор Направление движения гранаты (чтобы не вычеслять каждый кадр в update т.к. оно не меняется)
        //АНИМ.КРИВАЯ.У//*/
    }

    public int GetDamageRadiusInCells() //Раскроем _damageRadiusInCells
    {
        return _damageRadiusInCells;
    }

    public float GetDamageRadiusInWorldPosition() // Раскроем _damageRadiusInWorldPosition
    {
        if (_damageRadiusInWorldPosition == 0) // Если еще не вычислен
        {
            // Предварительные  вычисления для оптимизации (чтобы не вычеслять каждый кадр в update статические данные)
            float halfCentralCell = 0.5f; // Половина центральной ячейки
            return _damageRadiusInWorldPosition = (_damageRadiusInCells + halfCentralCell) * LevelGrid.Instance.GetCellSize(); // Радиус повреждения от гранаты = Радиус повреждения в Ячейках сетки(с учетом центральной ячейки) * размер ячейки
        }
        return _damageRadiusInWorldPosition;
    }

    public void SetTypeGrenade(TypeGrenade typeGrenade) // Установить тип ГРАНАТЫ
    {
        _typeGrenade = typeGrenade;
    }


    /*#if UNITY_EDITOR //Зависящая от платформы компиляция. Позволяют разбивать наш скрипт на части для компиляции и выполнения части кода исключительно для одной из поддерживаемых платформ.
        private void OnDrawGizmos() // Для рисования вспогательных объектов в сцене, в нашем случае круг действия гранаты
        {
            Handles.color = Color.red;
            Handles.DrawWireDisc(_targetPosition, Vector3.up , _damageRadiusInCells * LevelGrid.Instance.GetCellSize(), 4f);
        }
    #endif // При создании билда этот кусок кода не будет в него включаться а будет работать только в EDITOR(редактор)*/

}
