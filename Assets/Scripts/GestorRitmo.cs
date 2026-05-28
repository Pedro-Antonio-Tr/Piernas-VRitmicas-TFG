using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class GestorRitmo : MonoBehaviour
{
    public static GestorRitmo Instancia;

    [Header("Tracking del Jugador")]
    public Transform headAnchor;

    [Header("Archivos de Canción")]
    [Tooltip("El archivo creado con tu herramienta para el Tutorial")]
    public DatosCancionRitmo cancionTutorial;
    private AudioSource reproductorMusica;

    [Header("Prefabs de las Notas")]
    public GameObject[] prefabsNotas;

    [Header("Configuración de la Pista")]
    public Transform contenedorPista;
    public float zSpawn = 25f;
    public float distanciaPista = 1.0f;
    public float alturaPista = 1.0f;
    public float alturaMinimaSuelo = 0.3f;
    public float umbralMirarArriba = 0.15f;

    [Header("UI Puntuación y Vida")]
    public TextMeshProUGUI textoPuntuacion;
    public TextMeshProUGUI textoMultiplicador;
    public TextMeshProUGUI textoRacha;
    public Slider barraVida;
    public Slider barraMultiplicador;
    public Image rellenoBarraVida;
    [Tooltip("El texto 3D grande que flota en medio de la pista para la cuenta atrás")]
    public TextMeshProUGUI textoCuentaAtrasPista;

    [Header("Movimiento y Escala")]
    public float velocidadBase = 4f;
    public float velocidadAcercamiento = 16f;
    public float zTransicion = 7f;
    public float escalaMinima = 0.1f;

    [Header("Ajustes de Puntuación")]
    public int puntosPerfecto = 100;
    public int puntosMedio = 50;
    public int notasParaSubirMultiplicador = 4;
    public bool bajarSoloUnNivelAlFallar = true;
    private int[] nivelesMultiplicador = { 1, 2, 4, 8 };

    [Header("Ajustes de Vida")]
    public float vidaMax = 100f;
    public float vidaInicial = 50f;
    public float vidaPierdeFallo = 10f;
    public Color colorVidaNormal = Color.green;
    public Color colorVidaMuerte = Color.red;
    public Color colorFallo = Color.red;

    [Header("Feedback Visual")]
    public PruebaDeteccionRitmo zonaIzquierda;
    public PruebaDeteccionRitmo zonaDerecha;
    public PruebaDeteccionRitmo zonaArriba;
    public PruebaDeteccionRitmo zonaAbajo;
    public PruebaDeteccionRitmo zonaReposo;
    public Light luzFondo;

    [Header("Estado")]
    public bool juegoEmpezado = false;
    public bool enCuentaAtras = false;
    private bool esModoTutorial = false;

    private Coroutine bucleJuego;
    private int puntuacionTotal = 0;
    private int rachaActual = 0;
    private int rachaMaxima = 0;
    private int indiceMultiplicadorActual = 0;
    private int progresoMultiplicador = 0;
    private float vidaActual;
    private float intensidadObjetivoLuz = 0f;

    void Awake()
    {
        Instancia = this;
        reproductorMusica = gameObject.AddComponent<AudioSource>();
        if (textoCuentaAtrasPista != null) textoCuentaAtrasPista.gameObject.SetActive(false);
    }

    void Start()
    {
        if (luzFondo != null) { luzFondo.intensity = 0f; luzFondo.shadows = LightShadows.None; }
    }

    void Update()
    {
        if (luzFondo != null)
        {
            luzFondo.intensity = Mathf.MoveTowards(luzFondo.intensity, intensidadObjetivoLuz, 5f * Time.deltaTime);
            if (luzFondo.intensity == intensidadObjetivoLuz) intensidadObjetivoLuz = 0f;
        }

        if (juegoEmpezado && esModoTutorial && !reproductorMusica.isPlaying && !enCuentaAtras && Time.timeScale > 0)
        {
            StartCoroutine(VictoriaTutorial());
        }
    }

    public void EmpezarPruebaAleatoria()
    {
        esModoTutorial = false;
        IniciarPartidaComun();
    }

    public void EmpezarTutorial()
    {
        if (cancionTutorial == null)
        {
            Debug.LogError("¡No has asignado el archivo DatosCancionRitmo en el Gestor!");
            return;
        }
        esModoTutorial = true;
        reproductorMusica.clip = cancionTutorial.archivoAudio;
        IniciarPartidaComun();
    }

    private void IniciarPartidaComun()
    {
        CentrarPista();
        InicializarEstadisticas();
        LimpiarPista();
        juegoEmpezado = true;
        ReanudarJuegoConCuentaAtras();
    }

    public void PausarJuego()
    {
        Time.timeScale = 0f;
        if (reproductorMusica.isPlaying) reproductorMusica.Pause();
    }

    public void ReanudarJuegoConCuentaAtras()
    {
        StartCoroutine(RutinaCuentaAtras());
    }

    public void ReiniciarNivelActual()
    {
        DetenerTodo();
        if (esModoTutorial) EmpezarTutorial();
        else EmpezarPruebaAleatoria();
    }

    public void DetenerTodo()
    {
        juegoEmpezado = false;
        Time.timeScale = 1f;
        if (bucleJuego != null) StopCoroutine(bucleJuego);
        reproductorMusica.Stop();
        LimpiarPista();
    }

    private void LimpiarPista()
    {
        foreach (NotaRitmo nota in FindObjectsOfType<NotaRitmo>()) Destroy(nota.gameObject);
        if (luzFondo != null) luzFondo.intensity = 0f;
    }

    private IEnumerator RutinaCuentaAtras()
    {
        enCuentaAtras = true;
        if (textoCuentaAtrasPista != null)
        {
            textoCuentaAtrasPista.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--)
            {
                textoCuentaAtrasPista.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }
            textoCuentaAtrasPista.text = "¡YA!";
            yield return new WaitForSecondsRealtime(0.3f);
            textoCuentaAtrasPista.gameObject.SetActive(false);
        }

        enCuentaAtras = false;
        Time.timeScale = 1f;

        if (esModoTutorial)
        {
            reproductorMusica.Play();
            bucleJuego = StartCoroutine(BucleLectorTutorial());
        }
        else
        {
            bucleJuego = StartCoroutine(BucleGeneradorAleatorio());
        }
    }

    private IEnumerator BucleGeneradorAleatorio()
    {
        while (juegoEmpezado)
        {
            InstanciarNotaFisica(ObtenerNotaAleatoria());
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator BucleLectorTutorial()
    {
        List<DatosCancionRitmo.NotaGuardada> notas = cancionTutorial.notasFacil;
        int indiceNota = 0;

        while (juegoEmpezado && indiceNota < notas.Count)
        {
            float tiempoViaje = (zSpawn - zTransicion) / velocidadAcercamiento + (zTransicion - 2f) / velocidadBase;
            float tiempoNacimiento = notas[indiceNota].tiempoAparicion - tiempoViaje;

            if (reproductorMusica.time >= tiempoNacimiento)
            {
                InstanciarNotaFisica(notas[indiceNota].tipoNota);
                indiceNota++;
            }
            yield return null;
        }
    }

    private void InstanciarNotaFisica(NotaRitmo.TipoNota tipo)
    {
        if (prefabsNotas == null || prefabsNotas.Length == 0) return;

        GameObject prefabElegido = null;
        foreach (GameObject p in prefabsNotas)
        {
            if (p.GetComponent<NotaRitmo>().tipoDeNota == tipo)
            {
                prefabElegido = p;
                break;
            }
        }
        if (prefabElegido == null) return;

        Vector3 posicionLocalSpawn = new Vector3(0f, 0f, zSpawn);
        switch (tipo)
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

    private NotaRitmo.TipoNota ObtenerNotaAleatoria()
    {
        return (NotaRitmo.TipoNota)Random.Range(0, 5);
    }


    private void InicializarEstadisticas()
    {
        puntuacionTotal = 0;
        rachaActual = 0;
        rachaMaxima = 0;
        indiceMultiplicadorActual = 0;
        progresoMultiplicador = 0;
        vidaActual = vidaInicial;

        if (rellenoBarraVida != null) rellenoBarraVida.color = colorVidaNormal;
        ActualizarUI();
    }

    public void ProcesarNota(NotaRitmo.TipoNota tipo, int calidad)
    {
        if (!juegoEmpezado) return;

        Color colorDestello = colorFallo;

        if (calidad > 0)
        {
            rachaActual++;
            if (rachaActual > rachaMaxima) rachaMaxima = rachaActual;

            int multi = nivelesMultiplicador[indiceMultiplicadorActual];

            if (calidad == 2)
            {
                puntuacionTotal += puntosPerfecto * multi;
                vidaActual = Mathf.Clamp(vidaActual + 5f, 0, vidaMax);
                colorDestello = Color.green;

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
                vidaActual = Mathf.Clamp(vidaActual + 2f, 0, vidaMax);
                colorDestello = Color.yellow;
            }
        }
        else
        {
            rachaActual = 0;
            progresoMultiplicador = 0;
            vidaActual -= vidaPierdeFallo;
            colorDestello = colorFallo;

            indiceMultiplicadorActual = bajarSoloUnNivelAlFallar ? Mathf.Max(0, indiceMultiplicadorActual - 1) : 0;

            if (vidaActual <= 0)
            {
                vidaActual = 0;
                DerrotaPorVida();
            }
        }

        DispararFeedbackZona(tipo, colorDestello);
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoPuntuacion != null) textoPuntuacion.text = puntuacionTotal.ToString("N0");
        if (textoRacha != null) textoRacha.text = rachaActual + " Combo";
        if (textoMultiplicador != null) textoMultiplicador.text = "x" + nivelesMultiplicador[indiceMultiplicadorActual];
        if (barraVida != null) barraVida.value = vidaActual / vidaMax;

        if (barraMultiplicador != null)
        {
            if (indiceMultiplicadorActual == nivelesMultiplicador.Length - 1) barraMultiplicador.value = 1f;
            else barraMultiplicador.value = (float)progresoMultiplicador / notasParaSubirMultiplicador;
        }
    }

    private void DerrotaPorVida()
    {
        if (rellenoBarraVida != null) rellenoBarraVida.color = colorVidaMuerte;
        DetenerTodo();
        ControladorMenu.Instancia.MostrarResultadosFinales(false, puntuacionTotal, rachaMaxima);
    }

    private IEnumerator VictoriaTutorial()
    {
        yield return new WaitForSeconds(2f);
        DetenerTodo();
        ControladorMenu.Instancia.MostrarResultadosFinales(true, puntuacionTotal, rachaMaxima);
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
            luzFondo.intensity = 8f;
            intensidadObjetivoLuz = 0f;
        }
    }

    public void CentrarPista()
    {
        if (headAnchor == null || contenedorPista == null) return;

        Vector3 headPos = headAnchor.position;
        Vector3 lookDirection = headAnchor.forward;

        if (lookDirection.y < umbralMirarArriba)
        {
            lookDirection = Vector3.ProjectOnPlane(lookDirection, Vector3.up).normalized;
            if (lookDirection == Vector3.zero) lookDirection = Vector3.forward;

            Vector3 posPista = headPos + (lookDirection * distanciaPista);
            posPista.y = Mathf.Max(alturaPista, alturaMinimaSuelo);

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
}