using UnityEngine;

// Bola de fuego que lanza el Supay tirador. Se instancia, viaja en línea recta
// y se destruye al tocar al jugador, el suelo, el látigo o al pasar su tiempo de vida.
[RequireComponent(typeof(Rigidbody2D))]
public class ProyectilFuego : MonoBehaviour, IGolpeable
{
    public float tiempoVida = 4f;
    public GameObject prefabDestello;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Dinámico sin gravedad: así detecta también el suelo y las plataformas (que no tienen Rigidbody)
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        Destroy(gameObject, tiempoVida);
    }

    public void Lanzar(Vector2 velocidad)
    {
        rb.linearVelocity = velocidad;
        float ang = Mathf.Atan2(velocidad.y, velocidad.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, ang);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Player"))
        {
            if (GameManager.Instance != null) GameManager.Instance.JugadorGolpeado(transform.position);
            Explotar();
        }
        else if (!otro.isTrigger)
        {
            Explotar();    // chocó con suelo o plataforma
        }
    }

    public void RecibirLatigazo(Vector2 desde)
    {
        Explotar();
    }

    void Explotar()
    {
        Destello.Crear(prefabDestello, transform.position, new Color(1f, 0.55f, 0.1f), 8);
        Destroy(gameObject);
    }
}
