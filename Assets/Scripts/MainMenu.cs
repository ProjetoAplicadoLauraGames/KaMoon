using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Menus")]
    [Tooltip("Painel do menu de opcoes (Criado por Tools/KaMoon/Build Options Menu)")]
    public GameObject optionsMenuPanel;
    public Button firstSelectedButton;
    public GameObject briefingPanel;
    public Button briefingFirstButton;
    public GameObject modePanel;
    public Button modeFirstButton;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SelecionarPrimeiroBotao();
    }

    private void Start()
    {
        // O EventSystem pode inicializar depois do OnEnable do menu.
        SelecionarPrimeiroBotao();
    }

    private void SelecionarPrimeiroBotao()
    {
        if (EventSystem.current != null && firstSelectedButton != null)
            firstSelectedButton.Select();
    }

    public void ChangeScene()
    {
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OpenOptions()
    {
        if (optionsMenuPanel == null) return;

        optionsMenuPanel.SetActive(true);
        gameObject.SetActive(false);
    }

    public void OpenBriefing()
    {
        if (briefingPanel == null) return;
        gameObject.SetActive(false);
        briefingPanel.SetActive(true);
        if (briefingFirstButton != null && EventSystem.current != null)
            briefingFirstButton.Select();
    }

    public void OpenMode()
    {
        if (modePanel == null)
        {
            OpenBriefing();
            return;
        }

        gameObject.SetActive(false);
        modePanel.SetActive(true);
        if (modeFirstButton != null && EventSystem.current != null)
            modeFirstButton.Select();
    }

    public void CloseBriefing()
    {
        if (briefingPanel == null) return;
        briefingPanel.SetActive(false);
        gameObject.SetActive(true);
    }

    public void QuitApplication()
    {
        PlayerPrefs.Save();
        Debug.Log("Quit");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
