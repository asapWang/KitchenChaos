using UnityEngine;

public class ResetStaticDataManager : MonoBehaviour
{
    public void Awake()
    {
        BaseCounter.ResetStaticData();
        CuttingCounter.ResetStaticData();
        TrashCounter.ResetStaticData();
    }
}
