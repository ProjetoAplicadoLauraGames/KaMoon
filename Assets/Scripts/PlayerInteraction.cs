using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public ExplosaoBomba bombaScript; // Referência ao script da bomba para chamar a função Detonar
    public Transform InteractorSource;
    public float InteractRange;
    public GameObject dot;
    public GameObject GameOverUI;
    void Update()
    {
        Ray r = new Ray(InteractorSource.position, InteractorSource.forward);
        if (Physics.Raycast(r, out RaycastHit hit, InteractRange) && (hit.collider.CompareTag("WrongCable") || hit.collider.CompareTag("CorrectCable")))
        {
            dot.gameObject.SetActive(true);
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (hit.collider.CompareTag("WrongCable"))
                {
                    Debug.Log("Wrong cable cut!");
                    bombaScript.Detonar(); // Chama a função Detonar do script da bomba
                    StartCoroutine(GameOverRoutine());
                    
                }
                else if (hit.collider.CompareTag("CorrectCable"))
                {
                    Debug.Log("Correct cable cut!");
                }
                Destroy(hit.collider.gameObject);
            }
        }
        else
        {
            dot.gameObject.SetActive(false);
        }
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(3f);
        GameOverUI.SetActive(true);
    }
}
