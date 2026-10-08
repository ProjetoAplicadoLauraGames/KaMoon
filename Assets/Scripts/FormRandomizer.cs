using UnityEngine;

public class FormRandomizer : MonoBehaviour
{
    void Start()
    {

    }
    public int RandomizeForm()
    {
        int childCount = transform.childCount;

        int selectedChild = Random.Range(0, childCount);
        for (int i = 0; i < childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i == selectedChild);
        }
        Debug.Log("Selected child: " + (selectedChild + 1));
        return selectedChild + 1;
    }
}
