using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyMessageUI : BasePanel
{
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI messageText;
    private void Awake()
    {
        UIManager uiManager = GetComponentInParent<UIManager>();
        backButton.onClick.AddListener(() => uiManager.HidePanel(nameof(LobbyMessageUI)));
    }

    public override void Enter()
    {
        backButton.Select();
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
    }
}
