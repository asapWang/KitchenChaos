using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    private Player player;
    private float loopTimeMax = 0.1f;
    private float loopTime = 0f;
    private void Awake()
    {
        player = GetComponent<Player>();
    }
    private void Update()
    {
        loopTime += Time.deltaTime;
        if (loopTime >= loopTimeMax)
        {
            loopTime = 0f;
            if (player.IsMoving())
            {
                SoundManager.Instance.PlayFootStepSound(player.transform.position);
            }
        }
    }
}
