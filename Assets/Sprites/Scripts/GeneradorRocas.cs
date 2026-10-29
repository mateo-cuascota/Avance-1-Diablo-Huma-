using UnityEngine;

// Instancia rocas que caen desde arriba de la cámara mientras el jugador está dentro de la zona.
public class GeneradorRocas : MonoBehaviour
{
    public GameObject prefabRoca;
    public float xMin = 0f;
    public float xMax = 10f;
    public float intervalo = 1.6f;
    public float variacion = 0.6f;

    private Transform jugador;
    private Camera cam;
    private float proxima;

    void Start()
    {
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;
        cam = Camera.main;
        proxima = Time.time + intervalo;
    }

    void Update()
    {
        if (jugador == null || prefabRoca == null || cam == null) return;

        float x = jugador.position.x;
        if (x < xMin - 3f || x > xMax + 3f) return;    // solo cuando el jugador está cerca
        if (Time.time < proxima) return;

        proxima = Time.time + intervalo + Random.Range(-variacion, variacion);

        // Aparece cerca del jugador (un poco adelante) y justo en el borde superior de la cámara
        float xRoca = Mathf.Clamp(x + Random.Range(-2f, 5f), xMin, xMax);
        float yRoca = cam.transform.position.y + cam.orthographicSize - 0.4f;
        Instantiate(prefabRoca, new Vector3(xRoca, yRoca, 0f), Quaternion.identity);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawLine(new Vector3(xMin, transform.position.y), new Vector3(xMax, transform.position.y));
        Gizmos.DrawWireCube(new Vector3(xMin, transform.position.y), Vector3.one * 0.4f);
        Gizmos.DrawWireCube(new Vector3(xMax, transform.position.y), Vector3.one * 0.4f);
    }
}
