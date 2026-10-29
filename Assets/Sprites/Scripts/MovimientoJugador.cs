using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento del Diablo Huma con técnicas de plataformas para que se sienta fluido:
// aceleración, salto variable, coyote time, jump buffer, caída rápida y doble salto (pluma de cóndor).
// También maneja el latigazo (tecla X) que daña todo lo que implemente IGolpeable.
public class MovimientoJugador : MonoBehaviour
{
    [Header("Caminar")]
    public float velocidad = 6.5f;
    public float aceleracion = 60f;
    public float frenado = 70f;
    public float aceleracionAire = 35f;

    [Header("Salto")]
    public float fuerzaSalto = 12f;
    public float multiplicadorCaida = 1.6f;    // cae más rápido de lo que sube
    public float cortarSalto = 0.5f;           // al soltar el botón se corta el salto
    public float velocidadMaxCaida = 18f;
    public float tiempoCoyote = 0.1f;          // aún puede saltar justo después de dejar el borde
    public float tiempoBuffer = 0.12f;         // recuerda el salto si se presiona un poco antes de aterrizar

    [Header("Efectos")]
    public AudioClip sonidoSalto;
    public AudioClip sonidoAterrizar;
    public GameObject prefabPolvo;

    [Header("Latigazo (tecla X)")]
    public AudioClip sonidoLatigo;
    public Vector2 tamanoGolpe = new Vector2(2.6f, 2.4f);
    public float retrasoGolpe = 0.08f;         // espera a que el látigo se extienda en la animación
    public float enfriamientoLatigo = 0.4f;
    public AudioClip sonidoLatigoFuego;

    public bool Mirando { get; private set; } = true;   // true = derecha
    public bool EnSuelo => enSuelo;
    public bool TieneDobleSalto => Time.time < finDobleSalto;
    public float TiempoDobleSalto => Mathf.Max(0f, finDobleSalto - Time.time);

    private Rigidbody2D rb;
    private Collider2D col;
    private MascaraDual mascara;
    private Transform partesVisuales;
    private Animator[] animaciones;
    private bool[] animTieneVelocidad, animTieneEnAire, animTieneAtacar;

    private float movimiento;
    private bool enSuelo;
    private float ultimoSuelo = -10f;
    private float ultimoSalto = -10f;
    private bool soltoSalto;
    private bool saltosExtraUsados;
    private float finDobleSalto;
    private float bloqueoControl;
    private float proximoLatigo;
    private float golpePendiente = -1f;
    private bool estabaEnSuelo = true;
    private float caidaMaxima;

    private readonly List<Collider2D> resultados = new List<Collider2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        mascara = GetComponent<MascaraDual>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;   // evita temblores con la cámara
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Las partes del cuerpo se voltean para mirar hacia donde camina
        partesVisuales = transform.Find("Partes_DiabloHuma");

