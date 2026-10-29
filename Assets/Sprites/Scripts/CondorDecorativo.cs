using UnityEngine;

// Cóndor de decoración: vuela por el cielo de un extremo al otro del nivel (el aleteo lo hace su Animator).
public class CondorDecorativo : MonoBehaviour
{
    public float velocidad = 2f;
    public float xMin = -10f;
    public float xMax = 60f;
    public float ondulacion = 0.4f;

    private float yBase;
    private float desfase;

    void Start()
    {
        yBase = transform.position.y;
        desfase = Random.Range(0f, 10f);
        Vector3 e = transform.localScale;
        e.x = Mathf.Abs(e.x) * Mathf.Sign(velocidad);
        transform.localScale = e;
    }

    void Update()
    {
        Vector3 p = transform.position;
        p.x += velocidad * Time.deltaTime;
        p.y = yBase + Mathf.Sin((Time.time + desfase) * 0.8f) * ondulacion;
        if (velocidad > 0 && p.x > xMax) p.x = xMin;
        if (velocidad < 0 && p.x < xMin) p.x = xMax;
        transform.position = p;
    }
}
