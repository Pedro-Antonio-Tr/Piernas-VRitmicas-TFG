using System;
using System.IO;
using UnityEngine;

[Serializable]
public class DatosConfiguracion //Pongo los valores del default porque en las gafas no lee el json bien ¿?
{
    public float volumen = 1f;
    public int modoMando = 1;
    public int dificultad = 0;
    public float inclinacionPantallaX = 358.27069091796877f;

    public bool pantallaCurva = false;

    // Calibración Brazo IZQUIERDO
    public float centroX_L = 0.02702815644443035f;
    public float alcanceIzqX_L = -0.41913095116615298f;
    public float alcanceDerX_L = 0.3979409635066986f;

    // Calibración Brazo DERECHO
    public float centroX_R = 0.062207408249378207f;
    public float alcanceIzqX_R = -0.2471681386232376f;
    public float alcanceDerX_R = 0.6305392980575562f;

    public float tamanoMenu = 0.8f;
    public float distanciaPlana = 3.5854644775390627f;
    public float distanciaCurva = 4.248764514923096f;
}

public class GestorDatosUsuario : MonoBehaviour
{
    public static GestorDatosUsuario Instancia;

    [Header("Usuario Actual")]
    public string idUsuario = "Invitado";
    private string subRutaSesion = ""; // Para el timestamp si es invitado
    public DatosConfiguracion configActual = new DatosConfiguracion();

    public string RutaUsuario
    {
        get
        {
            string baseRuta = Path.Combine(Application.persistentDataPath, idUsuario);
            // Si es invitado, añadimos la subcarpeta de la sesión
            if (idUsuario == "Invitado" && !string.IsNullOrEmpty(subRutaSesion))
            {
                return Path.Combine(baseRuta, subRutaSesion);
            }
            return baseRuta;
        }
    }
    public string RutaTracking => Path.Combine(RutaUsuario, "Tracking");

    void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);

            CapturarIDDesdeIntent();

            if (idUsuario == "Invitado")
            {
                subRutaSesion = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            }

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
        // Solo intentamos esto en Android (Gafas), en el Editor fallaría
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
                        // Buscamos el extra "user"
                        using (AndroidJavaObject extras = intent.Call<AndroidJavaObject>("getExtras"))
                        {
                            if (extras != null)
                            {
                                string idCapturado = extras.Call<string>("getString", "user");
                                if (!string.IsNullOrEmpty(idCapturado))
                                {
                                    idUsuario = idCapturado;
                                    Debug.Log("ID de Usuario capturado con éxito: " + idUsuario);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("No se detectó parámetro de usuario en el Intent, usando Invitado. " + e.Message);
        }
    }

    void InicializarCarpetas()
    {
        if (!Directory.Exists(RutaUsuario))
        {
            Directory.CreateDirectory(RutaUsuario);
        }
        if (!Directory.Exists(RutaTracking))
        {
            Directory.CreateDirectory(RutaTracking);
        }
    }

    public void GuardarConfiguracion()
    {
        string json = JsonUtility.ToJson(configActual, true);
        File.WriteAllText(Path.Combine(RutaUsuario, "config.json"), json);
    }

    public void CargarConfiguracion()
    {
        string rutaUsuario = Path.Combine(RutaUsuario, "config.json");

        string rutaInvitado = Path.Combine(Application.persistentDataPath, "Invitado");
        string rutaDefaultPublica = Path.Combine(rutaInvitado, "default_config.json");

        if (File.Exists(rutaUsuario))
        {
            string json = File.ReadAllText(rutaUsuario);
            configActual = JsonUtility.FromJson<DatosConfiguracion>(json);
            Debug.Log("Configuración de usuario cargada.");
            return;
        }

        if (File.Exists(rutaDefaultPublica))
        {
            string json = File.ReadAllText(rutaDefaultPublica);
            configActual = JsonUtility.FromJson<DatosConfiguracion>(json);
            Debug.Log("Configuración default pública cargada.");
            return;
        }
        TextAsset defaultJson = Resources.Load<TextAsset>("default_config");
        if (defaultJson != null)
        {
            configActual = JsonUtility.FromJson<DatosConfiguracion>(defaultJson.text);

            if (!Directory.Exists(rutaInvitado)) Directory.CreateDirectory(rutaInvitado);
            File.WriteAllText(rutaDefaultPublica, defaultJson.text);

            Debug.Log("Configuración de fábrica extraída y guardada en carpeta Invitado para edición.");
        }
        else
        {
            configActual = new DatosConfiguracion();
            string jsonGenerado = JsonUtility.ToJson(configActual, true);

            if (!Directory.Exists(rutaInvitado)) Directory.CreateDirectory(rutaInvitado);
            File.WriteAllText(rutaDefaultPublica, jsonGenerado);

            Debug.LogWarning("No se encontró default_config en Resources. Creando una limpia en la carpeta Invitado.");
        }
    }

    public void GuardarPartidaRitmoCSV(string cancion, string dificultad, string resultado, float precision, float fatiga, float duracion)
    {
        string ruta = Path.Combine(RutaUsuario, "historial_ritmo.csv");
        bool existe = File.Exists(ruta);

        using (StreamWriter sw = new StreamWriter(ruta, true))
        {
            if (!existe)
            {
                sw.WriteLine("FechaHora;Cancion;Dificultad;Duracion(s);Resultado;Precision(%);IndiceFatiga");
            }
            sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss};{cancion};{dificultad};{duracion:F1};{resultado};{precision:F2};{fatiga:F2}");
        }
    }

    public int ObtenerRecordPorNivel(string nombreNivel)
    {
        string ruta = Path.Combine(RutaUsuario, "historial_partidas.csv");
        if (!File.Exists(ruta)) return 0;

        int recordMaximo = 0;

        try
        {
            string[] lineas = File.ReadAllLines(ruta);
            // Empezamos en 1 para saltar la cabecera
            for (int i = 1; i < lineas.Length; i++)
            {
                string[] columnas = lineas[i].Split(';');
                // Columna 1: Nivel, Columna 10: Puntuación
                if (columnas.Length > 10 && columnas[1] == nombreNivel)
                {
                    if (int.TryParse(columnas[10], out int puntos))
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