using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/UnitType")] // Для создания экзкмпляров ScriptableObject. В всплывающем меню во вкладке create появиться вкладка ScriptableObjects внутри будут UnitType(Тип здания)
public class UnitTypeSO : ScriptableObject //Тип Здания - это контейнер данных, который можно использовать для сохранения больших объемов данных, независимо от экземпляров класса.
{
    public string nameString;   // Имя Юнита
    public int health; // Здоровье
    public int moveDistance; // Дистанция движения (измеряется в узлах сетки, включает узел с самими Юнитом)
    public int shootDistance; // Дистанция выстрела
}