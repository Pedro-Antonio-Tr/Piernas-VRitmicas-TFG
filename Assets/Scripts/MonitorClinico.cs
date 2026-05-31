using System;
using System.IO;
using UnityEngine;

public class MonitorClinico : MonoBehaviour
{
    public static MonitorClinico Instancia;

    public enum NivelDificultad { Facil, Normal, Dificil }

    [Header("Ajustes de Partida")]
    public NivelDificultad dificultadActual = NivelDificultad.Facil;

    [Header("Referencias (Hardware)")]
    public Transform headAnchor; 
    public Transform mandoDerecho; 

    [Header("Métricas Clínicas Recopiladas")]
    public float indiceFatiga = 0f;

    public float maxExtensionDerX = 0f;
    public float maxExtensionIzqX = 0f;
    public float maxExtensionArribaY = 0f;
    public float maxExtensionAbajoY = 0f;

    [Header("Rendimiento Rítmico")]
    public int golpesPerfectos = 0;
    public int golpesMedios = 0;
    public int fallos = 0;

    [Header("Telemetría Cruda (Tracking)")]
    public float frecuenciaRegistro = 0.1f; 
    public float umbralMovimientoBrusco = 3.5f;

    private StreamWriter escritorTelemetria;
    private bool grabandoTelemetria = false;
    private float tiempoInicioSesionTelemetria;

    private Quaternion rotacionAnteriorDer;
    private string ultimoEventoRegistrado = "NORMAL";

    void Awake()
    {
        if (Instancia == null) Instancia = this;
    }

    void Start()
    {
        if (mandoDerecho != null) rotacionAnteriorDer = mandoDerecho.rotation;
    }

    void Update()
    {
        if (Time.timeScale == 0 || !grabandoTelemetria) return;

        MedirFatigaYRangoMovimiento();
    }

    void MedirFatigaYRangoMovimiento()
    {
        if (mandoDerecho != null)
        {
            float deltaRotDer = Quaternion.Angle(rotacionAnteriorDer, mandoDerecho.rotation);
            indiceFatiga += deltaRotDer;
            rotacionAnteriorDer = mandoDerecho.rotation;

            Vector3 posPierna = mandoDerecho.localPosition;

            if (posPierna.x > maxExtensionDerX) maxExtensionDerX = posPierna.x;
            if (posPierna.x < maxExtensionIzqX) maxExtensionIzqX = posPierna.x;
            if (posPierna.y > maxExtensionArribaY) maxExtensionArribaY = posPierna.y;
            if (posPierna.y < maxExtensionAbajoY) maxExtensionAbajoY = posPierna.y;
        }
    }

    public void RegistrarGolpe(int calidad)
    {
        if (calidad == 2) { golpesPerfectos++; ultimoEventoRegistrado = "HIT_PERFECTO"; }
        else if (calidad == 1) { golpesMedios++; ultimoEventoRegistrado = "HIT_TARDIO"; }
        else { fallos++; ultimoEventoRegistrado = "MISS"; }
    }

    public void IniciarTelemetria(string nombreNivel)
    {
        ReiniciarMetricas();
        string fechaHora = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string nombreArchivo = $"TelemetriaPierna_{GestorDatosUsuario.Instancia.idUsuario}_{nombreNivel}_{fechaHora}.csv";
        string ruta = Path.Combine(GestorDatosUsuario.Instancia.RutaTracking, nombreArchivo);

        try
        {
            escritorTelemetria = new StreamWriter(ruta, false);
            // Cabecera súper exhaustiva
            escritorTelemetria.WriteLine("Tiempo(s);Head_PosX;Head_PosY;Head_PosZ;Head_RotX;Head_RotY;Head_RotZ;Leg_PosX;Leg_PosY;Leg_PosZ;Leg_RotX;Leg_RotY;Leg_RotZ;Leg_Vel(m/s);Evento");

            tiempoInicioSesionTelemetria = Time.time;
            grabandoTelemetria = true;
            StartCoroutine(RutinaRegistroTelemetria());
            Debug.Log($"<color=cyan>Monitor Clínico:</color> Grabando telemetría en {nombreArchivo}");
        }
        catch (Exception e)
        {
            Debug.LogError("Error al crear archivo de telemetría: " + e.Message);
        }
    }

    public void DetenerTelemetriaYGuardarGlobal(string cancion, string resultado, int puntuacion, int rachaMax, float duracionCancion)
    {
        grabandoTelemetria = false;
        if (escritorTelemetria != null)
        {
            escritorTelemetria.Close();
            escritorTelemetria = null;
        }

        int totalNotas = golpesPerfectos + golpesMedios + fallos;
        float precision = 0f;
        if (totalNotas > 0)
        {
            float puntosObtenidos = (golpesPerfectos * 1f) + (golpesMedios * 0.5f);
            precision = (puntosObtenidos / totalNotas) * 100f;
        }

        GestorDatosUsuario.Instancia.GuardarPartidaRitmoCSV(
            cancion,
            dificultadActual.ToString(),
            resultado,
            puntuacion,
            rachaMax,
            precision,
            indiceFatiga,
            duracionCancion
        );

        Debug.Log($"ROM Registrado -> Max Arriba: {maxExtensionArribaY:F2}m | Max Abajo: {maxExtensionAbajoY:F2}m | Max Izq: {maxExtensionIzqX:F2}m | Max Der: {maxExtensionDerX:F2}m");
    }

    private void ReiniciarMetricas()
    {
        indiceFatiga = 0f;
        maxExtensionDerX = 0f; maxExtensionIzqX = 0f;
        maxExtensionArribaY = 0f; maxExtensionAbajoY = 0f;
        golpesPerfectos = 0; golpesMedios = 0; fallos = 0;
        ultimoEventoRegistrado = "INICIO";
    }

    private System.Collections.IEnumerator RutinaRegistroTelemetria()
    {
        while (grabandoTelemetria)
        {
            if (Time.timeScale > 0)
            {
                float t = Time.time - tiempoInicioSesionTelemetria;
                Vector3 hP = headAnchor != null ? headAnchor.localPosition : Vector3.zero;
                Vector3 hR = headAnchor != null ? headAnchor.eulerAngles : Vector3.zero;
                Vector3 lP = mandoDerecho != null ? mandoDerecho.localPosition : Vector3.zero;
                Vector3 lR = mandoDerecho != null ? mandoDerecho.eulerAngles : Vector3.zero;

                float velL = 0f;
                try { velL = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch).magnitude; } catch { }

                string eventoEscribir = ultimoEventoRegistrado;
                if (velL > umbralMovimientoBrusco) eventoEscribir = "ESPASMO_DETECTADO";

                string linea = $"{t:F2};{hP.x:F3};{hP.y:F3};{hP.z:F3};{hR.x:F2};{hR.y:F2};{hR.z:F2};{lP.x:F3};{lP.y:F3};{lP.z:F3};{lR.x:F2};{lR.y:F2};{lR.z:F2};{velL:F2};{eventoEscribir}";

                if (escritorTelemetria != null) escritorTelemetria.WriteLine(linea);

                ultimoEventoRegistrado = "NORMAL";
            }
            yield return new WaitForSeconds(frecuenciaRegistro);
        }
    }

    void OnDestroy()
    {
        grabandoTelemetria = false;
        if (escritorTelemetria != null) escritorTelemetria.Close();
    }
}