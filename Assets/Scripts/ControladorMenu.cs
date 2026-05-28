using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControladorMenu : MonoBehaviour
{
    public static ControladorMenu Instancia;

    [Header("Configuración Base")]
    public GameObject panelMenu;
    public Transform headAnchor;
    public Transform contenedorJuego;
    public float distanciaPantalla = 3.5f;

    [Header("Punteros Láser")]
    public PunteroLaserVR laserIzquierdo;
    public PunteroLaserVR laserDerecho;
    public UnityEngine.EventSystems.OVRInputModule inputModule;

    [Header("Paneles de la Interfaz")]
    public GameObject panelBienvenida;
    public GameObject panelNiveles;
    public GameObject panelPausa;
    public GameObject panelAjustes;
    public GameObject panelResultados;

    [Header("Textos Panel Resultados")]
    public TextMeshProUGUI textoTituloResultados;
    public TextMeshProUGUI textoStatsResultados;

    [Header("Calibración (Piernas)")]
    public GameObject panelCuentaAtras;
    public TextMeshProUGUI textoCuentaAtras;
    public TextMeshProUGUI textoInstrucciones;

    [Header("Sonidos de Interfaz")]
    public AudioClip sonidoBoton;
    private AudioSource audioSourceMenu;

    public bool calibracionEnProceso = false;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        audioSourceMenu = gameObject.AddComponent<AudioSource>();
        audioSourceMenu.playOnAwake = false;
        audioSourceMenu.ignoreListenerPause = true;

        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(false);
        panelMenu.SetActive(true);
        AbrirPanel(panelBienvenida);
        ColocarMenuDelanteDeLaMirada();
        ActualizarLaseres(true);
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.X))
        {
            if (GestorRitmo.Instancia != null && GestorRitmo.Instancia.juegoEmpezado && !calibracionEnProceso && !GestorRitmo.Instancia.enCuentaAtras)
            {
                BotonUI_PausarJuego();
            }
        }

        if (panelMenu.activeSelf)
        {
            ColocarMenuDelanteDeLaMirada();
        }
    }

    public void ReproducirSonidoClic()
    {
        if (sonidoBoton != null && audioSourceMenu != null) audioSourceMenu.PlayOneShot(sonidoBoton);
    }

    public void AbrirPanel(GameObject panelDestino)
    {
        panelCuentaAtras.SetActive(false);
        panelBienvenida.SetActive(false);
        panelNiveles.SetActive(false);
        panelPausa.SetActive(false);
        panelAjustes.SetActive(false);
        panelResultados.SetActive(false);

        panelDestino.SetActive(true);
    }

    public void BotonUI_AvanzarDesdeBienvenida()
    {
        AbrirPanel(panelNiveles);
    }

    public void BotonUI_IrAAjustes()
    {
        AbrirPanel(panelAjustes);
    }

    public void BotonUI_VolverDesdeAjustes()
    {
        AbrirPanel(panelNiveles);
    }

    public void BotonUI_JugarPruebaAleatoria()
    {
        CerrarMenuYMostrarPista();
        GestorRitmo.Instancia.EmpezarPruebaAleatoria();
    }

    public void BotonUI_JugarTutorial()
    {
        CerrarMenuYMostrarPista();
        GestorRitmo.Instancia.EmpezarTutorial();
    }

    public void BotonUI_PausarJuego()
    {
        panelMenu.SetActive(true);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(false);

        ActualizarLaseres(true);
        AbrirPanel(panelPausa);
        ColocarMenuDelanteDeLaMirada();

        GestorRitmo.Instancia.PausarJuego();
    }

    public void BotonUI_ReanudarJuego()
    {
        CerrarMenuYMostrarPista();
        GestorRitmo.Instancia.ReanudarJuegoConCuentaAtras();
    }

    public void BotonUI_ReiniciarNivel()
    {
        CerrarMenuYMostrarPista();
        GestorRitmo.Instancia.ReiniciarNivelActual();
    }

    public void BotonUI_VolverAlMenuPrincipal()
    {
        GestorRitmo.Instancia.DetenerTodo();
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(false);
        ActualizarLaseres(true);
        AbrirPanel(panelNiveles);
    }

    public void MostrarResultadosFinales(bool esVictoria, int puntuacion, int maxRacha)
    {
        panelMenu.SetActive(true);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(false);
        ActualizarLaseres(true);
        ColocarMenuDelanteDeLaMirada();

        textoTituloResultados.text = esVictoria ? "<color=green>¡NIVEL COMPLETADO!</color>" : "<color=red>¡DERROTA!</color>";
        textoStatsResultados.text = $"Puntuación Total: {puntuacion}\nRacha Máxima: {maxRacha} Combo";

        AbrirPanel(panelResultados);
    }

    private void CerrarMenuYMostrarPista()
    {
        panelMenu.SetActive(false);
        ActualizarLaseres(false);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(true);
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

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.unscaledDeltaTime * 5f);

        Vector3 direccionHaciaCabeza = transform.position - headPos;
        if (direccionHaciaCabeza != Vector3.zero)
        {
            Quaternion rotacionIdeal = Quaternion.LookRotation(direccionHaciaCabeza);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionIdeal, Time.unscaledDeltaTime * 5f);
        }
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

        if (laserIzquierdo != null) laserIzquierdo.enabled = true;
        if (laserDerecho != null) laserDerecho.enabled = true;
        if (inputModule != null && laserDerecho != null) inputModule.rayTransform = laserDerecho.transform;
    }

    public void IniciarCalibracionPierna()
    {
        if (calibracionEnProceso) return;
        StartCoroutine(RutinaCalibrarPierna());
    }

    private System.Collections.IEnumerator RutinaCalibrarPierna()
    {
        calibracionEnProceso = true;
        AbrirPanel(panelCuentaAtras);
        ActualizarLaseres(false);

        DetectorPiernaVR detector = DetectorPiernaVR.Instancia;

        yield return FaseContador("1/5: MANTÉN LA PIERNA EN REPOSO");
        detector.calibracion.reposo = detector.ObtenerDatosHardware();

        yield return FaseContador("2/5: INCLINA LA PIERNA A LA IZQUIERDA");
        detector.calibracion.izquierda = detector.ObtenerDatosHardware();

        yield return FaseContador("3/5: INCLINA LA PIERNA A LA DERECHA");
        detector.calibracion.derecha = detector.ObtenerDatosHardware();

        yield return FaseContador("4/5: ESTIRA LA PIERNA (ABAJO)");
        detector.calibracion.extendida = detector.ObtenerDatosHardware();

        yield return FaseContador("5/5: LEVANTA LA RODILLA (ARRIBA)");
        detector.calibracion.levantada = detector.ObtenerDatosHardware();

        textoInstrucciones.text = "¡CALIBRACIÓN GUARDADA!";
        textoCuentaAtras.text = "OK";
        yield return new WaitForSecondsRealtime(2f);

        panelCuentaAtras.SetActive(false);
        calibracionEnProceso = false;
        AbrirPanel(panelAjustes);
        ActualizarLaseres(true);
    }

    private System.Collections.IEnumerator FaseContador(string instruccion)
    {
        textoInstrucciones.text = instruccion;
        for (int i = 5; i > 0; i--)
        {
            textoCuentaAtras.text = i.ToString();
            ReproducirSonidoClic();
            yield return new WaitForSecondsRealtime(1f);
        }
        textoCuentaAtras.text = "OK";
        yield return new WaitForSecondsRealtime(0.5f);
    }

    public void BotonUI_CentrarVista()
    {
        if(GestorRitmo.Instancia != null)
        {
            GestorRitmo.Instancia.CentrarPista();
        }
    }
}