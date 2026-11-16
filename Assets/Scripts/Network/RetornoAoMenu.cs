using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Volta ao menu sem duplicar o NetworkManager.
///
/// O NetworkManager do Mirror passa para DontDestroyOnLoad, por isso recarregar o
/// StartMenu criava uma segunda cópia ("Multiple NetworkManagers detected"). Aqui a
/// rede é parada, o manager antigo é destruído e só depois a cena é carregada — a
/// cópia da cena passa a ser a única. Os componentes de rede instalados em runtime
/// (NetMatch, LobbyDiscovery, MatchGuard) são reinstalados na instância nova.
///
/// Corre num objeto próprio porque destruir o manager mataria uma corrotina que
/// vivesse nele.
/// </summary>
public class RetornoAoMenu : MonoBehaviour
{
    public static void Iniciar(string cenaMenu)
    {
        // Uma saída forçada (ex.: MatchGuard) pode ocorrer com a pausa aberta.
        PauseMenu.ResetarEstado();

        var objeto = new GameObject("KaMoonRetornoAoMenu");
        DontDestroyOnLoad(objeto);
        objeto.AddComponent<RetornoAoMenu>().StartCoroutine(Voltar(cenaMenu));
    }

    private static IEnumerator Voltar(string cena)
    {
        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active) NetworkManager.singleton.StopHost();
            else if (NetworkClient.active) NetworkManager.singleton.StopClient();
        }

        yield return null;

        if (NetworkManager.singleton != null)
            Destroy(NetworkManager.singleton.gameObject);

        yield return null;

        SceneManager.LoadScene(cena);

        yield return null;

        NetMatch.InstalarNoManager();

        foreach (RetornoAoMenu objeto in FindObjectsByType<RetornoAoMenu>(FindObjectsSortMode.None))
            Destroy(objeto.gameObject);
    }
}
