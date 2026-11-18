using UnityEngine;

public class OpenBox : MonoBehaviour, IInteragivel
{
    private GameObject BoxLid = null;
    private bool isOpen = false;

    void Start()
    {
        BoxLid = transform.GetChild(0).gameObject;
    }

    public void Interagir()
    {
        if (BoxLid != null)
        {
            if (!isOpen) {
                BoxLid.transform.Rotate(-70f, 0f, 0f);
                isOpen = true;
            } else {
                BoxLid.transform.Rotate(70f, 0f, 0f);
                isOpen = false;}
        }

    }
}
