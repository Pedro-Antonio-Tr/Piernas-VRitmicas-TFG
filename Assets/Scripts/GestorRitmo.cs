using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GestorRitmo : MonoBehaviour
{
    public static GestorRitmo Instancia;

    [Header("Tracking del Jugador")]
    public Transform headAnchor;

    [Header("Prefabs de las Notas")]
    public GameObject[] prefabsNotas;

    [Header("Configuración de la Pista")]
    public Transform contenedorPista;
    public float zSpawn = 25f;
    public float distanciaPista = 1.0f;
    [Tooltip("Altura base si el jugador mira recto o hacia abajo")]
    public float alturaPista = 1.0f;
    [Tooltip("Límite mínimo en el eje Y para que el Canvas y la pista no atraviesen el suelo físico")]
    public float alturaMinimaSuelo = 0.3f;
    [Tooltip("Cuánto hay que mirar hacia arriba (0.0 a 1.0) para que la pista empiece a inclinarse")]
    public float umbralMirarArriba = 0.15f;

    [Header("Movimiento y Escala")]
    public float velocidadBase = 4f;
    public float velocidadAcercamiento = 16f;
    public float zTransicion = 7f;
    public float escalaMinima = 0.1f;

    [Header("Ajustes de Puntuación y Racha")]
    public int puntosPerfecto = 100;
    public int puntosMedio = 50;
    public int notasParaSubirMultiplicador = 4;
    public bool bajarSoloUnNivelAlFallar = true;
    private int[] nivelesMultiplicador = { 1, 2, 4, 8 };

    [Header("Ajustes de Vida")]
    public float vidaMax = 100f;
    public float vidaInicial = 50f;
    public float vidaGanaPerfecto = 5f;
    public float vidaGanaMedio = 2f;
    public float vidaPierdeFallo = 10f;
    public Color colorVidaNormal = Color.green;
    public Color colorVidaMuerte = Color.red;

    [Header("UI Puntuación y Vida")]
    public TextMeshProUGUI textoPuntuacion;
    public TextMeshProUGUI textoMultiplicador;
    public TextMeshProUGUI textoRacha;
    public Slider barraVida;
    public Slider barraMultiplicador;
    public Image rellenoBarraVida;

    [Header("Feedback Visual en Zonas")]
    public Color colorAcierto = Color.green;
    public Color colorMedio = Color.yellow;
    public Color colorFallo = Color.red;
    public PruebaDeteccionRitmo zonaIzquierda;
    public PruebaDeteccionRitmo zonaDerecha;
    public PruebaDeteccionRitmo zonaArriba;
    public PruebaDeteccionRitmo zonaAbajo;
    public PruebaDeteccionRitmo zonaReposo;

    [Header("Sistema Volumétrico (Foco de Fondo)")]
    public Light luzFondo;
    public float intensidadMaximaLuz = 8f;
    public float velocidadDesvanecimientoLuz = 5f;

    [Header("Estado")]
    public bool modoPruebaActivo = false;
    public bool juegoEmpezado = false;
    public bool enCuentaAtras = false;

    private Coroutine bucleNotas;
    private float intensidadObjetivoLuz = 0f;
    private int puntuacionTotal = 0;
    private int rachaActual = 0;
    private int indiceMultiplicadorActual = 0;
    private int progresoMultiplicador = 0;
    private float vidaActual;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        if (luzFondo != null) { luzFondo.intensity = 0f; luzFondo.shadows = LightShadows.None; }
        InicializarEstadisticas();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            if (ControladorMenu.Instancia != null && !ControladorMenu.Instancia.calibracionEnProceso)
            {
                ToggleModoPrueba();
            }
        }

        if (luzFondo != null)
        {
            luzFondo.intensity = Mathf.MoveTowards(luzFondo.intensity, intensidadObjetivoLuz, velocidadDesvanecimientoLuz * Time.deltaTime);
            if (luzFondo.intensity == intensidadObjetivoLuz) intensidadObjetivoLuz = 0f;
        }
    }

    private void ToggleModoPrueba()
    {
        modoPruebaActivo = !modoPruebaActivo;

        if (modoPruebaActivo)
        {
            CentrarPista();
            InicializarEstadisticas();
            bucleNotas = StartCoroutine(BucleGeneracionNotas());
        }
        else
        {
            DetenerPrueba();
        }
    }

    private void DetenerPrueba()
    {
        modoPruebaActivo = false;
        if (bucleNotas != null) StopCoroutine(bucleNotas);
        foreach (NotaRitmo nota in FindObjectsOfType<NotaRitmo>()) Destroy(nota.gameObject);
        if (luzFondo != null) luzFondo.intensity = 0f;
    }

    private void InicializarEstadisticas()
    {
        puntuacionTotal = 0;
        rachaActual = 0;
        indiceMultiplicadorActual = 0;
        progresoMultiplicador = 0;
        vidaActual = vidaInicial;

        if (rellenoBarraVida != null) rellenoBarraVida.color = colorVidaNormal;
        ActualizarUI();
    }

    public void ProcesarNota(NotaRitmo.TipoNota tipo, int calidad)
    {
        if (!modoPruebaActivo) return;

        Color colorDestello = colorFallo;

        if (calidad > 0)
        {
            rachaActual++;
            int multi = nivelesMultiplicador[indiceMultiplicadorActual];

            if (calidad == 2)
            {
                puntuacionTotal += puntosPerfecto * multi;
                vidaActual = Mathf.Clamp(vidaActual + vidaGanaPerfecto, 0, vidaMax);
                colorDestello = colorAcierto;

                if (indiceMultiplicadorActual < nivelesMultiplicador.Length - 1)
                {
                    progresoMultiplicador++;
                    if (progresoMultiplicador >= notasParaSubirMultiplicador)
                    {
                        progresoMultiplicador = 0;
                        indiceMultiplicadorActual++;
                    }
                }
            }
            else
            {
                puntuacionTotal += puntosMedio * multi;
                vidaActual = Mathf.Clamp(vidaActual + vidaGanaMedio, 0, vidaMax);
                colorDestello = colorMedio;
            }
        }
        else
        {
            rachaActual = 0;
            progresoMultiplicador = 0;
            vidaActual -= vidaPierdeFallo;
            colorDestello = colorFallo;

            if (bajarSoloUnNivelAlFallar)
                indiceMultiplicadorActual = Mathf.Max(0, indiceMultiplicadorActual - 1);
            else
                indiceMultiplicadorActual = 0;

            if (vidaActual <= 0)
            {
                vidaActual = 0;
                MuertePorVida();
            }
        }

        DispararFeedbackZona(tipo, colorDestello);
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoPuntuacion != null) textoPuntuacion.text = puntuacionTotal.ToString("N0");
        if (textoRacha != null) textoRacha.text = rachaActual + " Combo";

        if (textoMultiplicador != null)
            textoMultiplicador.text = "x" + nivelesMultiplicador[indiceMultiplicadorActual];

        if (barraVida != null) barraVida.value = vidaActual / vidaMax;

        if (barraMultiplicador != null)
        {
            if (indiceMultiplicadorActual == nivelesMultiplicador.Length - 1)
                barraMultiplicador.value = 1f;
            else
                barraMultiplicador.value = (float)progresoMultiplicador / notasParaSubirMultiplicador;
        }
    }

    private void MuertePorVida()
    {
        Debug.Log("<color=red>¡VIDA AGOTADA! Fin de la prueba.</color>");
        if (rellenoBarraVida != null) rellenoBarraVida.color = colorVidaMuerte;
        DetenerPrueba();
    }

    public void DispararFeedbackZona(NotaRitmo.TipoNota tipo, Color colorFlash)
    {
        switch (tipo)
        {
            case NotaRitmo.TipoNota.Izquierda: if (zonaIzquierda != null) zonaIzquierda.ActivarDestello(colorFlash); break;
            case NotaRitmo.TipoNota.Derecha: if (zonaDerecha != null) zonaDerecha.ActivarDestello(colorFlash); break;
            case NotaRitmo.TipoNota.Arriba: if (zonaArriba != null) zonaArriba.ActivarDestello(colorFlash); break;
            case NotaRitmo.TipoNota.Abajo: if (zonaAbajo != null) zonaAbajo.ActivarDestello(colorFlash); break;
            case NotaRitmo.TipoNota.Reposo: if (zonaReposo != null) zonaReposo.ActivarDestello(colorFlash); break;
        }

        if (luzFondo != null)
        {
            luzFondo.color = colorFlash;
            luzFondo.intensity = intensidadMaximaLuz;
            intensidadObjetivoLuz = 0f;
        }
    }

    private void CentrarPista()
    {
        if (headAnchor == null || contenedorPista == null) return;

        Vector3 headPos = headAnchor.position;
        Vector3 lookDirection = headAnchor.forward;

        if (lookDirection.y < umbralMirarArriba)
        {
            lookDirection = Vector3.ProjectOnPlane(lookDirection, Vector3.up).normalized;
            if (lookDirection == Vector3.zero) lookDirection = Vector3.forward;

            Vector3 posPista = headPos + (lookDirection * distanciaPista);
            posPista.y = alturaPista;

            posPista.y = Mathf.Max(posPista.y, alturaMinimaSuelo);

            contenedorPista.position = posPista;
            contenedorPista.rotation = Quaternion.LookRotation(lookDirection);
        }
        else
        {
            Vector3 posPista = headPos + (lookDirection * distanciaPista);

            posPista.y = Mathf.Max(posPista.y, alturaMinimaSuelo);

            contenedorPista.position = posPista;

            contenedorPista.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    private IEnumerator BucleGeneracionNotas()
    {
        yield return new WaitForSeconds(1f);
        while (modoPruebaActivo)
        {
            SpawnNotaAleatoria();
            yield return new WaitForSeconds(2f);
        }
    }

    private void SpawnNotaAleatoria()
    {
        if (prefabsNotas == null || prefabsNotas.Length == 0 || contenedorPista == null) return;
        int indiceAleatorio = Random.Range(0, prefabsNotas.Length);
        GameObject prefabElegido = prefabsNotas[indiceAleatorio];

        NotaRitmo datosNota = prefabElegido.GetComponent<NotaRitmo>();
        if (datosNota == null) return;

        Vector3 posicionLocalSpawn = new Vector3(0f, 0f, zSpawn);
        switch (datosNota.tipoDeNota)
        {
            case NotaRitmo.TipoNota.Izquierda: posicionLocalSpawn.x = -0.7f; break;
            case NotaRitmo.TipoNota.Derecha: posicionLocalSpawn.x = 0.7f; break;
            case NotaRitmo.TipoNota.Arriba: posicionLocalSpawn.y = 0.7f; break;
            case NotaRitmo.TipoNota.Abajo: posicionLocalSpawn.y = -0.7f; break;
            case NotaRitmo.TipoNota.Reposo: break;
        }

        GameObject nuevaNota = Instantiate(prefabElegido, contenedorPista);
        nuevaNota.transform.localPosition = posicionLocalSpawn;
    }

    public void EmpezarPartidaDesdeMenu() { }
    public void ReiniciarNivelActual() { }
    public void VolverAlMenuPrincipal() { }
    public void AlternarPausa(bool pausa) { }
}