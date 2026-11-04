using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuCancel : MonoBehaviour, ICancelHandler
{
    public Button backButton;

    public void OnCancel(BaseEventData eventData)
    {
        if (GetComponent<Dropdown>() != null && transform.Find("Dropdown List") != null) return;
        if (backButton == null || !backButton.isActiveAndEnabled || !backButton.interactable) return;
        backButton.onClick.Invoke();
        eventData.Use();
    }
}
