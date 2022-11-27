using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Extensions;
using UnityEngine;

public class Plants : MonoBehaviour
{
    [SerializeField] private Plant prefab;

    private readonly List<Plant> plants = new();

    public void Add(Vector3 pos)
    {
        if (Random.value < 0.9f) return;
        if (plants.Any(p => Vector3.Distance(p.transform.position, pos) < 1f)) return;
        
        var plant = Instantiate(prefab, transform);
        plant.transform.position = pos.RandomOffset(0.5f);
        plants.Add(plant);
    }

    public void Clear(Vector3 pos)
    {
        plants.Where(p => Vector3.Distance(p.transform.position, pos) < 2f)
            .ToList()
            .ForEach(p => p.gameObject.SetActive(false));
    }
}