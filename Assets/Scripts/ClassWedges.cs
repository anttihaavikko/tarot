using System;
using System.Collections.Generic;
using AnttiStarterKit.Utils;
using UnityEngine;

public class ClassWedges : MonoBehaviour
{
    [SerializeField] private List<CardPreview> previews;

    private void Start()
    {
        previews.ForEach(p =>
        {
            p.MakeUnique();
            p.Show(EnumUtils.Random<CardType>());
        });
    }
}