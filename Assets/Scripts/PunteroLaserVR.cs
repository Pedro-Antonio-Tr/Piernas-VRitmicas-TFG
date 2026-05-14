using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PunteroLaserVR : MonoBehaviour
{
    private LineRenderer laser;
    public float distanciaLaser = 3f;

    void Start()
    {
        laser = GetComponent<LineRenderer>();

        // Hacemos que el láser sea una línea súper fina
        laser.startWidth = 0.005f;
        laser.endWidth = 0.005f;

        // Desactivamos las sombras 
        laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laser.receiveShadows = false;

    }

    void Update()
    {
        // El punto 0 del láser es la punta del mando
        laser.SetPosition(0, transform.position);

        // Disparamos un rayo invisible para ver si choca con la interfaz o un objeto
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, distanciaLaser))
        {
            // Si choca con algo, el láser se corta ahí exactamente
            laser.SetPosition(1, hit.point);
        }
        else
        {
            // Si no choca, se dibuja hasta la distancia máxima
            laser.SetPosition(1, transform.position + transform.forward * distanciaLaser);
        }
    }

    void OnEnable()
    {
        if (laser != null) laser.enabled = true;
    }

    void OnDisable()
    {
        if (laser != null) laser.enabled = false;
    }
}
