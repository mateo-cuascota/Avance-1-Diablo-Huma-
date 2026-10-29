using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Pantalla de inicio: título, personaje, brasas doradas y botones Jugar / Controles / Salir.
// Se maneja con mouse o con teclado (↑ ↓ y ENTER).
public class MenuPrincipal : MonoBehaviour
{
    [Header("Imágenes")]
    public Texture2D fondo;
    public Texture2D diabloHuma;
    public Texture2D supay;
    public Texture2D inti;

    [Header("Audio")]
    public AudioClip musica;
    public AudioClip sonidoSeleccion;

    public string escenaHistoria = "Historia";

    private readonly string[] opciones = { "JUGAR", "CONTROLES", "SALIR" };
    private int seleccion;
    private bool mostrandoControles;
    private float fadeSalida = -1f;
    private Vector2 ultimoMouse;

    private AudioSource fuente;
    private GUIStyle estilo;
    private GUIStyle sombra;
    private Vector2[] brasas;
    private float[] velBrasas;

    void Awake()
    {
        Time.timeScale = 1f;
        fuente = gameObject.AddComponent<AudioSource>();
        if (musica != null)
        {
            AudioSource m = gameObject.AddComponent<AudioSource>();
            m.clip = musica;
            m.loop = true;
            m.volume = 0.4f;
            m.Play();
        }

        brasas = new Vector2[40];
        velBrasas = new float[brasas.Length];
        for (int i = 0; i < brasas.Length; i++)
        {
            brasas[i] = new Vector2(Random.value, Random.value);
            velBrasas[i] = Random.Range(0.03f, 0.09f);
        }
    }

    void Update()
    {
        for (int i = 0; i < brasas.Length; i++)
        {
            brasas[i].y -= velBrasas[i] * Time.deltaTime;
            brasas[i].x += Mathf.Sin(Time.time + i) * 0.01f * Time.deltaTime;
            if (brasas[i].y < 0f) brasas[i] = new Vector2(Random.value, 1f);
        }

        if (fadeSalida >= 0f)
        {
            fadeSalida += Time.deltaTime * 1.5f;
            if (fadeSalida >= 1f) SceneManager.LoadScene(escenaHistoria);
            return;
        }

        Keyboard k = Keyboard.current;
        if (k == null) return;

        if (mostrandoControles)
        {
            if (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)
                mostrandoControles = false;
            return;
        }

        if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Mover(-1);
        if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Mover(1);
        if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Elegir(seleccion);
    }

    void Mover(int d)
    {
        seleccion = (seleccion + d + opciones.Length) % opciones.Length;
        Sonar();
    }

    void Sonar()
    {
        if (sonidoSeleccion != null) fuente.PlayOneShot(sonidoSeleccion, 0.6f);
    }

