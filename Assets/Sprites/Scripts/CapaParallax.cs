using UnityEngine;

// Capa de fondo con efecto parallax: se mueve más lento (o más rápido) que la cámara para dar profundidad.
// La imagen se repite sola hacia los lados, así cubre el nivel entero sin importar su largo.
//   factorX = 1   -> queda fija con la cámara (muy lejos, como el cielo)
//   factorX = 0   -> se mueve igual que el mundo (como el suelo)
//   factorX < 0   -> se mueve más rápido que el mundo (primer plano, delante del jugador)
public class CapaParallax : MonoBehaviour
{
    public float factorX = 0.5f;
    public float factorY = 0.8f;
    public float viento = 0f;          // movimiento propio, por ejemplo nubes que avanzan solas

    private Transform cam;
    private Vector3 inicioCapa;
    private Vector3 inicioCam;
    private float ancho;
    private float avanceViento;

    void Start()
    {
        if (Camera.main == null) { enabled = false; return; }
        cam = Camera.main.transform;
        inicioCapa = transform.position;
        inicioCam = cam.position;

        // La imagen original es el hijo 0; se crean copias a izquierda y derecha
        SpriteRenderer original = GetComponentInChildren<SpriteRenderer>();
        if (original == null) { enabled = false; return; }
        ancho = original.bounds.size.x;

        for (int lado = -1; lado <= 1; lado += 2)
        {
            GameObject copia = Instantiate(original.gameObject, transform);
            copia.transform.localPosition = original.transform.localPosition + Vector3.right * (ancho / transform.lossyScale.x) * lado;
        }
    }

    void LateUpdate()
    {
        avanceViento += viento * Time.deltaTime;

        float x = inicioCapa.x + (cam.position.x - inicioCam.x) * factorX + avanceViento;
        float y = inicioCapa.y + (cam.position.y - inicioCam.y) * factorY;

        // Salta de a un ancho de imagen para quedar siempre centrada con la cámara
        x += Mathf.Round((cam.position.x - x) / ancho) * ancho;

        transform.position = new Vector3(x, y, inicioCapa.z);
    }
}
