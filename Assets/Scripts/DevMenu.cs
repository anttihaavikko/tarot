using System;
using AnttiStarterKit.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevMenu : MonoBehaviour
{
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Board board;

    private void Start()
    {
        foreach (var type in EnumUtils.ToList<CardType>())
        {
            var button = Instantiate(buttonPrefab, transform);
            button.GetComponentInChildren<TMP_Text>().text = type.ToString();
            button.onClick.AddListener(() => board.ChangeDrawnTo(type));
        }
    }
}