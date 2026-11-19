using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD uGUI das duas baterias por jogador (substitui o IMGUI antigo).</summary>
public class GameHud : MonoBehaviour
{
    public Image cargaP1;
    public Image cargaP2;
    public Text textoP1;
    public Text textoP2;

    private Battery bateriaP1;
    private Battery bateriaP2;

    private void Awake()
    {
        foreach (Battery b in FindObjectsByType<Battery>(FindObjectsInactive.Include))
        {
            if (b.PlayerNumber == 1) bateriaP1 = b;
            else if (b.PlayerNumber == 2) bateriaP2 = b;
        }
    }

    private void Update()
    {
        Atualizar(cargaP1, textoP1, bateriaP1, "P1");
        Atualizar(cargaP2, textoP2, bateriaP2, "P2");
    }

    private static void Atualizar(Image barra, Text texto, Battery bateria, string nome)
    {
        if (bateria == null) return;
        float carga = Mathf.Clamp01(bateria.Charge);
        if (barra != null) barra.fillAmount = carga;
        if (texto != null)
            texto.text = carga <= 0.15f ? nome + " · 0%" : nome + " · " + Mathf.RoundToInt(carga * 100f) + "%";
    }
}
