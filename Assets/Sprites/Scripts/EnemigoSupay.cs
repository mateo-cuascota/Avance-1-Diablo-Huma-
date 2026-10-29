using System.Collections;
using UnityEngine;

// Enemigo Supay (espíritu oscuro). Flota y patrulla de lado a lado.
// - Si toca al jugador le quita una vida.
// - El látigo le quita vida; si el jugador le cae encima, muere de una.
// - Si "dispara" está activo, lanza bolas de fuego (instanciadas) cuando ve al jugador.
// - Al morir instancia un destello y a veces suelta un power-up.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemigoSupay : MonoBehaviour, IGolpeable
{
    public int vida = 2;

    [Header("Patrulla")]
    public bool patrulla = true;
    public float velocidad = 2f;
    public float distanciaPatrulla = 3f;    // hacia cada lado desde donde empieza
    public float alturaFlotar = 0.2f;

    [Header("Disparo")]
    public bool dispara;
    public GameObject prefabFuego;
    public float intervaloDisparo = 2.2f;
    public float rangoVision = 9f;
    public float velocidadFuego = 6f;

    [Header("Efectos")]
    public GameObject prefabDestello;
    public AudioClip sonidoMuerte;
    public AudioClip sonidoDisparo;
    private float ignorarJugadorHasta;
    public GameObject[] premios;
    [Range(0f, 1f)] public float probabilidadPremio = 0.35f;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform visual;
    private Transform jugador;
    private Vector2 inicio;
    private int direccion = 1;
    private float aturdido;
    private float proximoDisparo;
    private bool muerto;
    private float desfase;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        anim = GetComponentInChildren<Animator>();
        visual = anim != null ? anim.transform : transform;
    }

    void Start()
    {
        inicio = transform.position;
        desfase = Random.Range(0f, 6f);
        proximoDisparo = Time.time + Random.Range(1f, intervaloDisparo);
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;
    }

    void FixedUpdate()
    {
        if (muerto) return;

        Vector2 pos = rb.position;
        if (aturdido > 0f)
        {
            aturdido -= Time.fixedDeltaTime;
        }
        else if (patrulla)
        {
            pos.x += direccion * velocidad * Time.fixedDeltaTime;
            if (pos.x > inicio.x + distanciaPatrulla) direccion = -1;
            if (pos.x < inicio.x - distanciaPatrulla) direccion = 1;
        }
        pos.y = inicio.y + Mathf.Sin((Time.time + desfase) * 3f) * alturaFlotar;
        rb.MovePosition(pos);
    }

    void Update()
    {
        if (muerto) return;

        // Mira hacia donde patrulla, o hacia el jugador si es tirador
        int mirar = direccion;
        bool loVe = jugador != null && Vector2.Distance(jugador.position, transform.position) < rangoVision;
        if (dispara && loVe) mirar = jugador.position.x > transform.position.x ? 1 : -1;
        Vector3 e = visual.localScale;
        e.x = Mathf.Abs(e.x) * mirar;
        visual.localScale = e;

        if (dispara && loVe && prefabFuego != null && Time.time >= proximoDisparo)
        {
            proximoDisparo = Time.time + intervaloDisparo;
            StartCoroutine(Disparar());
        }
    }

    IEnumerator Disparar()
    {
        if (anim != null) anim.SetTrigger("Disparar");
        yield return new WaitForSeconds(0.25f);    // pequeño aviso antes de lanzar
        if (muerto || jugador == null) yield break;

        if (GameManager.Instance != null) GameManager.Instance.ReproducirSonido(sonidoDisparo);
        Vector2 dir = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
        GameObject f = Instantiate(prefabFuego, (Vector2)transform.position + dir * 0.9f, Quaternion.identity);
        ProyectilFuego p = f.GetComponent<ProyectilFuego>();
        if (p != null) p.Lanzar(dir * velocidadFuego);
    }

    public void RecibirLatigazo(Vector2 desde)
    {
        if (muerto) return;

        vida -= MascaraDual.DanoLatigo;    // el látigo de fuego (cara Killa) hace doble daño
        if (anim != null) anim.SetTrigger("Herido");
        Destello.Crear(prefabDestello, transform.position, new Color(1f, 0.9f, 0.4f), 6);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReproducirSonido(GameManager.Instance.sonidoGolpe);
            if (vida > 0) GameManager.Instance.Impacto(0.04f, 0.08f);
        }

        // Sale despedido hacia atrás
        float dir = transform.position.x >= desde.x ? 1f : -1f;
        inicio.x += dir * 1f;
        rb.position += new Vector2(dir * 1f, 0f);
        aturdido = 0.4f;

        if (vida <= 0) Morir();
    }

    void OnTriggerEnter2D(Collider2D otro) { TocarJugador(otro); }
    void OnTriggerStay2D(Collider2D otro) { TocarJugador(otro); }

    void TocarJugador(Collider2D otro)
    {
        if (muerto || !otro.CompareTag("Player") || Time.time < ignorarJugadorHasta) return;

        Rigidbody2D rbJugador = otro.attachedRigidbody;
        MovimientoJugador mov = otro.GetComponent<MovimientoJugador>();

        // Pisotón: el jugador viene cayendo y sus pies están por encima del centro del Supay
        if (rbJugador != null && rbJugador.linearVelocity.y < -0.1f && otro.bounds.min.y > transform.position.y)
        {
            if (mov != null) mov.Rebotar();
            ignorarJugadorHasta = Time.time + 0.35f;   // que el rebote no cuente como golpe
            Morir();
            return;
        }

        if (GameManager.Instance != null) GameManager.Instance.JugadorGolpeado(transform.position);
    }

    void Morir()
    {
        if (muerto) return;
        muerto = true;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnemigoDerrotado();
            GameManager.Instance.ReproducirSonido(sonidoMuerte);
            GameManager.Instance.Impacto(0.07f, 0.15f);
        }
        Destello.Crear(prefabDestello, transform.position, new Color(0.6f, 0.3f, 0.9f), 16);

        if (premios != null && premios.Length > 0 && Random.value < probabilidadPremio)
        {
            GameObject premio = premios[Random.Range(0, premios.Length)];
            if (premio != null) Instantiate(premio, transform.position, Quaternion.identity);
        }

        StartCoroutine(AnimacionMuerte());
    }

    // Gira, se achica y se desvanece
    IEnumerator AnimacionMuerte()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (anim != null) anim.enabled = false;
        Vector3 escala = transform.localScale;
        for (float t = 0; t < 0.45f; t += Time.deltaTime)
        {
            float p = t / 0.45f;
            transform.localScale = escala * (1f - p);
            transform.Rotate(0, 0, 900f * Time.deltaTime);
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - p);
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = Application.isPlaying ? (Vector3)inicio : transform.position;
        Gizmos.color = Color.magenta;
        if (patrulla)
            Gizmos.DrawLine(c + Vector3.left * distanciaPatrulla, c + Vector3.right * distanciaPatrulla);
        if (dispara)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, rangoVision);
        }
    }
}
