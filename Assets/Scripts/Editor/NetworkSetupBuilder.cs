#if UNITY_EDITOR
using kcp2k;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta a infraestrutura de rede na StartMenu: prefab do jogador de lobby e o NetworkManager
/// com KcpTransport. Segue o padrao dos outros builders: nao guarda a cena automaticamente.
/// </summary>
public static class NetworkSetupBuilder
{
    private const string ScenePath = "Assets/Scenes/StartMenu.unity";
    private const string PastaPrefabs = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/LobbyPlayer.prefab";
    private const string NomeManager = "NetworkManager";

    [MenuItem("Tools/KaMoon/Build Networking")]
    public static void Build()
    {
        var cena = SceneManager.GetActiveScene();
        if (Application.isPlaying || cena.path != ScenePath)
        {
            Debug.LogError("Abre Assets/Scenes/StartMenu.unity e sai do Play Mode antes de construir a rede.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int grupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build KaMoon Networking");

        GameObject prefab = GarantirPrefabJogador();
        GameObject manager = GarantirNetworkManager();

        var transporte = manager.GetComponent<KcpTransport>();
        if (transporte != null)
        {
            Undo.RecordObject(transporte, "Configure transport");
            transporte.port = LobbyRules.Porta;
            EditorUtility.SetDirty(transporte);
        }

        var componente = manager.GetComponent<NetworkManager>();
        Undo.RecordObject(componente, "Configure network manager");
        componente.transport = transporte;
        componente.playerPrefab = prefab;
        componente.maxConnections = LobbyRules.MaxPlayers;
        componente.dontDestroyOnLoad = true;
        componente.offlineScene = "";
        componente.onlineScene = "";
        EditorUtility.SetDirty(componente);

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(cena);
        Debug.Log("Rede configurada em StartMenu (KCP porta 7777, maximo 2 jogadores). Guarda a cena.");
    }

    private static GameObject GarantirPrefabJogador()
    {
        if (!AssetDatabase.IsValidFolder(PastaPrefabs))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var existente = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existente != null)
        {
            if (existente.TryGetComponent(out LobbyPlayer _)) return existente;

            GameObject conteudo = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (!conteudo.TryGetComponent(out NetworkIdentity _)) conteudo.AddComponent<NetworkIdentity>();
            conteudo.AddComponent<LobbyPlayer>();
            PrefabUtility.SaveAsPrefabAsset(conteudo, PrefabPath);
            PrefabUtility.UnloadPrefabContents(conteudo);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        var raiz = new GameObject("LobbyPlayer", typeof(NetworkIdentity), typeof(LobbyPlayer));
        var salvo = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabPath);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        return salvo;
    }

    private static GameObject GarantirNetworkManager()
    {
        var existente = Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
        if (existente != null)
        {
            if (!existente.TryGetComponent(out KcpTransport _))
                Undo.AddComponent<KcpTransport>(existente.gameObject);
            return existente.gameObject;
        }

        var manager = new GameObject(NomeManager);
        Undo.RegisterCreatedObjectUndo(manager, "Create NetworkManager");

        // O transporte tem de existir antes do NetworkManager para ele o encontrar sozinho.
        Undo.AddComponent<KcpTransport>(manager).port = LobbyRules.Porta;
        var componente = Undo.AddComponent<NetworkManager>(manager);
        componente.transport = manager.GetComponent<KcpTransport>();
        return manager;
    }
}
#endif
