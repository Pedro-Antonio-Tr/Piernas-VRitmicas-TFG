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

    [Header("Sistema de Atención")]
    public float anguloTolerancia = 45f; // Grados que puede girar la cabeza sin que salte el aviso
    public float tiempoParaAviso = 3f; // Segundos seguidos que tiene que estar mirando fuera
    private float tiempoMirandoFuera = 0f;
    private float cooldownAviso = 0f;

    [Header("Comportamiento del Menú")]
    public float distanciaToleranciaMenu = 0.8f;
    public float distanciaSaltoMenu = 1.5f;
    public float velocidadSeguimientoMenu = 5f;
    private bool menuEnMovimiento = false;

    [Header("Ajustes de Sonido")]
    public Slider sliderVolumen;

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

        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            if (GestorRitmo.Instancia != null)
            {
                GestorRitmo.Instancia.CentrarPista();
            }
            tiempoMirandoFuera = 0f;
            cooldownAviso = 15f;
        }

        if (panelMenu.activeSelf) ColocarMenuDelanteDeLaMirada();

        ComprobarAtencionJugador();
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

        float distanciaAlObjetivo = Vector3.Distance(transform.position, targetPos);

        if (distanciaAlObjetivo > distanciaSaltoMenu)
        {
            transform.position = targetPos;
            Vector3 direccionHaciaCabeza = transform.position - headPos;
            if (direccionHaciaCabeza != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direccionHaciaCabeza);
            }
            menuEnMovimiento = false;
        }
        else if (distanciaAlObjetivo > distanciaToleranciaMenu || menuEnMovimiento)
        {
            menuEnMovimiento = true;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.unscaledDeltaTime * velocidadSeguimientoMenu);

            Vector3 direccionHaciaCabeza = transform.position - headPos;
            if (direccionHaciaCabeza != Vector3.zero)
            {
                Quaternion rotacionIdeal = Quaternion.LookRotation(direccionHaciaCabeza);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacionIdeal, Time.unscaledDeltaTime * velocidadSeguimientoMenu);
            }

            if (distanciaAlObjetivo < 0.05f)
            {
                menuEnMovimiento = false;
            }
        }
    }

    public void CambiarVolumenGeneral()
    {
        if (sliderVolumen != null)
        {
            AudioListener.volume = sliderVolumen.value;
        }

        if (GestorDatosUsuario.Instancia != null && sliderVolumen != null)
        {
            GestorDatosUsuario.Instancia.configActual.volumen = sliderVolumen.value;
            GestorDatosUsuario.Instancia.GuardarConfiguracion();
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

        if (GestorDatosUsuario.Instancia == null)
        {
            Debug.LogError("Error: GestorDatosUsuario no está en la escena.");
            yield break;
        }

        if (GestorDatosUsuario.Instancia.configActual == null)
        {
            GestorDatosUsuario.Instancia.configActual = new DatosConfiguracion();
        }

        if (GestorDatosUsuario.Instancia.configActual.calibracionPierna == null)
        {
            GestorDatosUsuario.Instancia.configActual.calibracionPierna = new DatosCalibracionPierna();
        }
        DatosCalibracionPierna calibracion = GestorDatosUsuario.Instancia.configActual.calibracionPierna;
        DetectorPiernaVR detector = DetectorPiernaVR.Instancia;

        if (detector == null)
        {
            Debug.LogError("Error: DetectorPiernaVR no está en la escena.");
            yield break;
        }

        yield return FaseContador("1/5: MANTÉN LAS PIERNAS EN REPOSO");
        calibracion.reposo = detector.ObtenerDatosHardware();

        yield return FaseContador("2/5: INCLINA LAS PIERNAS A LA IZQUIERDA");
        calibracion.izquierda = detector.ObtenerDatosHardware();

        yield return FaseContador("3/5: INCLINA LAS PIERNAS A LA DERECHA");
        calibracion.derecha = detector.ObtenerDatosHardware();

        yield return FaseContador("4/5: ESTIRA LAS PIERNAS (ABAJO)");
        calibracion.extendida = detector.ObtenerDatosHardware();

        yield return FaseContador("5/5: LEVANTA LAS RODILLAS (ARRIBA)");
        calibracion.levantada = detector.ObtenerDatosHardware();

        GestorDatosUsuario.Instancia.GuardarConfiguracion();

        detector.calibracion = calibracion;

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

    void ComprobarAtencionJugador()
    {
        if (GestorRitmo.Instancia == null || (!GestorRitmo.Instancia.juegoEmpezado && !GestorRitmo.Instancia.enCuentaAtras))
            return;

        if (contenedorJuego == null || !contenedorJuego.gameObject.activeSelf)
            return;

        if (cooldownAviso > 0)
        {
            cooldownAviso -= Time.deltaTime;
        }

        Vector3 direccionHaciaPantalla = (contenedorJuego.position - headAnchor.position).normalized;

        float anguloDesvio = Vector3.Angle(headAnchor.forward, direccionHaciaPantalla);

        if (anguloDesvio > anguloTolerancia)
        {
            tiempoMirandoFuera += Time.deltaTime;

            if (tiempoMirandoFuera >= tiempoParaAviso && cooldownAviso <= 0)
            {
                if (NotificacionFlotanteVR.Instancia != null)
                {
                    NotificacionFlotanteVR.Instancia.MostrarNotificacion("Puedes centrar la pantalla pulsando Y.", 4f);
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