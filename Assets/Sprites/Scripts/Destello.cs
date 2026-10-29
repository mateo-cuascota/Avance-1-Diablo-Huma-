using UnityEngine;

// Efecto de partículas hecho con sprites: se instancia al recoger, golpear o destruir algo.
// Crea varias chispas que salen disparadas, se achican, se desvanecen y luego se destruye solo.
public class Destello : MonoBehaviour
{
    public Sprite particula;
    public Color color = Color.white;
    public int cantidad = 12;
    public float velocidad = 4f;
    public float duracion = 0.5f;
    public float tamano = 0.3f;

    private Transform[] chispas;
    private SpriteRenderer[] renders;
    private Vector2[] direcciones;
    private float inicio;

    // Instancia el prefab en una posición con un color (atajo para los demás scripts)
    public static Destello Crear(GameObject prefab, Vector3 posicion, Color color, int cantidad = 12)
    {
        if (prefab == null) return null;
        GameObject go = Instantiate(prefab, posicion, Quaternion.identity);
        Destello d = go.GetComponent<Destello>();
        if (d != null)
        {
            d.color = color;
            d.cantidad = cantidad;
        }
        return d;
    }

    // Nube de polvo pequeña y lenta (al saltar y aterrizar)
    public static void Polvo(GameObject prefab, Vector3 posicion, int cantidad = 7)
    {
        Destello d = Crear(prefab, posicion, new Color(0.75f, 0.62f, 0.45f, 0.8f), cantidad);
        if (d == null) return;
        d.velocidad = 2.2f;
        d.tamano = 0.45f;
        d.duracion = 0.4f;
    }

    void Start()
    {
        inicio = Time.time;
        chispas = new Transform[cantidad];
        renders = new SpriteRenderer[cantidad];
        direcciones = new Vector2[cantidad];

        for (int i = 0; i < cantidad; i++)
        {
            GameObject c = new GameObject("Chispa");
            c.transform.SetParent(transform, false);
            c.transform.localScale = Vector3.one * tamano * Random.Range(0.6f, 1.3f);
            SpriteRenderer sr = c.AddComponent<SpriteRenderer>();
            sr.sprite = particula;
            sr.color = color;
            sr.sortingOrder = 20;
            chispas[i] = c.transform;
            renders[i] = sr;
            float ang = (i / (float)cantidad) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            direcciones[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * velocidad * Random.Range(0.5f, 1.2f);
        }

        Destroy(gameObject, duracion);
    }

    void Update()
    {
        float p = (Time.time - inicio) / duracion;
        for (int i = 0; i < chispas.Length; i++)
        {
            chispas[i].localPosition += (Vector3)(direcciones[i] * Time.deltaTime);
            direcciones[i] *= 0.92f;
            Color c = color;
            c.a = 1f - p;
            renders[i].color = c;
        }
        transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, p);
    }
}
