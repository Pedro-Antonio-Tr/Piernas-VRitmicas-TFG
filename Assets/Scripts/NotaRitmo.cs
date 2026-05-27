using UnityEngine;

public class NotaRitmo : MonoBehaviour
{
    public enum TipoNota { Reposo, Izquierda, Derecha, Arriba, Abajo }
    public TipoNota tipoDeNota;

    private float zObjetivo = 2f;
    private float margenPerfectoZ = 0.25f;
    private float margenTardioZ = 0.45f;
    private float margenAciertoXY = 0.4f;

    private bool fueEvaluada = false;
    private Vector3 escalaNativa;

    void Start()
    {
        escalaNativa = transform.localScale;
    }

    void Update()
    {
        if (GestorRitmo.Instancia == null || !GestorRitmo.Instancia.modoPruebaActivo) return;

        GestorRitmo motor = GestorRitmo.Instancia;
        float zActual = transform.localPosition.z;
        float distanciaZ = zActual - zObjetivo;

        float velocidadActual;
        Vector3 escalaCalculada;

        if (zActual > motor.zTransicion)
        {
            velocidadActual = motor.velocidadAcercamiento;
            float tEscala = Mathf.InverseLerp(motor.zSpawn, motor.zTransicion, zActual);
            float multiplicadorEscala = Mathf.Lerp(motor.escalaMinima, 1f, tEscala);
            escalaCalculada = escalaNativa * multiplicadorEscala;
        }
        else
        {
            velocidadActual = motor.velocidadBase;
            escalaCalculada = escalaNativa;
        }

        transform.localScale = escalaCalculada;
        transform.localPosition += Vector3.back * velocidadActual * Time.deltaTime;

        if (!fueEvaluada)
        {
            bool enZonaPerfecta = Mathf.Abs(distanciaZ) <= margenPerfectoZ;
            bool enZonaTardia = distanciaZ < -margenPerfectoZ && distanciaZ >= -margenTardioZ;

            if (enZonaPerfecta || enZonaTardia)
            {
                if (DetectorPiernaVR.Instancia != null && DetectorPiernaVR.Instancia.mandoDetectado)
                {
                    Transform cursor = DetectorPiernaVR.Instancia.cursorDeteccion;
                    Vector2 posCursor = new Vector2(cursor.localPosition.x, cursor.localPosition.y);
                    Vector2 posNota = new Vector2(transform.localPosition.x, transform.localPosition.y);

                    if (Vector2.Distance(posCursor, posNota) <= margenAciertoXY)
                    {
                        fueEvaluada = true;
                        int calidad = enZonaPerfecta ? 2 : 1;
                        motor.ProcesarNota(tipoDeNota, calidad);
                        Destroy(gameObject);
                    }
                }
            }
            else if (distanciaZ < -margenTardioZ)
            {
                fueEvaluada = true;
                motor.ProcesarNota(tipoDeNota, 0);
                Destroy(gameObject);
            }
        }
    }
}