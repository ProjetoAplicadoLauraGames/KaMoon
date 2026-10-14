using UnityEngine;
using TMPro;

public class TimerScript : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText1;
    [SerializeField] private TMP_Text timerText2;
    public float startTime = 60f;
    private float currentTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentTime = startTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText1.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            timerText2.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

    }
}
