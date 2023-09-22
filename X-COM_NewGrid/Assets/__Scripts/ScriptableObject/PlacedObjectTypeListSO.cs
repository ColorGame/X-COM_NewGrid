using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/PlacedObjectTypeList")] 
public class PlacedObjectTypeListSO : ScriptableObject
{ // БУДЕТ ВСЕГО ОДИН ЭКЗЕМПЛЯР (один список)

    public List<PlacedObjectTypeSO> list; // Список Типов Размещяемых объектов
}

