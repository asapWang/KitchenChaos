using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TestNetcodeUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    private void Awake()
    {
        hostButton.onClick.AddListener(() =>
        {
            GameMultiplayer.Instance.StartHost();
            Hide();
        });
        clientButton.onClick.AddListener(() =>
        {
            GameMultiplayer.Instance.StartClient();
            Hide();
        });
    }

    private void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
