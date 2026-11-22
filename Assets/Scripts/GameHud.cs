using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD uGUI das duas baterias por jogador (substitui o IMGUI antigo).</summary>
public class GameHud : MonoBehaviour
{
    public Image cargaP1;
    public Image cargaP2;

    private Battery bateriaP1;
    private Battery bateriaP2;

    private void Awake()
    {
        foreach (Battery b in FindObjectsByType<Battery>(FindObjectsInactive.Include))
        {
            if (b.PlayerNumber == 1) bateriaP1 = b;
            else if (b.PlayerNumber == 2) bateriaP2 = b;
        }

        // Em rede cada ecra pertence a um jogador: mostra so a barra dele,
        // ao mesmo modo que a Hotbar esconde a barra alheia.
        if (GameSession.multiplayer)
            EsconderPainel(GameSession.room == 1 ? cargaP2 : cargaP1,
                           GameSession.room == 1 ? "HudP2" : "HudP1");
    }

    /// <summary>Sobe da barra ate ao painel HudP1/HudP2 e desativa-o (inclui o fundo).</summary>
    private static void EsconderPainel(Image barra, string nomePainel)
    {
        if (barra == null) return;

        Transform painel = barra.transform;
        while (painel != null && painel.name != nomePainel)
            painel = painel.parent;

        if (painel != null) painel.gameObject.SetActive(false);
    }

    private void Update()
    {
        Atualizar(cargaP1, bateriaP1);
        Atualizar(cargaP2, bateriaP2);
    }

    private static void Atualizar(Image barra, Battery bateria)
    {
        if (bateria == null || barra == null) return;
        float carga = Mathf.Clamp01(bateria.Charge);
        barra.fillAmount = carga;
        barra.color = carga <= 0.15f
            ? new Color(1f, 0.25f, 0.25f)
            : new Color(0.9f, 0.6f, 0.25f);
    }
}
