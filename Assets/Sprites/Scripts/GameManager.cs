using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Controla el reto del nivel: monedas Inti, vidas, enemigos, power-ups, pausa, meta, mensajes y sonidos.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Reto")]
    public Transform contenedorMonedas;   // Objeto padre que contiene todas las monedas
    public int vidasIniciales = 5;
    public int vidasMaximas = 6;
    public float tiempoInvulnerable = 2f;
    public string nombreNivel = "NIVEL 1 · Las cumbres del Inti";
    public string escenaMenu = "MenuPrincipal";

    [Header("Sonidos (opcional)")]
    public AudioClip sonidoMoneda;
    public AudioClip sonidoGolpe;
    public AudioClip sonidoVictoria;
    public AudioClip musicaFondo;
    public AudioClip sonidoPowerUp;
    public AudioClip sonidoEnemigo;
    public AudioClip sonidoDanio;
    public AudioClip sonidoCheckpoint;

    private enum Estado { Jugando, Pausado, Ganado, Perdido }

    private AudioSource fuenteMusica;
    private bool congelando;
    private bool hayJefe;
    private bool jefeDerrotado;
    private JefeSupay jefe;
    private Estado estado = Estado.Jugando;

    [Header("Interfaz")]
    public HUDJuego hud;    // HUD hecho con Canvas; si no hay, se dibuja el HUD de respaldo con OnGUI

    // --- Datos que lee el HUD ---
    public int Vidas => vidas;
    public int MonedasRecogidas => monedasRecogidas;
    public int TotalMonedas => totalMonedas;
    public int EnemigosDerrotados => enemigosDerrotados;
    public float TiempoJuego => tiempoJuego;
    public bool Jugando => estado == Estado.Jugando;
    public bool Pausado => estado == Estado.Pausado;
    public bool Ganado => estado == Estado.Ganado;
    public bool Perdido => estado == Estado.Perdido;
    public bool JefeVencido => jefeDerrotado;
    public JefeSupay Jefe => jefe;
    public MovimientoJugador Jugador => jugador;
    public string Mensaje => Time.unscaledTime < tiempoMensaje ? mensajeTemporal : "";
    public static string Formato(float t) => FormatoTiempo(t);

    private int totalMonedas;
    private int monedasRecogidas;
    private int vidas;
    private int enemigosDerrotados;
    private float tiempoJuego;
    private bool invulnerable;
    private string mensajeTemporal = "";
    private float tiempoMensaje;

    private MovimientoJugador jugador;
    private ReaparicionJugador reaparicion;
    private CamaraSeguir camara;
    private AudioSource fuenteEfectos;
    private GUIStyle estiloTexto;
    private GUIStyle estiloSombra;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;

        if (musicaFondo != null)
        {
            fuenteMusica = gameObject.AddComponent<AudioSource>();
            fuenteMusica.clip = musicaFondo;
            fuenteMusica.loop = true;
            fuenteMusica.volume = 0.35f;
            fuenteMusica.Play();
        }
    }

    void Start()
    {
        totalMonedas = contenedorMonedas != null ? contenedorMonedas.childCount : 0;
        vidas = vidasIniciales;
        jugador = FindAnyObjectByType<MovimientoJugador>();
        reaparicion = jugador != null ? jugador.GetComponent<ReaparicionJugador>() : null;
        camara = FindAnyObjectByType<CamaraSeguir>();
        hayJefe = FindAnyObjectByType<JefeSupay>() != null;
    }

    void Update()
    {
        if (estado == Estado.Jugando) tiempoJuego += Time.deltaTime;

        Keyboard k = Keyboard.current;
        if (k == null) return;

        if (k.escapeKey.wasPressedThisFrame)
        {
            if (estado == Estado.Jugando) Pausar(true);
            else if (estado == Estado.Pausado) Pausar(false);
        }

        if (estado != Estado.Jugando)
        {
            if (k.rKey.wasPressedThisFrame) Reiniciar();
            if (k.mKey.wasPressedThisFrame) IrAlMenu();
        }
    }

    void Pausar(bool pausa)
    {
        estado = pausa ? Estado.Pausado : Estado.Jugando;
        Time.timeScale = pausa ? 0f : 1f;
        AudioListener.pause = pausa;
    }

    // Botones del panel de pausa / final
    public void Continuar()
    {
        if (estado == Estado.Pausado) Pausar(false);
    }

    // Herramienta de pruebas (F3)
    public void RestaurarVidas(string mensaje = "[Prueba] Vidas restauradas")
    {
        vidas = vidasMaximas;
        MostrarMensaje(mensaje);
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(escenaMenu);
    }

    public void ReproducirSonido(AudioClip clip)
    {
        if (clip != null && fuenteEfectos != null)
            fuenteEfectos.PlayOneShot(clip);
    }

    public void RecogerMoneda()
    {
        if (estado != Estado.Jugando) return;

        monedasRecogidas++;
        ReproducirSonido(sonidoMoneda);

        if (monedasRecogidas == totalMonedas)
            MostrarMensaje("¡Tienes todos los Inti! Ve a la meta");
    }

    public void EnemigoDerrotado()
    {
        enemigosDerrotados++;
        ReproducirSonido(sonidoEnemigo);
    }

    public void PuntoDeControl()
    {
        ReproducirSonido(sonidoCheckpoint != null ? sonidoCheckpoint : sonidoPowerUp);
        MostrarMensaje("¡Punto de control!");
    }

    // "Hit-stop": congela la imagen un instante y sacude la cámara para que el golpe se sienta fuerte
    public void Impacto(float pausa, float sacudida)
    {
        if (camara != null && sacudida > 0f) camara.Sacudir(sacudida, 0.2f);
        if (pausa > 0f && !congelando && estado == Estado.Jugando) StartCoroutine(CongelarImagen(pausa));
    }

    IEnumerator CongelarImagen(float segundos)
    {
        congelando = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(segundos);
        if (estado == Estado.Jugando) Time.timeScale = 1f;
        congelando = false;
    }

    // --- Jefe final ---

    public void IniciarJefe(JefeSupay j)
    {
        jefe = j;
        if (fuenteMusica != null) fuenteMusica.pitch = 1.18f;   // la música se acelera en la pelea
        RestaurarVidas("¡EL GRAN SUPAY DESPIERTA!  ·  Vidas recuperadas");   // entra a la pelea con todas las vidas
    }

    public void JefeDerrotado()
    {
        jefeDerrotado = true;
        enemigosDerrotados++;
        if (fuenteMusica != null) fuenteMusica.pitch = 1f;
        ReproducirSonido(sonidoVictoria);
        MostrarMensaje(monedasRecogidas >= totalMonedas
            ? "¡Venciste al Gran Supay! La meta está abierta"
            : "¡Venciste al Gran Supay! Aún faltan Inti");
    }

    public void RecogerPowerUp(PowerUp.Tipo tipo)
    {
        if (estado != Estado.Jugando) return;
        ReproducirSonido(sonidoPowerUp);

        if (tipo == PowerUp.Tipo.Vida)
        {
            if (vidas < vidasMaximas)
            {
                vidas++;
                MostrarMensaje("¡Choclo sagrado! +1 vida");
            }
            else MostrarMensaje("¡Vidas al máximo!");
        }
        else if (tipo == PowerUp.Tipo.PlumaCondor && jugador != null)
        {
            jugador.ActivarDobleSalto(10f);
            MostrarMensaje("¡Pluma de cóndor! Doble salto por 10 s");
        }
    }

    // Lo llaman los pinchos (esCaida = false) y la caída al vacío (esCaida = true).
    // El jugador vuelve al último punto de control.
    public void JugadorHerido(ReaparicionJugador jugadorHerido, bool esCaida)
    {
        if (estado != Estado.Jugando) return;

        // Durante la invulnerabilidad los pinchos no hacen daño,
        // pero si cae al vacío igual debe reaparecer.
        if (invulnerable)
        {
            if (esCaida) jugadorHerido.Reaparecer();
            return;
        }

        if (!QuitarVida()) return;
        jugadorHerido.Reaparecer();
        StartCoroutine(Parpadeo(jugadorHerido.gameObject));
    }

    // Lo llaman enemigos, rocas y bolas de fuego: el jugador sale empujado pero no reaparece
    public void JugadorGolpeado(Vector2 desde)
    {
        if (estado != Estado.Jugando || invulnerable || jugador == null) return;

        if (!QuitarVida()) return;
        jugador.Empujar(desde);
        StartCoroutine(Parpadeo(jugador.gameObject));
    }

    // Devuelve false si el jugador se quedó sin vidas
    bool QuitarVida()
    {
        vidas--;
        ReproducirSonido(sonidoDanio != null ? sonidoDanio : sonidoGolpe);
        Impacto(0.08f, 0.3f);

        if (vidas <= 0)
        {
            estado = Estado.Perdido;
            Time.timeScale = 0f;
            return false;
        }
        return true;
    }

    public void LlegoMeta()
    {
        if (estado != Estado.Jugando) return;

        int faltan = totalMonedas - monedasRecogidas;
        if (faltan > 0)
        {
            MostrarMensaje("Te faltan " + faltan + " Inti para abrir la meta");
            return;
        }
        if (hayJefe && !jefeDerrotado)
        {
            MostrarMensaje("Derrota al Gran Supay para abrir la meta");
            return;
        }

        estado = Estado.Ganado;
        ReproducirSonido(sonidoVictoria);
        Time.timeScale = 0f;
    }

    public void MostrarMensaje(string texto, float duracion = 2.5f)
    {
        mensajeTemporal = texto;
        tiempoMensaje = Time.unscaledTime + duracion;
    }

    // El personaje parpadea en rojo mientras es invulnerable
    IEnumerator Parpadeo(GameObject objetivo)
    {
        invulnerable = true;
        SpriteRenderer[] partes = objetivo.GetComponentsInChildren<SpriteRenderer>();
        float fin = Time.time + tiempoInvulnerable;
        bool rojo = false;

        while (Time.time < fin)
        {
            rojo = !rojo;
            foreach (SpriteRenderer sr in partes)
                sr.color = rojo ? new Color(1f, 0.4f, 0.4f, 0.6f) : Color.white;
            yield return new WaitForSeconds(0.1f);
        }

        foreach (SpriteRenderer sr in partes)
            sr.color = Color.white;
        invulnerable = false;
    }

    void OnGUI()
    {
        if (hud != null) return;    // el HUD de Canvas se encarga

        if (estiloTexto == null)
        {
            estiloTexto = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            estiloSombra = new GUIStyle(estiloTexto);
        }

        int tam = Mathf.Max(18, Screen.height / 22);
        estiloTexto.fontSize = tam;
        estiloSombra.fontSize = tam;
        estiloTexto.normal.textColor = new Color(1f, 0.85f, 0.2f);
        estiloSombra.normal.textColor = Color.black;
        estiloTexto.alignment = TextAnchor.UpperLeft;
        estiloSombra.alignment = TextAnchor.UpperLeft;

        // HUD
        Texto(new Rect(20, 15, 600, tam * 1.5f), "Inti: " + monedasRecogidas + " / " + totalMonedas);
        estiloTexto.normal.textColor = new Color(1f, 0.45f, 0.45f);
        Texto(new Rect(20, 15 + tam * 1.4f, 600, tam * 1.5f), "Vidas: " + new string('♥', Mathf.Max(0, vidas)));
        estiloTexto.normal.textColor = new Color(0.8f, 0.6f, 1f);
        Texto(new Rect(20, 15 + tam * 2.8f, 600, tam * 1.5f), "Supay: " + enemigosDerrotados);

        estiloTexto.alignment = estiloSombra.alignment = TextAnchor.UpperRight;
        estiloTexto.normal.textColor = Color.white;
        Texto(new Rect(Screen.width - 420, 15, 400, tam * 1.5f), FormatoTiempo(tiempoJuego));
        if (jugador != null && jugador.TieneDobleSalto)
        {
            estiloTexto.normal.textColor = new Color(0.6f, 0.9f, 1f);
            Texto(new Rect(Screen.width - 620, 15 + tam * 1.4f, 600, tam * 1.5f),
                  "Doble salto: " + Mathf.CeilToInt(jugador.TiempoDobleSalto) + " s");
        }
        estiloTexto.alignment = estiloSombra.alignment = TextAnchor.UpperLeft;

        estiloTexto.fontSize = estiloSombra.fontSize = Mathf.Max(12, tam / 2);
        estiloTexto.normal.textColor = Color.white;
        Texto(new Rect(20, Screen.height - tam * 1.2f, 1200, tam),
              "← → moverse   ESPACIO saltar   X latigazo   ESC pausa");

        estiloTexto.alignment = estiloSombra.alignment = TextAnchor.MiddleCenter;
        Rect centro = new Rect(0, Screen.height * 0.3f, Screen.width, tam * 3);

        // Barra de vida del jefe
        if (jefe != null && jefe.Activo && !jefe.Muerto && estado != Estado.Ganado)
        {
            float ancho = Screen.width * 0.5f;
            Rect barra = new Rect((Screen.width - ancho) / 2f, Screen.height - tam * 2.6f, ancho, tam * 0.6f);
            Color antes = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(barra.x - 3, barra.y - 3, barra.width + 6, barra.height + 6), Texture2D.whiteTexture);
            GUI.color = new Color(0.3f, 0.05f, 0.1f);
            GUI.DrawTexture(barra, Texture2D.whiteTexture);
            GUI.color = jefe.EnFase2 ? new Color(1f, 0.25f, 0.1f) : new Color(0.65f, 0.2f, 0.9f);
            GUI.DrawTexture(new Rect(barra.x, barra.y, barra.width * jefe.VidaNormalizada, barra.height), Texture2D.whiteTexture);
            GUI.color = antes;
            estiloTexto.fontSize = estiloSombra.fontSize = Mathf.Max(12, tam * 2 / 3);
            estiloTexto.normal.textColor = Color.white;
            Texto(new Rect(0, barra.y - tam * 1.1f, Screen.width, tam), jefe.nombre + (jefe.EnFase2 ? "  ·  ¡FURIOSO!" : ""));
        }

        // Cartel del nombre del nivel al empezar
        if (tiempoJuego < 3f && estado == Estado.Jugando)
        {
            float a = Mathf.Clamp01(3f - tiempoJuego);
            estiloTexto.fontSize = estiloSombra.fontSize = Mathf.RoundToInt(tam * 1.3f);
            estiloTexto.normal.textColor = new Color(1f, 0.85f, 0.2f, a);
            estiloSombra.normal.textColor = new Color(0, 0, 0, a);
            Texto(new Rect(0, Screen.height * 0.18f, Screen.width, tam * 2), nombreNivel);
            estiloTexto.fontSize = estiloSombra.fontSize = tam * 2 / 3;
            estiloTexto.normal.textColor = new Color(1, 1, 1, a);
            Texto(new Rect(0, Screen.height * 0.18f + tam * 1.8f, Screen.width, tam * 1.2f),
                  "Recupera todos los Inti y llega a la meta");
            estiloSombra.normal.textColor = Color.black;
        }

        if (estado == Estado.Ganado)
        {
            PantallaFinal(tam, "¡NIVEL COMPLETADO!", "El Diablo Huma protegió los Andes",
                          "Tiempo " + FormatoTiempo(tiempoJuego) + "  ·  Supay vencidos: " + enemigosDerrotados +
                          (jefeDerrotado ? "  ·  Gran Supay ✔" : ""),
                          "R jugar otra vez   ·   M menú", new Color(1f, 0.85f, 0.2f));
        }
        else if (estado == Estado.Perdido)
        {
            PantallaFinal(tam, "GAME OVER", "Los Inti quedaron sin guardián",
                          "Inti recuperados: " + monedasRecogidas + " / " + totalMonedas,
                          "R reintentar   ·   M menú", new Color(1f, 0.35f, 0.35f));
        }
        else if (estado == Estado.Pausado)
        {
            PantallaFinal(tam, "PAUSA", "El sol espera a su guardián", "",
                          "ESC continuar   ·   R reiniciar   ·   M menú", Color.white);
        }
        else if (Time.unscaledTime < tiempoMensaje)
        {
            estiloTexto.fontSize = estiloSombra.fontSize = tam;
            estiloTexto.normal.textColor = Color.white;
            Texto(centro, mensajeTemporal);
        }
    }

    static string FormatoTiempo(float t)
    {
        int s = Mathf.FloorToInt(t);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    // Fondo oscuro + título, frase, dato y subtítulo en filas separadas para que no se corten
    void PantallaFinal(int tam, string titulo, string frase, string dato, string subtitulo, Color color)
    {
        Color anterior = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = anterior;

        float y = Screen.height * 0.24f;

        estiloTexto.fontSize = estiloSombra.fontSize = Mathf.RoundToInt(tam * 1.8f);
        estiloTexto.normal.textColor = color;
        Texto(new Rect(0, y, Screen.width, tam * 2.6f), titulo);

        estiloTexto.fontSize = estiloSombra.fontSize = Mathf.RoundToInt(tam * 1.1f);
        Texto(new Rect(0, y + tam * 2.6f, Screen.width, tam * 1.8f), frase);

        estiloTexto.fontSize = estiloSombra.fontSize = tam;
        estiloTexto.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
        Texto(new Rect(0, y + tam * 4.4f, Screen.width, tam * 1.6f), dato);

        estiloTexto.normal.textColor = Color.white;
        Texto(new Rect(0, y + tam * 6.2f, Screen.width, tam * 1.6f), subtitulo);
    }

    void Texto(Rect r, string texto)
    {
        Rect sombra = new Rect(r.x + 2, r.y + 2, r.width, r.height);
        GUI.Label(sombra, texto, estiloSombra);
        GUI.Label(r, texto, estiloTexto);
    }
}
