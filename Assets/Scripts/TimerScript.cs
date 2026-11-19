using UnityEngine;
using TMPro;

/// <summary>
/// Mostra o relogio unico (GameTimer) nos dois ecras. No ultimo minuto
/// faz o mesmo pulso suave a vermelho do relogio de parede.
/// A contagem, penalizacao e derrota pertencem ao GameTimer / NetMatch.
/// </summary>
public class TimerScript : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText1;
    [SerializeField] private TMP_Text timerText2;

    private Color corNormal1, corNormal2;
    private Vector3 escalaNormal1, escalaNormal2;

    void Start()
    {
        if (timerText1 != null)
        {
            corNormal1 = timerText1.color;
            escalaNormal1 = timerText1.rectTransform.localScale;
        }
        if (timerText2 != null)
        {
            corNormal2 = timerText2.color;
            escalaNormal2 = timerText2.rectTransform.localScale;
        }
    }

    void Update()
    {
        string texto = GameTimer.Formatar(GameTimer.TempoRestante);

        if (timerText1 != null)
        {
            timerText1.text = texto;
            GameTimer.AplicarAlerta(timerText1, corNormal1, escalaNormal1);
        }
        if (timerText2 != null)
        {
            timerText2.text = texto;
            GameTimer.AplicarAlerta(timerText2, corNormal2, escalaNormal2);
        }
    }
}
