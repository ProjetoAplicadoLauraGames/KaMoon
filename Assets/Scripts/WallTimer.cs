using TMPro;
using UnityEngine;

/// <summary>
/// Relogio de parede no vidro divisor: duas caras que leem o relogio unico.
/// Pisca vermelho abaixo de 60 s. A derrota e decidida pelo GameTimer/NetMatch.
/// </summary>
public class WallTimer : MonoBehaviour
{
    public TMP_Text[] displays;

    private void Update()
    {
        string texto = GameTimer.Formatar(GameTimer.TempoRestante);

        bool aceso = GameTimer.EmAlerta && Mathf.Sin(Time.unscaledTime * 8f) > 0f;
        Color cor = aceso ? new Color(1f, 0.2f, 0.2f) : new Color(0.87f, 0.94f, 0.91f);

        if (displays != null)
            foreach (TMP_Text t in displays)
            {
                if (t == null) continue;
                t.text = texto;
                t.color = cor;
            }
    }
}
