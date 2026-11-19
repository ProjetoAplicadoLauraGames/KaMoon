using TMPro;
using UnityEngine;

/// <summary>
/// Relogio de parede no vidro divisor: duas caras que leem o relogio unico.
/// No ultimo minuto faz um pulso suave a vermelho. A derrota e do GameTimer/NetMatch.
/// </summary>
public class WallTimer : MonoBehaviour
{
    public TMP_Text[] displays;

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
}
