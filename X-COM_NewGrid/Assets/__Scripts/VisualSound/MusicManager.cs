using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour   // Менеджер Фоновой музыки 
                                            // В настроиках поставить галочку возле Play On Awake
{
    public enum Music // Перечесление треков, что бы избежать строки (ПРОВЕРИТЬ НАЗВАНИЕ аудио клипов В ИЕРАРХИИ)
    {
        SeasonedOak,
        KingArthur58LegendOfTheSword,
        CaveFight,
        AssassinsBreathe,
        TheDarklands,
        KingArthur58DestinyOfTheSword,
        GrowingUpLondinium
    }
    private Music _music; // Аудио трек
    private float _musicTimer; //Таймер состояния

    private AudioSource audioSource; // Компонент источника звука (висит на MusicManager в сцене)
    private Dictionary<Music, AudioClip> musicAudioClipDictionary; // Словарь Звуковой Аудио-клип(состояние Звука - ключ, Аудиоклип- -значение)
    private float volume = .5f; // Громкость по умолчанию 50%

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        volume = PlayerPrefs.GetFloat("musicVolume", .5f); // Загрузим громкость из сохранения

        audioSource.volume = volume; // Установим громкость по умолчанию

        // Загрузим все звуки при запуске, чтобы не искать их при воспроизведении
        musicAudioClipDictionary = new Dictionary<Music, AudioClip>(); // Создадим словарь Звуковой Аудио-клип

        foreach (Music sound in System.Enum.GetValues(typeof(Music))) // Переберем массив состояния звука  (GetValues(Type) - Возвращает массив значений констант в указанном перечислении.)
        {
            musicAudioClipDictionary[sound] = Resources.Load<AudioClip>(sound.ToString()); //Присвоим ключу значение - ресурс запрошенного типа, хранящийся по адресу path(путь) в папке Resources(эту папку я создал в папке Sounds).
        }
    }

    private void Start()
    {
        _music= Music.SeasonedOak;   
        _musicTimer = musicAudioClipDictionary[_music].length;
        PlayMusic(_music); // Воспроизведем данный трек
    }


    private void Update()
    {
        _musicTimer -= Time.deltaTime; // Запустим таймер для переключения состояний
        
        if (_musicTimer <= 0) // По истечению времени _musicTimer вызовим NextMusic() которая в свою очередь переключит состояние. Например - у меня было TypeGrenade.Aiming: тогда в case TypeGrenade.Aiming: переключу на TypeGrenade.Shooting;
        {
            NextMusic(); //Следующая композиция
        }
    }

    public void NextMusic() //Автомат переключения состояний
    {
        switch (_music)
        {
            case Music.SeasonedOak:
                _music = Music.KingArthur58LegendOfTheSword;
               
                break;
            case Music.KingArthur58LegendOfTheSword:
                _music = Music.CaveFight;
                break;

            case Music.CaveFight:
                _music = Music.AssassinsBreathe;
                break;

            case Music.AssassinsBreathe:
                _music = Music.TheDarklands;
                break;

            case Music.TheDarklands:
                _music = Music.KingArthur58DestinyOfTheSword;
                break;

            case Music.KingArthur58DestinyOfTheSword:
                _music = Music.GrowingUpLondinium;
                break;

            case Music.GrowingUpLondinium:
                _music = Music.SeasonedOak;
                break;
        }
        audioSource.Stop();
        _musicTimer = musicAudioClipDictionary[_music].length; // Задаем продолжительность след трека
        PlayMusic(_music); // Воспроизведем полученный трек трек
    }

    public void PlayMusic(Music music) // Воспроизведение Звука
    {
        audioSource.PlayOneShot(musicAudioClipDictionary[music], volume);// Воспроизводит аудиоклип и масштабирует громкость аудиоисточника по шкале громкости.
    }
    public void IncreaseVolume() // Увеличить громкость
    {
        volume += .1f;
        volume = Mathf.Clamp01(volume); // Ограничем между 0 и 1
        audioSource.volume = volume; // Установим громкость
        PlayerPrefs.SetFloat("musicVolume", volume); // Сохраним установленную громкость
    }

    public void DecreaseVolume() // Уменьшить громкость
    {
        volume -= .1f;
        volume = Mathf.Clamp01(volume); // Ограничем между 0 и 1
        audioSource.volume = volume; // Установим громкость
        PlayerPrefs.SetFloat("musicVolume", volume); // Сохраним установленную громкость
    }

    public float GetVolume() // Получить громкость
    {
        return volume;
    }
}
