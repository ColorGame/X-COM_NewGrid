using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathfindingLinkMonoBehaviour))] // создание пользовательского редактора для созданного вами скрипта.
                                                     // Когда вы создаете скрипт в Unity, по умолчанию он наследуется от MonoBehaviour и, следовательно, является компонентом, который вы можете присоединить к GameObject.
                                                     // Когда вы размещаете компонент в GameObject, инспектор отображает интерфейс по умолчанию, который вы можете использовать для просмотра и редактирования каждой общедоступной переменной
                                                     // Пользовательский редактор - это отдельный скрипт, который заменяет этот макет по умолчанию любыми выбранными вами элементами управления редактора.
public class PathfindingLinkMonoBehaviourEditor : Editor
{


    private void OnSceneGUI() // Функция срабатывает каждый раз когда меняется сцена Позволяет редактору обрабатывать событие в режиме просмотра сцены.
    {
        PathfindingLinkMonoBehaviour pathfindingLinkMonoBehaviour = (PathfindingLinkMonoBehaviour)target; // target as PathfindingLinkMonoBehaviour -другой способ написания //target- Проверяемый объект.

        EditorGUI.BeginChangeCheck(); // Запускает новый блок кода для проверки изменений графического интерфейса пользователя.
        Vector3 newLinkPositionA = Handles.PositionHandle(pathfindingLinkMonoBehaviour.linkPositionA, Quaternion.identity); //(МАРКЕР ПОЛОЖЕНИЯ) Пользовательские элементы управления 3D GUI и рисование в режиме сцены. PositionHandle-Создайте дескриптор позиции.
        Vector3 newLinkPositionB = Handles.PositionHandle(pathfindingLinkMonoBehaviour.linkPositionB, Quaternion.identity);
        if (EditorGUI.EndChangeCheck()) // bool Возвращает true, если состояние графического интерфейса изменилось с момента вызова EditorGUI.BeginChangeCheck, в противном случае false.
        {
            Undo.RecordObject(pathfindingLinkMonoBehaviour, "Изменить положение ссылки"); // Записывает все изменения, внесенные в объект после функции RecordObject. Что бы после нажатия ctr + z можно было откатиться
            pathfindingLinkMonoBehaviour.linkPositionA = newLinkPositionA;
            pathfindingLinkMonoBehaviour.linkPositionB = newLinkPositionB;
        }
    }
}

