using System;
using System.IO;
using UnityEngine;

[Serializable]
public class DatosConfiguracion
{
    public float volumen = 1f;
    public DatosCalibracionPierna calibracionPierna = new DatosCalibracionPierna();
}

public class GestorDatosUsuario : MonoBehaviour
{
    public static GestorDatosUsuario Instancia;

    [Header("Usuario Actual")]
    public string idUsuario = "Invitado";
    private string subRutaSesion = "";
    public DatosConfiguracion configActual = new DatosConfiguracion();

    public string RutaUsuario
    {
        get
        {
            string baseRuta = Path.Combine(Application.persistentDataPath, idUsuario);
            if (idUsuario == "Invitado" && !string.IsNullOrEmpty(subRutaSesion))
            {
                return Path.Combine(baseRuta, subRutaSesion);
            }
            return baseRuta;
        }
    }

    public string RutaTracking => Path.Combine(RutaUsuario, "Tracking_Ritmo");

    void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);
            CapturarIDDesdeIntent();
            if (idUsuario == "Invitado") subRutaSesion = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            InicializarCarpetas();
            CargarConfiguracion();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CapturarIDDesdeIntent()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (currentActivity != null)
                {
                    AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
                    if (intent != null)
                    {
                        using (AndroidJavaObject extras = intent.Call<AndroidJavaObject>("getExtras"))
                        {
                            if (extras != null)
                            {
                                string idCapturado = extras.Call<string>("getString", "user");
                                if (!string.IsNullOrEmpty(idCapturado)) idUsuario = idCapturado;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("No se detectó parámetro de usuario, usando Invitado. " + e.Message);
        }
    }

    void InicializarCarpetas()
    {
        if (!Directory.Exists(RutaUsuario)) Directory.CreateDirectory(RutaUsuario);
        if (!Directory.Exists(RutaTracking)) Directory.CreateDirectory(RutaTracking);
    }

    public void GuardarConfiguracion()
    {
        string json = JsonUtility.ToJson(configActual, true);
        File.WriteAllText(Path.Combine(RutaUsuario, "config.json"), json);
    }

    public void CargarConfiguracion()
    {
        string rutaUsuario = Path.Combine(RutaUsuario, "config.json");

        if (File.Exists(rutaUsuario))
        {
            configActual = JsonUtility.FromJson<DatosConfiguracion>(File.ReadAllText(rutaUsuario));
        }
        else
        {
            configActual = new DatosConfiguracion();
            GuardarConfiguracion(); 
        }
    }


    public void GuardarPartidaRitmoCSV(string cancion, string dificultad, string resultado, int puntuacion, int rachaMax, float precision, float fatiga, float duracion)
    {
        string ruta = Path.Combine(RutaUsuario, "historial_ritmo.csv");
        bool existe = File.Exists(ruta);

        using (StreamWriter sw = new StreamWriter(ruta, true))
        {
            if (!existe)
            {
                sw.WriteLine("FechaHora;Cancion;Dificultad;Resultado;Puntuacion;RachaMaxima;Precision(%);IndiceFatiga;Duracion(s)");
            }
            sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss};{cancion};{dificultad};{resultado};{puntuacion};{rachaMax};{precision:F2};{fatiga:F2};{duracion:F1}");
        }
    }

    public int ObtenerRecordPorNivel(string nombreNivel)
    {
        string ruta = Path.Combine(RutaUsuario, "historial_ritmo.csv");
        if (!File.Exists(ruta)) return 0;

        int recordMaximo = 0;
        try
        {
            string[] lineas = File.ReadAllLines(ruta);
            for (int i = 1; i < lineas.Length; i++)
            {
                string[] columnas = lineas[i].Split(';');
                if (columnas.Length >= 5 && columnas[1] == nombreNivel)
                {
                    if (int.TryParse(columnas[4], out int puntos))
                    {
                        if (puntos > recordMaximo) recordMaximo = puntos;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error leyendo récords: " + e.Message);
        }

        return recordMaximo;
    }
}