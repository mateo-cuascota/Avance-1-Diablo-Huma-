using UnityEngine;

// Cámara que sigue al jugador mirando un poco hacia adelante, sin salirse de los límites del nivel,
// y que se sacude cuando el jugador recibe daño.
public class CamaraSeguir : MonoBehaviour
{
    public Transform jugador;
    public float suavidad = 5f;
    public float adelanto = 1.5f;            // mira hacia donde camina el jugador

    [Header("Límites del nivel")]
    public bool usarLimites;
    public float minX = -100f, maxX = 100f;
    public float minY = -100f, maxY = 100f;

    private MovimientoJugador movimiento;
    private float adelantoActual;
    private float fuerzaSacudida;
    private float finSacudida;

    void Start()
    {
        if (jugador != null) movimiento = jugador.GetComponent<MovimientoJugador>();
    }

    private bool zonaFija;
    private float zonaX, zonaY;

    // Durante la pelea con el jefe la cámara queda quieta mostrando toda la arena
    public void FijarZona(float x, float y)
    {
        zonaFija = true;
        zonaX = x;
        zonaY = y;
    }

    public void LiberarZona()
    {
        zonaFija = false;
    }

    public void Sacudir(float fuerza, float duracion)
    {
        fuerzaSacudida = fuerza;
        finSacudida = Time.unscaledTime + duracion;
    }

    void LateUpdate()
    {
        if (jugador == null)
            return;

        float objetivoAdelanto = movimiento != null ? (movimiento.Mirando ? adelanto : -adelanto) : 0f;
        adelantoActual = Mathf.Lerp(adelantoActual, objetivoAdelanto, 2f * Time.deltaTime);

        Vector3 posicionDeseada = new Vector3(
            jugador.position.x + adelantoActual,
            jugador.position.y,
            transform.position.z
        );

        Vector3 pos = Vector3.Lerp(
            transform.position,
            posicionDeseada,
            suavidad * Time.deltaTime
        );

        if (zonaFija)
        {
            pos.x = Mathf.Lerp(transform.position.x, zonaX, 3f * Time.deltaTime);
            pos.y = Mathf.Lerp(transform.position.y, zonaY, 3f * Time.deltaTime);
        }

        if (usarLimites)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
        }

        transform.position = pos;

        if (Time.unscaledTime < finSacudida)
            transform.position += (Vector3)(Random.insideUnitCircle * fuerzaSacudida);
    }
}
