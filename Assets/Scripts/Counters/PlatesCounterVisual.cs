using UnityEngine;
using System;
using System.Collections.Generic;

public class PlatesCounterVisual : MonoBehaviour
{
    [SerializeField] private PlatesCounter platesCounter;
    [SerializeField] private GameObject plateVisualPrefab;
    [SerializeField] private Transform counterTopPoint;
    private List<GameObject> plateVisualPrefabList;
    private int plateSpawnCount;
    private int plateVisualMaxCount = 4;
    private void Awake()
    {
        plateVisualPrefabList = new List<GameObject>();
    }
    private void Start()
    {
        plateSpawnCount = 0;
        platesCounter.OnPlateSpawned += PlatesCounter_OnPlateSpawned;
        platesCounter.OnPlateRemoved += PlatesCounter_OnPlateRemoved;
    }
    private void PlatesCounter_OnPlateSpawned(object sender, EventArgs e)
    {
        if(plateSpawnCount<plateVisualMaxCount)
        {
            Transform plateVisualPrefabTransform = Instantiate(plateVisualPrefab.transform, counterTopPoint);
            plateVisualPrefabTransform.localPosition = new Vector3(0, plateSpawnCount * 0.1f, 0);
            plateVisualPrefabList.Add(plateVisualPrefabTransform.gameObject);   
            plateSpawnCount++;  
        }
    }
    private void PlatesCounter_OnPlateRemoved(object sender, EventArgs e)
    {
            GameObject plateVisualPrefabToRemove = plateVisualPrefabList[plateSpawnCount - 1];
            plateVisualPrefabList.RemoveAt(plateSpawnCount - 1);
            Destroy(plateVisualPrefabToRemove);
            plateSpawnCount--;
    }
}
