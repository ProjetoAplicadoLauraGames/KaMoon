using UnityEngine;

public class OpenBox : MonoBehaviour
{
    private GameObject BoxLid = null;

    void Start()
    {
        BoxLid = transform.GetChild(0).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
