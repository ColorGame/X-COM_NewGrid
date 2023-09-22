using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneManager //статический класс - Менеджер игровых сцен
{

    public enum Scene // Перечесление сцен, что бы избежать строки (ПРОВЕРИТЬ НАЗВАНИЕ СЦЕН В ИЕРАРХИИ)
    {
        GameScene_MultiFloors,
        MainMenuScene,
    }

    public static void Load(Scene scene) 
    {
        SceneManager.LoadScene(scene.ToString()); //Загружает сцену по ее имени или индексу в настройках сборки.
                                                  //Примечание: В большинстве случаев, чтобы избежать пауз или сбоев в производительности во время загрузки, вам следует использовать асинхронную версию этой команды, которая является: LoadSceneAsync.
    }

}
