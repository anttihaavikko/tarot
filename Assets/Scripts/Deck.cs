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
    private readonly List<Transform> cards = new();

    private void Start()
    {
        for (var i = 0; i < 22; i++)
        {
            AddCard();
        }
    }

    private void AddCard()
    {
        var card = Instantiate(cardPrefab, transform);
        var t = card.transform;
        var i = cards.Count;
        t.position += Vector3.up * 0.1f * i;
        card.sortingOrder += i;
        cards.Add(t);
    }

    private void Shuffle()
    {
        for (var i = 0; i < 22; i++)
        {
            cards[i].gameObject.SetActive(true);
        }
        
        deck = new Stack<CardType>(EnumUtils.ToList<CardType>().OrderBy(_ => Random.value));
    }

    public void AddToTop(CardType type)
    {
        deck.Push(type);
        if(deck.Count > cards.Count) AddCard();
        GetCurrent().gameObject.SetActive(true);
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
        return cards[Mathf.Clamp(deck.Count - 1, 0, cards.Count - 1)];
    }
}