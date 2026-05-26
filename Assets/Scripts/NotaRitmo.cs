using UnityEngine;

public class NotaRitmo : MonoBehaviour
{
    public enum TipoNota { Reposo, Izquierda, Derecha, Arriba, Abajo }
    public TipoNota tipoDeNota;

    [Header("Ajustes de Movimiento")]
    public float velocidadBase = 4f;
    public float factorAceleracionLejana = 0.15f;

    private float zObjetivo = 2f;
    private float margenAciertoZ = 0.25f;
    private float margenAciertoXY = 0.4f;

    private bool fueAcertada = false;

    void Update()
    {
        float distanciaZ = transform.localPosition.z - zObjetivo;

        float velocidadActual = velocidadBase;
        if (distanciaZ > 0)
        {
            velocidadActual += (distanciaZ * distanciaZ) * factorAceleracionLejana;
        }

        transform.Translate(Vector3.back * velocidadActual * Time.deltaTime, Space.Self);

        if (!fueAcertada && Mathf.Abs(distanciaZ) <= margenAciertoZ)
        {
            if (DetectorPiernaVR.Instancia != null && DetectorPiernaVR.Instancia.mandoDetectado)
            {
                Transform cursor = DetectorPiernaVR.Instancia.cursorDeteccion;

                Vector2 posCursor = new Vector2(cursor.localPosition.x, cursor.localPosition.y);
                Vector2 posNota = new Vector2(transform.localPosition.x, transform.localPosition.y);

                if (Vector2.Distance(posCursor, posNota) <= margenAciertoXY)
                {
                    AcertarNota();
                }
            }
        }

        if (transform.localPosition.z < (zObjetivo - margenAciertoZ - 0.2f))
        {
            FallarNota();
        }
    }

    private void AcertarNota()
    {
        fueAcertada = true;
        Debug.Log($"<color=green>¡PERFECTO!</color> Nota {tipoDeNota} cazada.");
        Destroy(gameObject);
    }

    private void FallarNota()
    {
        Debug.Log($"<color=red>MISS:</color> Nota {tipoDeNota} perdida.");
        Destroy(gameObject);
    }
}