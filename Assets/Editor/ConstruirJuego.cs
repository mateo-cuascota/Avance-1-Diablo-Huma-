#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Herramienta de editor del Avance P1. Menú: Diablo Huma > Construir juego completo (Avance P1)
// 1. Dibuja por código los sprites nuevos (Supay, cóndor, roca, fuego, choclo, pluma, bandera, chispa).
// 2. Crea sus animaciones (Animator) y los prefabs que se instancian en el juego.
// 3. Amplía el Nivel 1: más suelo y plataformas, monedas, enemigos, rocas, puntos de control y meta al final.
// 4. Crea las escenas MenuPrincipal e Historia y las pone en el Build en orden.
// Se puede ejecutar varias veces: borra lo que generó antes y lo vuelve a crear.
public static class ConstruirJuego
{
    const string CarpetaGen = "Assets/Sprites/Generados";
    const string CarpetaAnim = "Assets/Sprites/Generados/Animaciones";
    const string CarpetaPrefabs = "Assets/Prefabs";
    const string RutaNivel = "Assets/Scenes/Nivel 1.unity";
    const string RutaMenu = "Assets/Scenes/MenuPrincipal.unity";
    const string RutaHistoria = "Assets/Scenes/Historia.unity";

    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    static readonly Dictionary<string, AudioClip> sonidos = new Dictionary<string, AudioClip>();

    [MenuItem("Diablo Huma/Construir juego completo (Avance P1)")]
    public static void Construir()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        CrearCarpeta("Assets/Sprites", "Generados");
        CrearCarpeta(CarpetaGen, "Animaciones");
        CrearCarpeta("Assets", "Prefabs");

        GenerarSprites();
        GenerarSonidos();
        CrearPrefabs();
        AmpliarNivel1();
        CrearMenu();
        CrearHistoria();
        ConfigurarBuild();

