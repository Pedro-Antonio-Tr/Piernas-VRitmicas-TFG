using UnityEngine;

public class PruebaDeteccionRitmo : MonoBehaviour
{
    [Header("Configuración de Distancia")]
    public Transform cursorDeteccion;
    public float distanciaAcierto = 0.3f;

    [Header("Referencias Visuales (Asignación Manual)")]
    [Tooltip("Arrastra aquí el SpriteRenderer de la Flecha o Círculo que está en el objeto hijo")]
    public SpriteRenderer miSprite;

    private Color colorOriginal;

    void Start()
    {
        if (miSprite == null)
        {
            miSprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (miSprite != null)
            colorOriginal = miSprite.color;
        else
            Debug.LogError($"[PruebaDeteccionRitmo] No se ha asignado ningún SpriteRenderer en el objeto: {gameObject.name}");
    }

    void Update()
    {
        if (cursorDeteccion == null || miSprite == null) return;

        float distancia = Vector3.Distance(transform.position, cursorDeteccion.position);

        if (distancia < distanciaAcierto)
        {
            miSprite.color = Color.green;
        }
        else
        {
            miSprite.color = colorOriginal;
        }
    }
}