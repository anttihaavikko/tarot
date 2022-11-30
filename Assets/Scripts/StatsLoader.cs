using System;
using System.Collections;
using System.Collections.Generic;
using AnttiStarterKit.Managers;
using Leaderboards;
using UnityEngine;
using UnityEngine.Networking;

public class StatsLoader : Manager<StatsLoader>
{
    public Action<LeaderboardStat> onLoaded;
    
    private const string Url = "https://games.sahaqiel.com/leaderboards/load-stats.php";
    private CertificateHandler certHandler;

    private Dictionary<string, LeaderboardStat> cache = new();
    
    public IEnumerator Load(string game, string player)
    {
        if (cache.ContainsKey(player))
        {
            yield return cache[player];
            yield break;
        }
        
        certHandler ??= new CustomCertificateHandler();
        
        var www = UnityWebRequest.Get(Url + "?game=" + game + "&player=" + player);
        www.certificateHandler = certHandler;

        yield return www.SendWebRequest();

        if (!string.IsNullOrEmpty(www.error)) yield break;
        
        var data = JsonUtility.FromJson<LeaderboardStat> (www.downloadHandler.text);
        if (!cache.ContainsKey(player))
        {
            cache.Add(player, data);   
        }
        yield return data;
    }
}

[Serializable]
public class LeaderboardStat
{
    public LeaderboardStatData normal;
    public LeaderboardStatData daily;
}

[Serializable]
public class LeaderboardStatData
{
    public int best;
    public int average;
    public int plays;
    public string previous;
}