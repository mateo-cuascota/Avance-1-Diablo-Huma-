using UnityEngine;

// Bandera de punto de control: al tocarla, el jugador reaparece aquí si cae o toca pinchos.
[RequireComponent(typeof(Collider2D))]
public class PuntoControl : MonoBehaviour
{
    public Sprite spriteInactivo;
    public Sprite spriteActivo;
    public Transform puntoReaparicion;
    public GameObject prefabDestello;

    private SpriteRenderer sr;
    private bool activo;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && spriteInactivo != null) sr.sprite = spriteInactivo;
    }

    void Update()
    {
        // La bandera activa "ondea"
        if (activo && sr != null)
            sr.transform.localScale = new Vector3(1f + Mathf.Sin(Time.time * 8f) * 0.05f, 1f, 1f);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activo || !otro.CompareTag("Player")) return;

        ReaparicionJugador r = otro.GetComponent<ReaparicionJugador>();
        if (r == null) return;

        activo = true;
        r.puntoInicio = puntoReaparicion != null ? puntoReaparicion : transform;
        if (sr != null && spriteActivo != null) sr.sprite = spriteActivo;
        Destello.Crear(prefabDestello, transform.position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.2f), 14);
        if (GameManager.Instance != null) GameManager.Instance.PuntoDeControl();
    }
}
