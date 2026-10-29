using UnityEngine;
using UnityEngine.InputSystem;

public enum Cara { Inti, Killa }

// La máscara de dos caras del Diablo Huma (tecla C).
//   Cara INTI (sol):    látigo normal y movimiento normal.
//   Cara KILLA (luna):  látigo de fuego (doble daño), salta más alto, corre más rápido y anochece.
public class MascaraDual : MonoBehaviour
{
    public static Cara Actual { get; private set; } = Cara.Inti;
    public static bool LatigoDeFuego => Actual == Cara.Killa;
    public static int DanoLatigo => LatigoDeFuego ? 2 : 1;

    [Header("Poderes de la cara Killa")]
    public float multiplicadorVelocidad = 1.25f;
    public float multiplicadorSalto = 1.15f;
    public Color colorLatigoFuego = new Color(1f, 0.45f, 0.1f);

    [Header("Cambio de cara")]
    public float enfriamiento = 0.3f;
    public AudioClip sonidoInti;
    public AudioClip sonidoKilla;
    public GameObject prefabDestello;

    [Header("Noche en la cara Killa")]
    public Color tinteKilla = new Color(0.5f, 0.48f, 0.85f);
    public string nombreFondo = "Fondo_Parallax";

    private Transform cabeza;
    private SpriteRenderer[] fondo;
    private Color tinteActual = Color.white;
    private SpriteRenderer latigo;
    private float escalaCabeza = 1f;
    private float giro = -1f;
    private float proximo;

    // Los usa MovimientoJugador
    public float MultVelocidad => Actual == Cara.Killa ? multiplicadorVelocidad : 1f;
    public float MultSalto => Actual == Cara.Killa ? multiplicadorSalto : 1f;

    void Awake()
    {
        Actual = Cara.Inti;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLower().Contains("cabeza")) { cabeza = t; escalaCabeza = t.localScale.x; }
            if (t.name == "Latigo") latigo = t.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (Time.timeScale > 0f && Keyboard.current != null &&
            Keyboard.current.cKey.wasPressedThisFrame && Time.time >= proximo)
            Cambiar();

        // El escenario anochece en la cara Killa (las copias del parallax existen después de Start)
        if (fondo == null || fondo.Length == 0)
        {
            GameObject f = GameObject.Find(nombreFondo);
            if (f != null) fondo = f.GetComponentsInChildren<SpriteRenderer>();
        }
        Color objetivo = Actual == Cara.Killa ? tinteKilla : Color.white;
        tinteActual = Color.Lerp(tinteActual, objetivo, 4f * Time.unscaledDeltaTime);
        if (fondo != null)
            foreach (SpriteRenderer sr in fondo)
                if (sr != null) sr.color = new Color(tinteActual.r, tinteActual.g, tinteActual.b, sr.color.a);

        // Animación de la máscara girando: la cabeza se "da la vuelta"
        if (cabeza != null && giro >= 0f)
        {
            giro += Time.deltaTime / 0.3f;
            Vector3 e = cabeza.localScale;
            e.x = escalaCabeza * Mathf.Cos(Mathf.Min(giro, 1f) * Mathf.PI * 2f);
            cabeza.localScale = e;
            if (giro >= 1f) giro = -1f;
        }

        // El látigo se ve encendido en la cara Killa (parpadea como fuego)
        if (latigo != null)
            latigo.color = LatigoDeFuego
                ? Color.Lerp(colorLatigoFuego, new Color(1f, 0.85f, 0.3f), Mathf.PingPong(Time.time * 6f, 1f))
                : Color.white;
    }

    public void Cambiar()
    {
        Actual = Actual == Cara.Inti ? Cara.Killa : Cara.Inti;
        proximo = Time.time + enfriamiento;
        giro = 0f;

        Color c = Actual == Cara.Inti ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.5f, 0.15f);
        Destello.Crear(prefabDestello, transform.position, c, 12);
        if (GameManager.Instance != null)
            GameManager.Instance.ReproducirSonido(Actual == Cara.Inti ? sonidoInti : sonidoKilla);
    }
}
