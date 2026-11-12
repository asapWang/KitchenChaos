using TMPro;
using UnityEngine;

public class TestPanel : BasePanel
{
    [SerializeField] private TMP_Text title;

    public override void Init()
    {
        title.text = "Mini UI Test";
        Debug.Log("TestPanel Init", this);
    }

    public override void Enter()
    {
        Debug.Log("TestPanel Enter", this);
    }

    public override void Exit()
    {
        Debug.Log("TestPanel Exit", this);
    }
}
