using UnityEngine;

/// <summary>Movimento ambiente discreto, exclusivo da câmara do menu.</summary>
public class MenuAmbientMotion : MonoBehaviour
{
    public const string ReducedMotionKey = "MenuReducedMotion";
    public static bool ReducedMotion => PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
    [SerializeField] private bool reduceMotion;
    [SerializeField] private Light accentLight;

    private Quaternion initialRotation;
    private float initialIntensity;

    private void OnEnable()
    {
        initialRotation = transform.localRotation;
        if (accentLight != null) initialIntensity = accentLight.intensity;
    }

    private void Update()
    {
        float movement = Wave(Time.unscaledTime, reduceMotion || ReducedMotion);
        transform.localRotation = initialRotation * Quaternion.Euler(0f, movement * 0.45f, 0f);
        if (accentLight != null)
            accentLight.intensity = initialIntensity * (1f + movement * 0.06f);
    }

    private void OnDisable()
    {
        transform.localRotation = initialRotation;
        if (accentLight != null) accentLight.intensity = initialIntensity;
    }

    public static float Wave(float time, bool reduced)
    {
        return reduced ? 0f : Mathf.Sin(time * 0.22f);
    }
}
