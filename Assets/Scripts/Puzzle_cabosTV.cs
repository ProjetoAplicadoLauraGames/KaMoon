using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PuzzleCabosTV : MonoBehaviour
{
    [Header("Painel UI")]
    public GameObject painelPuzzle;
    public Button botaoSair;

    [Header("Botoes")]
    public Button[] cabos;

    [Header("Regras")]
    [Tooltip("Indice do cabo certo (0=1º cabo, 1=2º cabo, etc.)")]
    public int indiceCaboCorreto = 1; // Ex: segundo cabo (Azul)

    [Header("Interacao")]
    public float distanciaInteracao = 4f;
    public Transform jogadorP2;

    [Header("Detonacao (Opcional)")]
    public ExplosaoBomba explosaoBomba;

    private bool moduloResolvido = false;

    void Start()
    {
        if (painelPuzzle != null)
            painelPuzzle.SetActive(false);

        for (int i = 0; i < cabos.Length; i++)
        {
            int index = i;
            if (cabos[index] != null)
                cabos[index].onClick.AddListener(() => CortarCabo(index));
        }

        if (botaoSair != null)
            botaoSair.onClick.AddListener(FecharPuzzle);
    }

    void Update()
    {
        if (moduloResolvido || jogadorP2 == null) return;

        float dist = Vector3.Distance(transform.position, jogadorP2.position);

        bool apertouInteragir = false;
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) apertouInteragir = true;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) apertouInteragir = true;
#endif

        if (dist <= distanciaInteracao && apertouInteragir && !painelPuzzle.activeSelf)
        {
            AbrirPuzzle();
        }
    }

    public void AbrirPuzzle()
    {
        painelPuzzle.SetActive(true);
    }

    public void FecharPuzzle()
    {
        painelPuzzle.SetActive(false);
    }

    public void CortarCabo(int index)
    {
        cabos[index].interactable = false;

        if (index == indiceCaboCorreto)
        {
            Debug.Log("<color=green>[PUZZLE]</color> Modulo Desativado!");
            moduloResolvido = true;
            Invoke(nameof(FecharPuzzle), 1.0f);
        }
        else
        {
            Debug.Log("<color=red>[PUZZLE]</color> Cabo Errado!");
            if (explosaoBomba != null)
            {
                explosaoBomba.Detonar();
                FecharPuzzle();
            }
        }
    }
}