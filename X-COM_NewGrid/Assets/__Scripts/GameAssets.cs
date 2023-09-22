using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GameAssets : MonoBehaviour //Игровые Активы
{

    private static GameAssets instance; // т.к. мы обрабатываем в ручную то не надо создавать его в сцене

    // Когда кто-то получит доступ к этому свойству оно запустит код (get) и при необходимости создаст экземпляр ИГРОВЫХ АКТИВОВ или вернет существующую
    public static GameAssets Instance // //(ОДНОЭЛЕМЕНТНЫЙ ПАТТЕРН SINGLETON) может быть прочитан GET любым другим классом
                                      // instance - экземпляр, У нас будет один экземпляр ResourceManager можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.
                                      // static Это значит то, что данная сущность принадлежит не конкретному объекту класса, а всему классу, как типу данных. Другими словами, если обычное поле класса принадлежит объекту, и у каждого конкретного объекта есть как бы своя копия данного поля, то статическое поле одно для всего класса.

    {
        get //(расширим свойство get) Будем обрабатывать вручную а не через компилятор
        {
            if (instance == null) // Если экземпляр нулевой то создадим его
            {
                instance = Resources.Load<GameAssets>("GameAssets"); //В поле экземпляра - (instance) установим ресурс запрошенного типа, хранящийся по адресу path(путь) в папке Resources(эту папку я создал в папке ScriptableObjects  и в папке Prefab).                                                                     
            }
            return instance; //Вернем поле экземпляра (если оно не нулевое то вернем существующее)
        }
    }

    public Transform grenadeProjectilePrefab; // Префаб граната
    public Transform bulletProjectilePrefab; // Префаб пули
    public Transform gridSystemVisualSinglePrefab; // Префаб визуализации узла сетки
    public Transform comboPartnerFXPrefab; // Префаб  частички взаимодействия
    public Transform grenadeExplosionFXPrefab; // Префаб частички взрыва гранаты //НЕЗАБУДЬ ПОСТАВИТЬ ГАЛОЧКУ У TRAIL самоуничтожение(Destroy) после проигрывания
    public Transform grenadeSmokeFXPrefab; // Префаб дыма от гранаты // Уничтожать дым будет скрипт прикрипленный к нему
    public Transform electricityWhiteFXPrefab; // Префаб электромагнитное облако
    public Transform bulletHitFXPrefab; // Префаб частиц от поппадания пули
    public Transform healFXPrefab; // Префаб Частички исцеления
    public Transform spotterFireFXPrefab; // Префаб Частички волны при наблюдении
}