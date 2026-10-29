using UnityEngine;

// Detector de la arena del jefe: cuando el jugador entra, se cierran los muros,
// la cámara se fija en la arena y el Gran Supay despierta. Al vencerlo todo se vuelve a abrir.
[RequireComponent(typeof(Collider2D))]
public class ZonaJefe : MonoBehaviour
{
    public JefeSupay jefe;
    public GameObject[] muros;
    public float camaraX = 70.5f;
    public float camaraY = 1.5f;

    private bool iniciado;
    private bool terminado;
    private CamaraSeguir camara;

    void Start()
    {
        camara = FindAnyObjectByType<CamaraSeguir>();
        foreach (GameObject m in muros) if (m != null) m.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (iniciado || jefe == null || !otro.CompareTag("Player")) return;

        iniciado = true;
        foreach (GameObject m in muros) if (m != null) m.SetActive(true);
        if (camara != null) camara.FijarZona(camaraX, camaraY);
        if (GameManager.Instance != null) GameManager.Instance.IniciarJefe(jefe);
        jefe.Activar();
    }

    void Update()
    {
        if (!iniciado || terminado) return;
        if (jefe != null && !jefe.Muerto) return;

        terminado = true;
        foreach (GameObject m in muros) if (m != null) m.SetActive(false);
        if (camara != null) camara.LiberarZona();
    }
}
