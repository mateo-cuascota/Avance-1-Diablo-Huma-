using UnityEngine;

// Roca que cae de la montaña. Primero tiembla un momento (aviso) y luego cae girando.
// Se rompe al tocar el suelo, al jugador o al recibir un latigazo.
[RequireComponent(typeof(Rigidbody2D))]
public class Roca : MonoBehaviour, IGolpeable
{
    public float tiempoAviso = 0.5f;
    public float gravedad = 1.4f;
    public GameObject prefabDestello;
    public AudioClip sonidoRomper;

    private Rigidbody2D rb;
    private Transform visual;
    private float caeEn;
    private float giro;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
        caeEn = Time.time + tiempoAviso;
        giro = Random.Range(-300f, 300f);
        float s = Random.Range(0.8f, 1.3f);
        transform.localScale = Vector3.one * s;
        Destroy(gameObject, 10f);
    }

    void Update()
    {
        if (Time.time < caeEn)
        {
            visual.localPosition = (Vector3)(Random.insideUnitCircle * 0.06f);   // tiembla
            return;
        }

        visual.localPosition = Vector3.zero;
        rb.gravityScale = gravedad;
        transform.Rotate(0, 0, giro * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (Time.time < caeEn) return;

        if (otro.CompareTag("Player"))
        {
            if (GameManager.Instance != null) GameManager.Instance.JugadorGolpeado(transform.position);
            Romper();
        }
        else if (!otro.isTrigger)
        {
            Romper();
        }
    }

    public void RecibirLatigazo(Vector2 desde)
    {
        Romper();
    }

    void Romper()
    {
        Destello.Crear(prefabDestello, transform.position, new Color(0.55f, 0.5f, 0.45f), 10);
        if (GameManager.Instance != null) GameManager.Instance.ReproducirSonido(sonidoRomper);
        Destroy(gameObject);
    }
}
