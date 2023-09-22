using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour // Главное меню
{

    private void Awake()
    {

        // Найдем кнопки и Добавим событие при нажатии на наши кнопки
        // AddListener() в аргумент должен получить делегат- ссылку на функцию. Функцию будем объявлять АНАНИМНО через лямбду () => {...} 
        transform.Find("playButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            GameSceneManager.Load(GameSceneManager.Scene.GameScene_MultiFloors);
        });

        transform.Find("quitButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            Application.Quit(); // Кнопка будет работать тоько после сборки
        });
    }
}
