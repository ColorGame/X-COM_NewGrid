using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour // Менеджер ЗВУКА
{

    public static SoundManager Instance { get; private set; }//(ОДНОЭЛЕМЕНТНЫЙ ПАТТЕРН SINGLETON) Это свойство которое может быть заданно (SET-присвоено) только этим классом, но может быть прочитан GET любым другим классом
                                                             // instance - экземпляр, У нас будет один экземпляр ResourceManager можно сдел его static. Instance нужен для того чтобы другие методы, через него, могли подписаться на Event.
                                                             // static Это значит то, что данная сущность принадлежит не конкретному объекту класса, а всему классу, как типу данных. Другими словами, если обычное поле класса принадлежит объекту, и у каждого конкретного объекта есть как бы своя копия данного поля, то статическое поле одно для всего класса.
    public enum Sound // Перечесление звуков, что бы избежать строки (ПРОВЕРИТЬ НАЗВАНИЕ аудио клипов В ИЕРАРХИИ)
    {
        DeathCry,
        DestructionCrate,
        DoorClosed,
        DoorOpen,
        GrenadeExplosion,
        GrenadeSmoke,
        GrenadeStun,
        GrenadeThrow,
        Heal,
        HookPull,
        HookShoot,
        Interact,
        Move,
        Shoot,
        Spotter,
        Sword
    }

    private AudioSource audioSource; // Компонент источника звука (висит на SoundManager в сцене)
    private Dictionary<Sound, AudioClip> soundAudioClipDictionary; // Словарь Звуковой Аудио-клип(состояние Звука - ключ, Аудиоклип- -значение)
    private float volume = .5f; // Громкость по умолчанию 50%

    private void Awake()
    {
        Instance = this; //созданим экземпляр класса

        audioSource = GetComponent<AudioSource>(); // Получим компонент источника звука

        volume = PlayerPrefs.GetFloat("soundVolume", .5f); // Загрузим громкость из сохранения

        // Загрузим все звуки при запуске, чтобы не искать их при воспроизведении
        soundAudioClipDictionary = new Dictionary<Sound, AudioClip>(); // Создадим словарь Звуковой Аудио-клип

        foreach (Sound sound in System.Enum.GetValues(typeof(Sound))) // Переберем массив состояния звука  (GetValues(Type) - Возвращает массив значений констант в указанном перечислении.)
        {
            soundAudioClipDictionary[sound] = Resources.Load<AudioClip>(sound.ToString()); //Присвоим ключу значение - ресурс запрошенного типа, хранящийся по адресу path(путь) в папке Resources(эту папку я создал в папке Sounds).
        }
    }

    public void PlaySoundOneShot(Sound sound) // Воспроизведение Звука один раз
    {
        audioSource.PlayOneShot(soundAudioClipDictionary[sound], volume);// Воспроизводит аудиоклип и масштабирует громкость аудиоисточника по шкале громкости.
    }

    public void IncreaseVolume() // Увеличение громкости
    {
        volume += .1f;
        volume = Mathf.Clamp01(volume); // Ограничем между 0 и 1
        PlayerPrefs.SetFloat("soundVolume", volume); // Сохраним установленную громкость
    }

    public void DecreaseVolume() // Уменьшение громкости
    {
        volume -= .1f;
        volume = Mathf.Clamp01(volume); // Ограничем между 0 и 1
        PlayerPrefs.SetFloat("soundVolume", volume); // Сохраним установленную громкость
    }

    public float GetVolume()
    {
        return volume;
    }

    public void SetLoop(bool loop)
    {
        audioSource.loop = loop;
    }

    public void Play(Sound sound)
    {
        audioSource.clip = soundAudioClipDictionary[sound];
        audioSource.volume = volume;
        audioSource.Play();
    }
    public void Stop()
    {       
        audioSource.Stop();
        audioSource.clip = null;
    }
}
