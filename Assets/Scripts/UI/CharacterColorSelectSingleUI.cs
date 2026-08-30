using UnityEngine;
using UnityEngine.UI;

public class CharacterColorSelectSingleUI : MonoBehaviour
{
    [SerializeField] private int colorID;
    [SerializeField] private Image image;
    [SerializeField] private GameObject selectedImage;
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            GameMultiplayer.Instance.SetPlayerColor(colorID);
        });
       
    }
    private void Start()
    {
        image.color = GameMultiplayer.Instance.GetPlayerColor(colorID);
        UpdateIsSelected();
        //订阅GameMultiplayer.Instance.OnPlayerDataNetworkListChanged事件，当playerDataNetworkList发生变化时，更新选中状态
        GameMultiplayer.Instance.OnPlayerDataNetworkListChanged += GameManager_OnPlayerDataNetworkListChanged;
    }
    private void GameManager_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdateIsSelected();
    }
    //更新选中状态
    private void UpdateIsSelected()
    {
        if(colorID == GameMultiplayer.Instance.GetPlayerData().colorID)
        {
            selectedImage.SetActive(true);
        }
        else
        {
            selectedImage.SetActive(false);
        }
    }
    private void OnDestroy()
    {
        if(GameMultiplayer.Instance != null)
        {
            GameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= GameManager_OnPlayerDataNetworkListChanged;
        }
    }
}
