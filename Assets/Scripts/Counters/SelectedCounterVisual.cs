using UnityEngine;

public class SelectedCounter : MonoBehaviour
{
    [SerializeField] private BaseCounter baseCounter;
    [SerializeField] private GameObject[] highlightGameObject;
    private void Start()
    {
        if(Player.LocalInstance != null)
            Player.LocalInstance.OnSelectedCounter += Player_OnSelectedCounter;
        else
            Player.OnPlayerSpawned += Player_OnPlayerSpawned;
    }
    private void Player_OnPlayerSpawned(object sender, System.EventArgs e)
    {
        //为了避免客户端多次建立这里重复订阅事件，先取消订阅再订阅
        Player.LocalInstance.OnSelectedCounter -= Player_OnSelectedCounter;
        Player.LocalInstance.OnSelectedCounter += Player_OnSelectedCounter;
    }
    private void Player_OnSelectedCounter(object sender, Player.SelectedCounterEventArgs e)
    {
        if (e.selectedCounter == baseCounter)
        {
            foreach (GameObject go in highlightGameObject)
            {
                go.SetActive(true);
            }
        }
        else
        {
            foreach (GameObject go in highlightGameObject)
            {
                go.SetActive(false);
            }
        }
    }
}
