using UnityEngine;

// Zona invisible que muestra un mensaje de ayuda la primera vez que el jugador la cruza (tutorial).
[RequireComponent(typeof(Collider2D))]
public class ZonaMensaje : MonoBehaviour
{
    [TextArea] public string mensaje;
    public float duracion = 4.5f;

    private bool mostrado;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (mostrado || !otro.CompareTag("Player") || GameManager.Instance == null) return;
        mostrado = true;
        GameManager.Instance.MostrarMensaje(mensaje, duracion);
    }
}
