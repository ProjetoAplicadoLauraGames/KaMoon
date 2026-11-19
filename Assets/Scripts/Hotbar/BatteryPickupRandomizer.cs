using UnityEngine;

/// <summary>
/// Escolhe uma das posições possíveis para a bateria no início da partida.
/// As posições são pontos seguros da sala, definidos na cena, para não
/// aparecer dentro de paredes ou mobiliário.
/// </summary>
public class BatteryPickupRandomizer : MonoBehaviour
{
    public Vector3[] pontos;

    private void Start()
    {
        if (pontos == null || pontos.Length == 0) return;
        transform.position = pontos[Random.Range(0, pontos.Length)];
    }
}
