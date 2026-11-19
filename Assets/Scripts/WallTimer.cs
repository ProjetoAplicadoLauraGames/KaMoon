using TMPro;
using UnityEngine;

/// <summary>
/// Relógio de parede colocado no vidro divisor: duas caras, uma por jogador.
/// Fonte: servidor em rede; contagem local caso contrário.
/// Abaixo de 60 s pisca vermelho. A zero dispara a derrota uma vez.
/// </summary>
public class WallTimer : MonoBehaviour
{
    public TMP_Text[] displays;
    public float startTime = 180f;

    private float atual;
    private bool terminado;

    private void Start()
    {
        atual = startTime;
    }

    private void Update()
    {
        if (NetMatch.EmPartida) atual = NetMatch.TempoRestante;
        else if (!terminado) atual -= Time.deltaTime;

        float seguro = Mathf.Max(0f, atual);
        string texto = Mathf.FloorToInt(seguro / 60f).ToString("00") + ":" + Mathf.FloorToInt(seguro % 60f).ToString("00");

        bool alerta = seguro <= 60f;
        bool aceso = alerta && Mathf.Sin(Time.unscaledTime * 8f) > 0f;
        Color cor = aceso ? new Color(1f, 0.2f, 0.2f) : new Color(0.87f, 0.94f, 0.91f);

        if (displays != null)
            foreach (TMP_Text t in displays)
            {
                if (t == null) continue;
                t.text = texto;
                t.color = cor;
            }

        if (seguro <= 0f && !terminado)
        {
            terminado = true;
            GameOverManager fim = FindAnyObjectByType<GameOverManager>();
            if (fim != null) fim.GameOver();
        }
    }
}
