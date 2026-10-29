using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Escena de historia: diapositivas con texto que se escribe letra por letra,
// fondo con zoom lento y transición con fundido. ESPACIO/ENTER/clic avanza, ESC salta todo.
public class Historia : MonoBehaviour
{
    [System.Serializable]
    public class Diapositiva
    {
        [TextArea(2, 4)] public string texto;
        public Texture2D imagen;            // personaje u objeto que aparece en el centro
        public Color tinte = new Color(0f, 0f, 0f, 0.35f);
    }

    public Texture2D fondo;
    public Diapositiva[] diapositivas;
    public AudioClip musica;
    public AudioClip sonidoLetra;
    public string escenaJuego = "Nivel 1";
    public float letrasPorSegundo = 38f;

    private int actual;
    private float letras;
    private float tiempoDiapositiva;
    private float fade = 1f;          // 1 = negro, 0 = visible
    private bool saliendo;
    private int siguiente = -1;

    private AudioSource fuente;
    private GUIStyle estilo;
    private GUIStyle sombra;

    void Awake()
    {
        Time.timeScale = 1f;
        fuente = gameObject.AddComponent<AudioSource>();
        if (musica != null)
        {
            AudioSource m = gameObject.AddComponent<AudioSource>();
            m.clip = musica;
            m.loop = true;
            m.volume = 0.3f;
            m.Play();
        }
    }

    void Update()
    {
        // Fundidos de entrada y salida
        if (saliendo || siguiente >= 0)
        {
            fade += Time.deltaTime * 2f;
            if (fade >= 1f)
            {
                if (saliendo) { SceneManager.LoadScene(escenaJuego); return; }
                actual = siguiente;
                siguiente = -1;
                letras = 0f;
                tiempoDiapositiva = 0f;
            }
            return;
        }

        fade = Mathf.Max(0f, fade - Time.deltaTime * 2f);
        tiempoDiapositiva += Time.deltaTime;

        string texto = diapositivas[actual].texto;
        int antes = (int)letras;
        letras = Mathf.Min(texto.Length, letras + letrasPorSegundo * Time.deltaTime);
        if ((int)letras != antes && sonidoLetra != null && (int)letras % 3 == 0)
            fuente.PlayOneShot(sonidoLetra, 0.15f);

        Keyboard k = Keyboard.current;
        bool avanzar = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (k != null)
        {
            avanzar |= k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame;
            if (k.escapeKey.wasPressedThisFrame) { saliendo = true; return; }
        }

        if (avanzar)
        {
            if (letras < texto.Length) letras = texto.Length;    // primero completa el texto
            else if (actual + 1 < diapositivas.Length) siguiente = actual + 1;
            else saliendo = true;
        }
    }

    void OnGUI()
    {
        if (diapositivas == null || diapositivas.Length == 0) return;

        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter, wordWrap = true };
            sombra = new GUIStyle(estilo);
            sombra.normal.textColor = Color.black;
        }

        float w = Screen.width, h = Screen.height;
        Diapositiva d = diapositivas[actual];

        // Fondo con zoom lento (efecto "Ken Burns")
        if (fondo != null)
        {
            float z = 0.85f - tiempoDiapositiva * 0.01f;
            z = Mathf.Max(0.7f, z);
            float x = (actual % 2 == 0) ? (1f - z) * 0.2f : (1f - z) * 0.8f;
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, w, h), fondo, new Rect(x, (1f - z) * 0.5f, z, z));
        }
        Rectangulo(new Rect(0, 0, w, h), d.tinte);

        // Imagen de la diapositiva: aparece desde abajo y flota
        if (d.imagen != null)
        {
            float entrada = Mathf.Clamp01(tiempoDiapositiva * 1.5f);
            float alto = h * 0.5f;
            float ancho = alto * d.imagen.width / d.imagen.height;
            float y = h * 0.1f + (1f - entrada) * 40f + Mathf.Sin(Time.time * 1.6f) * 6f;
            Color antes = GUI.color;
            GUI.color = new Color(1, 1, 1, entrada);
            GUI.DrawTexture(new Rect(w / 2 - ancho / 2, y, ancho, alto), d.imagen);
            GUI.color = antes;
        }

        // Caja de texto
        int tam = Mathf.Max(16, (int)(h / 26f));
        Rect caja = new Rect(w * 0.08f, h * 0.66f, w * 0.84f, h * 0.26f);
        Rectangulo(caja, new Color(0.05f, 0.02f, 0.08f, 0.82f));
        Rectangulo(new Rect(caja.x, caja.y, caja.width, 4), new Color(1f, 0.82f, 0.2f));

        string visible = d.texto.Substring(0, Mathf.Clamp((int)letras, 0, d.texto.Length));
        Rect rt = new Rect(caja.x + tam, caja.y + tam * 0.8f, caja.width - tam * 2, caja.height - tam);
        estilo.fontSize = sombra.fontSize = tam;
        estilo.normal.textColor = Color.white;
        GUI.Label(new Rect(rt.x + 2, rt.y + 2, rt.width, rt.height), visible, sombra);
        GUI.Label(rt, visible, estilo);

        // Indicadores
        int tp = Mathf.Max(11, tam * 2 / 3);
        estilo.fontSize = sombra.fontSize = tp;
        estilo.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
        GUI.Label(new Rect(caja.x, caja.yMax - tp * 1.8f, caja.width - tam, tp * 1.5f),
                  (actual + 1) + " / " + diapositivas.Length + "      ESC para saltar", estilo);
        if (letras >= d.texto.Length && Mathf.Repeat(Time.time, 1f) < 0.6f)
        {
            estilo.normal.textColor = new Color(1f, 0.82f, 0.2f);
            string pista = actual + 1 < diapositivas.Length ? "ESPACIO ►" : "¡COMENZAR! ►";
            GUI.Label(new Rect(caja.xMax - tam * 9, caja.yMax - tp * 1.8f, tam * 8, tp * 1.5f), pista, estilo);
        }

        if (fade > 0f) Rectangulo(new Rect(0, 0, w, h), new Color(0, 0, 0, fade));
    }

    static void Rectangulo(Rect r, Color c)
    {
        Color antes = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = antes;
    }
}
