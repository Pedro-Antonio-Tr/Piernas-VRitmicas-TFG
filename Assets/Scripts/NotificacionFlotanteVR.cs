using UnityEngine;
using TMPro;
using System.Collections;

public class NotificacionFlotanteVR : MonoBehaviour
{
    public static NotificacionFlotanteVR Instancia;

    [Header("Referencias")]
    public Transform headAnchor;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI textoNotificacion;

    [Header("Ajustes de Seguimiento (Lazy HUD)")]
    public float distanciaAdelante = 0.5f;
    public float desplazamientoAbajo = -0.3f; // Un poco por debajo de la línea de los ojos
    public float velocidadSeguimiento = 5f;

    private Coroutine rutinaOcultar;

    void Awake()
    {
        if (Instancia == null) Instancia = this;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    void LateUpdate()
    {
        if (headAnchor == null || canvasGroup == null || canvasGroup.alpha == 0f) return;

        Vector3 puntoIdeal = headAnchor.position + (headAnchor.forward * distanciaAdelante) + (headAnchor.up * desplazamientoAbajo);

        transform.position = Vector3.Lerp(transform.position, puntoIdeal, Time.unscaledDeltaTime * velocidadSeguimiento);

        Vector3 direccionHaciaCabeza = transform.position - headAnchor.position;
        if (direccionHaciaCabeza != Vector3.zero)
        {
            Quaternion rotacionIdeal = Quaternion.LookRotation(direccionHaciaCabeza);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionIdeal, Time.unscaledDeltaTime * velocidadSeguimiento);
        }
    }

    public void MostrarNotificacion(string mensaje, float duracion = 3f)
    {
        if (textoNotificacion != null) textoNotificacion.text = mensaje;

        Vector3 puntoIdeal = headAnchor.position + (headAnchor.forward * distanciaAdelante) + (headAnchor.up * desplazamientoAbajo);
        if (Vector3.Distance(transform.position, puntoIdeal) > 0.8f)
        {
            transform.position = puntoIdeal;
        }

        if (rutinaOcultar != null) StopCoroutine(rutinaOcultar);
        gameObject.SetActive(true);
        rutinaOcultar = StartCoroutine(RutinaAnimacionNotificacion(duracion));
    }

    private IEnumerator RutinaAnimacionNotificacion(float duracion)
    {
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.unscaledDeltaTime * 5f;
            yield return null;
        }

        yield return new WaitForSeconds(duracion);

        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.unscaledDeltaTime * 3f;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}