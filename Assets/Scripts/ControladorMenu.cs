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

    [Header("Catálogo de Niveles")]
    public DatosCancionRitmo[] catalogoCanciones;
    private int indiceCancionActual = 0;
    private AudioSource audioPreviewNivel;

    [Header("UI Selección de Niveles")]
    public TextMeshProUGUI textoNivelNombre;
    public TextMeshProUGUI textoNivelArtista;
    public TextMeshProUGUI textoNivelDuracion;
    public TextMeshProUGUI textoNivelRecord;

    [Header("UI Menú Pausa")]
    public TextMeshProUGUI textoPausaNombre;
    public TextMeshProUGUI textoPausaTiempo;
    public TextMeshProUGUI textoPausaPuntos;
    public TextMeshProUGUI textoPausaRecord;

    [Header("UI Bienvenida / Calibración")]
    public Button botonCalibrarBienvenida;

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

        audioPreviewNivel = gameObject.AddComponent<AudioSource>();
        audioPreviewNivel.loop = true;
        audioPreviewNivel.volume = 0.3f;
        audioPreviewNivel.playOnAwake = false;
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
        if (panelBienvenida.activeInHierarchy && botonCalibrarBienvenida != null)
        {
            bool gripApretado = OVRInput.Get(OVRInput.RawButton.RHandTrigger);
            botonCalibrarBienvenida.interactable = gripApretado;
        }

        if (OVRInput.GetDown(OVRInput.RawButton.X))
        {
            if (GestorRitmo.Instancia != null && GestorRitmo.Instancia.juegoEmpezado && !calibracionEnProceso && !GestorRitmo.Instancia.enCuentaAtras)
            {
                BotonUI_PausarJuego();
            }
        }

        if (panelMenu.activeSelf) ColocarMenuDelanteDeLaMirada();
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

        if (panelDestino != panelNiveles && audioPreviewNivel.isPlaying)
        {
            audioPreviewNivel.Stop();
        }
    }

    public void BotonUI_IrAAjustes()
    {
        AbrirPanel(panelAjustes);
    }

    public void BotonUI_VolverDesdeAjustes()
    {
        AbrirPanel(panelNiveles);
        ActualizarUINivelActual();
    }

    public void BotonUI_SiguienteNivel()
    {
        if (catalogoCanciones == null || catalogoCanciones.Length == 0) return;
        indiceCancionActual = (indiceCancionActual + 1) % catalogoCanciones.Length;
        ActualizarUINivelActual();
    }

    public void BotonUI_AnteriorNivel()
    {
        if (catalogoCanciones == null || catalogoCanciones.Length == 0) return;
        indiceCancionActual--;
        if (indiceCancionActual < 0) indiceCancionActual = catalogoCanciones.Length - 1;
        ActualizarUINivelActual();
    }

    private void ActualizarUINivelActual()
    {
        if (catalogoCanciones == null || catalogoCanciones.Length == 0) return;

        DatosCancionRitmo cancion = catalogoCanciones[indiceCancionActual];

        if (textoNivelNombre != null) textoNivelNombre.text = $"{indiceCancionActual}: {cancion.nombreCancion}"; //Empieza en 0 el índice para que sea ese el tutorial
        if (textoNivelArtista != null) textoNivelArtista.text = "Artista: " + cancion.artista;

        if (textoNivelDuracion != null)
        {
            float segs = cancion.archivoAudio != null ? cancion.archivoAudio.length : 0f;
            textoNivelDuracion.text = $"Duración: {Mathf.FloorToInt(segs / 60)}:{Mathf.FloorToInt(segs % 60):00}";
        }

        if (textoNivelRecord != null && GestorDatosUsuario.Instancia != null)
        {
            int record = GestorDatosUsuario.Instancia.ObtenerRecordPorNivel(cancion.nombreCancion);
            textoNivelRecord.text = $"Récord personal: {record} puntos";
        }

        if (cancion.archivoAudio != null)
        {
            audioPreviewNivel.clip = cancion.archivoAudio;
            audioPreviewNivel.Play();
        }
    }

    public void BotonUI_JugarNivelSeleccionado()
    {
        if (catalogoCanciones == null || catalogoCanciones.Length == 0) return;

        audioPreviewNivel.Stop(); 
        CerrarMenuYMostrarPista();

        GestorRitmo.Instancia.cancionActual = catalogoCanciones[indiceCancionActual];
        GestorRitmo.Instancia.EmpezarTutorial();
    }

    public void BotonUI_PausarJuego()
    {
        panelMenu.SetActive(true);
        if (contenedorJuego != null) contenedorJuego.gameObject.SetActive(false);
        ActualizarLaseres(true);

        if (GestorRitmo.Instancia.cancionActual != null)
        {
            if (textoPausaNombre != null) textoPausaNombre.text = GestorRitmo.Instancia.cancionActual.nombreCancion;

            if (textoPausaTiempo != null)
            {
                float tRestante = GestorRitmo.Instancia.ObtenerTiempoRestante();
                textoPausaTiempo.text = $"Faltan: {Mathf.FloorToInt(tRestante / 60)}:{Mathf.FloorToInt(tRestante % 60):00}";
            }

            if (textoPausaPuntos != null) textoPausaPuntos.text = $"Puntos actuales: {GestorRitmo.Instancia.ObtenerPuntuacionActual()}";

            if (textoPausaRecord != null && GestorDatosUsuario.Instancia != null)
            {
                int record = GestorDatosUsuario.Instancia.ObtenerRecordPorNivel(GestorRitmo.Instancia.cancionActual.nombreCancion);
                textoPausaRecord.text = $"Récord a batir: {record}";
            }
        }

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
        ActualizarUINivelActual();
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
        if (laserDerecho != null) laserDerecho.enabled = false; //Como el láser derecho se usa para la pierna, lo mantenemos desactivado en el menú para evitar confusiones
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

        yield return FaseContador("1/5: MANTÉN LAS PIERNAS EN REPOSO");
        detector.calibracion.reposo = detector.ObtenerDatosHardware();

        yield return FaseContador("2/5: INCLINA LAS PIERNAS A LA IZQUIERDA");
        detector.calibracion.izquierda = detector.ObtenerDatosHardware();

        yield return FaseContador("3/5: INCLINA LAS PIERNAS A LA DERECHA");
        detector.calibracion.derecha = detector.ObtenerDatosHardware();

        yield return FaseContador("4/5: ESTIRA LAS PIERNAS (ABAJO)");
        detector.calibracion.extendida = detector.ObtenerDatosHardware();

        yield return FaseContador("5/5: LEVANTA LAS RODILLAS (ARRIBA)");
        detector.calibracion.levantada = detector.ObtenerDatosHardware();

        textoInstrucciones.text = "¡CALIBRACIÓN GUARDADA!";
        textoCuentaAtras.text = "OK";
        yield return new WaitForSecondsRealtime(2f);

        calibracionEnProceso = false;

        if (NotificacionFlotanteVR.Instancia != null)
        {
            NotificacionFlotanteVR.Instancia.MostrarNotificacion("Si durante el juego sientes que la calibración no es adecuada, puedes volver a calibrar desde Ajustes.", 6f);
        }

        AbrirPanel(panelNiveles);
        ActualizarUINivelActual();
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