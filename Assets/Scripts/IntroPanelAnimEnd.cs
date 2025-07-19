using UnityEngine;

public class IntroPanelAnimEnd : MonoBehaviour
{
    public TheFlow flow;
    public void AnimationEnd()
    {
        flow.SetAnimEnd();
    }
}
