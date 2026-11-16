using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Menu de pausa aberto por ESC durante a partida. Corre antes dos puzzles para
/// decidir no mesmo frame se o ESC fecha um painel modal ou abre a pausa.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PauseMenu : MonoBehaviour
{
    public static bool EmPausa { get; private set; }
    public static bool HaModalAberto => modaisAbertos > 0;

    private static int modaisAbertos;

    [Header("Painéis")]
    public GameObject painelPausa;
    public GameObject painelSettings;

    [Header("Botões")]
    public Button botaoRetomar;
    public Button botaoSettings;
    public Button botaoSair;
    public Button botaoVoltar;

    [Header("Definições")]
    public Slider sliderSensibilidade;
    public TMP_Text textoSensibilidade;
    public Slider sliderVolume;
    public TMP_Text textoVolume;
    public Toggle toggleSilenciar;

    private readonly List<Behaviour> entradasDesativadas = new List<Behaviour>();
    private GameObject focoAnterior;

    private const float SensibilidadeRatoDefault = 0.8f;
    private const float SensibilidadeComandoDefault = 2.2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimparEstado() => ResetarEstado();

    public static void ResetarEstado()
    {
        modaisAbertos = 0;
        EmPausa = false;
    }

    public static void NotificarModal(bool aberto)
    {
        modaisAbertos = Mathf.Max(0, modaisAbertos + (aberto ? 1 : -1));
    }

    private void Awake()
    {
        if (botaoRetomar != null) botaoRetomar.onClick.AddListener(Retomar);
        if (botaoSettings != null) botaoSettings.onClick.AddListener(AbrirSettings);
        if (botaoSair != null) botaoSair.onClick.AddListener(Sair);
        if (botaoVoltar != null) botaoVoltar.onClick.AddListener(FecharSettings);
        if (sliderSensibilidade != null) sliderSensibilidade.onValueChanged.AddListener(AplicarSensibilidade);
        if (sliderVolume != null) sliderVolume.onValueChanged.AddListener(AplicarVolume);
        if (toggleSilenciar != null) toggleSilenciar.onValueChanged.AddListener(OnSilenciarChanged);
    }

    private void OnDestroy()
    {
        if (botaoRetomar != null) botaoRetomar.onClick.RemoveListener(Retomar);
        if (botaoSettings != null) botaoSettings.onClick.RemoveListener(AbrirSettings);
        if (botaoSair != null) botaoSair.onClick.RemoveListener(Sair);
        if (botaoVoltar != null) botaoVoltar.onClick.RemoveListener(FecharSettings);
        if (sliderSensibilidade != null) sliderSensibilidade.onValueChanged.RemoveListener(AplicarSensibilidade);
        if (sliderVolume != null) sliderVolume.onValueChanged.RemoveListener(AplicarVolume);
        if (toggleSilenciar != null) toggleSilenciar.onValueChanged.RemoveListener(OnSilenciarChanged);
    }

    private void Update()
    {
        if (!EmPausa)
        {
            if (HaModalAberto || !EscPressionado()) return;
            AbrirPausa();
            return;
        }

        if (!EscPressionado()) return;

        if (painelSettings != null && painelSettings.activeSelf)
            FecharSettings();
        else
            Retomar();
    }

    private static bool EscPressionado()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void AbrirPausa()
    {
        if (EmPausa) return;

        EmPausa = true;
        focoAnterior = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        painelPausa?.SetActive(true);
        painelSettings?.SetActive(false);

        // Num cliente o servidor continua a correr: only o host e o jogo local congelam.
        if (!GameSession.multiplayer || NetworkServer.active)
            Time.timeScale = 0f;

        DesativarEntradas();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CarregarDefinicoes();
        Focar(botaoRetomar);
    }

    public void Retomar()
    {
        if (!EmPausa) return;

        Time.timeScale = 1f;
        RestaurarEntradas();
        painelSettings?.SetActive(false);
        painelPausa?.SetActive(false);
        EmPausa = false;

        foreach (CapsuleMovement capsule in FindObjectsByType<CapsuleMovement>())
            capsule.AplicarCursor();

        if (focoAnterior != null && focoAnterior.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(focoAnterior);
    }

    public void AbrirSettings()
    {
        if (!EmPausa) return;

        painelPausa?.SetActive(false);
        painelSettings?.SetActive(true);
        CarregarDefinicoes();
        Focar(botaoVoltar);
    }

    public void FecharSettings()
    {
        if (!EmPausa) return;

        painelSettings?.SetActive(false);
        painelPausa?.SetActive(true);
        Focar(botaoSettings);
    }

    public void Sair()
    {
        if (!EmPausa) return;

        ModeMenu.AbrirLobbyAoIniciar = GameSession.multiplayer;
        Time.timeScale = 1f;
        RestaurarEntradas();
        painelSettings?.SetActive(false);
        painelPausa?.SetActive(false);
        EmPausa = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RetornoAoMenu.Iniciar("StartMenu");
    }

    private void CarregarDefinicoes()
    {
        float sensibilidade = Mathf.Clamp(PlayerPrefs.GetFloat("MouseSensitivity", SensibilidadeRatoDefault), 0.1f, 2f);
        if (sliderSensibilidade != null)
            sliderSensibilidade.SetValueWithoutNotify(sensibilidade);
        if (textoSensibilidade != null)
            textoSensibilidade.text = sensibilidade.ToString("0.00");

        float volume = OptionsMenu.VolumeAtual;
        if (sliderVolume != null)
            sliderVolume.SetValueWithoutNotify(volume);
        if (textoVolume != null)
            textoVolume.text = Mathf.RoundToInt(volume * 100f) + "%";
        if (toggleSilenciar != null)
            toggleSilenciar.SetIsOnWithoutNotify(OptionsMenu.SomSilenciado);
    }

    public void AplicarSensibilidade(float valor)
    {
        valor = Mathf.Clamp(valor, 0.1f, 2f);
        // Mantém a proporção entre os defaults de rato e comando do CapsuleMovement.
        float comando = valor * (SensibilidadeComandoDefault / SensibilidadeRatoDefault);
        PlayerPrefs.SetFloat("MouseSensitivity", valor);
        PlayerPrefs.SetFloat("GamepadSensitivity", comando);
        PlayerPrefs.Save();

        foreach (CapsuleMovement capsule in FindObjectsByType<CapsuleMovement>())
        {
            capsule.MouseSensitivity = valor;
            capsule.GamepadSensitivity = comando;
        }

        if (textoSensibilidade != null)
            textoSensibilidade.text = valor.ToString("0.00");
    }

    public void AplicarVolume(float valor)
    {
        OptionsMenu.AplicarSom(valor, toggleSilenciar != null && toggleSilenciar.isOn);
        if (textoVolume != null)
            textoVolume.text = Mathf.RoundToInt(Mathf.Clamp01(valor) * 100f) + "%";
    }

    private void OnSilenciarChanged(bool silenciado)
    {
        OptionsMenu.AplicarSom(OptionsMenu.VolumeAtual, silenciado);
    }

    private void DesativarEntradas()
    {
        entradasDesativadas.Clear();
        Desativar<CapsuleMovement>();
        Desativar<Hotbar>();
        Desativar<PlayerInteraction>();
        Desativar<GestorLanternas>();
        Desativar<LocalVoiceChat>();
        // Também leem E/Espaço/A diretamente: sem isto abriam puzzles por trás do menu.
        Desativar<PuzzleKeypad>();
        Desativar<PuzzleCabosTV>();
        Desativar<LeitorPistaArmario>();
        Desativar<PuzzleEstante3D>();
        Desativar<InteracaoEstante>();
        Desativar<InterruptorComutador>();
    }

    private void Desativar<T>() where T : Behaviour
    {
        foreach (T componente in FindObjectsByType<T>())
        {
            if (!componente.enabled) continue;
            componente.enabled = false;
            entradasDesativadas.Add(componente);
        }
    }

    private void RestaurarEntradas()
    {
        foreach (Behaviour componente in entradasDesativadas)
        {
            if (componente != null)
                componente.enabled = true;
        }
        entradasDesativadas.Clear();
    }

    private static void Focar(Button botao)
    {
        if (botao != null && EventSystem.current != null)
            botao.Select();
    }
}
