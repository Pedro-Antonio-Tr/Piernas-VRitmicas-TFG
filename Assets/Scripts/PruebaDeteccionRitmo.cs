using UnityEngine;

public class PruebaDeteccionRitmo : MonoBehaviour
{
    [Header("Identificador")]
    public NotaRitmo.TipoNota tipoZona; 

    [Header("Configuración de Distancia")]
    public Transform cursorDeteccion;
    public float distanciaAcierto = 0.3f;

    [Header("Referencias Visuales")]
    public SpriteRenderer miSprite;

    [Tooltip("Color cuando el cuadrado del paciente pasa por encima")]
    public Color colorHover = Color.cyan;

    private Color colorOriginal;

    private float tiempoDestelloRestante = 0f;
    private Color colorDestelloActual;
    private float duracionDestello = 0.4f; 

    void Start()
    {
        if (miSprite == null) miSprite = GetComponentInChildren<SpriteRenderer>();
        if (miSprite != null) colorOriginal = miSprite.color;
        else Debug.LogError($"[PruebaDeteccionRitmo] Sin SpriteRenderer en: {gameObject.name}");
    }

    void Update()
    {
        if (cursorDeteccion == null || miSprite == null) return;

        if (tiempoDestelloRestante > 0)
        {
            tiempoDestelloRestante -= Time.deltaTime;

            float porcentaje = tiempoDestelloRestante / duracionDestello;
            miSprite.color = Color.Lerp(colorOriginal, colorDestelloActual, porcentaje);
            return;
        }

        float distancia = Vector3.Distance(transform.position, cursorDeteccion.position);

        if (distancia < distanciaAcierto)
        {
            miSprite.color = colorHover; 
        }
        else
        {
            miSprite.color = colorOriginal; 
        }
    }

    public void ActivarDestello(Color colorFlash)
    {
        colorDestelloActual = colorFlash;
        tiempoDestelloRestante = duracionDestello;
        if (miSprite != null) miSprite.color = colorFlash; 
    }
}