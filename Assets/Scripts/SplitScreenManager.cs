using UnityEngine;

public class SplitScreenManager : MonoBehaviour
{
    public enum SplitLayout { Horizontal, Vertical }

    [Header("Layout")]
    [SerializeField] private SplitLayout layout = SplitLayout.Vertical;

    [Header("Referências (auto se vazio)")]
    [SerializeField] private Camera cameraP1;
    [SerializeField] private Camera cameraP2;

    [Header("Identificação")]
    [SerializeField] private bool showPlayerLabels = true;
    [SerializeField] private string labelP1 = "PLAYER 1";
    [SerializeField] private string labelP2 = "PLAYER 2";

    private void Awake()
    {
        ResolveCameras();
        ApplyLayout();
        FixAudioListeners();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Permite ver o layout sem dar Play, se as refs já estiverem postas.
        if (cameraP1 != null && cameraP2 != null)
            ApplyLayout();
    }
#endif

    private void ResolveCameras()
    {
        if (cameraP1 == null)
        {
            GameObject p1 = GameObject.Find("Player");
            if (p1 != null)
                cameraP1 = p1.GetComponentInChildren<Camera>();
            if (cameraP1 == null)
            {
                GameObject mainCam = GameObject.Find("Main Camera");
                if (mainCam != null)
                    cameraP1 = mainCam.GetComponent<Camera>();
            }
        }

        if (cameraP2 == null)
        {
            GameObject p2 = GameObject.Find("Player2");
            if (p2 != null)
                cameraP2 = p2.GetComponentInChildren<Camera>();
            if (cameraP2 == null)
            {
                GameObject cam2 = GameObject.Find("CameraP2");
                if (cam2 != null)
                    cameraP2 = cam2.GetComponent<Camera>();
            }
        }
    }

    private void ApplyLayout()
    {
        if (cameraP1 == null || cameraP2 == null)
        {
            Debug.LogWarning("[SplitScreen] P1 ou P2 em falta. Esperado Player/Main Camera e Player2/CameraP2.");
            return;
        }

        cameraP1.enabled = true;
        cameraP2.enabled = true;

        if (layout == SplitLayout.Horizontal)
        {
            // P1 cima, P2 baixo
            cameraP1.rect = new Rect(0f, 0.5f, 1f, 0.5f);
            cameraP2.rect = new Rect(0f, 0f, 1f, 0.5f);
        }
        else
        {
            // P1 esquerda, P2 direita
            cameraP1.rect = new Rect(0f, 0f, 0.5f, 1f);
            cameraP2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        }
    }

    private void FixAudioListeners()
    {
        // Unity dá warning com 2 AudioListeners. Mantém só o do P1.
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (listeners.Length <= 1)
            return;

        AudioListener keep = null;
        if (cameraP1 != null)
            keep = cameraP1.GetComponentInChildren<AudioListener>();
        if (keep == null)
            keep = listeners[0];

        foreach (AudioListener al in listeners)
        {
            if (al != keep)
                al.enabled = false;
        }
    }

    public void SetLayout(SplitLayout newLayout)
    {
        layout = newLayout;
        ApplyLayout();
    }

    private void OnGUI()
    {
        if (!showPlayerLabels)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        // Sombra simples para legibilidade
        GUIStyle shadow = new GUIStyle(style)
        {
            normal = { textColor = Color.black }
        };

        Rect r1, r2;
        if (layout == SplitLayout.Horizontal)
        {
            r1 = new Rect(0f, 10f, Screen.width, Screen.height * 0.5f);
            r2 = new Rect(0f, Screen.height * 0.5f + 10f, Screen.width, Screen.height * 0.5f);
        }
        else
        {
            r1 = new Rect(0f, 10f, Screen.width * 0.5f, 40f);
            r2 = new Rect(Screen.width * 0.5f, 10f, Screen.width * 0.5f, 40f);
        }

        // Linha divisória
        Color old = GUI.color;
        GUI.color = Color.black;
        if (layout == SplitLayout.Horizontal)
            GUI.DrawTexture(new Rect(0f, Screen.height * 0.5f - 1f, Screen.width, 2f), Texture2D.whiteTexture);
        else
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 1f, 0f, 2f, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(r1.x + 2f, r1.y + 2f, r1.width, r1.height), labelP1, shadow);
        GUI.Label(r1, labelP1, style);
        GUI.Label(new Rect(r2.x + 2f, r2.y + 2f, r2.width, r2.height), labelP2, shadow);
        GUI.Label(r2, labelP2, style);
    }
}