        EditorSceneManager.OpenScene(RutaMenu, OpenSceneMode.Single);
        Debug.Log("Avance P1 construido: menú, historia, enemigos Supay, rocas, fuego, power-ups, puntos de control y nivel ampliado. Dale Play desde MenuPrincipal.");
    }

    static void CrearCarpeta(string padre, string nombre)
    {
        if (!AssetDatabase.IsValidFolder(padre + "/" + nombre))
            AssetDatabase.CreateFolder(padre, nombre);
    }

    // =====================================================================
    // 1. SPRITES DIBUJADOS POR CÓDIGO
    // =====================================================================

    class Lienzo
    {
        public readonly int W, H;
        readonly Color[] px;

        public Lienzo(int w, int h)
        {
            W = w; H = h;
            px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        }

        public void Pintar(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Color d = px[y * W + x];
            float a = c.a + d.a * (1f - c.a);
            if (a <= 0f) return;
            Color r = (c * c.a + d * d.a * (1f - c.a)) / a;
            r.a = a;
            px[y * W + x] = r;
        }

        public void Elipse(float cx, float cy, float rx, float ry, Color c, float ang = 0f)
        {
            float cos = Mathf.Cos(-ang), sin = Mathf.Sin(-ang);
            float r = Mathf.Max(rx, ry);
            for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
                for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float ex = dx * cos - dy * sin, ey = dx * sin + dy * cos;
                    float d = (ex * ex) / (rx * rx) + (ey * ey) / (ry * ry);
                    if (d <= 1f)
                    {
                        // Borde suavizado
                        float borde = Mathf.Clamp01((1f - d) * Mathf.Min(rx, ry) * 0.5f);
                        Color cc = c;
                        cc.a *= Mathf.Clamp01(borde * 2f);
                        Pintar(x, y, cc);
                    }
                }
        }

        public void Circulo(float cx, float cy, float r, Color c) { Elipse(cx, cy, r, r, c); }

        public void Poligono(Color c, params Vector2[] p)
        {
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (Vector2 v in p) { x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y); }
            for (int y = (int)y0; y <= y1; y++)
                for (int x = (int)x0; x <= x1; x++)
                {
                    Vector2 q = new Vector2(x + 0.5f, y + 0.5f);
                    bool dentro = false;
                    for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                        if ((p[i].y > q.y) != (p[j].y > q.y) &&
                            q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                            dentro = !dentro;
                    if (dentro) Pintar(x, y, c);
                }
        }

        public void Rectangulo(float x0, float y0, float x1, float y1, Color c)
        {
            for (int y = (int)y0; y < y1; y++)
                for (int x = (int)x0; x < x1; x++)
                    Pintar(x, y, c);
        }

        // Agrega un borde oscuro alrededor de lo dibujado (estilo caricatura)
        // Variación aleatoria de brillo pixel por pixel (textura granulada)
        public void Ruido(float cantidad, int semilla)
        {
            var r = new System.Random(semilla);
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a < 0.5f) continue;
                float f = 1f + ((float)r.NextDouble() * 2f - 1f) * cantidad;
                px[i] = new Color(px[i].r * f, px[i].g * f, px[i].b * f, px[i].a);
            }
        }

        public void Contorno(Color c, int grosor)
        {
            Color[] copia = (Color[])px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (copia[y * W + x].a > 0.5f) continue;
                    bool cerca = false;
                    for (int oy = -grosor; oy <= grosor && !cerca; oy++)
                        for (int ox = -grosor; ox <= grosor && !cerca; ox++)
                        {
                            int nx = x + ox, ny = y + oy;
                            if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                            if (ox * ox + oy * oy > grosor * grosor) continue;
                            if (copia[ny * W + nx].a > 0.5f) cerca = true;
                        }
                    if (cerca) px[y * W + x] = c;
                }
        }

        public Sprite Guardar(string nombre, float ppu, Vector2 pivote)
        {
            string ruta = CarpetaGen + "/" + nombre + ".png";
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.filterMode = FilterMode.Bilinear;
            var ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
            ajustes.spritePivot = pivote;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();

            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            sprites[nombre] = s;
            return s;
        }
    }

    static Color C(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }
    static Vector2 V(float x, float y) { return new Vector2(x, y); }
    static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);

    static void GenerarSprites()
    {
        // --- Supay: 4 cuadros flotando + 1 disparando ---
        for (int f = 0; f < 5; f++)
            DibujarSupay(f).Guardar(f < 4 ? "Supay_" + f : "Supay_Disparo", 70f, Centro);

        // --- Cóndor: alas arriba, medio, abajo ---
        for (int f = 0; f < 3; f++)
            DibujarCondor(f).Guardar("Condor_" + f, 60f, Centro);

        // --- Roca ---
        {
            var l = new Lienzo(64, 64);
            Random.InitState(7);
            var puntos = new Vector2[11];
            for (int i = 0; i < puntos.Length; i++)
            {
                float a = i / (float)puntos.Length * Mathf.PI * 2f;
                float r = Random.Range(22f, 28f);
                puntos[i] = V(32 + Mathf.Cos(a) * r, 32 + Mathf.Sin(a) * r);
            }
            l.Poligono(C(0.45f, 0.41f, 0.38f), puntos);
            l.Elipse(25, 40, 11, 6, C(0.62f, 0.58f, 0.53f), 0.4f);
            l.Circulo(40, 24, 5, C(0.34f, 0.31f, 0.29f));
            l.Circulo(22, 22, 3, C(0.34f, 0.31f, 0.29f));
            l.Circulo(44, 40, 3, C(0.34f, 0.31f, 0.29f));
            l.Contorno(C(0.15f, 0.12f, 0.1f), 2);
            l.Guardar("Roca", 55f, Centro);
        }

        // --- Bola de fuego (2 cuadros, mira a la derecha) ---
        for (int f = 0; f < 2; f++)
        {
            var l = new Lienzo(64, 64);
            float k = f == 0 ? 0f : 2f;
            l.Circulo(40, 32, 22, C(1f, 0.4f, 0f, 0.25f));
            l.Poligono(C(1f, 0.42f, 0.05f), V(36, 44 + k), V(4 - k * 2, 32), V(36, 20 - k));
            l.Poligono(C(1f, 0.7f, 0.1f), V(38, 38), V(14 - k * 2, 32 + k), V(38, 26));
            l.Circulo(40, 32, 17 + k * 0.5f, C(1f, 0.45f, 0.05f));
            l.Circulo(42, 32, 11, C(1f, 0.75f, 0.15f));
            l.Circulo(44, 32, 5 + k * 0.5f, C(1f, 1f, 0.85f));
            l.Guardar("Fuego_" + f, 70f, Centro);
        }

        // --- Choclo sagrado (power-up de vida) ---
        {
            var l = new Lienzo(64, 64);
            l.Circulo(32, 32, 30, C(1f, 0.9f, 0.3f, 0.25f));
            l.Elipse(32, 38, 11, 22, C(1f, 0.8f, 0.18f));
            for (int y = 22; y <= 56; y += 6)
                for (int x = 25; x <= 39; x += 6)
                    if (((x - 32) * (x - 32)) / 121f + ((y - 38) * (y - 38)) / 484f < 0.8f)
                        l.Circulo(x + (y / 6 % 2) * 2, y, 2.2f, C(0.95f, 0.62f, 0.08f));
            l.Poligono(C(0.3f, 0.65f, 0.2f), V(32, 4), V(12, 38), V(27, 30));
            l.Poligono(C(0.25f, 0.55f, 0.15f), V(32, 4), V(52, 38), V(37, 30));
            l.Contorno(C(0.25f, 0.15f, 0.05f), 2);
            l.Guardar("Choclo", 55f, Centro);
        }

        // --- Pluma de cóndor (power-up de doble salto) ---
        {
            var l = new Lienzo(64, 64);
            l.Circulo(32, 32, 30, C(0.6f, 0.85f, 1f, 0.25f));
            l.Elipse(32, 32, 26, 9, C(0.93f, 0.93f, 0.97f), 0.75f);
            l.Elipse(43, 45, 11, 7, C(0.12f, 0.12f, 0.15f), 0.75f);
            l.Elipse(32, 32, 26, 1.3f, C(0.55f, 0.55f, 0.6f), 0.75f);
            l.Contorno(C(0.15f, 0.2f, 0.35f), 2);
            l.Guardar("Pluma", 55f, Centro);
        }

        // --- Bandera de punto de control (apagada y con la bandera del Ecuador) ---
        // Doble resolución (128x320 a 100 px/unidad = mismo tamaño en el juego) para que se vea el escudo
        for (int f = 0; f < 2; f++)
        {
            var l = new Lienzo(128, 320);
            l.Elipse(22, 10, 26, 12, C(0.42f, 0.4f, 0.38f));                 // base de piedra
            l.Rectangulo(16, 4, 28, 304, C(0.45f, 0.3f, 0.15f));              // asta de madera
            l.Rectangulo(16, 4, 20, 304, C(0.55f, 0.38f, 0.2f));              // brillo del asta
            l.Circulo(22, 306, 7, C(1f, 0.82f, 0.2f));                         // remate dorado
            if (f == 0)
            {
                l.Poligono(C(0.55f, 0.55f, 0.55f), V(28, 80), V(104, 60), V(28, 40));   // bandera arriada
            }
            else
            {
                // Bandera del Ecuador: amarillo (mitad), azul y rojo (un cuarto cada uno)
                l.Rectangulo(28, 258, 116, 292, C(1f, 0.82f, 0f));
                l.Rectangulo(28, 241, 116, 258, C(0.02f, 0.2f, 0.6f));
                l.Rectangulo(28, 224, 116, 241, C(0.84f, 0.07f, 0.12f));

                // Escudo de armas (simplificado): óvalo con el Chimborazo, el sol y el cóndor encima
                float cx = 72, cy = 256;
                l.Elipse(cx, cy, 12, 15, C(0.8f, 0.6f, 0.12f));                  // marco dorado
                l.Elipse(cx, cy, 10, 13, C(0.5f, 0.75f, 0.95f));                 // cielo
                l.Elipse(cx, cy - 9, 9, 4, C(0.2f, 0.55f, 0.25f));               // campo verde
                l.Poligono(C(0.55f, 0.55f, 0.6f), V(cx - 8, cy - 7), V(cx, cy + 6), V(cx + 8, cy - 7));   // Chimborazo
                l.Poligono(Color.white, V(cx - 2.5f, cy + 2), V(cx, cy + 6), V(cx + 2.5f, cy + 2));     // nieve
                l.Rectangulo(cx - 7, cy - 10, cx + 7, cy - 9, C(0.25f, 0.5f, 0.85f));                   // río Guayas
                l.Circulo(cx, cy + 9.5f, 2.6f, C(1f, 0.85f, 0.15f));             // sol
                l.Poligono(C(0.08f, 0.08f, 0.1f), V(cx - 2, cy + 16), V(cx - 17, cy + 23), V(cx - 12, cy + 15));   // cóndor
                l.Poligono(C(0.08f, 0.08f, 0.1f), V(cx + 2, cy + 16), V(cx + 17, cy + 23), V(cx + 12, cy + 15));
                l.Elipse(cx, cy + 16, 3.5f, 2.5f, C(0.08f, 0.08f, 0.1f));
                l.Circulo(cx, cy + 19.5f, 1.8f, C(0.08f, 0.08f, 0.1f));

                // Pliegues de tela
                for (int x = 40; x < 116; x += 24)
                    l.Rectangulo(x, 224, x + 9, 292, C(0f, 0f, 0f, 0.08f));
            }
            l.Contorno(C(0.12f, 0.08f, 0.05f), 3);
            l.Guardar(f == 0 ? "Bandera_Off" : "Bandera_On", 100f, V(22f / 128f, 0f));
        }

        // --- Chispa para los destellos ---
        {
            var l = new Lienzo(16, 16);
            for (int r = 8; r > 0; r--)
                l.Circulo(8, 8, r, C(1f, 1f, 1f, 0.18f));
            l.Guardar("Chispa", 16f, Centro);
        }

        GenerarCapasFondo();
        GenerarPiedraYPasto();

        // Luna creciente (ícono de la cara Killa)
        {
            var l = new Lienzo(64, 64);
            l.Circulo(32, 32, 30, C(0.6f, 0.7f, 1f, 0.25f));
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float a = (x - 30) * (x - 30) + (y - 32) * (y - 32);
                    float b = (x - 42) * (x - 42) + (y - 38) * (y - 38);
                    if (a < 25 * 25 && b > 21 * 21) l.Pintar(x, y, C(0.82f, 0.87f, 1f));
                }
            l.Contorno(C(0.15f, 0.2f, 0.4f), 2);
            l.Guardar("Luna", 64f, Centro);
        }
    }

    // ---------- Piedra andina (muro inca) y pasto: se repiten sin cortes en suelos y plataformas ----------
    static void GenerarPiedraYPasto()
    {
        // 128x128 a 128 px/unidad = 1 unidad de juego por mosaico
        {
            const int T = 128;
            var l = new Lienzo(T, T);
            l.Rectangulo(0, 0, T, T, C(0.17f, 0.14f, 0.12f));      // juntura oscura
            Random.InitState(11);
            int[] filas = { 30, 34, 32, 32 };                    // suman 128: se repite en vertical
            int y0 = 0;
            for (int fila = 0; fila < filas.Length; fila++)
            {
                int alto = filas[fila];
                // Anchos que suman exactamente 128 para que se repita en horizontal
                var anchos = new List<float>();
                float suma = 0;
                while (suma < T) { float a = Random.Range(34f, 58f); anchos.Add(a); suma += a; }
                float x = Random.Range(0f, 40f);
                foreach (float a0 in anchos)
                {
                    float a = a0 * T / suma;
                    // Andesita: grises cálidos con variación por piedra
                    Color c = Color.Lerp(C(0.44f, 0.4f, 0.36f), C(0.64f, 0.6f, 0.53f), Random.value);
                    c = Color.Lerp(c, C(0.55f, 0.5f, 0.4f), Random.Range(0f, 0.3f));
                    bool grieta = Random.value < 0.35f;
                    bool musgo = fila == filas.Length - 1 && Random.value < 0.6f;
                    int semilla = Random.Range(0, 99999);
                    for (int k = -1; k <= 1; k++)
                        Bloque(l, x + k * T + 2f, y0 + 2f, x + k * T + a - 2f, y0 + alto - 2f, c, grieta, musgo, semilla);
                    x += a;
                }
                y0 += alto;
            }
            l.Ruido(0.07f, 5);                                   // textura granulada de la piedra
            l.Guardar("Piedra", 128f, Centro);
        }

        // Pasto: 128x64 a 128 px/unidad (1 x 0.5 unidades)
        {
            var l = new Lienzo(128, 64);
            Random.InitState(3);
            l.Rectangulo(0, 0, 128, 12, C(0f, 0f, 0f, 0.3f));   // sombra que el pasto proyecta sobre la piedra
            l.Rectangulo(0, 10, 128, 28, C(0.27f, 0.52f, 0.18f));
            for (int x = 0; x < 128; x += 6)                    // borde irregular que cuelga
                for (int k = -1; k <= 1; k++)
                    l.Circulo(x + 3 + k * 128, 11, Random.Range(3f, 5.5f), C(0.22f, 0.44f, 0.15f));
            for (int i = 0; i < 120; i++)                       // hojas de pasto de distintos verdes
            {
                float x = Random.Range(0f, 128f), h = Random.Range(10f, 34f), inc = Random.Range(-6f, 6f);
                Color c = Color.Lerp(C(0.2f, 0.42f, 0.13f), C(0.5f, 0.75f, 0.3f), Random.value);
                for (int k = -1; k <= 1; k++)
                    l.Poligono(c, V(x - 2.4f + k * 128, 24), V(x + 2.4f + k * 128, 24), V(x + inc + k * 128, 24 + h));
            }
            for (int i = 0; i < 9; i++)                         // florcitas
            {
                float x = Random.Range(0f, 128f), y = Random.Range(30f, 46f);
                Color c = i % 3 == 0 ? C(1f, 0.9f, 0.3f) : (i % 3 == 1 ? Color.white : C(0.85f, 0.4f, 0.8f));
                for (int k = -1; k <= 1; k++) l.Circulo(x + k * 128, y, 2.2f, c);
            }
            l.Ruido(0.06f, 9);
            l.Guardar("Pasto", 128f, Centro);
        }
    }

    // Bloque de piedra con relieve: sombra abajo-derecha, luz arriba-izquierda, manchas, grieta y musgo
    static void Bloque(Lienzo l, float x0, float y0, float x1, float y1, Color c, bool grieta, bool musgo, int semilla)
    {
        var r = new System.Random(semilla);
        float J() => (float)r.NextDouble() * 2.4f - 1.2f;
        float k = 6f;
        Vector2[] forma =
        {
            V(x0 + k + J(), y0 + J()), V(x1 - k + J(), y0 + J()), V(x1 + J(), y0 + k + J()), V(x1 + J(), y1 - k + J()),
            V(x1 - k + J(), y1 + J()), V(x0 + k + J(), y1 + J()), V(x0 + J(), y1 - k + J()), V(x0 + J(), y0 + k + J())
        };
        Vector2[] Mover(float dx, float dy) => forma.Select(p => p + new Vector2(dx, dy)).ToArray();

        l.Poligono(Tono(c, 0.5f), Mover(2.5f, -2.5f));        // sombra
        l.Poligono(Tono(c, 1.3f), Mover(-1.5f, 1.5f));        // luz del borde
        l.Poligono(c, forma);
        // Cara un poco más clara arriba (luz del sol)
        l.Rectangulo(x0 + k, Mathf.Lerp(y0, y1, 0.55f), x1 - k, y1 - 3, C(1f, 1f, 1f, 0.07f));

        // Manchas y vetas
        for (int i = 0; i < 7; i++)
        {
            float px = Mathf.Lerp(x0 + 5, x1 - 5, (float)r.NextDouble());
            float py = Mathf.Lerp(y0 + 5, y1 - 5, (float)r.NextDouble());
            l.Circulo(px, py, 1f + (float)r.NextDouble() * 2.2f, i % 2 == 0 ? C(0f, 0f, 0f, 0.12f) : C(1f, 1f, 1f, 0.08f));
        }

        if (grieta)
        {
            float gx = Mathf.Lerp(x0 + 8, x1 - 8, (float)r.NextDouble());
            float gy = y1 - 2;
            for (int s = 0; s < 3; s++)
            {
                float nx = gx + (float)r.NextDouble() * 8f - 4f, ny = gy - (y1 - y0) * 0.22f;
                l.Poligono(C(0.12f, 0.1f, 0.09f, 0.85f), V(gx - 0.8f, gy), V(gx + 0.8f, gy), V(nx + 0.6f, ny), V(nx - 0.6f, ny));
                gx = nx; gy = ny;
            }
        }

        if (musgo)
            for (int i = 0; i < 4; i++)
                l.Circulo(Mathf.Lerp(x0 + 6, x1 - 6, (float)r.NextDouble()), y1 - 2, 2.5f + (float)r.NextDouble() * 3f,
                          C(0.3f, 0.48f, 0.2f, 0.7f));
    }

    static Color Tono(Color c, float f) { return new Color(c.r * f, c.g * f, c.b * f, 1f); }

    // ---------- Capas del fondo con profundidad ----------
    // Todas se repiten sin cortes: las ondas usan periodos que entran exacto en el ancho de la imagen.
    static void GenerarCapasFondo()
    {
        const int W = 512;
        float Onda(int x, float veces, float fase) { return Mathf.Sin(2f * Mathf.PI * veces * x / W + fase); }

        // Cielo: la imagen original + su espejo, así al repetirse no se nota la unión
        {
            string ruta = "Assets/Environment/Fondo1.jpg";
            var original = new Texture2D(2, 2);
            original.LoadImage(File.ReadAllBytes(ruta));
            int w = original.width, h = original.height;
            var doble = new Lienzo(w * 2, h);
            Color[] px = original.GetPixels();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = px[y * w + x];
                    doble.Pintar(x, y, c);
                    doble.Pintar(w * 2 - 1 - x, y, c);
                }
            Object.DestroyImmediate(original);
            doble.Guardar("Cielo", 100f, Centro);
        }

        // Colinas con terrazas andinas (dos filas: lejana y cercana)
        {
            var l = new Lienzo(W, 160);
            for (int x = 0; x < W; x++)
            {
                float h1 = 92 + 16 * Onda(x, 2, 0.5f) + 9 * Onda(x, 5, 1.3f);
                float h2 = 52 + 12 * Onda(x, 3, 2f) + 6 * Onda(x, 7, 0.2f);
                for (int y = 0; y < 160; y++)
                {
                    if (y < h2)
                    {
                        bool terraza = Mathf.Repeat(y + 5 * Onda(x, 4, 0f), 11f) < 2f;
                        l.Pintar(x, y, terraza ? C(0.17f, 0.32f, 0.13f) : C(0.22f, 0.4f, 0.17f));
                    }
                    else if (y < h1)
                    {
                        bool terraza = Mathf.Repeat(y + 6 * Onda(x, 3, 1f), 13f) < 2f;
                        l.Pintar(x, y, terraza ? C(0.3f, 0.47f, 0.25f) : C(0.38f, 0.56f, 0.3f, 0.95f));
                    }
                }
            }
            l.Guardar("Colinas", 24f, V(0.5f, 0f));
        }

        // Primer plano: pasto, agaves y flores andinas (va delante del jugador)
        {
            var l = new Lienzo(W, 96);
            Random.InitState(21);
            Color oscuro = C(0.07f, 0.17f, 0.06f);
            l.Rectangulo(0, 0, W, 12, oscuro);
            for (int i = 0; i < 170; i++)
            {
                float x = Random.Range(0f, W), alto = Random.Range(8f, 30f), ancho = Random.Range(2f, 5f);
                float incl = Random.Range(-6f, 6f);
                Color c = Color.Lerp(oscuro, C(0.12f, 0.28f, 0.1f), Random.value);
                for (int k = -1; k <= 1; k++)
                {
                    float ox = x + k * W;
                    l.Poligono(c, V(ox - ancho, 10), V(ox + ancho, 10), V(ox + incl, 10 + alto));
                    if (i % 9 == 0) l.Circulo(ox + incl, 10 + alto, 2.5f, i % 2 == 0 ? C(0.9f, 0.2f, 0.25f) : C(1f, 0.8f, 0.2f));
                }
            }
            for (int a = 0; a < 6; a++)
            {
                float x = a * W / 6f + Random.Range(0f, 40f), alto = Random.Range(32f, 52f);
                for (int k = -1; k <= 1; k++)
                    for (int hoja = -3; hoja <= 3; hoja++)
                    {
                        float ox = x + k * W;
                        l.Poligono(C(0.1f, 0.24f, 0.16f), V(ox - 4, 8), V(ox + 4, 8), V(ox + hoja * 9, 8 + alto - Mathf.Abs(hoja) * 6));
                    }
            }
            l.Guardar("PrimerPlano", 24f, V(0.5f, 0f));
        }

        // Nubes suaves
        {
            var l = new Lienzo(W, 128);
            Random.InitState(5);
            for (int n = 0; n < 4; n++)
            {
                float cx = n * W / 4f + Random.Range(0f, 60f), cy = Random.Range(50f, 90f);
                for (int k = -1; k <= 1; k++)
                    for (int b = 0; b < 7; b++)
                    {
                        float bx = cx + k * W + (b - 3) * 13 + Random.Range(-4f, 4f);
                        float r = 20 - Mathf.Abs(b - 3) * 3 + Random.Range(-2f, 2f);
                        l.Circulo(bx, cy - 4, r, C(0.85f, 0.87f, 0.92f, 0.5f));
                        l.Circulo(bx, cy, r, C(1f, 1f, 1f, 0.75f));
                    }
            }
            l.Guardar("Nubes", 24f, Centro);
        }
    }

    // =====================================================================
    // SONIDOS SINTETIZADOS (se guardan como .wav en Assets/Audio/Generados)
    // =====================================================================

    enum Forma { Seno, Cuadrada, Triangulo }

    static void GenerarSonidos()
    {
        CrearCarpeta("Assets/Audio", "Generados");

        Sonido("salto", 0.16f, t => Mathf.Lerp(260f, 720f, t / 0.16f), Forma.Triangulo, 0f, 1f, 0.35f);
        Sonido("aterrizar", 0.14f, t => Mathf.Lerp(130f, 55f, t / 0.14f), Forma.Seno, 0.5f, 0.2f, 0.45f);
        Sonido("danio", 0.35f, t => Mathf.Lerp(520f, 140f, t / 0.35f) + Mathf.Sin(t * 180f) * 30f, Forma.Cuadrada, 0f, 1f, 0.25f);
        Sonido("muerte_supay", 0.45f, t => Mathf.Lerp(700f, 90f, t / 0.45f) + Mathf.Sin(t * 120f) * 60f, Forma.Triangulo, 0.2f, 0.3f, 0.4f);
        Sonido("disparo", 0.3f, t => Mathf.Lerp(200f, 520f, t / 0.3f), Forma.Seno, 0.75f, 0.15f, 0.35f);
        Sonido("roca", 0.3f, t => 80f, Forma.Seno, 0.9f, 0.08f, 0.45f);
        Sonido("checkpoint", 0.42f, t => Nota(t, 0.08f, 523f, 659f, 784f, 1047f), Forma.Triangulo, 0f, 1f, 0.3f);
        Sonido("powerup", 0.4f, t => Nota(t, 0.06f, 523f, 659f, 784f, 1047f, 1319f, 1568f), Forma.Cuadrada, 0f, 1f, 0.18f);
        Sonido("rugido", 1.0f, t => 70f + Mathf.Sin(t * 50f) * 12f, Forma.Cuadrada, 0.5f, 0.1f, 0.45f, 0.05f);
        Sonido("impacto_suelo", 0.5f, t => Mathf.Lerp(90f, 35f, t / 0.5f), Forma.Seno, 0.6f, 0.05f, 0.65f);
        Sonido("cara_inti", 0.28f, t => Nota(t, 0.06f, 660f, 880f, 1320f), Forma.Triangulo, 0f, 1f, 0.3f);
        Sonido("cara_killa", 0.28f, t => Nota(t, 0.06f, 880f, 660f, 440f), Forma.Seno, 0f, 1f, 0.35f);
    }

    static float Nota(float t, float paso, params float[] notas)
    {
        return notas[Mathf.Min(notas.Length - 1, (int)(t / paso))];
    }

    static void Sonido(string nombre, float duracion, System.Func<float, float> frecuencia, Forma forma,
                       float ruido, float filtroRuido, float volumen, float ataque = 0.005f)
    {
        const int muestreo = 22050;
        int n = (int)(duracion * muestreo);
        var datos = new short[n];
        var azar = new System.Random(nombre.GetHashCode());
        float fase = 0f, ruidoFiltrado = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)muestreo;
            fase += frecuencia(t) / muestreo;
            float p = fase - Mathf.Floor(fase);
            float onda = forma == Forma.Seno ? Mathf.Sin(p * Mathf.PI * 2f)
                       : forma == Forma.Cuadrada ? (p < 0.5f ? 0.7f : -0.7f)
                       : 1f - 4f * Mathf.Abs(p - 0.5f);
            ruidoFiltrado += filtroRuido * ((float)azar.NextDouble() * 2f - 1f - ruidoFiltrado);
            float envolvente = Mathf.Min(1f, t / ataque) * Mathf.Pow(1f - t / duracion, 2f);
            float s = (onda * (1f - ruido) + ruidoFiltrado * ruido * (filtroRuido < 0.5f ? 3f : 1f)) * envolvente * volumen;
            datos[i] = (short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue);
        }

        string ruta = "Assets/Audio/Generados/" + nombre + ".wav";
        using (var w = new BinaryWriter(File.Create(ruta)))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + n * 2);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(muestreo); w.Write(muestreo * 2);
            w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            w.Write(n * 2);
            foreach (short d in datos) w.Write(d);
        }
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        sonidos[nombre] = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
    }

    static Lienzo DibujarSupay(int f)
    {
        var l = new Lienzo(128, 128);
        float fase = f * Mathf.PI / 2f;
        Color cuerpo = C(0.22f, 0.08f, 0.32f);
        Color claro = C(0.36f, 0.15f, 0.5f);

        for (int i = 0; i < 3; i++)                                           // cola ondulada
            l.Circulo(40 + i * 24, 30 + Mathf.Sin(fase + i * 2f) * 5f, 15, cuerpo);
        l.Elipse(64, 68, 40, 38 + (f % 2 == 0 ? 1.5f : -1.5f), cuerpo);         // cuerpo
        l.Elipse(56, 80, 18, 13, claro, 0.3f);                                 // brillo
        l.Poligono(C(0.75f, 0.15f, 0.12f), V(34, 92), V(50, 102), V(24, 124)); // cuernos
        l.Poligono(C(0.75f, 0.15f, 0.12f), V(94, 92), V(78, 102), V(104, 124));
        l.Elipse(48, 74, 10, 7, C(1f, 0.85f, 0.2f), -0.3f);                    // ojos
        l.Elipse(80, 74, 10, 7, C(1f, 0.85f, 0.2f), 0.3f);
        l.Circulo(50, 73, 4, C(0.8f, 0.05f, 0.05f));
        l.Circulo(82, 73, 4, C(0.8f, 0.05f, 0.05f));

        if (f < 4)
        {
            l.Rectangulo(46, 50, 82, 57, C(0.06f, 0f, 0.06f));                  // boca cerrada
            for (int i = 0; i < 3; i++)
                l.Poligono(Color.white, V(49 + i * 12, 57), V(57 + i * 12, 57), V(53 + i * 12, 51));
        }
        else
        {
            l.Elipse(64, 52, 15, 11, C(0.06f, 0f, 0.06f));                      // boca abierta
            l.Elipse(64, 51, 9, 6, C(1f, 0.5f, 0.05f));
        }

        l.Contorno(C(0.07f, 0.01f, 0.09f), 2);
        return l;
    }

    static Lienzo DibujarCondor(int f)
    {
        var l = new Lienzo(160, 80);
        Color negro = C(0.08f, 0.08f, 0.1f);
        Color gris = C(0.55f, 0.55f, 0.6f);

        if (f == 0)
        {
            l.Poligono(negro, V(66, 46), V(98, 46), V(62, 78), V(36, 76));
            l.Poligono(gris, V(70, 52), V(84, 52), V(60, 72), V(52, 72));
        }
        else if (f == 1)
        {
            l.Poligono(negro, V(64, 44), V(98, 44), V(36, 54), V(8, 50));
            l.Poligono(gris, V(70, 46), V(84, 46), V(40, 51), V(30, 50));
        }
        else
        {
            l.Poligono(negro, V(66, 38), V(98, 38), V(54, 8), V(30, 12));
            l.Poligono(gris, V(70, 34), V(84, 34), V(58, 16), V(50, 17));
        }

        l.Poligono(negro, V(48, 42), V(18, 48), V(18, 32), V(48, 36));          // cola
        l.Elipse(80, 40, 34, 13, negro);                                        // cuerpo
        l.Elipse(110, 45, 8, 9, Color.white);                                   // collar blanco
        l.Circulo(120, 50, 8, C(0.62f, 0.45f, 0.45f));                          // cabeza
        l.Poligono(C(0.9f, 0.85f, 0.7f), V(126, 53), V(141, 47), V(126, 45));   // pico
        l.Circulo(122, 52, 1.6f, Color.black);                                  // ojo
        return l;
    }

    // =====================================================================
    // 2. ANIMACIONES Y PREFABS
    // =====================================================================

    static AnimationClip ClipSprites(string nombre, float fps, bool loop, params Sprite[] cuadros)
    {
        var clip = new AnimationClip { frameRate = 60 };
        var bind = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var claves = new ObjectReferenceKeyframe[cuadros.Length + 1];
        for (int i = 0; i < cuadros.Length; i++)
            claves[i] = new ObjectReferenceKeyframe { time = i / fps, value = cuadros[i] };
        // Repite el último cuadro al final para que dure lo mismo que los demás
        claves[cuadros.Length] = new ObjectReferenceKeyframe { time = cuadros.Length / fps, value = cuadros[cuadros.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, bind, claves);

        var aj = AnimationUtility.GetAnimationClipSettings(clip);
        aj.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, aj);
        GuardarAsset(clip, CarpetaAnim + "/" + nombre + ".anim");
        return clip;
    }

    static void GuardarAsset(Object obj, string ruta)
    {
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(obj, ruta);
    }

    static AnimatorController ControladorSupay()
    {
        Sprite[] f = { sprites["Supay_0"], sprites["Supay_1"], sprites["Supay_2"], sprites["Supay_3"] };
        AnimationClip flotar = ClipSprites("Supay_Flotar", 8f, true, f);
        AnimationClip disparo = ClipSprites("Supay_Disparar", 4f, false, sprites["Supay_Disparo"], sprites["Supay_Disparo"]);

        // Herido: parpadea entre rojo y blanco
        AnimationClip herido = ClipSprites("Supay_Herido", 10f, false, sprites["Supay_0"], sprites["Supay_0"], sprites["Supay_0"]);
        var rojo = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.3f, 1f));
        var gb = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.1f, 1f), new Keyframe(0.2f, 0.2f), new Keyframe(0.3f, 1f));
        AnimationUtility.SetEditorCurve(herido, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Color.r"), rojo);
        AnimationUtility.SetEditorCurve(herido, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Color.g"), gb);
        AnimationUtility.SetEditorCurve(herido, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Color.b"), gb);
        AnimationUtility.SetEditorCurve(herido, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Color.a"),
                                        AnimationCurve.Constant(0f, 0.3f, 1f));
        EditorUtility.SetDirty(herido);

        string ruta = CarpetaAnim + "/Supay.controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Herido", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Disparar", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var eFlotar = sm.AddState("Flotar");
        eFlotar.motion = flotar;
        var eHerido = sm.AddState("Herido");
        eHerido.motion = herido;
        var eDisparar = sm.AddState("Disparar");
        eDisparar.motion = disparo;
        sm.defaultState = eFlotar;

        var t = sm.AddAnyStateTransition(eHerido);
        t.AddCondition(AnimatorConditionMode.If, 0, "Herido");
        t.duration = 0f;
        t.canTransitionToSelf = true;
        Volver(eHerido, eFlotar);

        var t2 = eFlotar.AddTransition(eDisparar);
        t2.AddCondition(AnimatorConditionMode.If, 0, "Disparar");
        t2.hasExitTime = false;
        t2.duration = 0f;
        Volver(eDisparar, eFlotar);
        return ctrl;
    }

    static void Volver(AnimatorState desde, AnimatorState hacia)
    {
        var t = desde.AddTransition(hacia);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0f;
    }

    static AnimatorController ControladorSimple(string nombre, AnimationClip clip)
    {
        string ruta = CarpetaAnim + "/" + nombre + ".controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        var estado = ctrl.layers[0].stateMachine.AddState(nombre);
        estado.motion = clip;
        ctrl.layers[0].stateMachine.defaultState = estado;
        return ctrl;
    }

    static GameObject Visual(GameObject padre, Sprite sprite, int orden, RuntimeAnimatorController ctrl = null)
    {
        var v = new GameObject("Visual");
        v.transform.SetParent(padre.transform, false);
        var sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;
        if (ctrl != null) v.AddComponent<Animator>().runtimeAnimatorController = ctrl;
        return v;
    }

    static GameObject GuardarPrefab(GameObject go)
    {
        string ruta = CarpetaPrefabs + "/" + go.name + ".prefab";
        GameObject p = PrefabUtility.SaveAsPrefabAsset(go, ruta);
        Object.DestroyImmediate(go);
        prefabs[p.name] = p;
        return p;
    }

    static T Trigger<T>(GameObject go) where T : Collider2D
    {
        T c = go.AddComponent<T>();
        c.isTrigger = true;
        return c;
    }

    static void CrearPrefabs()
    {
        // Destello (partículas)
        var destello = new GameObject("Destello");
        destello.AddComponent<Destello>().particula = sprites["Chispa"];
        GameObject pDestello = GuardarPrefab(destello);

        // Power-ups
        GameObject pVida = CrearPowerUp("PowerUp_Choclo", sprites["Choclo"], PowerUp.Tipo.Vida, new Color(1f, 0.85f, 0.2f), pDestello);
        GameObject pPluma = CrearPowerUp("PowerUp_Pluma", sprites["Pluma"], PowerUp.Tipo.PlumaCondor, new Color(0.6f, 0.9f, 1f), pDestello);

        // Bola de fuego
        var fuegoClip = ClipSprites("Fuego", 12f, true, sprites["Fuego_0"], sprites["Fuego_1"]);
        var fuego = new GameObject("BolaFuego");
        var rbF = fuego.AddComponent<Rigidbody2D>();
        rbF.gravityScale = 0f;
        Trigger<CircleCollider2D>(fuego).radius = 0.25f;
        fuego.AddComponent<ProyectilFuego>().prefabDestello = pDestello;
        Visual(fuego, sprites["Fuego_0"], 8, ControladorSimple("Fuego", fuegoClip));
        GameObject pFuego = GuardarPrefab(fuego);

        // Roca
        var roca = new GameObject("Roca");
        var rbR = roca.AddComponent<Rigidbody2D>();
        rbR.gravityScale = 0f;
        Trigger<CircleCollider2D>(roca).radius = 0.42f;
        var compRoca = roca.AddComponent<Roca>();
        compRoca.prefabDestello = pDestello;
        compRoca.sonidoRomper = sonidos["roca"];
        Visual(roca, sprites["Roca"], 7);
        GuardarPrefab(roca);

        // Supay normal y Supay tirador
        AnimatorController ctrlSupay = ControladorSupay();
        for (int i = 0; i < 2; i++)
        {
            bool tirador = i == 1;
            var s = new GameObject(tirador ? "SupayTirador" : "Supay");
            s.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Trigger<CircleCollider2D>(s).radius = 0.6f;
            var e = s.AddComponent<EnemigoSupay>();
            e.prefabDestello = pDestello;
            e.premios = new[] { pVida, pPluma };
            e.prefabFuego = pFuego;
            e.sonidoMuerte = sonidos["muerte_supay"];
            e.sonidoDisparo = sonidos["disparo"];
            if (tirador)
            {
                e.dispara = true;
                e.patrulla = false;
                e.vida = 3;
                e.probabilidadPremio = 0.6f;
                s.transform.localScale = Vector3.one * 1.2f;
            }
            GameObject v = Visual(s, sprites["Supay_0"], 6, ctrlSupay);
            if (tirador) v.GetComponent<SpriteRenderer>().color = new Color(1f, 0.6f, 0.6f);
            GuardarPrefab(s);
        }

        // Jefe final: Gran Supay (2.5 veces más grande y rojo oscuro)
        {
            var j = new GameObject("JefeSupay");
            j.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Trigger<CircleCollider2D>(j).radius = 0.6f;
            j.transform.localScale = Vector3.one * 2.5f;
            var jefe = j.AddComponent<JefeSupay>();
            jefe.prefabFuego = pFuego;
            jefe.prefabRoca = prefabs["Roca"];
            jefe.prefabDestello = pDestello;
            jefe.premio = pVida;
            jefe.sonidoRugido = sonidos["rugido"];
            jefe.sonidoDisparo = sonidos["disparo"];
            jefe.sonidoGolpe = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/golpe.wav");
            jefe.sonidoImpactoSuelo = sonidos["impacto_suelo"];
            jefe.sonidoMuerte = sonidos["muerte_supay"];
            GameObject v = Visual(j, sprites["Supay_0"], 6, ctrlSupay);
            v.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0.45f, 0.5f);
            GuardarPrefab(j);
        }

        // Cóndor decorativo
        var condorClip = ClipSprites("Condor_Volar", 7f, true,
            sprites["Condor_0"], sprites["Condor_1"], sprites["Condor_2"], sprites["Condor_1"]);
        var condor = new GameObject("Condor");
        var src = condor.AddComponent<SpriteRenderer>();
        src.sprite = sprites["Condor_0"];
        src.color = new Color(1f, 1f, 1f, 0.9f);
        condor.AddComponent<Animator>().runtimeAnimatorController = ControladorSimple("Condor", condorClip);
        condor.AddComponent<CondorDecorativo>();
        GuardarPrefab(condor);

        // Bandera / punto de control
        var bandera = new GameObject("PuntoControl");
        var box = Trigger<BoxCollider2D>(bandera);
        box.size = new Vector2(1.2f, 3f);
        box.offset = new Vector2(0.2f, 1.5f);
        Visual(bandera, sprites["Bandera_Off"], 3);
        var punto = new GameObject("Reaparicion");
        punto.transform.SetParent(bandera.transform, false);
        punto.transform.localPosition = new Vector3(0.6f, 1.8f, 0f);
        var pc = bandera.AddComponent<PuntoControl>();
        pc.spriteInactivo = sprites["Bandera_Off"];
        pc.spriteActivo = sprites["Bandera_On"];
        pc.puntoReaparicion = punto.transform;
        pc.prefabDestello = pDestello;
        GuardarPrefab(bandera);
    }

    static GameObject CrearPowerUp(string nombre, Sprite sprite, PowerUp.Tipo tipo, Color color, GameObject destello)
    {
        var go = new GameObject(nombre);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 6;
        Trigger<CircleCollider2D>(go).radius = 0.5f;
        var p = go.AddComponent<PowerUp>();
        p.tipo = tipo;
        p.colorDestello = color;
        p.prefabDestello = destello;
        return GuardarPrefab(go);
    }

    // =====================================================================
    // 3. AMPLIAR EL NIVEL 1
    // =====================================================================

    static void AmpliarNivel1()
    {
        var escena = EditorSceneManager.OpenScene(RutaNivel, OpenSceneMode.Single);

        GameObject jugador = GameObject.Find("DiabloHuma");
        GameObject suelo = GameObject.Find("Suelo");
        GameObject plataforma = GameObject.Find("Plataforma1");
        GameObject meta = GameObject.Find("Meta_Nivel");
        GameObject pinchos = GameObject.Find("Pinchos");
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (jugador == null || suelo == null || plataforma == null || gm == null || gm.contenedorMonedas == null)
        {
            Debug.LogError("Falta DiabloHuma, Suelo, Plataforma1 o el GameManager con sus monedas. " +
                           "Ejecuta primero 'Diablo Huma > Configurar reto del Nivel 1'.");
            return;
        }

        // Limpiar lo generado antes
        GameObject viejo = GameObject.Find("Extension_Nivel");
        if (viejo != null) Object.DestroyImmediate(viejo);
        Transform monedas = gm.contenedorMonedas;
        for (int i = monedas.childCount - 1; i >= 0; i--)
            if (monedas.GetChild(i).name.StartsWith("Inti_Ext")) Object.DestroyImmediate(monedas.GetChild(i).gameObject);
        GameObject modeloMoneda = monedas.childCount > 0 ? monedas.GetChild(0).gameObject : null;

        var raiz = new GameObject("Extension_Nivel").transform;
        var tSuelos = Hijo(raiz, "Suelos");
        var tEnemigos = Hijo(raiz, "Enemigos");
        var tObjetos = Hijo(raiz, "Objetos");
        var tFondos = Hijo(raiz, "Fondo_Parallax");
        var tArena = Hijo(raiz, "Arena_Jefe");

        // --- Jugador: física más ágil y sin quedarse pegado a las paredes ---
        var rb = jugador.GetComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        string rutaMat = CarpetaGen + "/SinFriccion.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(rutaMat);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, rutaMat);
        }
        jugador.GetComponent<Collider2D>().sharedMaterial = mat;
        var mov = jugador.GetComponent<MovimientoJugador>();
        mov.velocidad = 6.5f;
        mov.fuerzaSalto = 12f;
        mov.sonidoSalto = sonidos["salto"];
        mov.sonidoAterrizar = sonidos["aterrizar"];
        mov.prefabPolvo = prefabs["Destello"];
        mov.sonidoLatigoFuego = sonidos["disparo"];
        EditorUtility.SetDirty(mov);
        EditorUtility.SetDirty(rb);

        // --- Los pisos originales pasan de rectángulos marrones a piedra andina con pasto ---
        foreach (string nombre in new[] { "Suelo", "Plataforma1", "Plataform2" })
        {
            GameObject p = GameObject.Find(nombre);
            if (p != null) Revestir(p, TamanoActual(p));
        }

        // --- Suelos nuevos (copias de los originales) ---
        Piso(suelo, tSuelos, "Suelo2", 16.5f, -3f, 14f, 1f);
        Piso(suelo, tSuelos, "Suelo3", 61f, -3f, 42f, 1f);     // llega hasta la arena del jefe
        // El personaje mide ~3.2 de alto: las plataformas bajas sobre el suelo funcionan como escalón,
        // y las del abismo (4, 5 y 6) forman una subida con saltos.
        Piso(plataforma, tSuelos, "Plataforma3", 17f, -1.25f, 3f, 0.5f);
        Piso(plataforma, tSuelos, "Plataforma4", 27.5f, -1.75f, 3f, 0.5f);
        Piso(plataforma, tSuelos, "Plataforma5", 32.25f, -0.25f, 2.5f, 0.5f);
        Piso(plataforma, tSuelos, "Plataforma6", 37f, 1.25f, 3f, 0.5f);

        // --- Monedas Inti nuevas (van dentro del mismo contenedor para que cuenten) ---
        Vector2[] posMonedas =
        {
            V(12.5f, -1.2f), V(17f, 2.2f), V(20.5f, 1f), V(24.75f, -0.5f), V(32.25f, 1.3f),
            V(36.5f, 2.8f), V(39.5f, 0.5f), V(46f, 1f), V(53.5f, 1.4f), V(57f, -1.2f)
        };
        if (modeloMoneda != null)
            for (int i = 0; i < posMonedas.Length; i++)
            {
                GameObject m = Object.Instantiate(modeloMoneda, monedas);
                m.name = "Inti_Ext_" + (i + 1);
                m.transform.position = posMonedas[i];
            }

        // --- Carteles del tutorial de la máscara ---
        Aviso(tObjetos, "Aviso_Mascara", 8.5f,
              "Pulsa C para girar tu máscara a KILLA (luna): látigo de fuego, más salto y más velocidad");
        Aviso(tObjetos, "Aviso_Jefe", 61f,
              "¡El Gran Supay! Usa la cara KILLA: tu látigo de fuego le hace doble daño");

        // --- Pinchos ---
        if (pinchos != null)
        {
            Copiar(pinchos, tObjetos, "Pinchos2", 46f, -2.5f, 1f, 1f);
            Copiar(pinchos, tObjetos, "Pinchos3", 50.5f, -2.5f, 1f, 1f);
        }

        // --- Enemigos (instancias de prefab) ---
        Enemigo("Supay", tEnemigos, 13.25f, -1.3f, 1.75f);       // patrulla en el suelo
        Enemigo("Supay", tEnemigos, 27.5f, -0.3f, 1f);           // sobre la plataforma 4
        Enemigo("SupayTirador", tEnemigos, 38f, 4.5f, 0f);       // vigila la plataforma 6
        Enemigo("Supay", tEnemigos, 53f, -1.3f, 2.5f);           // cruza sobre los pinchos
        Enemigo("SupayTirador", tEnemigos, 55.5f, 3.3f, 0f);     // vigila la entrada a la arena

        // --- Generadores de rocas ---
        Generador(tObjetos, "GeneradorRocas_1", 18f, 23f, 1.8f);
        Generador(tObjetos, "GeneradorRocas_2", 43f, 57f, 2.0f);

        // --- Puntos de control y power-ups ---
        Instanciar("PuntoControl", tObjetos, 10f, -2.5f);
        Instanciar("PuntoControl", tObjetos, 22f, -2.5f);
        Instanciar("PuntoControl", tObjetos, 41.5f, -2.5f);
        Instanciar("PowerUp_Pluma", tObjetos, 23.2f, -0.8f);
        Instanciar("PowerUp_Choclo", tObjetos, 44f, -1.2f);
        Instanciar("PuntoControl", tObjetos, 59.5f, -2.5f);       // justo antes de la arena

        // --- Arena del jefe final (x 62 a 79) ---
        // Más bajas (arriba en -0.5) para que el Diablo Huma suba con un salto normal
        Piso(plataforma, tArena, "Arena_Plataforma_Izq", 66f, -0.75f, 2.5f, 0.5f);
        Piso(plataforma, tArena, "Arena_Plataforma_Der", 75f, -0.75f, 2.5f, 0.5f);
        GameObject muroIzq = Muro(suelo, tArena, "Muro_Izq", 61.5f);
        GameObject muroDer = Muro(suelo, tArena, "Muro_Der", 79.5f);

        GameObject jefe = Instanciar("JefeSupay", tArena, 70.5f, 3.2f);
        var zona = new GameObject("Zona_Jefe");
        zona.transform.SetParent(tArena, false);
        zona.transform.position = new Vector3(64.5f, 0f, 0f);
        var zCol = zona.AddComponent<BoxCollider2D>();
        zCol.isTrigger = true;
        zCol.size = new Vector2(1f, 14f);
        var zj = zona.AddComponent<ZonaJefe>();
        zj.jefe = jefe.GetComponent<JefeSupay>();
        zj.muros = new[] { muroIzq, muroDer };
        zj.camaraX = 70.5f;
        zj.camaraY = 1.5f;

        // --- Meta al final, detrás de la arena: se abre con todos los Inti y el jefe vencido ---
        if (meta != null) meta.transform.position = new Vector3(80.5f, -1.5f, meta.transform.position.z);

        // --- Fondo con profundidad (parallax) en lugar del fondo plano ---
        foreach (GameObject r in escena.GetRootGameObjects())
            if (r.name == "Fondo" || r.name == "Fondo1_0") r.SetActive(false);

        CapaFondo(tFondos, "Capa_Cielo", sprites["Cielo"], new Vector3(0f, 0.5f, 0f), 1.25f, 0.92f, 0.9f, 0f, -50);
        CapaFondo(tFondos, "Capa_Nubes", sprites["Nubes"], new Vector3(0f, 3.2f, 0f), 1f, 0.8f, 0.85f, 0.35f, -45);
        CapaFondo(tFondos, "Capa_Colinas", sprites["Colinas"], new Vector3(0f, -5.4f, 0f), 1f, 0.55f, 0.5f, 0f, -40);
        CapaFondo(tFondos, "Capa_PrimerPlano", sprites["PrimerPlano"], new Vector3(0f, -5.8f, 0f), 1f, -0.35f, 1f, 0f, -5);   // detrás de personajes y suelo para no taparlos

        // --- Cóndores de decoración (entre el cielo y las colinas) ---
        GameObject c1 = Instanciar("Condor", tObjetos, 5f, 3.3f);
        GameObject c2 = Instanciar("Condor", tObjetos, 30f, 2.7f);
        foreach (GameObject c in new[] { c1, c2 })
        {
            var sr = c.GetComponent<SpriteRenderer>();
            sr.sortingOrder = -42;
            var cd = c.GetComponent<CondorDecorativo>();
            cd.xMin = -12f;
            cd.xMax = 86f;
        }
        c2.GetComponent<CondorDecorativo>().velocidad = -2.4f;
        c2.transform.localScale = Vector3.one * 0.8f;

        // --- Cámara con límites ---
        var cam = Object.FindAnyObjectByType<CamaraSeguir>();
        if (cam != null)
        {
            cam.usarLimites = true;
            cam.minX = 0f;
            cam.maxX = 72.5f;
            cam.minY = -0.5f;
            cam.maxY = 2.5f;
            EditorUtility.SetDirty(cam);
        }

        // --- GameManager ---
        gm.sonidoPowerUp = sonidos["powerup"];
        gm.sonidoEnemigo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/golpe.wav");
        gm.sonidoDanio = sonidos["danio"];
        gm.sonidoCheckpoint = sonidos["checkpoint"];
        gm.vidasIniciales = 5;          // más vidas para poder vencer al Gran Supay
        gm.vidasMaximas = 6;
        gm.tiempoInvulnerable = 2f;
        if (gm.GetComponent<DepuracionJuego>() == null) gm.gameObject.AddComponent<DepuracionJuego>();

        // --- Máscara de dos caras en el jugador ---
        var mascara = jugador.GetComponent<MascaraDual>();
        if (mascara == null) mascara = jugador.AddComponent<MascaraDual>();
        mascara.prefabDestello = prefabs["Destello"];
        mascara.sonidoInti = sonidos["cara_inti"];
        mascara.sonidoKilla = sonidos["cara_killa"];
        Transform aura = jugador.transform.Find("AuraCara");     // brillo de versiones anteriores
        if (aura != null) Object.DestroyImmediate(aura.gameObject);
        EditorUtility.SetDirty(mascara);

        // --- HUD con Canvas ---
        gm.hud = CrearHUD(escena, gm);
        EditorUtility.SetDirty(gm);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    static Transform Hijo(Transform padre, string nombre)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        return t;
    }

    static GameObject Copiar(GameObject original, Transform padre, string nombre, float x, float y, float ancho, float alto)
    {
        GameObject c = Object.Instantiate(original, padre);
        c.name = nombre;
        c.transform.position = new Vector3(x, y, original.transform.position.z);
        c.transform.localScale = new Vector3(ancho, alto, 1f);
        return c;
    }

    // Copia un suelo o plataforma y lo reviste de piedra andina con el tamaño pedido
    static GameObject Piso(GameObject original, Transform padre, string nombre, float x, float y, float ancho, float alto)
    {
        GameObject c = Copiar(original, padre, nombre, x, y, 1f, 1f);
        Revestir(c, new Vector2(ancho, alto));
        return c;
    }

    // Tamaño del piso: con escala (rectángulo original) o con el tamaño del sprite en mosaico (ya revestido)
    static Vector2 TamanoActual(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.drawMode == SpriteDrawMode.Tiled) return sr.size;
        return new Vector2(Mathf.Abs(go.transform.localScale.x), Mathf.Abs(go.transform.localScale.y));
    }

    // Cambia el rectángulo por piedra en mosaico (Tiled) y le pone pasto encima.
    // La escala queda en 1 y el tamaño se maneja con el Sprite Renderer y el collider.
    static void Revestir(GameObject go, Vector2 tam)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        var box = go.GetComponent<BoxCollider2D>();
        if (sr == null) return;

        go.transform.localScale = Vector3.one;
        sr.sprite = sprites["Piedra"];
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = tam;
        sr.color = Color.white;
        if (box != null)
        {
            box.autoTiling = false;
            box.size = tam;
            box.offset = Vector2.zero;
        }

        Transform pasto = go.transform.Find("Pasto");
        if (pasto == null)
        {
            pasto = new GameObject("Pasto").transform;
            pasto.SetParent(go.transform, false);
        }
        pasto.localScale = Vector3.one;
        pasto.localPosition = new Vector3(0f, tam.y / 2f + 0.11f, 0f);
        var srP = pasto.GetComponent<SpriteRenderer>();
        if (srP == null) srP = pasto.gameObject.AddComponent<SpriteRenderer>();
        srP.sprite = sprites["Pasto"];
        srP.drawMode = SpriteDrawMode.Tiled;
        srP.size = new Vector2(tam.x + 0.1f, 0.5f);
        srP.sortingOrder = sr.sortingOrder + 1;
    }

    // =====================================================================
    // HUD CON CANVAS (objetos reales en la jerarquía: HUD_Canvas)
    // =====================================================================

    static Font fuente;

    static HUDJuego CrearHUD(UnityEngine.SceneManagement.Scene escena, GameManager gm)
    {
        foreach (GameObject r in escena.GetRootGameObjects())
            if (r.name == "HUD_Canvas" || r.name == "EventSystem") Object.DestroyImmediate(r);

        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite sCabeza = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/cabeza.png").OfType<Sprite>().FirstOrDefault();
        Sprite sInti = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Reto/Moneda_Inti.png").OfType<Sprite>().FirstOrDefault();
        Color dorado = new Color(1f, 0.85f, 0.25f);

        // Canvas que se adapta a cualquier resolución
        var raiz = new GameObject("HUD_Canvas");
        var canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var escalador = raiz.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 0.5f;
        raiz.AddComponent<GraphicRaycaster>();
        var hud = raiz.AddComponent<HUDJuego>();
        Transform c = raiz.transform;

        Vector2 arribaIzq = new Vector2(0, 1), arribaDer = new Vector2(1, 1), arribaCentro = new Vector2(0.5f, 1);
        Vector2 centro = new Vector2(0.5f, 0.5f), abajoIzq = Vector2.zero, abajoCentro = new Vector2(0.5f, 0);

        // Panel de contadores (arriba a la izquierda)
        var panel = ImagenUI(c, "Panel_Contadores", arribaIzq, new Vector2(24, -24), new Vector2(430, 205), null, new Color(0, 0, 0, 0.35f));
        ImagenUI(panel.transform, "Icono_Inti", arribaIzq, new Vector2(14, -12), new Vector2(62, 62), sInti, Color.white);
        hud.textoInti = TextoUI(panel.transform, "Texto_Inti", arribaIzq, new Vector2(90, -14), new Vector2(300, 60), 46, dorado, TextAnchor.MiddleLeft, "0 / 0");
        hud.iconosVida = new Image[6];
        for (int i = 0; i < 6; i++)
            hud.iconosVida[i] = ImagenUI(panel.transform, "Vida_" + (i + 1), arribaIzq, new Vector2(14 + i * 64, -82), new Vector2(58, 58), sCabeza, Color.white);
        ImagenUI(panel.transform, "Icono_Supay", arribaIzq, new Vector2(16, -148), new Vector2(50, 50), sprites["Supay_0"], Color.white);
        hud.textoSupay = TextoUI(panel.transform, "Texto_Supay", arribaIzq, new Vector2(80, -146), new Vector2(300, 54), 38, new Color(0.8f, 0.6f, 1f), TextAnchor.MiddleLeft, "0");

        // Tiempo y doble salto (arriba a la derecha)
        hud.textoTiempo = TextoUI(c, "Texto_Tiempo", arribaDer, new Vector2(-30, -24), new Vector2(300, 60), 46, Color.white, TextAnchor.MiddleRight, "00:00");
        hud.textoDobleSalto = TextoUI(c, "Texto_DobleSalto", arribaDer, new Vector2(-30, -86), new Vector2(500, 50), 30, new Color(0.6f, 0.9f, 1f), TextAnchor.MiddleRight);

        // Indicador de la cara de la máscara (arriba al centro)
        ImagenUI(c, "Panel_Cara", arribaCentro, new Vector2(0, -18), new Vector2(300, 96), null, new Color(0, 0, 0, 0.35f));
        hud.iconoCara = ImagenUI(c, "Icono_Cara", arribaCentro, new Vector2(-90, -26), new Vector2(80, 80), sInti, Color.white);
        hud.textoCara = TextoUI(c, "Texto_Cara", arribaCentro, new Vector2(40, -32), new Vector2(200, 64), 44, dorado, TextAnchor.MiddleLeft, "INTI");
        TextoUI(c, "Texto_AyudaCara", arribaCentro, new Vector2(0, -116), new Vector2(400, 34), 22, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter, "C  girar máscara");
        hud.spriteSol = sInti;
        hud.spriteLuna = sprites["Luna"];

        // Mensajes y cartel del nivel (centro)
        hud.textoMensaje = TextoUI(c, "Texto_Mensaje", centro, new Vector2(0, 170), new Vector2(1600, 150), 48, Color.white, TextAnchor.MiddleCenter);
        var cartel = UI("Cartel_Nivel", c, centro, centro, centro, new Vector2(0, 320), new Vector2(1600, 200));
        hud.cartelNivel = cartel.gameObject.AddComponent<CanvasGroup>();
        TextoUI(cartel, "Titulo_Nivel", centro, new Vector2(0, 30), new Vector2(1600, 100), 72, dorado, TextAnchor.MiddleCenter, gm.nombreNivel);
        TextoUI(cartel, "Subtitulo_Nivel", centro, new Vector2(0, -50), new Vector2(1600, 60), 34, Color.white, TextAnchor.MiddleCenter,
                "Recupera todos los Inti, vence al Gran Supay y llega a la meta");

        // Controles (abajo a la izquierda)
        TextoUI(c, "Texto_Controles", abajoIzq, new Vector2(24, 18), new Vector2(1500, 40), 24, Color.white, TextAnchor.MiddleLeft,
                "A/D o ← → moverse   ·   ESPACIO saltar   ·   X latigazo   ·   C máscara   ·   ESC pausa");

        // Barra del jefe (abajo al centro)
        var panelJefe = UI("Panel_Jefe", c, abajoCentro, abajoCentro, abajoCentro, new Vector2(0, 70), new Vector2(900, 90));
        hud.panelJefe = panelJefe.gameObject;
        hud.nombreJefe = TextoUI(panelJefe, "Nombre_Jefe", new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(900, 44), 32, Color.white, TextAnchor.MiddleCenter, "GRAN SUPAY");
        var fondoBarra = ImagenUI(panelJefe, "Barra_Fondo", abajoCentro, new Vector2(0, 4), new Vector2(900, 30), null, new Color(0.25f, 0.04f, 0.08f, 0.9f));
        var relleno = UI("Barra_Relleno", fondoBarra.transform, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        relleno.offsetMin = relleno.offsetMax = Vector2.zero;
        hud.imagenRellenoJefe = relleno.gameObject.AddComponent<Image>();
        hud.imagenRellenoJefe.color = new Color(0.65f, 0.2f, 0.9f);
        hud.imagenRellenoJefe.raycastTarget = false;
        hud.rellenoJefe = relleno;
        panelJefe.gameObject.SetActive(false);

        // Panel de pausa / victoria / derrota con botones
        var final = UI("Panel_Final", c, Vector2.zero, Vector2.one, centro, Vector2.zero, Vector2.zero);
        final.offsetMin = final.offsetMax = Vector2.zero;
        final.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        hud.panelFinal = final.gameObject;
        hud.tituloFinal = TextoUI(final, "Titulo", centro, new Vector2(0, 230), new Vector2(1600, 140), 100, dorado, TextAnchor.MiddleCenter, "PAUSA");
        hud.fraseFinal = TextoUI(final, "Frase", centro, new Vector2(0, 110), new Vector2(1600, 70), 46, Color.white, TextAnchor.MiddleCenter);
        hud.datoFinal = TextoUI(final, "Dato", centro, new Vector2(0, 30), new Vector2(1600, 60), 34, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
        hud.botonContinuar = BotonUI(final, "Boton_Continuar", new Vector2(-360, -120), "Continuar", gm.Continuar).gameObject;
        BotonUI(final, "Boton_Reintentar", new Vector2(0, -120), "Reintentar", gm.Reiniciar);
        BotonUI(final, "Boton_Menu", new Vector2(360, -120), "Menú", gm.IrAlMenu);
        final.gameObject.SetActive(false);

        // EventSystem para que los botones respondan al mouse
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

        return hud;
    }

    static RectTransform UI(string nombre, Transform padre, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    static Text TextoUI(Transform padre, string nombre, Vector2 ancla, Vector2 pos, Vector2 tam, int tamFuente, Color color,
                        TextAnchor alineacion, string texto = "")
    {
        var rt = UI(nombre, padre, ancla, ancla, ancla, pos, tam);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = fuente;
        t.fontSize = tamFuente;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.alignment = alineacion;
        t.text = texto;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var borde = rt.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(0, 0, 0, 0.85f);
        borde.effectDistance = new Vector2(2, -2);
        return t;
    }

    static Image ImagenUI(Transform padre, string nombre, Vector2 ancla, Vector2 pos, Vector2 tam, Sprite sprite, Color color)
    {
        var rt = UI(nombre, padre, ancla, ancla, ancla, pos, tam);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = sprite != null;
        img.raycastTarget = false;
        return img;
    }

    static Button BotonUI(Transform padre, string nombre, Vector2 pos, string texto, UnityAction accion)
    {
        var centro = new Vector2(0.5f, 0.5f);
        var rt = UI(nombre, padre, centro, centro, centro, pos, new Vector2(300, 84));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.62f, 0.12f, 0.12f, 0.95f);
        var b = rt.gameObject.AddComponent<Button>();
        var colores = b.colors;
        colores.highlightedColor = new Color(1f, 0.85f, 0.4f);
        colores.pressedColor = new Color(0.8f, 0.6f, 0.2f);
        b.colors = colores;
        UnityEventTools.AddPersistentListener(b.onClick, accion);
        TextoUI(rt, "Texto", centro, Vector2.zero, new Vector2(300, 84), 38, Color.white, TextAnchor.MiddleCenter, texto);
        return b;
    }

    static void Aviso(Transform padre, string nombre, float x, string mensaje)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(x, 0f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 14f);
        var z = go.AddComponent<ZonaMensaje>();
        z.mensaje = mensaje;
    }

    static GameObject Instanciar(string prefab, Transform padre, float x, float y)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[prefab], padre);
        go.transform.position = new Vector3(x, y, 0f);
        return go;
    }

    static void Enemigo(string prefab, Transform padre, float x, float y, float patrulla)
    {
        GameObject go = Instanciar(prefab, padre, x, y);
        var e = go.GetComponent<EnemigoSupay>();
        if (patrulla > 0f) e.distanciaPatrulla = patrulla;
    }

    static void Generador(Transform padre, string nombre, float xMin, float xMax, float intervalo)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3((xMin + xMax) / 2f, 4f, 0f);
        var g = go.AddComponent<GeneradorRocas>();
        g.prefabRoca = prefabs["Roca"];
        g.xMin = xMin;
        g.xMax = xMax;
        g.intervalo = intervalo;
    }

    // Muro mágico (morado y semitransparente) que encierra la arena durante la pelea
    static GameObject Muro(GameObject suelo, Transform padre, string nombre, float x)
    {
        GameObject m = Piso(suelo, padre, nombre, x, 2f, 0.4f, 14f);
        Transform pasto = m.transform.Find("Pasto");
        if (pasto != null) Object.DestroyImmediate(pasto.gameObject);
        var sr = m.GetComponent<SpriteRenderer>();
        sr.color = new Color(0.55f, 0.2f, 0.85f, 0.45f);
        sr.sortingOrder = 9;
        return m;
    }

    static void CapaFondo(Transform padre, string nombre, Sprite sprite, Vector3 pos, float escala,
                          float factorX, float factorY, float viento, int orden)
    {
        var capa = new GameObject(nombre);
        capa.transform.SetParent(padre, false);
        capa.transform.position = pos;
        var p = capa.AddComponent<CapaParallax>();
        p.factorX = factorX;
        p.factorY = factorY;
        p.viento = viento;

        var img = new GameObject("Imagen");
        img.transform.SetParent(capa.transform, false);
        img.transform.localScale = Vector3.one * escala;
        var sr = img.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;
    }

    // =====================================================================
    // 4. ESCENAS DE MENÚ E HISTORIA
    // =====================================================================

    static Texture2D Tex(string ruta) { return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta); }
    static AudioClip Audio(string nombre) { return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + nombre + ".wav"); }

    static void NuevaEscenaConCamara()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera");
        cam.tag = "MainCamera";
        var c = cam.AddComponent<Camera>();
        c.orthographic = true;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = Color.black;
        cam.AddComponent<AudioListener>();
        cam.transform.position = new Vector3(0, 0, -10);
    }

    static void CrearMenu()
    {
        NuevaEscenaConCamara();
        var go = new GameObject("MenuPrincipal");
        var m = go.AddComponent<MenuPrincipal>();
        m.fondo = Tex("Assets/Environment/Fondo1.jpg");
        m.diabloHuma = Tex("Assets/Sprites/DH.png");
        m.supay = sprites["Supay_0"].texture;
        m.inti = Tex("Assets/Sprites/Reto/Moneda_Inti.png");
        m.musica = Audio("musica_andina");
        m.sonidoSeleccion = Audio("moneda");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), RutaMenu);
    }

    static void CrearHistoria()
    {
        NuevaEscenaConCamara();
        var go = new GameObject("Historia");
        var h = go.AddComponent<Historia>();
        h.fondo = Tex("Assets/Environment/Fondo1.jpg");
        h.musica = Audio("musica_andina");

        Texture2D inti = Tex("Assets/Sprites/Reto/Moneda_Inti.png");
        Texture2D supay = sprites["Supay_0"].texture;
        Texture2D dh = Tex("Assets/Sprites/DH.png");

        h.diapositivas = new[]
        {
            D("En lo alto de los Andes, cada solsticio los pueblos celebran el Inti Raymi, la gran fiesta del Sol.",
              inti, new Color(0f, 0f, 0f, 0.25f)),
            D("Los Inti, soles sagrados, guardan la luz de las montañas y hacen crecer el maíz de las comunidades.",
              inti, new Color(0.3f, 0.15f, 0f, 0.3f)),
            D("Pero una noche sin luna, los Supay, espíritus de la oscuridad, robaron los Inti y los escondieron entre las cumbres.",
              supay, new Color(0.15f, 0f, 0.25f, 0.6f)),
            D("Sin los Inti, el sol se apagará para siempre y la fiesta jamás llegará.",
              supay, new Color(0.08f, 0f, 0.12f, 0.75f)),
            D("Entonces despertó el Diablo Huma, guardián de los Andes. Su máscara de dos caras mira al pasado y al futuro.",
              dh, new Color(0.3f, 0.1f, 0f, 0.35f)),
            D("Con su látigo sagrado deberá vencer a los Supay, recuperar cada Inti y devolver la luz a su pueblo.",
              dh, new Color(0.4f, 0.2f, 0f, 0.3f)),
        };
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), RutaHistoria);
    }

    static Historia.Diapositiva D(string texto, Texture2D imagen, Color tinte)
    {
        return new Historia.Diapositiva { texto = texto, imagen = imagen, tinte = tinte };
    }

    static void ConfigurarBuild()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(RutaMenu, true),
            new EditorBuildSettingsScene(RutaHistoria, true),
            new EditorBuildSettingsScene(RutaNivel, true),
        };
    }
}
#endif
