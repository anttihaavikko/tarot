using System;
using System.Collections;
using System.Collections.Generic;
using AnttiStarterKit.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Leaderboards
{
    public class ScoreRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_Text namePart, scorePart, nameShadow, scoreShadow;
        public RawImage flag;
        [SerializeField] private GameObject popup;
        [SerializeField] private Color textColor, hoverColor;
        [SerializeField] private TMP_Text values;
        [SerializeField] private GameObject content, spinner;

        private Camera cam;
        private string identifier;

        private void Awake()
        {
            cam = Camera.main;
        }

        public void Setup(string nam, string sco, string locale, string id)
        {
            identifier = id;
            namePart.text = nameShadow.text = nam;
            scorePart.text = scoreShadow.text = int.Parse(sco).AsScore();
            FlagManager.SetFlag(flag, locale);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var mp = cam.ScreenToWorldPoint(Input.mousePosition);
            popup.transform.position = popup.transform.position.WhereX(mp.x); 
            popup.SetActive(true);
            namePart.color = scorePart.color = hoverColor;
            CursorManager.Instance.Use(1);
            StartCoroutine(LoadStats());
        }

        private IEnumerator LoadStats()
        {
            var load = StatsLoader.Instance.Load("tarot", identifier);
            yield return load;
            var data = (LeaderboardStat)load.Current;
            
            if(data == null) yield break;
            
            content.SetActive(true);
            spinner.SetActive(false);

            values.text = string.Join("\n", new List<string>
            {
                data.normal.plays.ToString(),
                data.normal.best.AsScore(),
                data.normal.average.AsScore(),
                FormatDate(data.normal.previous),
                "",
                data.daily.plays.ToString(),
                data.daily.best.AsScore(),
                data.daily.average.AsScore(),
                FormatDate(data.daily.previous)
            });
        }

        private string FormatDate(string input)
        {
            var ok = DateTime.TryParse(input, out var date);
            return ok ? DailyState.FormatDate(date) : "-";
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            popup.SetActive(false);
            namePart.color = scorePart.color = textColor;
            CursorManager.Instance.Use(0);
        }
    }
}
