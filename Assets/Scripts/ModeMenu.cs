using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Escolha de modo (local/multiplayer) e lobby local sem rede.</summary>
public class ModeMenu : MonoBehaviour
{
    [Header("Paineis")]
    public GameObject modePanel;
    public GameObject lobbyPanel;

    [Header("Lobby")]
    public Button createButton;
    public Button joinButton;
    public Button startButton;
    public TMP_Text lobbyStatus;
    public GameObject roomChoice;
    public Button roomOneButton;
    public Button roomTwoButton;
    public TMP_Text roomOneLabel;
    public TMP_Text roomTwoLabel;
    public Color roomNormalColor = new Color(0.87f, 0.94f, 0.91f);
    public Color roomSelectedColor = new Color(0.9f, 0.6f, 0.25f);

    [Header("Navegacao")]
    public MainMenu mainMenu;
    public Button modeFirstButton;

    private bool salaCriada;
    private int salaEscolhida; // 0 = nenhuma

    private void OnEnable()
    {
        MenuLocalization.LanguageChanged += AplicarEstadoLobby;
        ReporLobby();
    }

    private void OnDisable()
    {
        MenuLocalization.LanguageChanged -= AplicarEstadoLobby;
    }

    public void Local()
    {
        GameSession.multiplayer = false;
        GameSession.room = 0;
        modePanel.SetActive(false);
        if (mainMenu != null) mainMenu.OpenBriefing();
    }

    public void AbrirLobby()
    {
        modePanel.SetActive(false);
        lobbyPanel.SetActive(true);
        ReporLobby();
        Focar(createButton);
    }

    public void CriarSala()
    {
        salaCriada = true;
        AplicarEstadoLobby();
        Focar(roomOneButton);
    }

    public void EscolherSala1()
    {
        EscolherSala(1);
    }

    public void EscolherSala2()
    {
        EscolherSala(2);
    }

    public void Iniciar()
    {
        if (salaEscolhida == 0) return;

        GameSession.multiplayer = true;
        GameSession.room = salaEscolhida;
        lobbyPanel.SetActive(false);
        if (mainMenu != null) mainMenu.OpenBriefing();
    }

    public void VoltarLobby()
    {
        lobbyPanel.SetActive(false);
        modePanel.SetActive(true);
        Focar(modeFirstButton);
    }

    public void Voltar()
    {
        modePanel.SetActive(false);
        if (mainMenu != null) mainMenu.gameObject.SetActive(true);
    }

    private void EscolherSala(int sala)
    {
        salaEscolhida = sala;
        AplicarEstadoLobby();
        Focar(startButton);
    }

    private void ReporLobby()
    {
        salaCriada = false;
        salaEscolhida = 0;
        AplicarEstadoLobby();
    }

    private void AplicarEstadoLobby()
    {
        if (createButton != null) createButton.gameObject.SetActive(!salaCriada);
        if (joinButton != null) joinButton.gameObject.SetActive(!salaCriada);
        if (startButton != null)
        {
            startButton.gameObject.SetActive(salaCriada);
            startButton.interactable = salaEscolhida != 0;
        }
        if (lobbyStatus != null)
        {
            lobbyStatus.gameObject.SetActive(salaCriada);
            lobbyStatus.text = MenuLocalization.GetText(MenuTextKey.LobbyCreated);
        }
        if (roomChoice != null) roomChoice.SetActive(salaCriada);

        PintarSala(roomOneLabel, salaEscolhida == 1);
        PintarSala(roomTwoLabel, salaEscolhida == 2);
    }

    private void PintarSala(TMP_Text rotulo, bool escolhida)
    {
        if (rotulo != null) rotulo.color = escolhida ? roomSelectedColor : roomNormalColor;
    }

    private void Focar(Button botao)
    {
        if (botao != null && EventSystem.current != null) botao.Select();
    }
}
