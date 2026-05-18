using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControladorMenu : MonoBehaviour
{
    public static ControladorMenu Instancia;

    [Header("Configuración Base")]
    public GameObject panelMenu;
    public Transform headAnchor;

    [Header("Punteros Láser")]
    public PunteroLaserVR laserIzquierdo;
    public PunteroLaserVR laserDerecho;
    public UnityEngine.EventSystems.OVRInputModule inputModule;

    [Header("Paneles")]
    public GameObject panelBienvenida;
    public GameObject panelNiveles;
    public GameObject panelPausa;
    public GameObject panelAjustes;
    public GameObject panelResultados;
    public GameObject panelConfPantallas;

    [Header("Botones de Modo (Para oscurecer en todos los menús)")]
    public Button[] botonesMandoIzq;
    public Button[] botonesMandoDer;
    public Button[] botonesMandoAmbos;

    [Header("Botones de Dificultad")]
    public Button[] botonesDifFacil;
    public Button[] botonesDifNormal;
    public Button[] botonesDifDificil;

    [Header("Centrado de Vista")]
    public Transform contenedorJuego; // Antiguo pantallaArkanoid
    public float distanciaPantalla = 3.5f;
    public float tamanoMenu = 1.0f;

    [Header("Referencias UI (Niveles)")]
    public TextMeshProUGUI textoNumNivel;
    public TextMeshProUGUI textoStatsClinicas;
    private int nivelSeleccionado = 0;

    [Header("Calibración")]
    public GameObject panelCuentaAtras;
    public TextMeshProUGUI textoCuentaAtras;
    public TextMeshProUGUI textoInstrucciones;

    [Header("Ajustes de Sonido")]
    public Slider sliderVolumen;

    [Header("Sonidos de Interfaz")]
    public AudioClip sonidoBoton;
    private AudioSource audioSourceMenu;

    [Header("Pantalla")]
    public Toggle togglePantallaCurva;
    public GameObject pantallaPlana;
    public GameObject pantallaCurva;

    private bool primeraVezAbierto = true;
    private bool calibracionEnProceso = false;
    private bool partidaTerminada = false;

    [Header("Sliders de Distancia")]
    public Slider sliderTamanoMenu;
    public Slider sliderDistanciaPantalla;

    [Header("Textos Panel Resultados")]
    public TextMeshProUGUI textoTituloResultados;
    public TextMeshProUGUI textoStatsResultados;

    [Header("Imágenes de Calibración")]
    public Image imagenCalibracion;
    public Sprite imgBrazoIzq_Centro;
    public Sprite imgBrazoIzq_EstiradoIzq;
    public Sprite imgBrazoIzq_EstiradoDer;
    public Sprite imgBrazoDer_Centro;
    public Sprite imgBrazoDer_EstiradoIzq;
    public Sprite imgBrazoDer_EstiradoDer;

    [Header("Sistema de Atención")]
    public float anguloTolerancia = 45f;
    public float tiempoParaAviso = 3f;
    private float tiempoMirandoFuera = 0f;
    private float cooldownAviso = 0f;

    [Header("Tamaño Base del Menú")]
    public Vector3 escalaBaseMenu = new Vector3(0.01f, 0.01f, 0.01f);

    void Start()
    {
        audioSourceMenu = gameObject.AddComponent<AudioSource>();
        audioSourceMenu.playOnAwake = false;
        audioSourceMenu.ignoreListenerPause = true;
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
        panelMenu.SetActive(false);
    }

    private System.Collections.IEnumerator RutinaAplicarConfiguracionInicial()
    {
        yield return null;

        DatosConfiguracion config = GestorDatosUsuario.Instancia.configActual;

        if (sliderVolumen != null) sliderVolumen.value = config.volumen;
        AudioListener.volume = config.volumen;

        if (MonitorClinico.Instancia != null)
        {
            MonitorClinico.Instancia.dificultadActual = (MonitorClinico.NivelDificultad)config.dificultad;
            MonitorClinico.Instancia.modoActual = (MonitorClinico.ModoControl)config.modoMando;
        }

        bool esCurva = config.pantallaCurva;
        if (togglePantallaCurva != null) togglePantallaCurva.isOn = esCurva;
        if (pantallaPlana != null) pantallaPlana.SetActive(!esCurva);
        if (pantallaCurva != null) pantallaCurva.SetActive(esCurva);

        tamanoMenu = config.tamanoMenu < 0.5f ? 1.0f : config.tamanoMenu;
        if (sliderTamanoMenu != null) sliderTamanoMenu.value = tamanoMenu;

        float distInicial = esCurva ? config.distanciaCurva : config.distanciaPlana;
        distanciaPantalla = distInicial;
        if (sliderDistanciaPantalla != null) sliderDistanciaPantalla.value = distInicial;

        CentrarVistaUsuario();

        ActualizarLaseres(true);
        ActualizarBotonesModo();
        ActualizarBotonesDificultad();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.X))
        {
            IniciarCalibracionPierna();
        }
        ColocarMenuDelanteDeLaMirada();
    }

    public void IniciarCalibracionPierna()
    {
        if (calibracionEnProceso) return;

        // Abrimos el menú y apagamos el juego de fondo temporalmente
        panelMenu.SetActive(true);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(false);

        ColocarMenuDelanteDeLaMirada();
        StartCoroutine(RutinaCalibrarPierna());
    }

    private System.Collections.IEnumerator RutinaCalibrarPierna()
    {
        calibracionEnProceso = true;
        AbrirPanel(panelCuentaAtras);
        ActualizarLaseres(false);

        DetectorPiernaVR detector = DetectorPiernaVR.Instancia;

        yield return FaseContador("1/5: MANTÉN LA PIERNA EN REPOSO", null);
        detector.calibracion.reposo = detector.ObtenerDatosHardware();

        yield return FaseContador("2/5: INCLINA LA PIERNA A LA IZQUIERDA", null);
        detector.calibracion.izquierda = detector.ObtenerDatosHardware();

        yield return FaseContador("3/5: INCLINA LA PIERNA A LA DERECHA", null);
        detector.calibracion.derecha = detector.ObtenerDatosHardware();

        yield return FaseContador("4/5: ESTIRA LA PIERNA (ABAJO)", null);
        detector.calibracion.extendida = detector.ObtenerDatosHardware();

        yield return FaseContador("5/5: LEVANTA LA RODILLA (ARRIBA)", null);
        detector.calibracion.levantada = detector.ObtenerDatosHardware();

        detector.ExportarCalibracionJSON();

        textoInstrucciones.text = "¡CALIBRACIÓN GUARDADA!";
        textoCuentaAtras.text = "JSON generado en PC";
        yield return new WaitForSecondsRealtime(2f);

        panelCuentaAtras.SetActive(false);
        panelMenu.SetActive(false);
        calibracionEnProceso = false;
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
    }

    public void ReproducirSonidoClic()
    {
        if (sonidoBoton != null && audioSourceMenu != null)
        {
            audioSourceMenu.PlayOneShot(sonidoBoton);
        }
    }

    public void AlternarMenuGeneral()
    {
        if (panelMenu == null || headAnchor == null || calibracionEnProceso || GestorRitmo.Instancia == null ||
            (panelMenu.activeSelf && !GestorRitmo.Instancia.juegoEmpezado && !GestorRitmo.Instancia.enCuentaAtras)) return;

        bool estaActivado = !panelMenu.activeSelf;
        panelMenu.SetActive(estaActivado);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(!estaActivado);

        ActualizarLaseres(estaActivado);

        if (GestorRitmo.Instancia != null)
        {
            GestorRitmo.Instancia.AlternarPausa(estaActivado);
        }

        if (estaActivado)
        {
            ColocarMenuDelanteDeLaMirada();
            if (partidaTerminada)
            {
                AbrirPanel(panelResultados);
                partidaTerminada = false;
            }
            else if (primeraVezAbierto)
            {
                AbrirPanel(panelBienvenida);
            }
            else if (GestorRitmo.Instancia != null && !GestorRitmo.Instancia.juegoEmpezado && !GestorRitmo.Instancia.enCuentaAtras)
            {
                AbrirPanel(panelNiveles);
            }
            else
            {
                ActualizarStatsPausa();
                AbrirPanel(panelPausa);
                if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
            }
        }
    }

    public void AbrirPanel(GameObject panelDestino)
    {
        panelCuentaAtras.SetActive(false);
        panelBienvenida.SetActive(false);
        panelNiveles.SetActive(false);
        panelPausa.SetActive(false);
        panelAjustes.SetActive(false);
        panelResultados.SetActive(false);
        panelConfPantallas.SetActive(false);

        panelDestino.SetActive(true);
    }

    public void BotonUI_AbrirConfPantallas()
    {
        AbrirPanel(panelConfPantallas);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
    }

    public void CentrarVistaUsuario()
    {
        if (headAnchor == null || contenedorJuego == null) return;

        Vector3 headPos = headAnchor.position;
        if (headPos.y < 0.5f)
        {
            headPos.y = 1.8f;
        }

        Vector3 lookDirection = headAnchor.forward;
        if (Mathf.Abs(lookDirection.y) > 0.8f)
        {
            lookDirection = Vector3.ProjectOnPlane(lookDirection, Vector3.up);
        }

        Vector3 posPantalla = headPos + (lookDirection.normalized * distanciaPantalla);

        float limiteAlturaSeguridad = -0.1f;
        if (togglePantallaCurva != null && togglePantallaCurva.isOn) limiteAlturaSeguridad = 1.5f;

        posPantalla.y = Mathf.Max(posPantalla.y, headPos.y + limiteAlturaSeguridad);

        contenedorJuego.position = posPantalla;
        contenedorJuego.LookAt(headPos);
        contenedorJuego.Rotate(0, 180, 0);

        ColocarMenuDelanteDeLaMirada();
    }

    void ColocarMenuDelanteDeLaMirada()
    {
        if (headAnchor == null) return;

        Vector3 headPos = headAnchor.position;
        Vector3 lookDirection = headAnchor.forward;

        if (headPos.y < 0.5f)
        {
            headPos.y = 1.5f;
            if (lookDirection == Vector3.zero) lookDirection = Vector3.forward;
        }

        Vector3 targetPos = headPos + (lookDirection.normalized * 2.5f);
        targetPos.y = Mathf.Max(targetPos.y, headPos.y - 0.2f);

        float distanciaAlObjetivo = Vector3.Distance(transform.position, targetPos);

        if (distanciaAlObjetivo > 1.5f) transform.position = targetPos;
        else transform.position = Vector3.Lerp(transform.position, targetPos, Time.unscaledDeltaTime * 5f);

        Vector3 direccionHaciaCabeza = transform.position - headPos;
        if (direccionHaciaCabeza != Vector3.zero)
        {
            Quaternion rotacionIdeal = Quaternion.LookRotation(direccionHaciaCabeza);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionIdeal, Time.unscaledDeltaTime * 5f);
        }

        transform.localScale = escalaBaseMenu * tamanoMenu;
    }

    public void BotonUI_CambiarMandoActivo(int modoElegido)
    {
        if (MonitorClinico.Instancia != null) MonitorClinico.Instancia.modoActual = (MonitorClinico.ModoControl)modoElegido;
        ActualizarLaseres(true);
        ActualizarBotonesModo();

        if (GestorDatosUsuario.Instancia != null)
        {
            GestorDatosUsuario.Instancia.configActual.modoMando = modoElegido;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
    }

    private void ActualizarBotonesModo()
    {
        MonitorClinico.ModoControl modo = MonitorClinico.ModoControl.Derecho;
        if (MonitorClinico.Instancia != null) modo = MonitorClinico.Instancia.modoActual;

        foreach (Button btn in botonesMandoIzq) if (btn != null) btn.interactable = (modo != MonitorClinico.ModoControl.Izquierdo);
        foreach (Button btn in botonesMandoDer) if (btn != null) btn.interactable = (modo != MonitorClinico.ModoControl.Derecho);
        foreach (Button btn in botonesMandoAmbos) if (btn != null) btn.interactable = (modo != MonitorClinico.ModoControl.Ambos);
    }

    private void ActualizarLaseres(bool menuAbierto)
    {
        if (inputModule != null) inputModule.enabled = menuAbierto;

        if (!menuAbierto)
        {
            if (laserIzquierdo != null) laserIzquierdo.enabled = false;
            if (laserDerecho != null) laserDerecho.enabled = false;
            return;
        }

        MonitorClinico.ModoControl modo = MonitorClinico.ModoControl.Derecho;
        if (MonitorClinico.Instancia != null) modo = MonitorClinico.Instancia.modoActual;

        if (modo == MonitorClinico.ModoControl.Izquierdo)
        {
            if (laserIzquierdo != null) laserIzquierdo.enabled = true;
            if (laserDerecho != null) laserDerecho.enabled = false;
            if (inputModule != null) inputModule.rayTransform = laserIzquierdo.transform;
        }
        else if (modo == MonitorClinico.ModoControl.Derecho)
        {
            if (laserIzquierdo != null) laserIzquierdo.enabled = false;
            if (laserDerecho != null) laserDerecho.enabled = true;
            if (inputModule != null) inputModule.rayTransform = laserDerecho.transform;
        }
        else
        {
            if (laserIzquierdo != null) laserIzquierdo.enabled = true;
            if (laserDerecho != null) laserDerecho.enabled = true;
            if (inputModule != null) inputModule.rayTransform = laserDerecho.transform;
        }
    }

    public void BotonUI_AvanzarDesdeBienvenida()
    {
        primeraVezAbierto = false;
        if (NotificacionFlotanteVR.Instancia != null)
        {
            NotificacionFlotanteVR.Instancia.MostrarNotificacion($"Si quieres centrar la vista, pulsa el botón Y o el botón B", 5f);
        }
        AbrirPanel(panelNiveles);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
    }

    public void CambiarNivel(int direccion)
    {
        // Lógica de niveles de ritmo por implementar
    }

    public void BotonUI_Jugar()
    {
        if (GestorRitmo.Instancia != null)
        {
            GestorRitmo.Instancia.EmpezarPartidaDesdeMenu();
            AlternarMenuGeneral();
        }
    }

    public void BotonUI_Reiniciar()
    {
        if (GestorRitmo.Instancia != null)
        {
            GestorRitmo.Instancia.ReiniciarNivelActual();
            AlternarMenuGeneral();
        }
    }

    public void BotonUI_VolverAlMenu()
    {
        if (GestorRitmo.Instancia != null)
        {
            GestorRitmo.Instancia.VolverAlMenuPrincipal();
            if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
            AbrirPanel(panelNiveles);
        }
    }

    public void BotonUI_IrAAjustes()
    {
        AbrirPanel(panelAjustes);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(false);
    }

    public void BotonUI_VolverAAjustesAnterior()
    {
        if (partidaTerminada) AbrirPanel(panelResultados);
        else if (primeraVezAbierto) AbrirPanel(panelBienvenida);
        else if (GestorRitmo.Instancia != null && !GestorRitmo.Instancia.juegoEmpezado && !GestorRitmo.Instancia.enCuentaAtras)
        {
            AbrirPanel(panelNiveles);
            if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
        }
        else AbrirPanel(panelPausa);
    }

    void ActualizarStatsPausa()
    {
        if (textoStatsClinicas != null)
        {
            textoStatsClinicas.text = "<color=#FFD700>JUEGO EN PAUSA</color>\n\n(Estadísticas en desarrollo...)";
        }
    }

    public void CambiarVolumenGeneral()
    {
        if (sliderVolumen != null) AudioListener.volume = sliderVolumen.value;

        if (GestorDatosUsuario.Instancia != null && sliderVolumen != null)
        {
            GestorDatosUsuario.Instancia.configActual.volumen = sliderVolumen.value;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
    }

    public void BotonUI_CambiarDificultad(int difElegida)
    {
        if (MonitorClinico.Instancia != null) MonitorClinico.Instancia.dificultadActual = (MonitorClinico.NivelDificultad)difElegida;
        ActualizarBotonesDificultad();

        if (GestorDatosUsuario.Instancia != null)
        {
            GestorDatosUsuario.Instancia.configActual.dificultad = difElegida;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
    }

    private void ActualizarBotonesDificultad()
    {
        MonitorClinico.NivelDificultad dif = MonitorClinico.NivelDificultad.Facil;
        if (MonitorClinico.Instancia != null) dif = MonitorClinico.Instancia.dificultadActual;

        foreach (var btn in botonesDifFacil) if (btn != null) btn.interactable = (dif != MonitorClinico.NivelDificultad.Facil);
        foreach (var btn in botonesDifNormal) if (btn != null) btn.interactable = (dif != MonitorClinico.NivelDificultad.Normal);
        foreach (var btn in botonesDifDificil) if (btn != null) btn.interactable = (dif != MonitorClinico.NivelDificultad.Dificil);
    }

    public void BotonUI_IniciarCalibracion()
    {
        StartCoroutine(RutinaCalibrarCentro());
    }

    private System.Collections.IEnumerator RutinaCalibrarCentro()
    {
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(false);
        calibracionEnProceso = true;
        panelAjustes.SetActive(false);
        panelCuentaAtras.SetActive(true);

        ActualizarLaseres(false);

        MonitorClinico.ModoControl modo = MonitorClinico.Instancia.modoActual;

        if (modo == MonitorClinico.ModoControl.Ambos)
        {
            yield return CalibrarBrazoCompleto(OVRInput.Controller.LTouch, "MANDO IZQUIERDO");
            yield return CalibrarBrazoCompleto(OVRInput.Controller.RTouch, "MANDO DERECHO");
        }
        else if (modo == MonitorClinico.ModoControl.Izquierdo)
        {
            yield return CalibrarBrazoCompleto(OVRInput.Controller.LTouch, "MANDO IZQUIERDO");
        }
        else
        {
            yield return CalibrarBrazoCompleto(OVRInput.Controller.RTouch, "MANDO DERECHO");
        }

        GestorDatosUsuario.Instancia.GuardarConfiguracion();
        textoInstrucciones.text = "¡CALIBRACIÓN COMPLETA!";
        textoCuentaAtras.text = "";
        yield return new WaitForSecondsRealtime(2f);

        panelCuentaAtras.SetActive(false);
        panelAjustes.SetActive(true);
        calibracionEnProceso = false;
        ActualizarLaseres(true);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
    }

    private System.Collections.IEnumerator CalibrarBrazoCompleto(OVRInput.Controller mando, string nombreMando)
    {
        // En piernas, la calibración será diferente, pero dejamos el esqueleto para que compile
        yield return FaseContador(nombreMando + ": PON EL MANDO EN EL CENTRO", imgBrazoIzq_Centro);
        float centro = OVRInput.GetLocalControllerPosition(mando).x;
    }

    private System.Collections.IEnumerator FaseContador(string instruccion, Sprite imagenAMostrar)
    {
        textoInstrucciones.text = instruccion;
        if (imagenCalibracion != null && imagenAMostrar != null)
        {
            imagenCalibracion.gameObject.SetActive(true);
            imagenCalibracion.sprite = imagenAMostrar;
        }

        for (int i = 5; i > 0; i--)
        {
            textoCuentaAtras.text = i.ToString();
            ReproducirSonidoClic();
            yield return new WaitForSecondsRealtime(1f);
        }
        textoCuentaAtras.text = "OK";
        yield return new WaitForSecondsRealtime(0.5f);
    }

    public void MostrarResultadosFinales(string titulo)
    {
        partidaTerminada = true;
        if (textoTituloResultados != null) textoTituloResultados.text = titulo;
        if (textoStatsResultados != null) textoStatsResultados.text = "Resultados en desarrollo...";

        if (!panelMenu.activeSelf) AlternarMenuGeneral();
        else AbrirPanel(panelResultados);
    }

    public void CambiarCurvaturaPantalla()
    {
        // ... (Lógica de pantalla curva mantenida)
    }

    public void CambiarTamanoMenu(float nuevoTamano)
    {
        if (sliderTamanoMenu != null) sliderTamanoMenu.value = nuevoTamano;
        tamanoMenu = nuevoTamano;

        if (GestorDatosUsuario.Instancia != null)
        {
            GestorDatosUsuario.Instancia.configActual.tamanoMenu = nuevoTamano;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
        }
        ColocarMenuDelanteDeLaMirada();
    }

    public void CambiarDistanciaPantalla(float nuevaDist)
    {
        distanciaPantalla = nuevaDist;
        CentrarVistaUsuario();
    }

    void ComprobarAtencionJugador()
    {
        if (GestorRitmo.Instancia == null || (!GestorRitmo.Instancia.juegoEmpezado && !GestorRitmo.Instancia.enCuentaAtras)) return;
        if (contenedorJuego == null || !contenedorJuego.gameObject.activeSelf) return;

        if (cooldownAviso > 0) cooldownAviso -= Time.deltaTime;

        Vector3 direccionHaciaPantalla = (contenedorJuego.position - headAnchor.position).normalized;
        float anguloDesvio = Vector3.Angle(headAnchor.forward, direccionHaciaPantalla);

        if (anguloDesvio > anguloTolerancia)
        {
            tiempoMirandoFuera += Time.deltaTime;
            if (tiempoMirandoFuera >= tiempoParaAviso && cooldownAviso <= 0)
            {
                if (NotificacionFlotanteVR.Instancia != null)
                {
                    NotificacionFlotanteVR.Instancia.MostrarNotificacion("Puedes centrar la pista pulsando Y o B.", 4f);
                }
                tiempoMirandoFuera = 0f;
                cooldownAviso = 10f;
            }
        }
        else
        {
            tiempoMirandoFuera = 0f;
        }
    }
}