using System.Collections;
using UnityEngine;

// Jefe final: el Gran Supay. Duerme flotando hasta que el jugador entra a su arena (ZonaJefe).
// Repite un ciclo de ataques:
//   1. Abanico de bolas de fuego dirigido al jugador
//   2. Embestida: tiembla, se lanza contra el suelo y queda aturdido (momento para atacarlo)
//   3. Lluvia de rocas sobre la arena
// Con la mitad de vida entra en fase 2 (furioso): un poco más rápido y más proyectiles.
// El látigo normal le quita 1 de vida, el látigo de fuego (cara Killa) 2 y el pisotón 2.
[RequireComponent(typeof(Rigidbody2D))]
public class JefeSupay : MonoBehaviour, IGolpeable
{
    public string nombre = "GRAN SUPAY";
    public int vidaMaxima = 6;

    [Header("Arena")]
    public float xMin = 64f;
    public float xMax = 77f;
    public float alturaVuelo = 3.2f;
    public float alturaSuelo = -1f;
    public float velocidad = 3f;

    [Header("Ataques (prefabs instanciados)")]
    public GameObject prefabFuego;
    public GameObject prefabRoca;
    public GameObject prefabDestello;
    public GameObject premio;
    public float velocidadFuego = 4f;

    [Header("Sonidos")]
    public AudioClip sonidoRugido;
    public AudioClip sonidoDisparo;
    public AudioClip sonidoGolpe;
    public AudioClip sonidoImpactoSuelo;
    public AudioClip sonidoMuerte;

    public int Vida { get; private set; }
    public bool Activo { get; private set; }
    public bool Muerto { get; private set; }
    public bool EnFase2 => Vida <= vidaMaxima / 2;
    public float VidaNormalizada => Mathf.Clamp01(Vida / (float)vidaMaxima);

