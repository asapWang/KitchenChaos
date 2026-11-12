using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas botCanvas;
    [SerializeField] private Canvas midCanvas;
    [SerializeField] private Canvas topCanvas;
    [SerializeField] private Canvas systemCanvas;

    private readonly Dictionary<string, BasePanel> panels =
        new Dictionary<string, BasePanel>(StringComparer.OrdinalIgnoreCase);

    public BasePanel GetPanel(string panelName)
    {
        if (string.IsNullOrWhiteSpace(panelName))
            return null;

        panels.TryGetValue(panelName, out BasePanel panel);
        return panel;
    }

    public void ShowPanel(string panelName)
    {
        if (string.IsNullOrWhiteSpace(panelName))
        {
            Debug.LogError("面板名称不能为空。", this);
            return;
        }

        BasePanel panel = GetPanel(panelName);
        if (panel == null)
            panel = CreatePanel(panelName);

        if (panel == null || panel.gameObject.activeSelf)
            return;

        panel.gameObject.SetActive(true);
        panel.Enter();
    }

    public void HidePanel(string panelName)
    {
        BasePanel panel = GetPanel(panelName);
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
