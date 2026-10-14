using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PuzzleKeypad : MonoBehaviour
{
    [Header("Interface UI")]
    public GameObject painelKeypad;
    public TMP_Text textoDisplay; // Ou Text se usares o sistema clássico

    [Header("Configuração do Código")]
    public string codigoCorreto = "4281";
    public int limiteDigitos = 4;

    [Header("Interação")]
    public float distanciaInteracao = 4f;
    [Tooltip("Arrasta o jogador que vai interagir com este terminal")]
    public Transform jogadorInterativo;

    [Header("Detonação / Feedback")]
    public ExplosaoBomba scriptExplosao;

    private string entradaAtual = "";
    private bool painelAberto = false;
    private bool moduloResolvido = false;

    void Start()
    {
        if (painelKeypad != null)
            painelKeypad.SetActive(false);

        AtualizarDisplay();
    }

    void Update()
    {
        if (moduloResolvido || jogadorInterativo == null) return;

        // Se o painel estiver aberto e carregar em B no comando (ou Esc no teclado) -> Fecha
#if ENABLE_INPUT_SYSTEM
        if (painelAberto)
        {
            if ((Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                FecharKeypad();
                return;
            }
        }
#endif

        float dist = Vector3.Distance(transform.position, jogadorInterativo.position);

        bool apertouInteragir = false;
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) apertouInteragir = true;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) apertouInteragir = true;
#endif

        if (dist <= distanciaInteracao && apertouInteragir && !painelAberto)
        {
            AbrirKeypad();
        }
    }

    public void InserirDigito(string digito)
    {
        if (moduloResolvido) return;

        if (entradaAtual.Length < limiteDigitos)
        {
            entradaAtual += digito;
            AtualizarDisplay();
        }
    }

    public void Limpar()
    {
        entradaAtual = "";
        AtualizarDisplay();
    }

    public void Submeter()
    {
        if (entradaAtual == codigoCorreto)
        {
            textoDisplay.text = "<color=green>ACESSO OK</color>";
            moduloResolvido = true;
            Debug.Log("<color=green>[KEYPAD]</color> Código correto! Módulo desativado.");
            Invoke(nameof(FecharKeypad), 1.2f);
        }
        else
        {
            textoDisplay.text = "<color=red>ERRO</color>";
            Debug.Log("<color=red>[KEYPAD]</color> Código incorreto!");

            if (scriptExplosao != null)
            {
                scriptExplosao.Detonar();
                FecharKeypad();
            }
            else
            {
                Invoke(nameof(Limpar), 0.8f);
            }
        }
    }

    private void AtualizarDisplay()
    {
        if (textoDisplay != null)
        {
            if (string.IsNullOrEmpty(entradaAtual))
                textoDisplay.text = "_ _ _ _";
            else
                textoDisplay.text = entradaAtual;
        }
    }

    public void AbrirKeypad()
    {
        painelAberto = true;
        painelKeypad.SetActive(true);
        Limpar();
    }

    public void FecharKeypad()
    {
        painelAberto = false;
        painelKeypad.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}   