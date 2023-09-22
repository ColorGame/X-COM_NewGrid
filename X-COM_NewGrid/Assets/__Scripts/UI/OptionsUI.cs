using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsUI : MonoBehaviour // Меню настроик 
{

    [SerializeField] private SoundManager _soundManager; // Менеджер ЗВУКА
    [SerializeField] private MusicManager _musicManager; // Менеджер МУЗЫКИ
    
    private ControlMenuUI _controlMenu; // Меню управления

    private TextMeshProUGUI _soundVolumeText; // Текст громкости звука
    private TextMeshProUGUI _musicVolumeText; // Текст громкости музыки

    private void Awake()
    {
        _soundVolumeText = transform.Find("soundVolumeText").GetComponent<TextMeshProUGUI>();
        _musicVolumeText = transform.Find("musicVolumeText").GetComponent<TextMeshProUGUI>();
        _controlMenu = GetComponentInChildren<ControlMenuUI>();

        // Найдем кнопки и Добавим событие при нажатии на наши кнопки
        // AddListener() в аргумент должен получить делегат- ссылку на функцию. Функцию будем объявлять АНАНИМНО через лямбду () => {...} 
        transform.Find("soundIncreaseButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            _soundManager.IncreaseVolume();
            UpdateText();
        });
        transform.Find("soundDecreaseButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            _soundManager.DecreaseVolume();
            UpdateText();
        });

        transform.Find("musicIncreaseButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            _musicManager.IncreaseVolume();
            UpdateText();
        });
        transform.Find("musicDecreaseButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            _musicManager.DecreaseVolume();
            UpdateText();
        });

        transform.Find("musicNextButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            _musicManager.NextMusic();
        });

        transform.Find("mainMenuButton").GetComponent<Button>().onClick.AddListener(() =>
        {           
            GameSceneManager.Load(GameSceneManager.Scene.MainMenuScene);
        });

        transform.Find("edgeScrollingToggle").GetComponent<Toggle>().onValueChanged.AddListener((bool set) => // Подпишемся на изменение значения Тумблера прокрутка по краям (принимает булевое значение)
        {
            CameraController.Instance.SetEdgeScrolling(set);
        });

        transform.Find("resumeButton").GetComponent<Button>().onClick.AddListener(() =>
        {
           
            gameObject.SetActive(false); // спрячем меню
        });

        transform.Find("controlButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            ToggleVisibleControlMenu();
        });
        
    }

    private void Start()
    {
        UpdateText();
        gameObject.SetActive(false); // спрячем меню

        transform.Find("edgeScrollingToggle").GetComponent<Toggle>().SetIsOnWithoutNotify(CameraController.Instance.GetEdgeScrolling()); // Установим актуальное значения Тумблера прокрутка по краям
    } 


    private void UpdateText() // Обновим текс громкости
    {
        // Что бы легче читалось умножим на 10 и округлим до целых
        _soundVolumeText.SetText(Mathf.RoundToInt(_soundManager.GetVolume() * 10).ToString());
        _musicVolumeText.SetText(Mathf.RoundToInt(_musicManager.GetVolume() * 10).ToString());
    }

    public void ToggleVisible() // Переключатель видимости меню НАСТРОЙКИ (будем вызывать через инспектор кнопкой OptionsButton)
    {
        gameObject.SetActive(!gameObject.activeSelf); // Переключим в противоположное состояние        
    }  


    public void ToggleVisibleControlMenu()
    {
        _controlMenu.SetIsOpen(!_controlMenu.GetIsOpen()); // Переключим в противоположное состояние
        _controlMenu.UpdateStateControlMenu(_controlMenu.GetIsOpen());
    }

}
