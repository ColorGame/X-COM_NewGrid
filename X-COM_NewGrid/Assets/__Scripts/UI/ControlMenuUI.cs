using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class ControlMenuUI : MonoBehaviour
{ 

    private HashAnimationName _animBase = new HashAnimationName();
    private Animator _animator; //Аниматор на двери
    private bool _isOpen = false; //


    private void Awake()
    {
        _animator = GetComponent<Animator>();       
    }

   

    public void UpdateStateControlMenu(bool isOpen)
    {
        if (isOpen)
        {
            OpenControlMenu();
        }
        else
        {
            CloseControlMenu();
        }
    }

    public void OpenControlMenu()
    {
        _isOpen = true;      
       _animator.CrossFade(_animBase.MenuControlOpen, 0);
    }

    public void CloseControlMenu()
    {
        _isOpen = false;      
        _animator.CrossFade(_animBase.MenuControlClose, 0);
    }

    public bool GetIsOpen()
    {
        return _isOpen;
    }

    public void SetIsOpen(bool isOpen)
    {
        _isOpen = isOpen;
    }
}
