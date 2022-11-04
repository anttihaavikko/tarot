using System;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class Deck : MonoBehaviour
{
    [SerializeField] private SortingGroup cardPrefab;

    private Stack<CardType> deck = new();
    private List<Transform> cards = new();

    private void Start()
    {
        for (var i = 0; i < 22; i++)
        {
            var card = Instantiate(cardPrefab, transform);
            var t = card.transform;
            t.position += Vector3.up * 0.1f * i;
            card.sortingOrder += i;
            cards.Add(t);
        }
    }

    private void Shuffle()
    {
        cards.ForEach(c => c.gameObject.SetActive(true));
        deck = new Stack<CardType>(EnumUtils.ToList<CardType>().OrderBy(_ => Random.value));
    }

    public CardType Pull()
    {
        if (deck.Count == 0)
        {
            Shuffle();
        }
        
        GetCurrent().gameObject.SetActive(false);

        return deck.Pop();
    }

    public Vector3 GetSpawn()
    {
        return GetCurrent().position;
    }

    private Transform GetCurrent()
    {
        return cards[Mathf.Max(0, deck.Count - 1)];
    }
}