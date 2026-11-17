using UnityEngine;

/// <summary>
/// Item apanhável no mundo quando o jogador interage com ele.
/// Uso: prefab do item (ex: lanterna, bateria) + HotbarItem + este script.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HotbarPickup : MonoBehaviour
{
    [Tooltip("Item que entra na hotbar ao apanhar.")]
    [SerializeField] private HotbarItem item;

    [Tooltip("0 = qualquer jogador apanha. 1/2 = só esse jogador.")]
    [SerializeField] private int onlyPlayerNumber = 0;

    [Tooltip("Destrói ao apanhar. OFF = só desativa (para respawn manual).")]
    [SerializeField] private bool destroyOnPickup = true;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    /// <summary>Devolve true se o item entrou na hotbar.</summary>
    public bool TryPickup(Hotbar hotbar)
    {
        if (hotbar == null)
            return false;

        if (onlyPlayerNumber != 0 && hotbar.PlayerNumber != onlyPlayerNumber)
            return false;

        if (item == null)
        {
            Debug.LogWarning("[HotbarPickup] Sem HotbarItem atribuído em " + name);
            return false;
        }

        if (!hotbar.AddItem(item))
            return false;

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        return true;
    }
}
