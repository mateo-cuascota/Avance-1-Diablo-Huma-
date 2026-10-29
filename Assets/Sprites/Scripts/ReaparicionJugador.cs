using UnityEngine;

public class ReaparicionJugador : MonoBehaviour
{
    public Transform puntoInicio;      // lo actualizan los puntos de control
    public float limiteCaida = -8f;

    private Rigidbody2D rb;
    private Vector3 posicionInicial;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        posicionInicial = transform.position;
    }

    void Update()
    {
        if (transform.position.y < limiteCaida)
        {
            // Caer al vacío cuesta una vida (lo controla el GameManager)
            if (GameManager.Instance != null)
                GameManager.Instance.JugadorHerido(this, true);
            else
                Reaparecer();
        }
    }

    public void Reaparecer()
    {
        // Si no hay punto asignado se usa la posición donde empezó (antes daba error)
        transform.position = puntoInicio != null ? puntoInicio.position : posicionInicial;
        rb.linearVelocity = Vector2.zero;

        MovimientoJugador mov = GetComponent<MovimientoJugador>();
        if (mov != null) mov.Detener();
    }
}
