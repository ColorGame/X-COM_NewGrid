using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Rope : MonoBehaviour // Веревка Юнита
{
    private RopeRanderer _ropeRanderer;

    private void Awake()
    {
        _ropeRanderer = GetComponentInChildren<RopeRanderer>();
    }

    private void Start()
    {
        HideRope();
    }

    public void ShowRope()
    {
        _ropeRanderer.gameObject.SetActive(true);
    }

    public void HideRope()
    {
        _ropeRanderer.gameObject.SetActive(false);
    }

    public RopeRanderer GetRopeRanderer()
    {
        return _ropeRanderer;
    }
}
