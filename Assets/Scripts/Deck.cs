using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Managers;
using AnttiStarterKit.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class Deck : MonoBehaviour
{
    [SerializeField] private SortingGroup cardPrefab;
    [SerializeField] private Board board;
    [SerializeField] private Skills skills;

    private Stack<CardType> deck = new();
    private readonly List<Transform> cards = new();

    private int shuffles;

    private const int DeckSize = 22;
    private const float DealDelay = 0.03f;

    private Vector3 DealPos => transform.position + board.SkyPoint.magnitude * Vector3.down + Vector3.left;
    
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        for (var i = 0; i < DeckSize; i++)
        {
            AddCard();
        }
    }

    public void Init()
    {
        SetupStack();
        IsInitialized = true;
    }

    private void AddCard()
    {
        var card = Instantiate(cardPrefab, transform);
        var t = card.transform;
        var i = cards.Count;
        t.position += Vector3.up * (0.1f * i) + Random.Range(-0.03f, 0.03f) * Vector3.right;
        card.sortingOrder += i;
        cards.Add(t);
    }

    public IEnumerator TryShuffle()
    {
        if (deck.Any()) yield break;
        Shuffle();
        yield return new WaitForSeconds(DeckSize * DealDelay + 0.5f);
    }

    private void Shuffle()
    {
        for (var i = 0; i < DeckSize; i++)
        {
            var t = cards[i].transform;
            var p = t.position;
            t.position = DealPos.RandomOffset(1f);
            t.gameObject.SetActive(true);
            Tweener.MoveToBounceOut(t, p, 0.3f, i * DealDelay);
            this.StartCoroutine(() => AudioManager.Instance.PlayEffectFromCollection(3, p), i * DealDelay);
        }
        
        SetupStack();
        board.Shuffled();
    }

    private void SetupStack()
    {
        DailyState.Instance.Seed(shuffles + 666);
        var ordererSkill = skills.GetTriggered(Passive.DeckOrder, Vector3.zero);
        var orderMod = ordererSkill.Any() ? ordererSkill.First().amount : 0;
        deck = new Stack<CardType>(EnumUtils.ToList<CardType>().OrderByDescending(t => (int)t * orderMod).ThenBy(_ => Random.value).Take(DeckSize));
        shuffles++;
    }

    public void AddToTop(CardType type, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            deck.Push(type);
            if(deck.Count > cards.Count) AddCard();
            var top = GetCurrent();
            top.gameObject.SetActive(true);
            board.PulseAt(top.position);
        }
    }

    public CardType Pull()
    {
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