using UnityEngine;

/// <summary>
/// Define um item da hotbar (task #21). Para já serve a lanterna;
/// a bateria (task #17) e futuros itens reutilizam este asset.
/// Criar em: Project > Create > KaMoon > Hotbar Item.
/// </summary>
[CreateAssetMenu(fileName = "HotbarItem", menuName = "KaMoon/Hotbar Item")]
public class HotbarItem : ScriptableObject
{
    [Tooltip("Nome mostrado no slot (ex: Lanterna, Bateria).")]
    public string displayName = "Item";

    [Tooltip("Ícone do slot. Opcional no protótipo (usa a inicial do nome).")]
    public Sprite icon;

    [Tooltip("Id estável para o código (ex: flashlight, battery).")]
    public string itemId = "item";
}
