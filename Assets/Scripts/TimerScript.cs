using UnityEngine;
using TMPro;

/// <summary>
/// Mostra o relogio unico (GameTimer) nos dois ecras. A contagem, a penalizacao
/// e a derrota pertencem ao GameTimer / NetMatch, nao a este componente.
/// </summary>
public class TimerScript : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText1;
    [SerializeField] private TMP_Text timerText2;

    void Update()
    {
        string texto = GameTimer.Formatar(GameTimer.TempoRestante);
        if (timerText1 != null) timerText1.text = texto;
        if (timerText2 != null) timerText2.text = texto;
    }
}