        // Se revisa una sola vez qué parámetros tiene cada Animator (antes se hacía en cada frame)
        animaciones = GetComponentsInChildren<Animator>();
        animTieneVelocidad = new bool[animaciones.Length];
        animTieneEnAire = new bool[animaciones.Length];
        animTieneAtacar = new bool[animaciones.Length];
        for (int i = 0; i < animaciones.Length; i++)
        {
            foreach (AnimatorControllerParameter p in animaciones[i].parameters)
            {
                if (p.name == "Velocidad" && p.type == AnimatorControllerParameterType.Float) animTieneVelocidad[i] = true;
                if (p.name == "EnAire" && p.type == AnimatorControllerParameterType.Bool) animTieneEnAire[i] = true;
                if (p.name == "Atacar" && p.type == AnimatorControllerParameterType.Trigger) animTieneAtacar[i] = true;
            }
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        movimiento = 0f;
        bool atacar = false;
        Keyboard k = Keyboard.current;

        if (k != null)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) movimiento -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) movimiento += 1f;

            if (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)
                ultimoSalto = Time.time;

            if (k.spaceKey.wasReleasedThisFrame || k.wKey.wasReleasedThisFrame || k.upArrowKey.wasReleasedThisFrame)
                soltoSalto = true;

            if (k.xKey.wasPressedThisFrame && Time.time >= proximoLatigo)
                atacar = true;
        }

        if (bloqueoControl > 0f)
        {
            bloqueoControl -= Time.deltaTime;
            movimiento = 0f;
        }

        if (movimiento != 0f) Mirando = movimiento > 0f;
        if (partesVisuales != null)
        {
            Vector3 escala = partesVisuales.localScale;
            escala.x = Mathf.Abs(escala.x) * (Mirando ? 1f : -1f);
            partesVisuales.localScale = escala;
        }

        if (atacar)
        {
            proximoLatigo = Time.time + enfriamientoLatigo;
            golpePendiente = Time.time + retrasoGolpe;
            if (GameManager.Instance != null) GameManager.Instance.ReproducirSonido(sonidoLatigo);
        }

        if (golpePendiente > 0f && Time.time >= golpePendiente)
        {
            golpePendiente = -1f;
            AplicarLatigazo();
        }

        for (int i = 0; i < animaciones.Length; i++)
        {
            Animator anim = animaciones[i];
            if (anim == null) continue;
            if (animTieneVelocidad[i]) anim.SetFloat("Velocidad", Mathf.Abs(rb.linearVelocity.x) / velocidad);
            if (animTieneEnAire[i]) anim.SetBool("EnAire", !enSuelo);
            if (animTieneAtacar[i] && atacar) anim.SetTrigger("Atacar");
        }
    }

    void FixedUpdate()
    {
        RevisarSuelo();

        // Movimiento horizontal con aceleración y frenado
        Vector2 v = rb.linearVelocity;
        if (bloqueoControl <= 0f)
        {
            float objetivo = movimiento * velocidad * MultVelocidad;
            float acel = !enSuelo ? aceleracionAire : (Mathf.Abs(objetivo) > 0.01f ? aceleracion : frenado);
            v.x = Mathf.MoveTowards(v.x, objetivo, acel * Time.fixedDeltaTime);
        }

        // Salto (con coyote time y jump buffer)
        bool pidioSalto = Time.time - ultimoSalto <= tiempoBuffer;
        bool puedeSaltar = Time.time - ultimoSuelo <= tiempoCoyote;
        if (pidioSalto && puedeSaltar)
        {
            v.y = fuerzaSalto * MultSalto;
            ultimoSalto = -10f;
            ultimoSuelo = -10f;
            soltoSalto = false;
            EfectoSalto();
        }
        else if (pidioSalto && !enSuelo && TieneDobleSalto && !saltosExtraUsados)
        {
            v.y = fuerzaSalto * MultSalto * 0.9f;
            saltosExtraUsados = true;
            ultimoSalto = -10f;
            soltoSalto = false;
            EfectoSalto();
        }

        // Salto variable: si suelta el botón mientras sube, el salto se corta
        if (soltoSalto)
        {
            if (v.y > 0f) v.y *= cortarSalto;
            soltoSalto = false;
        }

        // Caída más rápida para que el salto no se sienta "flotante"
        if (v.y < 0f)
        {
            v.y += Physics2D.gravity.y * rb.gravityScale * (multiplicadorCaida - 1f) * Time.fixedDeltaTime;
            v.y = Mathf.Max(v.y, -velocidadMaxCaida);
        }

        rb.linearVelocity = v;
    }

    // Detecta el suelo con una caja delgada bajo los pies (ignora triggers y al propio jugador)
    void RevisarSuelo()
    {
        Bounds b = col.bounds;
        Vector2 centro = new Vector2(b.center.x, b.min.y);
        Vector2 tamano = new Vector2(b.size.x * 0.8f, 0.12f);

        enSuelo = false;
        if (rb.linearVelocity.y <= 0.01f)
        {
            ContactFilter2D filtro = new ContactFilter2D { useTriggers = false };
            Physics2D.OverlapBox(centro, tamano, 0f, filtro, resultados);
            foreach (Collider2D c in resultados)
            {
                if (c.attachedRigidbody == rb) continue;
                enSuelo = true;
                break;
            }
        }

        if (enSuelo)
        {
            ultimoSuelo = Time.time;
            saltosExtraUsados = false;
        }

        // Aterrizaje: si venía cayendo rápido levanta polvo
        if (!enSuelo) caidaMaxima = Mathf.Min(caidaMaxima, rb.linearVelocity.y);
        if (enSuelo && !estabaEnSuelo)
        {
            if (caidaMaxima < -6f)
            {
                Destello.Polvo(prefabPolvo, Pies(), 9);
                if (GameManager.Instance != null) GameManager.Instance.ReproducirSonido(sonidoAterrizar);
            }
            caidaMaxima = 0f;
        }
        estabaEnSuelo = enSuelo;
    }

    Vector3 Pies()
    {
        return new Vector3(col.bounds.center.x, col.bounds.min.y + 0.1f, 0f);
    }

    void EfectoSalto()
    {
        Destello.Polvo(prefabPolvo, Pies(), 6);
        if (GameManager.Instance != null) GameManager.Instance.ReproducirSonido(sonidoSalto);
    }

    Vector2 CentroGolpe()
    {
        Bounds b = col.bounds;
        float dir = Mirando ? 1f : -1f;
        return new Vector2(b.center.x + dir * (b.extents.x * 0.5f + tamanoGolpe.x * 0.5f), b.center.y);
    }

    // Poderes de la cara Killa (más velocidad y salto)
    float MultVelocidad => mascara != null ? mascara.MultVelocidad : 1f;
    float MultSalto => mascara != null ? mascara.MultSalto : 1f;

    void AplicarLatigazo()
    {
        // Látigo de fuego: llamarada en el área del golpe
        if (MascaraDual.LatigoDeFuego)
        {
            Destello d = Destello.Crear(prefabPolvo, CentroGolpe(), new Color(1f, 0.5f, 0.1f), 16);
            if (d != null) { d.velocidad = 3f; d.tamano = 0.5f; }
            Destello.Crear(prefabPolvo, CentroGolpe(), new Color(1f, 0.85f, 0.3f), 8);
            if (GameManager.Instance != null) GameManager.Instance.ReproducirSonido(sonidoLatigoFuego);
        }

        ContactFilter2D filtro = new ContactFilter2D { useTriggers = true };
        Physics2D.OverlapBox(CentroGolpe(), tamanoGolpe, 0f, filtro, resultados);
        HashSet<IGolpeable> golpeados = new HashSet<IGolpeable>();
        foreach (Collider2D c in resultados)
        {
            IGolpeable g = c.GetComponentInParent<IGolpeable>();
            if (g != null && golpeados.Add(g))
                g.RecibirLatigazo(transform.position);
        }
    }

    // --- Lo llaman otros scripts ---

    public void Rebotar(float fuerza = 0.8f)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto * fuerza);
        saltosExtraUsados = false;
    }

    public void Empujar(Vector2 desde)
    {
        float dir = transform.position.x >= desde.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * 7f, 7f);
        bloqueoControl = 0.3f;
    }

    public void ActivarDobleSalto(float segundos)
    {
        finDobleSalto = Time.time + segundos;
        saltosExtraUsados = false;
    }

    public void Detener()
    {
        rb.linearVelocity = Vector2.zero;
        bloqueoControl = 0.15f;
    }

    // Dibuja en la vista Scene el área del latigazo y del detector de suelo (depuración)
    void OnDrawGizmosSelected()
    {
        Collider2D c = col != null ? col : GetComponent<Collider2D>();
        if (c == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(Application.isPlaying ? (Vector3)CentroGolpe()
                            : c.bounds.center + Vector3.right * (c.bounds.extents.x * 0.5f + tamanoGolpe.x * 0.5f), tamanoGolpe);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(c.bounds.center.x, c.bounds.min.y), new Vector3(c.bounds.size.x * 0.8f, 0.12f));
    }
}
