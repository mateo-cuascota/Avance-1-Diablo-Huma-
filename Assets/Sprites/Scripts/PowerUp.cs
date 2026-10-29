using UnityEngine;

// Power-up que flota y late. Choclo sagrado = +1 vida, Pluma de cóndor = doble salto por 10 s.
[RequireComponent(typeof(Collider2D))]
public class PowerUp : MonoBehaviour
{
    public enum Tipo { Vida, PlumaCondor }

    public Tipo tipo;
    public GameObject prefabDestello;
    public Color colorDestello = new Color(1f, 0.9f, 0.3f);

    private Vector3 inicio;
    private Vector3 escala;
    private float aparecio;

    void Start()
    {
        inicio = transform.position;
        escala = transform.localScale;
        aparecio = Time.time;
    }

    void Update()
    {
        // Al ser soltado por un enemigo sube un poco; luego flota y late
        float subida = Mathf.Clamp01((Time.time - aparecio) * 3f) * 0.5f;
        transform.position = inicio + Vector3.up * (subida + Mathf.Sin(Time.time * 3f) * 0.15f);
        transform.localScale = escala * (1f + Mathf.Sin(Time.time * 6f) * 0.06f);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;

        if (GameManager.Instance != null) GameManager.Instance.RecogerPowerUp(tipo);
        Destello.Crear(prefabDestello, transform.position, colorDestello, 14);
        Destroy(gameObject);
    }
}
