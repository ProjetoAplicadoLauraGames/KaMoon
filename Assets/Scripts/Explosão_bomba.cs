using System.Collections;
using UnityEngine;

public class ExplosaoBomba : MonoBehaviour
{
    [Header("Efeitos Visuais e Luz")]
    public ParticleSystem particulasExplosao;
    public Light claraoLuz;
    public GameObject modeloBomba; // O modelo 3D que vai desaparecer apos a explosao

    [Header("Audio (Opcional)")]
    public AudioSource audioSource;
    public AudioClip somExplosao;

    void Update()
    {
        // Tecla de teste durante o desenvolvimento (carrega no K para testar o KaBoom)
        if (Input.GetKeyDown(KeyCode.K))
        {
            Detonar();
        }
    }

    public void Detonar()
    {
        StartCoroutine(RotinaExplosao());
    }

    private IEnumerator RotinaExplosao()
    {
        // 1. Tocar som se existir
        if (audioSource != null && somExplosao != null)
        {
            audioSource.PlayOneShot(somExplosao);
        }

        // 2. Disparar particulas
        if (particulasExplosao != null)
        {
            particulasExplosao.Play();
        }

        // 3. Ativar o clarao de luz
        if (claraoLuz != null)
        {
            claraoLuz.enabled = true;
        }

        // 4. Esconder a carcaca da bomba (ficou destruida)
        if (modeloBomba != null)
        {
            modeloBomba.SetActive(false);
        }

        // 5. Suavizar e apagar o clarao passado 0.2 segundos
        yield return new WaitForSeconds(0.2f);
        if (claraoLuz != null)
        {
            claraoLuz.enabled = false;
        }

        Debug.Log("BOOM! A bomba explodiu.");
        // chamar aqui a tela de Game Over 
    }
}