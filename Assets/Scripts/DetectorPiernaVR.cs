using UnityEngine;
using System.IO;

[System.Serializable]
public class PuntoCalibracion
{
    public Vector3 posicion;
    public Quaternion rotacion;

    public PuntoCalibracion(float px, float py, float pz, float rx, float ry, float rz, float rw)
    {
        posicion = new Vector3(px, py, pz);
        rotacion = new Quaternion(rx, ry, rz, rw);
    }

    public PuntoCalibracion() { }
}

[System.Serializable]
public class DatosCalibracionPierna
{
    // El orden de los números es: (Posicion X, Y, Z,  Rotacion X, Y, Z, W)

    public PuntoCalibracion reposo = new PuntoCalibracion(
        0.05609453469514847f, 0.6282438039779663f, 0.052570682018995288f, 0.6021556854248047f, -0.5007703304290772f, 0.5359848737716675f, -0.3152106702327728f);

    public PuntoCalibracion izquierda = new PuntoCalibracion(
        -0.041846226900815967f, 0.6106699109077454f, 0.004304872825741768f, 0.6760044693946838f, -0.2835719585418701f, 0.5125647783279419f, -0.44708189368247988f);

    public PuntoCalibracion derecha = new PuntoCalibracion(
        0.15625353157520295f, 0.5934007167816162f, 0.10290711373090744f, 0.4704399108886719f, -0.7299387454986572f, 0.4393942654132843f, -0.22980061173439027f);

    public PuntoCalibracion extendida = new PuntoCalibracion(
        0.03641977906227112f, 0.5495352149009705f, 0.13157197833061219f, -0.5198755264282227f, -0.6096248030662537f, 0.3485104739665985f, -0.4864436388015747f);

    public PuntoCalibracion levantada = new PuntoCalibracion(
        0.11424533277750015f, 0.6807211637496948f, -0.048261236399412158f, 0.6188517212867737f, -0.44458454847335818f, 0.6421487927436829f, -0.08373743295669556f);
}

public class DetectorPiernaVR : MonoBehaviour
{
    public static DetectorPiernaVR Instancia;

    [Header("Hardware")]
    public OVRInput.Controller mandoAUsar = OVRInput.Controller.RTouch;
    public bool mandoDetectado = false;

    [Header("Referencias Visuales")]
    public Transform cursorDeteccion;

    [Tooltip("Arrastra aquí el SpriteRenderer de tu nuevo cuadrado hueco")]
    public SpriteRenderer spriteCursor; 

    public Color colorActivo = Color.cyan;
    public Color colorInactivo = Color.gray;

    [Header("Ajustes de Movimiento Libre")]
    public float rangoVisualMenu = 0.7f; 
    public float suavizado = 15f;
    private Vector3 posicionLocalObjetivo;

    [Header("Datos de Calibración")]
    public DatosCalibracionPierna calibracion = new DatosCalibracionPierna();

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        CargarCalibracionPorDefecto();
    }

    void Update()
    {
        if (cursorDeteccion == null) return;

        ComprobarBotonGrip();

        if (mandoDetectado)
        {
            MoverCursorPorVectores();
        }
        else
        {
            posicionLocalObjetivo = new Vector3(0, 0, 1.99f);
        }

        cursorDeteccion.localPosition = Vector3.Lerp(cursorDeteccion.localPosition, posicionLocalObjetivo, Time.deltaTime * suavizado);
    }

    private void ComprobarBotonGrip()
    {
        bool gripPulsado = false;
        if (mandoAUsar == OVRInput.Controller.RTouch) gripPulsado = OVRInput.Get(OVRInput.RawButton.RHandTrigger);
        else if (mandoAUsar == OVRInput.Controller.LTouch) gripPulsado = OVRInput.Get(OVRInput.RawButton.LHandTrigger);

        if (gripPulsado != mandoDetectado)
        {
            mandoDetectado = gripPulsado;
            CambiarColorCursor(mandoDetectado ? colorActivo : colorInactivo);
        }
    }

    private void MoverCursorPorVectores()
    {
        Vector3 posActual = OVRInput.GetLocalControllerPosition(mandoAUsar);
        Vector3 vectorMovimiento = posActual - calibracion.reposo.posicion;

        Vector3 ejeArriba = calibracion.levantada.posicion - calibracion.reposo.posicion;
        Vector3 ejeAbajo = calibracion.extendida.posicion - calibracion.reposo.posicion;
        Vector3 ejeIzquierda = calibracion.izquierda.posicion - calibracion.reposo.posicion;
        Vector3 ejeDerecha = calibracion.derecha.posicion - calibracion.reposo.posicion;

        float movX = 0f;
        float movY = 0f;

        if (Vector3.Dot(vectorMovimiento, ejeArriba) > 0 && ejeArriba.sqrMagnitude > 0)
            movY = Vector3.Dot(vectorMovimiento, ejeArriba) / ejeArriba.sqrMagnitude;
        else if (ejeAbajo.sqrMagnitude > 0)
            movY = -(Vector3.Dot(vectorMovimiento, ejeAbajo) / ejeAbajo.sqrMagnitude);

        if (Vector3.Dot(vectorMovimiento, ejeDerecha) > 0 && ejeDerecha.sqrMagnitude > 0)
            movX = Vector3.Dot(vectorMovimiento, ejeDerecha) / ejeDerecha.sqrMagnitude;
        else if (ejeIzquierda.sqrMagnitude > 0)
            movX = -(Vector3.Dot(vectorMovimiento, ejeIzquierda) / ejeIzquierda.sqrMagnitude);

        movX = Mathf.Clamp(movX, -1.2f, 1.2f);
        movY = Mathf.Clamp(movY, -1.2f, 1.2f);

        posicionLocalObjetivo = new Vector3(movX * rangoVisualMenu, movY * rangoVisualMenu, 1.99f);
    }

    private void CambiarColorCursor(Color nuevoColor)
    {
        if (spriteCursor != null)
        {
            spriteCursor.color = nuevoColor;
        }
    }

    public PuntoCalibracion ObtenerDatosHardware()
    {
        return new PuntoCalibracion
        {
            posicion = OVRInput.GetLocalControllerPosition(mandoAUsar),
            rotacion = OVRInput.GetLocalControllerRotation(mandoAUsar)
        };
    }

    public void ExportarCalibracionJSON()
    {
        string json = JsonUtility.ToJson(calibracion, true);
        string ruta = Path.Combine(Application.persistentDataPath, "Calibracion_Piernas_Debug.json");
        File.WriteAllText(ruta, json);
        Debug.Log("Calibración JSON generada en: " + ruta);
    }

    private void CargarCalibracionPorDefecto()
    {
        string ruta = Path.Combine(Application.persistentDataPath, "Calibracion_Piernas_Debug.json");
        if (File.Exists(ruta))
        {
            string json = File.ReadAllText(ruta);
            calibracion = JsonUtility.FromJson<DatosCalibracionPierna>(json);
        }
    }
}