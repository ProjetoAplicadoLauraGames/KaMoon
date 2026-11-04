using System.Collections;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public ExplosaoBomba Bomba;
    public GameObject GameOverUI;

    public void GameOver()
    {
        Bomba.Detonar();
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(3f);
        GameOverUI.SetActive(true);
        // 3. (Opcional) Congela o tempo do jogo para parar animações, física e inimigos
        Time.timeScale = 0f;

        // 4. Libera o cursor do mouse para o jogador poder clicar nos botões do menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
