using System;
using System.IO;
using UnityEngine;

public class MonitorClinico : MonoBehaviour
{
    public static MonitorClinico Instancia;

    public enum ModoControl { Izquierdo, Derecho, Ambos }
    public enum NivelDificultad { Facil, Normal, Dificil }

    [Header("Ajustes de Dificultad")]
    public NivelDificultad dificultadActual = NivelDificultad.Facil;

    [Header("Configuración Actual")]
    public ModoControl modoActual = ModoControl.Derecho;

    [Header("Referencias (Trackers)")]
    public Transform mandoIzquierdo;
    public Transform mandoDerecho;

    [Header("Métricas Recopiladas")]
    public float tiempoMandoIzquierdo = 0f;
    public float tiempoMandoDerecho = 0f;
    public float tiempoAmbosMandos = 0f;
    public float indiceFatiga = 0f; // Acumulación de micro-temblores

    [Header("Registro de golpes")]
    public int golpesIzquierda = 0;
    public int golpesDerecha = 0;

    [Header("Telemetría (Tracking Raw)")]
    public Transform headAnchor;
    public float frecuenciaRegistro = 0.1f;
    public float umbralMovimientoBrusco = 3.0f;

    private StreamWriter escritorTelemetria;
    private bool grabandoTelemetria = false;
    private float tiempoInicioSesionTelemetria;

    // Variables internas para fatiga
    private Quaternion rotacionAnteriorIzq;
    private Quaternion rotacionAnteriorDer;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
    }

    void Start()
    {
        if (mandoIzquierdo != null) rotacionAnteriorIzq = mandoIzquierdo.rotation;
        if (mandoDerecho != null) rotacionAnteriorDer = mandoDerecho.rotation;
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        RegistrarTiempoUso();
        MedirFatiga();
    }

    void RegistrarTiempoUso()
    {
        switch (modoActual)
        {
            case ModoControl.Izquierdo: tiempoMandoIzquierdo += Time.deltaTime; break;
            case ModoControl.Derecho: tiempoMandoDerecho += Time.deltaTime; break;
            case ModoControl.Ambos: tiempoAmbosMandos += Time.deltaTime; break;
        }
    }

    void MedirFatiga()
    {
        if (modoActual == ModoControl.Izquierdo || modoActual == ModoControl.Ambos)
        {
            if (mandoIzquierdo != null)
            {
                float deltaRotIzq = Quaternion.Angle(rotacionAnteriorIzq, mandoIzquierdo.rotation);
                indiceFatiga += deltaRotIzq;
                rotacionAnteriorIzq = mandoIzquierdo.rotation;
            }
        }

        if (modoActual == ModoControl.Derecho || modoActual == ModoControl.Ambos)
        {
            if (mandoDerecho != null)
            {
                float deltaRotDer = Quaternion.Angle(rotacionAnteriorDer, mandoDerecho.rotation);
                indiceFatiga += deltaRotDer;
                rotacionAnteriorDer = mandoDerecho.rotation;
            }
        }
    }

    public void GuardarDatosCSV()
    {
        string fechaHora = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string nombreArchivo = $"Sesion_{fechaHora}.csv";
        string ruta = Path.Combine(Application.persistentDataPath, nombreArchivo);

        try
        {
            using (StreamWriter writer = new StreamWriter(ruta, false))
            {
                writer.WriteLine("Fecha,Modo_Mando_Derecho,Modo_Mando_Izquierdo,Modo_Ambos_Mandos,Dificultad,Indice_Fatiga");
                writer.WriteLine($"{fechaHora},{tiempoMandoDerecho:F2},{tiempoMandoIzquierdo:F2},{tiempoAmbosMandos:F2},{dificultadActual},{indiceFatiga:F2}");
            }
            Debug.Log("¡CSV Guardado con éxito en: " + ruta + "!");
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar el CSV: " + e.Message);
        }
    }

    public void ReiniciarContadoresLateralidad()
    {
        golpesIzquierda = 0;
        golpesDerecha = 0;
    }

    public void IniciarTelemetria(string nombreNivel)
    {
        string fechaHora = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string nombreArchivo = $"Telemetria_{GestorDatosUsuario.Instancia.idUsuario}_{nombreNivel}_{fechaHora}.csv";
        string ruta = Path.Combine(GestorDatosUsuario.Instancia.RutaTracking, nombreArchivo);

        try
        {
            escritorTelemetria = new StreamWriter(ruta, false);
            escritorTelemetria.WriteLine("Tiempo(s);Head_RotX;Head_RotY;Head_RotZ;L_PosX;L_PosY;L_PosZ;L_Vel(m/s);R_PosX;R_PosY;R_PosZ;R_Vel(m/s);Evento");

            tiempoInicioSesionTelemetria = Time.time;
            grabandoTelemetria = true;
            StartCoroutine(RutinaRegistroTelemetria());
        }
        catch (Exception e)
        {
            Debug.LogError("Error al crear archivo de telemetría: " + e.Message);
        }
    }

    public void DetenerTelemetria()
    {
        grabandoTelemetria = false;
        if (escritorTelemetria != null)
        {
            escritorTelemetria.Close();
            escritorTelemetria = null;
        }
    }

    private System.Collections.IEnumerator RutinaRegistroTelemetria()
    {
        while (grabandoTelemetria)
        {
            if (Time.timeScale > 0)
            {
                float t = Time.time - tiempoInicioSesionTelemetria;
                Vector3 hR = headAnchor != null ? headAnchor.eulerAngles : Vector3.zero;
                Vector3 lP = mandoIzquierdo != null ? mandoIzquierdo.localPosition : Vector3.zero;
                Vector3 rP = mandoDerecho != null ? mandoDerecho.localPosition : Vector3.zero;

                float velL = 0f; float velR = 0f;
                try
                {
                    velL = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch).magnitude;
                    velR = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch).magnitude;
                }
                catch { }

                string evento = "NORMAL";
                if (velL > umbralMovimientoBrusco) evento = "MOVIMIENTO_BRUSCO_IZQ";
                if (velR > umbralMovimientoBrusco) evento = "MOVIMIENTO_BRUSCO_DER";

                string linea = $"{t:F2};{hR.x:F2};{hR.y:F2};{hR.z:F2};{lP.x:F2};{lP.y:F2};{lP.z:F2};{velL:F2};{rP.x:F2};{rP.y:F2};{rP.z:F2};{velR:F2};{evento}";

                if (escritorTelemetria != null)
                {
                    escritorTelemetria.WriteLine(linea);
                }
            }
            yield return new WaitForSeconds(frecuenciaRegistro);
        }
    }

    void OnDestroy()
    {
        DetenerTelemetria();
    }
}