    private Rigidbody2D rb;
    private Animator anim;
    private Transform visual;
    private Transform jugador;
    private float invulnerableHasta;
    private bool avisoFase2;
    private Vector2 inicio;
    private float ignorarJugadorHasta;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        anim = GetComponentInChildren<Animator>();
        visual = anim != null ? anim.transform : transform;
        Vida = vidaMaxima;
    }

    void Start()
    {
        inicio = transform.position;
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;
    }

    void Update()
    {
        if (Muerto) return;

        // Mientras duerme solo flota
        if (!Activo)
        {
            rb.MovePosition(inicio + Vector2.up * Mathf.Sin(Time.time * 1.5f) * 0.25f);
            return;
        }

        // Mira al jugador
        if (jugador != null)
        {
            Vector3 e = visual.localScale;
            e.x = Mathf.Abs(e.x) * (jugador.position.x > transform.position.x ? 1f : -1f);
            visual.localScale = e;
        }
    }

    public void Activar()
    {
        if (Activo || Muerto) return;
        Activo = true;
        StartCoroutine(Ciclo());
    }

    // ---------------- Ciclo de ataques ----------------

    IEnumerator Ciclo()
    {
        yield return Rugir(1.2f);

        int turno = 0;
        while (!Muerto)
        {
            yield return Flotar(EnFase2 ? 2.6f : 3.2f);   // descansa más entre ataques
            switch (turno % 3)
            {
                case 0: yield return Abanico(); break;
                case 1: yield return Embestida(); break;
                default: yield return LluviaRocas(); break;
            }
            turno++;
        }
    }

    IEnumerator Rugir(float duracion)
    {
        Sonar(sonidoRugido);
        if (anim != null) anim.SetTrigger("Disparar");
        if (GameManager.Instance != null) GameManager.Instance.Impacto(0f, 0.35f);
        yield return Temblar(duracion);
    }

    // Sigue al jugador desde arriba, ondulando
    IEnumerator Flotar(float duracion)
    {
        for (float t = 0; t < duracion && !Muerto; t += Time.fixedDeltaTime)
        {
            float xObj = jugador != null ? Mathf.Clamp(jugador.position.x, xMin, xMax) : rb.position.x;
            float vel = EnFase2 ? velocidad * 1.2f : velocidad;
            Vector2 destino = new Vector2(Mathf.MoveTowards(rb.position.x, xObj, vel * Time.fixedDeltaTime),
                                          alturaVuelo + Mathf.Sin(Time.time * 3f) * 0.4f);
            rb.MovePosition(destino);
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator Abanico()
    {
        int rafagas = 1;
        for (int r = 0; r < rafagas && !Muerto; r++)
        {
            if (anim != null) anim.SetTrigger("Disparar");
            yield return Temblar(0.7f);                          // aviso
            if (Muerto || jugador == null) yield break;

            Sonar(sonidoDisparo);
            int cantidad = 3;
            float apertura = 70f;
            Vector2 haciaJugador = ((Vector2)jugador.position - rb.position).normalized;
            float angBase = Mathf.Atan2(haciaJugador.y, haciaJugador.x) * Mathf.Rad2Deg;
            for (int i = 0; i < cantidad; i++)
            {
                float ang = (angBase - apertura / 2f + apertura * i / (cantidad - 1)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                GameObject f = Instantiate(prefabFuego, rb.position + dir * 1.6f, Quaternion.identity);
                ProyectilFuego p = f.GetComponent<ProyectilFuego>();
                if (p != null) p.Lanzar(dir * velocidadFuego);
            }
            yield return new WaitForSeconds(0.7f);
        }
    }

    IEnumerator Embestida()
    {
        if (jugador == null) yield break;

        // Se coloca encima del jugador y tiembla como aviso
        float x = Mathf.Clamp(jugador.position.x, xMin, xMax);
        yield return MoverA(new Vector2(x, alturaVuelo + 0.8f), 9f);
        yield return Temblar(EnFase2 ? 0.8f : 1f);
        if (Muerto) yield break;

        // Cae en picada
        yield return MoverA(new Vector2(rb.position.x, alturaSuelo), EnFase2 ? 13f : 11f);
        if (Muerto) yield break;
        Sonar(sonidoImpactoSuelo);
        if (GameManager.Instance != null) GameManager.Instance.Impacto(0.05f, 0.4f);
        Destello.Polvo(prefabDestello, (Vector2)transform.position + Vector2.down * 1.4f, 16);

        // Queda aturdido: oportunidad del jugador
        float aturdido = EnFase2 ? 2.4f : 3f;
        for (float t = 0; t < aturdido && !Muerto; t += Time.deltaTime)
        {
            visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 14f) * 12f);
            yield return null;
        }
        visual.localRotation = Quaternion.identity;

        yield return MoverA(new Vector2(rb.position.x, alturaVuelo), 5f);
    }

    IEnumerator LluviaRocas()
    {
        yield return Rugir(0.5f);
        Camera cam = Camera.main;
        int cantidad = 3;
        for (int i = 0; i < cantidad && !Muerto; i++)
        {
            float x = Random.Range(xMin - 1f, xMax + 1f);
            float y = cam != null ? cam.transform.position.y + cam.orthographicSize - 0.4f : 6f;
            Instantiate(prefabRoca, new Vector3(x, y, 0f), Quaternion.identity);
            yield return new WaitForSeconds(0.6f);
        }
        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator MoverA(Vector2 destino, float vel)
    {
        while (!Muerto && Vector2.Distance(rb.position, destino) > 0.05f)
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, destino, vel * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator Temblar(float duracion)
    {
        for (float t = 0; t < duracion && !Muerto; t += Time.deltaTime)
        {
            visual.localPosition = (Vector3)(Random.insideUnitCircle * 0.08f);
            yield return null;
        }
        visual.localPosition = Vector3.zero;
    }

    // ---------------- Daño ----------------

    public void RecibirLatigazo(Vector2 desde)
    {
        Danar(MascaraDual.DanoLatigo);    // 2 con el látigo de fuego
    }

    void OnTriggerEnter2D(Collider2D otro) { TocarJugador(otro); }
    void OnTriggerStay2D(Collider2D otro) { TocarJugador(otro); }

    void TocarJugador(Collider2D otro)
    {
        if (!Activo || Muerto || !otro.CompareTag("Player") || Time.time < ignorarJugadorHasta) return;

        Rigidbody2D rbJugador = otro.attachedRigidbody;
        if (rbJugador != null && rbJugador.linearVelocity.y < -0.1f && otro.bounds.min.y > transform.position.y)
        {
            MovimientoJugador mov = otro.GetComponent<MovimientoJugador>();
            if (mov != null) mov.Rebotar(1.1f);
            ignorarJugadorHasta = Time.time + 0.35f;
            Danar(2);
            return;
        }

        if (GameManager.Instance != null) GameManager.Instance.JugadorGolpeado(transform.position);
    }

    void Danar(int cantidad)
    {
        if (!Activo || Muerto || Time.time < invulnerableHasta) return;

        Vida -= cantidad;
        invulnerableHasta = Time.time + 0.2f;
        if (anim != null) anim.SetTrigger("Herido");
        Sonar(sonidoGolpe);
        Destello.Crear(prefabDestello, transform.position, new Color(1f, 0.9f, 0.4f), 10);
        if (GameManager.Instance != null) GameManager.Instance.Impacto(0.06f, 0.15f);

        if (Vida <= 0)
        {
            Morir();
        }
        else if (EnFase2 && !avisoFase2)
        {
            avisoFase2 = true;
            if (GameManager.Instance != null) GameManager.Instance.MostrarMensaje("¡El Gran Supay está furioso!");
            Sonar(sonidoRugido);
        }
    }

    void Morir()
    {
        Muerto = true;
        Vida = 0;
        StopAllCoroutines();
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        if (GameManager.Instance != null) GameManager.Instance.JefeDerrotado();
        StartCoroutine(Explotar());
    }

    // Varias explosiones seguidas, luego se desvanece y suelta un premio
    IEnumerator Explotar()
    {
        if (anim != null) anim.enabled = false;
        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
        Color[] colores = { new Color(0.6f, 0.3f, 0.9f), new Color(1f, 0.5f, 0.1f), new Color(1f, 0.9f, 0.4f) };

        for (int i = 0; i < 8; i++)
        {
            Vector2 p = (Vector2)transform.position + Random.insideUnitCircle * 1.5f;
            Destello.Crear(prefabDestello, p, colores[i % colores.Length], 14);
            Sonar(i % 2 == 0 ? sonidoMuerte : sonidoGolpe);
            if (GameManager.Instance != null) GameManager.Instance.Impacto(0f, 0.25f);
            visual.localPosition = (Vector3)(Random.insideUnitCircle * 0.15f);
            yield return new WaitForSeconds(0.15f);
        }

        Vector3 escala = transform.localScale;
        for (float t = 0; t < 0.6f; t += Time.deltaTime)
        {
            float k = t / 0.6f;
            transform.localScale = escala * (1f + k * 0.4f);
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - k);
            yield return null;
        }

        Destello.Crear(prefabDestello, transform.position, new Color(1f, 0.85f, 0.2f), 24);
        if (premio != null) Instantiate(premio, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void Sonar(AudioClip clip)
    {
        if (GameManager.Instance != null) GameManager.Instance.ReproducirSonido(clip);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(xMin, alturaVuelo), new Vector3(xMax, alturaVuelo));
        Gizmos.DrawLine(new Vector3(xMin, alturaSuelo), new Vector3(xMax, alturaSuelo));
    }
}
