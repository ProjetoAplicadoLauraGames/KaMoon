using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MenuLocalizedText : MonoBehaviour
{
    [SerializeField] private MenuTextKey textKey;
    private TMP_Text texto;

    private void OnEnable()
    {
        MenuLocalization.LanguageChanged += AtualizarTexto;
        AtualizarTexto();
    }

    private void OnDisable()
    {
        MenuLocalization.LanguageChanged -= AtualizarTexto;
    }

    public void SetKey(MenuTextKey key)
    {
        textKey = key;
        AtualizarTexto();
    }

    private void AtualizarTexto()
    {
        if (texto == null) texto = GetComponent<TMP_Text>();
        texto.text = MenuLocalization.GetText(textKey);
    }
}
