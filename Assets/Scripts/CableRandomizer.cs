using System.Collections.Generic;
using UnityEngine;

public class CableRandomizer : MonoBehaviour
{

    public FormRandomizer currentForm;
    private List<GameObject> cables = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int cableToCut = -1;

        switch (RandomizeCables())
        {
            case 3:
                switch (currentForm.RandomizeForm())
                {
                    case 1:
                        cableToCut = 2;
                        break;
                    case 2:
                        cableToCut = 4;
                        break;
                    case 3:
                        cableToCut = 2;
                        break;
                }
                break;
            case 4:
                switch (currentForm.RandomizeForm())
                {
                    case 1:
                        cableToCut = 3;
                        break;
                    case 2:
                        cableToCut = 1;
                        break;
                    case 3:
                        cableToCut = 1;
                        break;
                }
                break;
            case 5:
                switch (currentForm.RandomizeForm())
                {
                    case 1:
                        cableToCut = 3;
                        break;
                    case 2:
                        cableToCut = 2;
                        break;
                    case 3:
                        cableToCut = 5;
                        break; 
                }
                break;
            }

            SetCableTags(cableToCut);

    }

    public int RandomizeCables()
    {
        cables.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject cable = transform.GetChild(i).gameObject;
            cable.SetActive(false);
            cables.Add(cable);
        }

        int maxCables = Mathf.Min(5, cables.Count);
        int minCables = Mathf.Min(3, maxCables);
        int cableCount = Random.Range(minCables, maxCables + 1);

        for (int i = cables.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            GameObject cable = cables[i];
            cables[i] = cables[randomIndex];
            cables[randomIndex] = cable;
        }

        for (int i = 0; i < cableCount; i++)
        {
            cables[i].SetActive(true);
        }

        for (int i = cableCount; i < cables.Count; i++)
        {
            Destroy(cables[i]);
        }

        return cableCount;
    }

    private void SetCableTags(int cableToCut)
    {
        cables.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject cable = transform.GetChild(i).gameObject;
            if (cable.activeSelf)
            {
                cables.Add(cable);
                cable.tag = "WrongCable";
            }
        }

        if (cableToCut < 1 || cableToCut > cables.Count)
        {
            Debug.LogError("The selected cable index is outside the cables list.", this);
            return;
        }
        Debug.Log("The correct cable to cut is: " + cableToCut);
        Debug.Log(cables[0].name);
        Debug.Log(cables[1].name);
        Debug.Log(cables[2].name);
        cables[cableToCut - 1].tag = "CorrectCable";
    }
}
