using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Pathfinding;
using Unity.VisualScripting;

public class DestructibleCrate : MonoBehaviour // Разрушаемый ящик
{
    public static event EventHandler OnAnyDestroyed; // static - обозначает что event будет существовать для всего класса, а не для оттдельного ящика. Поэтому для прослушивания этого события слушателю не нужна ссылка на конкретный объект, они могут получить доступ к событию через класс, который затем запускает одно и то же событие для каждого объекта. 
                                                     // Мы запустим событие Event ЛЮБЙ(Any) объект разрушен.

    [SerializeField] private Transform _crate; // целый ящик //Откючение Рендер не подойдет т.к. при смене этажа камерой, рендер проверяется и включается заново
    [SerializeField] private Transform _crateDestroyed; // Разрушенный ящик     

    private GridPositionXZ _gridPosition; // Позиция сетки нашего ящика
    private SingleNodeBlocker _singleNodeBlocker;

    private void Awake()
    {      
        _singleNodeBlocker = GetComponent<SingleNodeBlocker>();
    }

    private void Start()
    {
        _crateDestroyed.gameObject.SetActive(false); // Скрыть Разрушенный ящик (на всякий случай если забыли скрыть в инспекторе)
        _gridPosition = LevelGrid.Instance.GetGridPosition(transform.position); //Получим сеточную позицию ящика
        _singleNodeBlocker.BlockAtCurrentPosition();// Заблокирую узел
    }
    public void Damage(Vector3 explosionPosition)
    {
        _crateDestroyed.gameObject.SetActive(true); // Активируем Разрушенный ящик
        _crateDestroyed.parent = null; // Отсоеденим Разрушенный ящик от родителя 
        Destroy(gameObject); // Уничтожим ящик

        GraphNode graphNode = AstarPath.active.GetNearest(transform.position).node; // Получим проверяемый узел
        BlockManager.Instance.InternalUnblock(graphNode, _singleNodeBlocker); // Разблокируем узел

        ApplyExplosionToChildren(_crateDestroyed, 150f, explosionPosition, 10f); // Применим взрыв к разрушенному ящику, с силой 150, в той же позиции, и радиус действия 10
               
        OnAnyDestroyed?.Invoke(this, EventArgs.Empty);
    }

    public GridPositionXZ GetGridPosition()
    {
        return _gridPosition;
    }

    private void ApplyExplosionToChildren(Transform root, float explosionForce, Vector3 explosionPosition, float explosionRange) // Применить Взрыв к Детям (explosionRange Диапазон взрыва)
    {
        foreach (Transform child in root)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childrigidbody)) // Попробуем получить риджибоди дочерних объектов 
            {
                childrigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRange);
            }

            ApplyExplosionToChildren(child, explosionForce, explosionPosition, explosionRange);  // Рекурсивная функция
        }
    }    

}
