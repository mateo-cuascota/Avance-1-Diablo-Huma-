using UnityEngine;
using UnityEngine.UI;

// HUD del juego hecho con Canvas: cada elemento es un objeto de la jerarquía (HUD_Canvas)
// que se puede mover, cambiar de tamaño o de color en el editor. Este script solo actualiza los valores.
public class HUDJuego : MonoBehaviour
{
    [Header("Contadores")]
    public Image[] iconosVida;
    public Text textoInti;
    public Text textoSupay;
    public Text textoTiempo;
    public Text textoDobleSalto;

    [Header("Máscara")]
    public Image iconoCara;
    public Text textoCara;
    public Sprite spriteSol;
    public Sprite spriteLuna;

    [Header("Mensajes")]
    public Text textoMensaje;
    public CanvasGroup cartelNivel;

    [Header("Jefe")]
    public GameObject panelJefe;
    public RectTransform rellenoJefe;
    public Image imagenRellenoJefe;
    public Text nombreJefe;

    [Header("Pausa y final")]
    public GameObject panelFinal;
    public Text tituloFinal;
    public Text fraseFinal;
    public Text datoFinal;
    public GameObject botonContinuar;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // Vidas: una máscara por vida
        for (int i = 0; i < iconosVida.Length; i++)
        {
            bool tiene = i < gm.Vidas;
            iconosVida[i].enabled = i < gm.vidasMaximas;
            iconosVida[i].color = tiene ? Color.white : new Color(0f, 0f, 0f, 0.35f);
        }

        textoInti.text = gm.MonedasRecogidas + " / " + gm.TotalMonedas;
        textoSupay.text = gm.EnemigosDerrotados.ToString();
        textoTiempo.text = GameManager.Formato(gm.TiempoJuego);

        bool doble = gm.Jugador != null && gm.Jugador.TieneDobleSalto;
        textoDobleSalto.enabled = doble;
        if (doble) textoDobleSalto.text = "Doble salto  " + Mathf.CeilToInt(gm.Jugador.TiempoDobleSalto) + " s";

        // Cara actual de la máscara
        bool inti = MascaraDual.Actual == Cara.Inti;
        iconoCara.sprite = inti ? spriteSol : spriteLuna;
        textoCara.text = inti ? "INTI" : "KILLA";
        textoCara.color = inti ? new Color(1f, 0.85f, 0.25f) : new Color(0.65f, 0.75f, 1f);

        textoMensaje.text = gm.Mensaje;

        // Cartel con el nombre del nivel que se desvanece
        if (cartelNivel != null)
        {
            cartelNivel.alpha = Mathf.Clamp01(3f - gm.TiempoJuego);
            cartelNivel.gameObject.SetActive(cartelNivel.alpha > 0f);
        }

        // Barra del jefe
        JefeSupay jefe = gm.Jefe;
        bool mostrarJefe = jefe != null && jefe.Activo && !jefe.Muerto && !gm.Ganado;
        panelJefe.SetActive(mostrarJefe);
        if (mostrarJefe)
        {
            rellenoJefe.anchorMax = new Vector2(jefe.VidaNormalizada, 1f);
            imagenRellenoJefe.color = jefe.EnFase2 ? new Color(1f, 0.25f, 0.1f) : new Color(0.65f, 0.2f, 0.9f);
            nombreJefe.text = jefe.nombre + (jefe.EnFase2 ? "  ·  ¡FURIOSO!" : "");
        }

        // Pausa, victoria o derrota
        bool final = gm.Pausado || gm.Ganado || gm.Perdido;
        panelFinal.SetActive(final);
        if (!final) return;

        botonContinuar.SetActive(gm.Pausado);
        CentrarBotones();
        if (gm.Ganado)
        {
            tituloFinal.text = "¡NIVEL COMPLETADO!";
            tituloFinal.color = new Color(1f, 0.85f, 0.2f);
            fraseFinal.text = "El Diablo Huma protegió los Andes";
            datoFinal.text = "Tiempo " + GameManager.Formato(gm.TiempoJuego) + "   ·   Supay vencidos: " + gm.EnemigosDerrotados +
                             (gm.JefeVencido ? "   ·   Gran Supay ✔" : "");
        }
        else if (gm.Perdido)
        {
            tituloFinal.text = "GAME OVER";
            tituloFinal.color = new Color(1f, 0.35f, 0.35f);
            fraseFinal.text = "Los Inti quedaron sin guardián";
            datoFinal.text = "Inti recuperados: " + gm.MonedasRecogidas + " / " + gm.TotalMonedas;
        }
        else
        {
            tituloFinal.text = "PAUSA";
            tituloFinal.color = Color.white;
            fraseFinal.text = "El sol espera a su guardián";
            datoFinal.text = "ESC continuar   ·   R reiniciar   ·   M menú";
        }
    }

    // Reparte los botones visibles alrededor del centro (2 en victoria/derrota, 3 en pausa)
    void CentrarBotones()
    {
        const float separacion = 360f;
        var visibles = new System.Collections.Generic.List<RectTransform>();
        foreach (Transform hijo in panelFinal.transform)
            if (hijo.name.StartsWith("Boton_") && hijo.gameObject.activeSelf)
                visibles.Add((RectTransform)hijo);
        visibles.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

        float inicio = -separacion * (visibles.Count - 1) / 2f;
        for (int i = 0; i < visibles.Count; i++)
        {
            Vector2 p = visibles[i].anchoredPosition;
            p.x = inicio + i * separacion;
            visibles[i].anchoredPosition = p;
        }
    }
}
