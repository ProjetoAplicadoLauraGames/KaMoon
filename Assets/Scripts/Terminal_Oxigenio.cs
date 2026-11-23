using System.Collections;
using UnityEngine;

public class BotaoTerminalOxigenio : MonoBehaviour, IInteragivel
{
    [Header("Configuração do Terminal")]
    [Tooltip("1 = Terminal do Player 1 | 2 = Terminal do Player 2")]
    public int ladoJogador = 1;
    public EventoOxigenio gestorOxigenio;

    [Header("Feedback Visual Opcional (Luz / Ecrã do Terminal)")]
    public Renderer indicadorVisual;
    public Color corNormal = Color.cyan;
    public Color corAlarme = Color.red;
    public Color corPulsoClique = Color.green;

    private Coroutine pulsoCoroutine;

    void Start()
    {
        if (gestorOxigenio == null)
            gestorOxigenio = FindFirstObjectByType<EventoOxigenio>();

        AtualizarEstadoTerminal(false, false);
    }

    public void Interagir()
    {
        if (gestorOxigenio == null || !gestorOxigenio.AlarmeAtivo)
        {
            Debug.Log($"[Terminal Oxigénio P{ladoJogador}] Nenhum alarme ativo neste momento.");
            return;
        }

        gestorOxigenio.RegistarCliqueJogador(ladoJogador);

        // Pisca a verde durante a janela de tolerância para dar feedback ao jogador
        if (pulsoCoroutine != null) StopCoroutine(pulsoCoroutine);
        pulsoCoroutine = StartCoroutine(PulsoFeedback());
    }

    private IEnumerator PulsoFeedback()
    {
        AplicarCor(corPulsoClique);
        float espera = gestorOxigenio != null ? gestorOxigenio.toleranciaSincronizacao : 1.5f;
        yield return new WaitForSeconds(espera);

        if (gestorOxigenio != null && gestorOxigenio.AlarmeAtivo)
            AplicarCor(corAlarme);
        else
            AplicarCor(corNormal);
    }

    public void AtualizarEstadoTerminal(bool emAlarme, bool estabilizado)
    {
        if (pulsoCoroutine != null)
        {
            StopCoroutine(pulsoCoroutine);
            pulsoCoroutine = null;
        }

        AplicarCor(emAlarme ? corAlarme : corNormal);
    }

    private void AplicarCor(Color cor)
    {
        if (indicadorVisual == null) return;
        Material mat = indicadorVisual.material;
        mat.color = cor;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", cor);

        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", cor * 2f);
    }
}