using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameEndUI : MonoBehaviour
{
    

    private void Awake()
    {        

        transform.Find("mainMenuButton").GetComponent<Button>().onClick.AddListener(() =>
        {            
            GameSceneManager.Load(GameSceneManager.Scene.MainMenuScene);
        });

        transform.Find("againButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            GameSceneManager.Load(GameSceneManager.Scene.GameScene_MultiFloors);
        });
    }   
}
