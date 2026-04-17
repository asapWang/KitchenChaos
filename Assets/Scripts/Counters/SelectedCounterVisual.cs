using UnityEngine;

public class SelectedCounter : MonoBehaviour
{
    [SerializeField] private BaseCounter baseCounter;
    [SerializeField] private GameObject[] highlightGameObject;
    private void Start()
    {
        //Player.Instance.OnSelectedCounter += Player_OnSelectedCounter;
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
