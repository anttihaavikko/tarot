using System;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Utils;
using UnityEngine;

public class ClassWedges : MonoBehaviour
{
    [SerializeField] private List<CardPreview> previews;

    private void Start()
    {
        var classes = EnumUtils.ToList<CardType>().RandomOrder().Take(previews.Count).ToList();
        var i = 0;
        previews.ForEach(p =>
        {
            p.MakeUnique();
            p.Show(classes[i]);
            i++;
        });
    }
}