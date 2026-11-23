using UnityEngine;

public class RepararTimerLado : MonoBehaviour, IInteragivel
{
    [Header("Identificação deste Lado")]
    [Tooltip("1 = Lado do Player 1 | 2 = Lado do Player 2")]
    public int ladoJogador = 1;
    public EventoCurtoCircuitoTimer gestorEvento;

    [Header("Requisito da Chave de Fendas")]
    [Tooltip("Deve ser exatamente igual ao itemId do HotbarItem da screwdriver")]
    public string idScrewdriver = "screwdriver";
    [Tooltip("Se marcado, a screwdriver tem de estar selecionada na mão. Se desmarcado, basta tê-la na hotbar.")]
    public bool exigirNaMao = true;

    [Header("Feedback Visual Opcional (LED / Luz do Painel)")]
    public Renderer ledIndicador;
    public Color corNormal = Color.green;
    public Color corAvariado = Color.red;
    public Color corLadoConsertado = Color.yellow;

    private Hotbar hotbarDono;
    private bool esteLadoJaReparado = false;

    void Start()
    {
        if (gestorEvento == null)
            gestorEvento = FindFirstObjectByType<EventoCurtoCircuitoTimer>();

        // Encontra a Hotbar do respetivo jogador
        foreach (Hotbar hb in FindObjectsByType<Hotbar>(FindObjectsSortMode.None))
        {
            if (hb.PlayerNumber == ladoJogador)
            {
                hotbarDono = hb;
                break;
            }
        }

        AtualizarVisualEstado(false, false);
    }

    public void Interagir()
    {
        // Só interage se o timer estiver mesmo em curto-circuito e este lado ainda não estiver consertado
        if (!GameTimer.EmCurtoCircuito || esteLadoJaReparado) return;

        if (!TemScrewdriver())
        {
            Debug.Log($"<color=orange>[TIMER P{ladoJogador}] Precisas da chave de fendas ({idScrewdriver}) equipada para consertar!</color>");
            return;
        }

        esteLadoJaReparado = true;
        if (gestorEvento != null)
        {
            gestorEvento.RegistarReparacaoLado(ladoJogador);
        }
    }

    private bool TemScrewdriver()
    {
        if (hotbarDono == null) return false;

        if (exigirNaMao)
            return hotbarDono.SelectedItemId == idScrewdriver;
        else
            return hotbarDono.HasItem(idScrewdriver);
    }

    public void AtualizarVisualEstado(bool emCurto, bool reparado)
    {
        esteLadoJaReparado = reparado;

        if (ledIndicador == null) return;

        Color corFinal = !emCurto ? corNormal : (reparado ? corLadoConsertado : corAvariado);
        ledIndicador.material.color = corFinal;
        if (ledIndicador.material.HasProperty("_BaseColor"))
            ledIndicador.material.SetColor("_BaseColor", corFinal);
    }
}