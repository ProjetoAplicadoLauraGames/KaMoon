using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuPanelTransition : MonoBehaviour
{
    private const float Duration = 0.18f;
    private CanvasGroup group;

    private void OnEnable()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
    }

    private void Update()
    {
        group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime / Duration);
    }
}
