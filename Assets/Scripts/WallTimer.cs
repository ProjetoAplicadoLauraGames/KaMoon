using TMPro;
using UnityEngine;

/// <summary>
/// Relogio de parede no vidro divisor: duas caras, uma por jogador.
/// Cada cara e orientada para a camara do seu jogador em LateUpdate, para o
/// texto nunca aparecer ao contrario. No ultimo minuto faz um pulso suave a vermelho.
/// </summary>
public class WallTimer : MonoBehaviour
{
    public TMP_Text[] displays;

    [Header("Camaras (opcional - detetadas pelo nome se vazias)")]
    [SerializeField] private Camera cameraP1;
    [SerializeField] private Camera cameraP2;

    private Color[] coresNormais;
    private Vector3[] escalasNormais;

    private void Start()
    {
        int total = displays != null ? displays.Length : 0;
        coresNormais = new Color[total];
        escalasNormais = new Vector3[total];

        for (int i = 0; i < total; i++)
        {
            if (displays[i] == null) continue;
            coresNormais[i] = displays[i].color;
            escalasNormais[i] = displays[i].rectTransform.localScale;
        }

        if (cameraP1 == null) cameraP1 = CamaraDe("Player", "Main Camera");
        if (cameraP2 == null) cameraP2 = CamaraDe("Player2", "CameraP2");
    }

    private static Camera CamaraDe(params string[] nomes)
    {
        foreach (string nome in nomes)
        {
            GameObject obj = GameObject.Find(nome);
            if (obj == null) continue;

            Camera cam = obj.GetComponent<Camera>();
            if (cam == null) cam = obj.GetComponentInChildren<Camera>(true);
            if (cam != null) return cam;
        }
        return null;
    }

    private void Update()
    {
        string texto = GameTimer.Formatar(GameTimer.TempoRestante);

        if (displays == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            displays[i].text = texto;
            GameTimer.AplicarAlerta(displays[i], coresNormais[i], escalasNormais[i]);
        }
    }

    private void LateUpdate()
    {
        if (displays == null) return;

        // Em rede so existe uma camara local; usa-a para ambas as caras.
        Camera segunda = cameraP2 != null ? cameraP2 : cameraP1;
        if (displays.Length > 0) VirarPara(displays[0], cameraP1);
        if (displays.Length > 1) VirarPara(displays[1], segunda);
    }

    private static void VirarPara(TMP_Text texto, Camera camara)
    {
        if (texto == null || camara == null) return;

        Canvas canvas = texto.canvas;
        if (canvas == null) return;

        Vector3 alvo = camara.transform.position - canvas.transform.position;
        alvo.y = 0f;
        if (alvo.sqrMagnitude < 0.0001f) return;

        canvas.transform.rotation = Quaternion.LookRotation(alvo.normalized, Vector3.up);
    }
}
