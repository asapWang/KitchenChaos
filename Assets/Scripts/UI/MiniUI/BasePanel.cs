using UnityEngine;

public enum UILayer
{
    Bot,
    Mid,
    Top,
    System
}

public abstract class BasePanel : MonoBehaviour
{
    [SerializeField] private UILayer layer = UILayer.Mid;

    public UILayer Layer => layer;

    public virtual void Init() { }
    public virtual void Enter() { }
    public virtual void Pause() { }
    public virtual void Resume() { }
    public virtual void Exit() { }
}
