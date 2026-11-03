using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Idioma e som")]
    public Dropdown languageDropdown;
    public Slider volumeSlider;
    public TMP_Text volumeValueText;
    public Toggle muteToggle;
    public Toggle reduceMotionToggle;

    [Header("Ecra")]
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;

    [Header("Volta ao menu principal")]
    public Button backButton;
    public GameObject menuPrincipal;
    public Button returnFocusButton;

    private const string KeyVolume = "MasterVolume";
    private const string KeyMuted = "MasterMuted";
    private const string KeyFullscreen = "Fullscreen";
    private const string KeyResolution = "ResolutionIndex";
    private const string KeyWidth = "ResolutionWidth";
    private const string KeyHeight = "ResolutionHeight";
    private const string KeyQuality = "QualityLevel";

    private Resolution[] resolusoesDisponiveis;

    // Restaura as definicoes mesmo que o jogador nunca abra o painel de opcoes.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AplicarDefinicoesGuardadas()
    {
        AplicarSom();

        if (PlayerPrefs.HasKey(KeyQuality) && QualitySettings.names.Length > 0)
        {
            int nivel = Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality), 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(nivel, true);
        }

        if (PlayerPrefs.HasKey(KeyWidth) || PlayerPrefs.HasKey(KeyResolution) || PlayerPrefs.HasKey(KeyFullscreen))
        {
            var resolucoes = ResolucoesUnicas();
            var resolucao = resolucoes[IndiceResolucaoGuardada(resolucoes)];
            Screen.SetResolution(resolucao.width, resolucao.height, EcraInteiro());
        }
    }

    private void Awake()
    {
        if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (muteToggle != null) muteToggle.onValueChanged.AddListener(OnMuteChanged);
        if (reduceMotionToggle != null) reduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (backButton != null) backButton.onClick.AddListener(FechaOpcoes);
    }

    private void OnDestroy()
    {
        if (languageDropdown != null) languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (muteToggle != null) muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
        if (reduceMotionToggle != null) reduceMotionToggle.onValueChanged.RemoveListener(OnReduceMotionChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        if (backButton != null) backButton.onClick.RemoveListener(FechaOpcoes);
    }

    private void OnEnable()
    {
        CarregarDefinicoes();
        if (EventSystem.current != null && languageDropdown != null)
            languageDropdown.Select();
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    private void CarregarDefinicoes()
    {
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string>(MenuLocalization.LanguageNames));
            languageDropdown.SetValueWithoutNotify(MenuLocalization.CurrentLanguage);
        }

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(VolumeGuardado());
        if (muteToggle != null)
            muteToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(KeyMuted, 0) == 1);
        if (reduceMotionToggle != null)
            reduceMotionToggle.SetIsOnWithoutNotify(MenuAmbientMotion.ReducedMotion);
        AtualizarPercentagemVolume();
        AplicarSom();

        resolusoesDisponiveis = ResolucoesUnicas();
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var nomes = new List<string>();
            foreach (var resolucao in resolusoesDisponiveis)
                nomes.Add(resolucao.width + " x " + resolucao.height);
            resolutionDropdown.AddOptions(nomes);
            resolutionDropdown.SetValueWithoutNotify(IndiceResolucaoGuardada(resolusoesDisponiveis));
        }

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(EcraInteiro());

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            qualityDropdown.interactable = QualitySettings.names.Length > 0;
            if (qualityDropdown.interactable)
            {
                int nivel = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1);
                qualityDropdown.SetValueWithoutNotify(nivel);
            }
        }
    }

    private static Resolution[] ResolucoesUnicas()
    {
        var vistas = new HashSet<(int largura, int altura)>();
        var lista = new List<Resolution>();
        foreach (var resolucao in Screen.resolutions)
        {
            if (vistas.Add((resolucao.width, resolucao.height)))
                lista.Add(resolucao);
        }

        // Inclui o tamanho da janela e funciona quando Screen.resolutions esta vazio (Editor/WebGL).
        int largura = Mathf.Max(1, Screen.width);
        int altura = Mathf.Max(1, Screen.height);
        if (vistas.Add((largura, altura)))
            lista.Add(new Resolution { width = largura, height = altura });

        lista.Sort((a, b) => a.width != b.width ? b.width.CompareTo(a.width) : b.height.CompareTo(a.height));
        return lista.ToArray();
    }

    private static int IndiceResolucaoGuardada(Resolution[] resolucoes)
    {
        if (PlayerPrefs.HasKey(KeyWidth) && PlayerPrefs.HasKey(KeyHeight))
        {
            int largura = PlayerPrefs.GetInt(KeyWidth);
            int altura = PlayerPrefs.GetInt(KeyHeight);
            for (int i = 0; i < resolucoes.Length; i++)
            {
                if (resolucoes[i].width == largura && resolucoes[i].height == altura)
                    return i;
            }
        }
        else if (PlayerPrefs.HasKey(KeyResolution))
        {
            // Compatibilidade com as preferencias da primeira versao do menu.
            return Mathf.Clamp(PlayerPrefs.GetInt(KeyResolution), 0, resolucoes.Length - 1);
        }

        for (int i = 0; i < resolucoes.Length; i++)
        {
            if (resolucoes[i].width == Screen.width && resolucoes[i].height == Screen.height)
                return i;
        }
        return 0;
    }

    private static float VolumeGuardado() => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolume, 1f));
    private static bool EcraInteiro() => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;

    private static void AplicarSom()
    {
        AudioListener.volume = PlayerPrefs.GetInt(KeyMuted, 0) == 1 ? 0f : VolumeGuardado();
    }

    private void AtualizarPercentagemVolume()
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(VolumeGuardado() * 100f) + "%";
    }

    public void FechaOpcoes()
    {
        gameObject.SetActive(false);
        if (menuPrincipal != null)
            menuPrincipal.SetActive(true);
        if (returnFocusButton != null && EventSystem.current != null)
            returnFocusButton.Select();
    }

    private void OnLanguageChanged(int indice)
    {
        MenuLocalization.SetLanguage(indice);
    }

    private void OnVolumeChanged(float valor)
    {
        PlayerPrefs.SetFloat(KeyVolume, Mathf.Clamp01(valor));
        AplicarSom();
        AtualizarPercentagemVolume();
    }

    private void OnMuteChanged(bool silenciar)
    {
        PlayerPrefs.SetInt(KeyMuted, silenciar ? 1 : 0);
        AplicarSom();
    }

    private void OnReduceMotionChanged(bool reduced)
    {
        PlayerPrefs.SetInt(MenuAmbientMotion.ReducedMotionKey, reduced ? 1 : 0);
    }

    private void OnFullscreenChanged(bool ativo)
    {
        PlayerPrefs.SetInt(KeyFullscreen, ativo ? 1 : 0);
        if (resolusoesDisponiveis == null || resolusoesDisponiveis.Length == 0) return;

        AplicarResolucao(resolutionDropdown != null ? resolutionDropdown.value : IndiceResolucaoGuardada(resolusoesDisponiveis));
    }

    private void OnResolutionChanged(int indice)
    {
        AplicarResolucao(indice);
    }

    private void AplicarResolucao(int indice)
    {
        if (resolusoesDisponiveis == null || resolusoesDisponiveis.Length == 0) return;

        indice = Mathf.Clamp(indice, 0, resolusoesDisponiveis.Length - 1);
        var resolucao = resolusoesDisponiveis[indice];
        PlayerPrefs.SetInt(KeyWidth, resolucao.width);
        PlayerPrefs.SetInt(KeyHeight, resolucao.height);
        Screen.SetResolution(resolucao.width, resolucao.height, EcraInteiro());
    }

    private void OnQualityChanged(int nivel)
    {
        if (QualitySettings.names.Length == 0) return;

        nivel = Mathf.Clamp(nivel, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(nivel, true);
        PlayerPrefs.SetInt(KeyQuality, nivel);
    }
}
