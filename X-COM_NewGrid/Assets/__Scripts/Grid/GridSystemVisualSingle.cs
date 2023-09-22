//#define HEX_GRID_SYSTEM //ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА //  В C# определен ряд директив препроцессора, оказывающих влияние на интерпретацию исходного кода программы компилятором. 
//Эти директивы определяют порядок интерпретации текста программы перед ее трансляцией в объектный код в том исходном файле, где они появляются. 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisualSingle : MonoBehaviour //Сеточная система визуализации еденицы сетки (ячейки) // Лежит на самом префабе (белая рамка)
{
    [SerializeField] private MeshRenderer _meshRendererQuad; // Будем включать и выкл. MeshRenderer что бы скрыть или показать наш визуальный объект

    [SerializeField] private MeshRenderer _meshRendererHex; // Будем включать и выкл. MeshRenderer что бы скрыть или показать наш визуальный объект

    [SerializeField] private MeshRenderer _meshRendererСircleGrenade; // Будем включать и выкл. MeshRenderer что бы скрыть или показать наш визуальный объект
    
    [SerializeField] private MeshRenderer _meshRendererQuadGrenade; // Будем включать и выкл. MeshRenderer что бы скрыть или показать наш визуальный объект

    // Для отладки шестигранной сетки (отображение ячейки под мышкой)
    //[SerializeField] private GameObject _SelectedGameObject; // Для отладки Шестигранной сетки

#if HEX_GRID_SYSTEM // Если ШЕСТИГРАННАЯ СЕТОЧНАЯ СИСТЕМА

    private void Start()
    {
       _meshRendererQuad.enabled = false; //Скрыть Квадратную Ячейку
    }

    public void Show(Material material) // Показать
    {
        _meshRendererHex.enabled = true;
        _meshRendererHex.material = material; // Установим переданный нам материал
    }

    public void Hide() // Скрыть
    {
        _meshRendererHex.enabled = false;
    }

    // Для отладки шестигранной сетки (отображение ячейки под мышкой)
    /*public void ShowSelected() // Показать Выделенный объект
    {
        _SelectedGameObject.SetActive(true);
    }
    
    public void HideSelected() // Скрыть Выделенный объект
    {
        _SelectedGameObject.SetActive(false);
    }*/


#else//в противном случае компилировать

    private void Start()
    {
        _meshRendererHex.enabled = false; //Скрыть Шестигранную Ячейку
        _meshRendererСircleGrenade.enabled = false; //Скрыть Круг
        _meshRendererQuadGrenade.enabled=false; // Скрыть квадрат
    }

    public void ShowСircle(float radius) // Показать Круг нужного диаметра
    {
        _meshRendererСircleGrenade.enabled = true;
        _meshRendererСircleGrenade.transform.localScale = Vector3.one* 2*radius;
    }

    public void HideСircle() // Скрыть
    {
        _meshRendererСircleGrenade.enabled = false;
    }

    public void ShowQuad(Material material, float radius = 1) // Показать Квадрат нужного диаметра
    {
        _meshRendererQuadGrenade.enabled = true;
        _meshRendererQuadGrenade.transform.localScale = Vector3.one * 2 * radius;
        _meshRendererQuadGrenade.material = material; // Установим переданный нам материал
    }

    public void HideQuad() // Скрыть
    {
        _meshRendererQuadGrenade.enabled = false;
    }

    public void Show(Material material) // Показать
    {
        _meshRendererQuad.enabled = true;
        _meshRendererQuad.material = material; // Установим переданный нам материал
    }

    public void Hide() // Скрыть
    {
        _meshRendererQuad.enabled = false;
        _meshRendererСircleGrenade.enabled = false;
        _meshRendererQuadGrenade.enabled = false;
    }
#endif

}
