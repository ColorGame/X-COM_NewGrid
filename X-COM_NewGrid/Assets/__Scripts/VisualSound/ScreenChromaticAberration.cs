using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ScreenChromaticAberration : MonoBehaviour// Эффект хроматической аберрации (разделение по краям экрана RGB потоков) висит на отдельной постобработке
{

    public static ScreenChromaticAberration Instance { get; private set; }  //(ОДНОЭЛЕМЕНТНЫЙ ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                                            // instance - экземпляр, У нас будет один экземпляр ResourceManager можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.
                                                                            // static Это значит то, что данная сущность принадлежит не конкретному объекту класса, а всему классу, как типу данных. Другими словами, если обычное поле класса принадлежит объекту, и у каждого конкретного объекта есть как бы своя копия данного поля, то статическое поле одно для всего класса.

    private const float ATTENUATION_SPEED = 1.5f; // Скорость затухания

    private Volume _volume; // Компонент Volume  на постобработке
    private float _attenuationSpeed; // Скорость затухания
   

    private void Awake()
    {
        Instance = this;

        _volume = GetComponent<Volume>(); // Получим ссылку на этот компонент
    }
    private void Update()
    {
        if (_volume.weight > 0) //Если вес Volume больше нуля то будем уменьшать его
        {            
            _volume.weight -= Time.deltaTime * _attenuationSpeed;
        }
    }

    public void SetWeight(float weight, float attenuationSpeed = ATTENUATION_SPEED) // Установим вес Volume и скорость затухания
    {
        _volume.weight = weight;
        _attenuationSpeed = attenuationSpeed;
    }

}