    void Elegir(int i)
    {
        Sonar();
        if (i == 0) fadeSalida = 0f;
        else if (i == 1) mostrandoControles = true;
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void OnGUI()
    {
        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            sombra = new GUIStyle(estilo);
            sombra.normal.textColor = Color.black;
        }

        float w = Screen.width, h = Screen.height;
        float t = Time.time;

        // Fondo con paneo lento
        if (fondo != null)
        {
            float pan = (Mathf.Sin(t * 0.08f) + 1f) * 0.08f;
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), fondo, new Rect(pan, 0.05f, 0.8f, 0.9f));
        }
        Rectangulo(new Rect(0, 0, w, h), new Color(0.08f, 0.02f, 0.12f, 0.55f));
        Rectangulo(new Rect(0, h * 0.62f, w, h * 0.38f), new Color(0f, 0f, 0f, 0.35f));

        // Brasas doradas que caen
        foreach (Vector2 b in brasas)
            Rectangulo(new Rect(b.x * w, (1f - b.y) * h, 4, 4), new Color(1f, 0.75f, 0.2f, 0.8f));

        // Personajes
        if (diabloHuma != null)
        {
            float alto = h * 0.62f;
            float ancho = alto * diabloHuma.width / diabloHuma.height;
            GUI.DrawTexture(new Rect(w * 0.70f - ancho / 2, h * 0.33f + Mathf.Sin(t * 1.5f) * 6f, ancho, alto), diabloHuma);
        }
        if (supay != null)
        {
            float s = h * 0.2f;
            GUI.DrawTexture(new Rect(w * 0.12f, h * 0.55f + Mathf.Sin(t * 2.2f) * 12f, s, s), supay);
        }

        // Título
        int tam = Mathf.Max(20, (int)(h / 9f));
        if (inti != null)
        {
            float s = tam * 1.4f;
            float giro = Mathf.Abs(Mathf.Cos(t * 1.8f));
            GUI.DrawTexture(new Rect(w * 0.3f - s * giro / 2, h * 0.04f, s * giro, s), inti);
        }
        Texto(new Rect(0, h * 0.10f, w * 0.6f, tam * 1.6f), "DIABLO HUMA", tam, new Color(1f, 0.82f, 0.2f));
        Texto(new Rect(0, h * 0.10f + tam * 1.4f, w * 0.6f, tam), "Guardián de los Andes", tam / 2, Color.white);

        if (mostrandoControles)
        {
            PanelControles(w, h, tam);
        }
        else
        {
            // Botones
            int tb = Mathf.Max(16, tam / 2);
            bool mouseMovido = Event.current.type == EventType.Repaint && Event.current.mousePosition != ultimoMouse;
            if (Event.current.type == EventType.Repaint) ultimoMouse = Event.current.mousePosition;
            for (int i = 0; i < opciones.Length; i++)
            {
                Rect r = new Rect(w * 0.3f - w * 0.14f, h * 0.45f + i * tb * 2.2f, w * 0.28f, tb * 1.8f);
                bool encima = r.Contains(Event.current.mousePosition);
                // Si el mouse se movió sobre un botón, ese pasa a estar seleccionado
                if (encima && mouseMovido && seleccion != i) { seleccion = i; Sonar(); }
                bool activo = seleccion == i;

                Rectangulo(r, activo ? new Color(0.75f, 0.12f, 0.12f, 0.9f) : new Color(0f, 0f, 0f, 0.5f));
                if (activo) Rectangulo(new Rect(r.x, r.yMax - 4, r.width, 4), new Color(1f, 0.82f, 0.2f));
                string txt = activo ? "►  " + opciones[i] + "  ◄" : opciones[i];
                Texto(r, txt, tb, activo ? Color.white : new Color(1f, 1f, 1f, 0.75f));

                if (Event.current.type == EventType.MouseDown && encima) Elegir(i);
            }

            Texto(new Rect(0, h - tam * 0.7f, w, tam * 0.5f),
                  "↑ ↓ para elegir   ·   ENTER para aceptar", Mathf.Max(12, tam / 4), new Color(1f, 1f, 1f, 0.6f));
        }

        // Transición a negro al empezar
        if (fadeSalida > 0f)
            Rectangulo(new Rect(0, 0, w, h), new Color(0, 0, 0, fadeSalida));
    }

    // Panel de controles: tabla con la tecla en un recuadro a la izquierda y la acción a la derecha
    void PanelControles(float w, float h, int tam)
    {
        Color dorado = new Color(1f, 0.82f, 0.2f);
        int tt = Mathf.Max(14, (int)(h / 32f));
        float fila = tt * 2f;

        string[,] teclas =
        {
            { "A / D   ← →", "Moverse" },
            { "ESPACIO", "Saltar  ·  mantén = más alto" },
            { "X", "Latigazo sagrado" },
            { "C", "Girar la máscara (Inti / Killa)" },
            { "ESC", "Pausa" }
        };
        int n = teclas.GetLength(0);

        // Oscurece todo el menú y centra el panel
        Rectangulo(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.6f));
        float pw = Mathf.Min(w * 0.85f, tt * 36f);
        float ph = tt * 3f + n * fila + tt * 6.4f + tt * 2.2f;
        Rect panel = new Rect((w - pw) / 2f, (h - ph) / 2f, pw, ph);
        Rectangulo(panel, new Color(0.06f, 0.03f, 0.08f, 0.95f));
        Rectangulo(new Rect(panel.x, panel.y, panel.width, 4), dorado);
        Rectangulo(new Rect(panel.x, panel.yMax - 4, panel.width, 4), dorado);

        float y = panel.y + tt * 0.8f;
        Texto(new Rect(panel.x, y, panel.width, tt * 1.8f), "CONTROLES", tt + 8, dorado);
        y += tt * 2.4f;

        // Filas de teclas
        float margen = tt * 1.2f;
        float anchoTecla = pw * 0.3f;
        for (int i = 0; i < n; i++)
        {
            Rect tecla = new Rect(panel.x + margen, y + tt * 0.2f, anchoTecla, fila - tt * 0.4f);
            Rectangulo(tecla, new Color(0.62f, 0.12f, 0.12f, 0.95f));
            Texto(tecla, teclas[i, 0], tt, Color.white);
            TextoIzquierda(new Rect(tecla.xMax + tt, y, pw - anchoTecla - margen * 2 - tt, fila), teclas[i, 1], tt, Color.white);
            y += fila;
        }

        // Separador y explicación de la máscara
        y += tt * 0.4f;
        Rectangulo(new Rect(panel.x + margen, y, pw - margen * 2, 2), new Color(1f, 1f, 1f, 0.25f));
        y += tt * 0.6f;
        Texto(new Rect(panel.x, y, pw, tt * 1.6f), "Cara KILLA: látigo de fuego, más salto y velocidad", tt, new Color(1f, 0.6f, 0.25f));
        y += tt * 1.7f;
        Texto(new Rect(panel.x, y, pw, tt * 1.6f), "Salta sobre un Supay para vencerlo", tt, Color.white);
        y += tt * 1.7f;
        Texto(new Rect(panel.x, y, pw, tt * 1.6f), "Recoge los Inti y vence al Gran Supay", tt, dorado);

        Texto(new Rect(panel.x, panel.yMax - tt * 2f, pw, tt * 1.5f), "ESC o ENTER para volver", tt - 3, new Color(1f, 1f, 1f, 0.55f));

        if (Event.current.type == EventType.MouseDown) mostrandoControles = false;
    }

    void TextoIzquierda(Rect r, string s, int tam, Color c)
    {
        TextAnchor antes = estilo.alignment;
        estilo.alignment = sombra.alignment = TextAnchor.MiddleLeft;
        Texto(r, s, tam, c);
        estilo.alignment = sombra.alignment = antes;
    }

    void Texto(Rect r, string s, int tam, Color c)
    {
        estilo.fontSize = sombra.fontSize = tam;
        estilo.normal.textColor = c;
        float d = Mathf.Max(2, tam / 14);
        GUI.Label(new Rect(r.x + d, r.y + d, r.width, r.height), s, sombra);
        GUI.Label(r, s, estilo);
    }

    static void Rectangulo(Rect r, Color c)
    {
        Color antes = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = antes;
    }
}
