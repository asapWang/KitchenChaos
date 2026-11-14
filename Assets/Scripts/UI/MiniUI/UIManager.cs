using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas botCanvas;
    [SerializeField] private Canvas midCanvas;
    [SerializeField] private Canvas topCanvas;
    [SerializeField] private Canvas systemCanvas;
    [SerializeField] private bool enableEscapeBack;

    private readonly Dictionary<string, BasePanel> panels =
        new Dictionary<string, BasePanel>(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<BasePanel> panelStack = new Stack<BasePanel>();

    private void Update()
    {
        if (enableEscapeBack && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            PopPanel();
    }

    public BasePanel GetPanel(string panelName)
    {
        if (string.IsNullOrWhiteSpace(panelName))
            return null;

        panels.TryGetValue(panelName, out BasePanel panel);
        return panel;
    }

    public void ShowPanel(string panelName)
    {
        ShowPanel(panelName, pushToStack: false);
    }

    public void PushPanel(string panelName)
    {
        ShowPanel(panelName, pushToStack: true);
    }

    public BasePanel PeekPanel()
    {
        return panelStack.Count > 0 ? panelStack.Peek() : null;
    }

    public void PopPanel()
    {
        if (panelStack.Count == 0)
            return;

        HidePanel(panelStack.Pop());

        if (panelStack.Count == 0)
            return;

        BasePanel previousPanel = panelStack.Peek();
        previousPanel.gameObject.SetActive(true);
        previousPanel.Resume();
    }

    private void ShowPanel(string panelName, bool pushToStack)
    {
        if (string.IsNullOrWhiteSpace(panelName))
        {
            Debug.LogError("面板名称不能为空。", this);
            return;
        }

        BasePanel panel = GetPanel(panelName);
        if (panel == null)
            panel = CreatePanel(panelName);

        if (panel == null || panelStack.Contains(panel))
            return;

        if (pushToStack)
        {
            if (panelStack.Count > 0)
            {
                BasePanel previousPanel = panelStack.Peek();
                previousPanel.Pause();
                previousPanel.gameObject.SetActive(false);
            }

            panelStack.Push(panel);
        }

        if (panel.gameObject.activeSelf)
            return;

        panel.gameObject.SetActive(true);
        panel.Enter();
    }

    public void HidePanel(string panelName)
    {
        BasePanel panel = GetPanel(panelName);
        if (panel != null && panelStack.Contains(panel))
        {
            if (panelStack.Peek() == panel)
                PopPanel();

            return;
        }

        HidePanel(panel);
    }

    private void HidePanel(BasePanel panel)
    {
        if (panel == null || !panel.gameObject.activeSelf)
            return;

        panel.Exit();
        panel.gameObject.SetActive(false);
    }

    private BasePanel CreatePanel(string panelName)
    {
        string path = $"UI/{panelName}";
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"找不到 UI Prefab：{path}", this);
            return null;
        }

        if (!prefab.TryGetComponent(out BasePanel template))
        {
            Debug.LogError($"Prefab 根对象缺少 BasePanel：{path}", prefab);
            return null;
        }

        Canvas canvas = template.Layer switch
        {
            UILayer.Bot => botCanvas,
            UILayer.Mid => midCanvas,
            UILayer.Top => topCanvas,
            UILayer.System => systemCanvas,
            _ => null
        };

        if (canvas == null)
        {
            Debug.LogError($"UIRoot 未绑定 Canvas：{template.Layer}", this);
            return null;
        }

        GameObject instance = Instantiate(prefab, canvas.transform, false);
        instance.SetActive(false);

        BasePanel panel = instance.GetComponent<BasePanel>();
        panels[panelName] = panel;
        panel.Init();
        return panel;
    }
}
