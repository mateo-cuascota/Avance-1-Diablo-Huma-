#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramienta de editor: arma automáticamente el reto del "Nivel 1".
// Menú: Diablo Huma > Configurar reto del Nivel 1
// Se puede ejecutar varias veces: borra lo que generó antes y lo vuelve a crear.
public static class ConfigurarNivel
{
    const string RutaEscena = "Assets/Scenes/Nivel 1.unity";
    const string RutaMoneda = "Assets/Sprites/Reto/Moneda_Inti.png";
    const string RutaPincho = "Assets/Sprites/Reto/Pincho.png";
    const string RutaLatigoE = "Assets/Sprites/Latigo E.png";
    const string RutaLatigoR = "Assets/Sprites/Latigo R.png";
    const string CarpetaAnim = "Assets/Sprites/Latigo";

    [MenuItem("Diablo Huma/Configurar reto del Nivel 1")]
    public static void Configurar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        ConfigurarSprite(RutaMoneda, 130f, SpriteAlignment.Center);
        ConfigurarSprite(RutaPincho, 160f, SpriteAlignment.BottomCenter);

        var escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);

        GameObject jugador = GameObject.Find("DiabloHuma");
        if (jugador == null)
        {
            Debug.LogError("No se encontró el objeto DiabloHuma en la escena.");
            return;
        }

        CrearLatigo(jugador);
        CrearReto(jugador);
        AgregarEscenaAlBuild();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("Nivel 1 configurado: monedas Inti, pinchos, GameManager y latigazo (X).");
    }

    // ---------- Importación de sprites ----------
    static void ConfigurarSprite(string ruta, float ppu, SpriteAlignment alineacion)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogError("No existe " + ruta); return; }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;

        var ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)alineacion;
        imp.SetTextureSettings(ajustes);
        imp.SaveAndReimport();
    }

    static Sprite CargarSprite(string ruta)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
    }

    // ---------- Latigazo: 4ta animación (tecla X) ----------
    static void CrearLatigo(GameObject jugador)
    {
        Transform brazo = jugador.transform.Find("Partes_DiabloHuma/Brazo I_0");
        if (brazo == null)
        {
            Debug.LogWarning("No se encontró Brazo I_0; se omite el látigo.");
            return;
        }

        Transform viejo = brazo.Find("Latigo");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        Sprite enrollado = CargarSprite(RutaLatigoR);
        Sprite extendido = CargarSprite(RutaLatigoE);

        if (!AssetDatabase.IsValidFolder(CarpetaAnim))
            AssetDatabase.CreateFolder("Assets/Sprites", "Latigo");

        // Clip 1: látigo enrollado en la mano (quieto)
        var clipEnrollado = new AnimationClip { frameRate = 60 };
        var bindSprite = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clipEnrollado, bindSprite, new[]
        {
            new ObjectReferenceKeyframe { time = 0f, value = enrollado }
        });
        var ajustesLoop = AnimationUtility.GetAnimationClipSettings(clipEnrollado);
        ajustesLoop.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clipEnrollado, ajustesLoop);

        // Clip 2: latigazo (se extiende, golpea y se vuelve a enrollar)
        var clipLatigazo = new AnimationClip { frameRate = 60 };
        AnimationUtility.SetObjectReferenceCurve(clipLatigazo, bindSprite, new[]
        {
            new ObjectReferenceKeyframe { time = 0f,    value = enrollado },
            new ObjectReferenceKeyframe { time = 0.05f, value = extendido },
            new ObjectReferenceKeyframe { time = 0.6f,  value = enrollado }
        });
        var rotZ = new AnimationCurve(
            new Keyframe(0f, 70f), new Keyframe(0.12f, -20f),
            new Keyframe(0.4f, -5f), new Keyframe(0.6f, 0f));
        var cero = AnimationCurve.Constant(0f, 0.6f, 0f);
        AnimationUtility.SetEditorCurve(clipLatigazo,
            EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.x"), cero);
        AnimationUtility.SetEditorCurve(clipLatigazo,
            EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.y"), cero);
        AnimationUtility.SetEditorCurve(clipLatigazo,
            EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.z"), rotZ);
        // El látigo extendido se ve más grande que el enrollado
        var escala = new AnimationCurve(
            new Keyframe(0f, 0.8f), new Keyframe(0.05f, 1.5f),
            new Keyframe(0.55f, 1.5f), new Keyframe(0.6f, 0.8f));
        foreach (string eje in new[] { "x", "y" })
        {
            AnimationUtility.SetEditorCurve(clipLatigazo,
                EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale." + eje), escala);
            AnimationUtility.SetEditorCurve(clipEnrollado,
                EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale." + eje),
                AnimationCurve.Constant(0f, 0.1f, 0.8f));
        }
        AnimationUtility.SetEditorCurve(clipLatigazo,
            EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.z"), AnimationCurve.Constant(0f, 0.6f, 1f));
        AnimationUtility.SetEditorCurve(clipEnrollado,
            EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.z"), AnimationCurve.Constant(0f, 0.1f, 1f));

        var ajustesUnaVez = AnimationUtility.GetAnimationClipSettings(clipLatigazo);
        ajustesUnaVez.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clipLatigazo, ajustesUnaVez);

        GuardarAsset(clipEnrollado, CarpetaAnim + "/Latigo_Enrollado.anim");
        GuardarAsset(clipLatigazo, CarpetaAnim + "/Latigazo.anim");

        // Animator Controller con trigger "Atacar"
        string rutaCtrl = CarpetaAnim + "/Latigo.controller";
        AssetDatabase.DeleteAsset(rutaCtrl);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(rutaCtrl);
        ctrl.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var estadoQuieto = sm.AddState("Enrollado");
        estadoQuieto.motion = clipEnrollado;
        var estadoAtaque = sm.AddState("Latigazo");
        estadoAtaque.motion = clipLatigazo;
        sm.defaultState = estadoQuieto;

        var ida = estadoQuieto.AddTransition(estadoAtaque);
        ida.AddCondition(AnimatorConditionMode.If, 0, "Atacar");
        ida.hasExitTime = false;
        ida.duration = 0f;

        var vuelta = estadoAtaque.AddTransition(estadoQuieto);
        vuelta.hasExitTime = true;
        vuelta.exitTime = 1f;
        vuelta.duration = 0f;

        // Objeto del látigo en la mano del personaje
        var latigo = new GameObject("Latigo");
        latigo.transform.SetParent(brazo, false);
        latigo.transform.localPosition = new Vector3(0.83f, -1.15f, 0f);
        latigo.transform.localScale = Vector3.one * 0.8f;
        var sr = latigo.AddComponent<SpriteRenderer>();
        sr.sprite = enrollado;
        sr.sortingOrder = 4;
        var anim = latigo.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        // Sonido del latigazo en el script de movimiento
        var mov = jugador.GetComponent<MovimientoJugador>();
        if (mov != null)
        {
            mov.sonidoLatigo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/latigo.wav");
            EditorUtility.SetDirty(mov);
        }
    }

    static void GuardarAsset(Object obj, string ruta)
    {
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(obj, ruta);
    }

    // ---------- Reto: GameManager, monedas y pinchos ----------
    static void CrearReto(GameObject jugador)
    {
        GameObject viejo = GameObject.Find("Reto_Nivel");
        if (viejo != null) Object.DestroyImmediate(viejo);

        var raiz = new GameObject("Reto_Nivel");

        // Medidas del escenario existente
        Bounds suelo = Limites("Suelo", new Bounds(new Vector3(0, -3, 0), new Vector3(12, 1, 0)));
        Bounds p1 = Limites("Plataforma1", new Bounds(new Vector3(3, -1, 0), new Vector3(3, 0.5f, 0)));
        Bounds p2 = Limites("Plataform2", new Bounds(new Vector3(6, 0.5f, 0), new Vector3(3, 0.5f, 0)));
        float g = suelo.max.y;
        float xInicio = jugador.transform.position.x;
        float xPinchos = p1.min.x - 1.25f;

        // GameManager
        var gmObj = new GameObject("GameManager");
        gmObj.transform.SetParent(raiz.transform, false);
        var gm = gmObj.AddComponent<GameManager>();
        gm.sonidoMoneda = Audio("moneda");
        gm.sonidoGolpe = Audio("golpe");
        gm.sonidoVictoria = Audio("victoria");
        gm.musicaFondo = Audio("musica_andina");

        // Monedas Inti
        var monedas = new GameObject("Monedas");
        monedas.transform.SetParent(raiz.transform, false);
        gm.contenedorMonedas = monedas.transform;

        Sprite spMoneda = CargarSprite(RutaMoneda);
        var posiciones = new List<Vector2>
        {
            new Vector2(xInicio - 0.6f, g + 4.1f), // sobre el inicio (hay que saltar)
            new Vector2(xInicio + 2.2f, g + 1.5f), // en el camino
            new Vector2(xPinchos,        g + 3.9f), // sobre los pinchos
            new Vector2(p1.center.x,     p1.max.y + 3.6f), // sobre la plataforma 1
            new Vector2(p2.center.x,     p2.max.y + 3.65f) // sobre la plataforma 2
        };

        for (int i = 0; i < posiciones.Count; i++)
        {
            var m = new GameObject("Inti_" + (i + 1));
            m.transform.SetParent(monedas.transform, false);
            m.transform.position = posiciones[i];
            var sr = m.AddComponent<SpriteRenderer>();
            sr.sprite = spMoneda;
            sr.sortingOrder = 5;
            var col = m.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            m.AddComponent<MonedaInti>();
        }

        // Pinchos (obstáculo)
        Sprite spPincho = CargarSprite(RutaPincho);
        var pinchos = new GameObject("Pinchos");
        pinchos.transform.SetParent(raiz.transform, false);
        pinchos.transform.position = new Vector3(xPinchos, g, 0);
        for (int i = -1; i <= 1; i++)
        {
            var p = new GameObject("Pincho");
            p.transform.SetParent(pinchos.transform, false);
            p.transform.localPosition = new Vector3(i * 0.7f, 0, 0);
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = spPincho;
            sr.sortingOrder = 4;
        }
        var trig = pinchos.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(1.9f, 0.45f);
        trig.offset = new Vector2(0, 0.22f);
        pinchos.AddComponent<Pinchos>();
    }

    static Bounds Limites(string nombre, Bounds porDefecto)
    {
        var go = GameObject.Find(nombre);
        if (go == null) return porDefecto;
        var r = go.GetComponent<Renderer>();
        return r != null ? r.bounds : porDefecto;
    }

    static AudioClip Audio(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + nombre + ".wav");
    }

    static void AgregarEscenaAlBuild()
    {
        // Si ya está en el Build no se toca el orden (el menú principal debe seguir primero)
        if (EditorBuildSettings.scenes.Any(s => s.path == RutaEscena))
            return;
        var lista = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(RutaEscena, true) };
        lista.AddRange(EditorBuildSettings.scenes);
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
#endif
