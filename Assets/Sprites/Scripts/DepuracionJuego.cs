using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Herramientas de prueba y depuración (para probar rápido y mostrar en la presentación):
//   F1  muestra/oculta el panel de datos (FPS, posición, velocidad, suelo, cara, vidas, Inti)
//   F2  teletransporta al siguiente punto de control
//   F3  restaura las vidas
//   F4  teletransporta a la entrada de la arena del jefe
public class DepuracionJuego : MonoBehaviour
{
    public bool mostrarPanel;
    public float xArenaJefe = 62.5f;

    private MovimientoJugador jugador;
    private Rigidbody2D rbJugador;
    private PuntoControl[] puntos;
    private int indicePunto = -1;
    private float fps;

    void Start()
    {
        jugador = FindAnyObjectByType<MovimientoJugador>();
        if (jugador != null) rbJugador = jugador.GetComponent<Rigidbody2D>();
        puntos = FindObjectsByType<PuntoControl>()
                 .OrderBy(p => p.transform.position.x).ToArray();
    }

    void Update()
    {
        fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);

        Keyboard k = Keyboard.current;
        if (k == null || jugador == null) return;

        if (k.f1Key.wasPressedThisFrame) mostrarPanel = !mostrarPanel;
        if (k.f3Key.wasPressedThisFrame && GameManager.Instance != null) GameManager.Instance.RestaurarVidas();

        if (k.f2Key.wasPressedThisFrame && puntos.Length > 0)
        {
            indicePunto = (indicePunto + 1) % puntos.Length;
            Teletransportar(puntos[indicePunto].transform.position + Vector3.up * 1.8f, "punto de control " + (indicePunto + 1));
        }

        if (k.f4Key.wasPressedThisFrame)
            Teletransportar(new Vector3(xArenaJefe, -0.8f, 0f), "arena del jefe");
    }

    void Teletransportar(Vector3 destino, string nombre)
    {
        jugador.transform.position = destino;
        rbJugador.linearVelocity = Vector2.zero;
        if (GameManager.Instance != null) GameManager.Instance.MostrarMensaje("[Prueba] Teletransporte a " + nombre);
    }

    void OnGUI()
    {
        if (!mostrarPanel || jugador == null) return;

        GameManager gm = GameManager.Instance;
        string texto =
            "DEPURACIÓN (F1)\n" +
            "FPS: " + Mathf.RoundToInt(fps) + "\n" +
            "Posición: " + jugador.transform.position.ToString("F1") + "\n" +
            "Velocidad: " + rbJugador.linearVelocity.ToString("F1") + "\n" +
            "En suelo: " + (jugador.EnSuelo ? "sí" : "no") + "\n" +
            "Cara: " + MascaraDual.Actual + "\n" +
            (gm != null ? "Vidas: " + gm.Vidas + "   Inti: " + gm.MonedasRecogidas + "/" + gm.TotalMonedas + "\n" : "") +
            "Rocas activas: " + FindObjectsByType<Roca>().Length + "\n" +
            "Fuegos activos: " + FindObjectsByType<ProyectilFuego>().Length + "\n" +
            "F2 punto de control · F3 vidas · F4 jefe";

        Rect r = new Rect(Screen.width - 340, Screen.height * 0.25f, 320, 230);
        Color antes = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = antes;
        GUI.Label(new Rect(r.x + 10, r.y + 8, r.width - 20, r.height - 16), texto);
    }
}
