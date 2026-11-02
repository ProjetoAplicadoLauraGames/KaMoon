using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Controlos")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;

    [Header("Volta ao menu principal")]
    public Button backButton;
    [Tooltip("Arrastar o MainMenu ou o painel inicial para reactivar quando as opcoes fecham")]
    public GameObject menuPrincipal;

    private const string KeyVolume = "MasterVolume";
    private const string KeyResolution = "ResolutionIndex";
    private const string KeyQuality = "QualityLevel";

    private Resolution[] resolusoesDisponiveis;

    void Awake()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (backButton != null) backButton.onClick.AddListener(FechaOpcoes);
    }

    void OnDestroy()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        if (backButton != null) backButton.onClick.RemoveListener(FechaOpcoes);
    }

    void OnEnable()
    {
        CarregarDefinicoes();
    }

    private void CarregarDefinicoes()
    {
        // Volume (afecta o AudioListener da cena; 0..1)
        AudioListener.volume = PlayerPrefs.GetFloat(KeyVolume, 1f);
        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(Mathf.Clamp01(AudioListener.volume));

        // Fullscreen + resolucao
        ResolucoesUnicas();
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var nomes = new List<string>();
            foreach (var r in resolusoesDisponiveis)
                nomes.Add(r.width + " x " + r.height);
            resolutionDropdown.AddOptions(nomes);

            int guardado = PlayerPrefs.GetInt(KeyResolution, IndiceResolucaoAtual());
            int indice = Mathf.Clamp(guardado, 0, resolusoesDisponiveis.Length - 1);
            resolutionDropdown.SetValueWithoutNotify(indice);
            AplicarResolucao(indice);
        }

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        // Qualidade grafica
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));

            int nivel = Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel()),
                                    0, QualitySettings.names.Length - 1);
            qualityDropdown.SetValueWithoutNotify(nivel);
            QualitySettings.SetQualityLevel(nivel, true);
        }
    }

    private int IndiceResolucaoAtual()
    {
        for (int i = 0; i < resolusoesDisponiveis.Length; i++)
        {
            var r = resolusoesDisponiveis[i];
            if (r.width == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
                return i;
        }
        return 0;
    }

    private void ResolucoesUnicas()
    {
        // Remove duplicados de refresh rate para uma lista limpa
        var vistas = new HashSet<(int largura, int altura)>();
        var lista = new List<Resolution>();
        foreach (var r in Screen.resolutions)
        {
            if (vistas.Add((r.width, r.height)))
                lista.Add(r);
        }
        lista.Reverse(); // resolues grandes primeiro
        resolusoesDisponiveis = lista.ToArray();
    }

    public void FechaOpcoes()
    {
        if (menuPrincipal != null)
            menuPrincipal.SetActive(true);
        gameObject.SetActive(false);
    }

    private void OnVolumeChanged(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(KeyVolume, valor);
    }

    private void OnFullscreenChanged(bool ativo)
    {
        Screen.fullScreen = ativo;
    }

    private void OnResolutionChanged(int indice)
    {
        AplicarResolucao(indice);
        PlayerPrefs.SetInt(KeyResolution, indice);
    }

    private void AplicarResolucao(int indice)
    {
        if (resolusoesDisponiveis == null || resolusoesDisponiveis.Length == 0) return;
        indice = Mathf.Clamp(indice, 0, resolusoesDisponiveis.Length - 1);
        var r = resolusoesDisponiveis[indice];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
    }

    private void OnQualityChanged(int nivel)
    {
        QualitySettings.SetQualityLevel(Mathf.Clamp(nivel, 0, QualitySettings.names.Length - 1), true);
        PlayerPrefs.SetInt(KeyQuality, nivel);
    }
}
