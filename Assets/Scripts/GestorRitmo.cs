using UnityEngine;
using System.Collections;

public class GestorRitmo : MonoBehaviour
{
    public static GestorRitmo Instancia;

    [Header("Prefabs de las Notas")]
    public GameObject[] prefabsNotas;

    [Header("Configuración de la Pista")]
    public Transform contenedorPista;
    public float zSpawn = 25f;

    [Header("Estado")]
    public bool modoPruebaActivo = false;

    private Coroutine bucleNotas;
    public bool juegoEmpezado = false;
    public bool enCuentaAtras = false;

    void Awake()
    {
        Instancia = this;
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            if (ControladorMenu.Instancia != null && !ControladorMenu.Instancia.calibracionEnProceso)
            {
                ToggleModoPrueba();
            }
        }
    }

    private void ToggleModoPrueba()
    {
        modoPruebaActivo = !modoPruebaActivo;

        if (modoPruebaActivo)
        {
            Debug.Log("¡Modo Prueba de Ritmo INICIADO! Spawneando notas cada 2 segundos.");
            bucleNotas = StartCoroutine(BucleGeneracionNotas());
        }
        else
        {
            Debug.Log("Modo Prueba de Ritmo FINALIZADO.");
            if (bucleNotas != null) StopCoroutine(bucleNotas);

            foreach (NotaRitmo notaRestante in FindObjectsByType<NotaRitmo>(FindObjectsSortMode.None))
            {
                Destroy(notaRestante.gameObject);
            }
        }
    }

    private IEnumerator BucleGeneracionNotas()
    {
        yield return new WaitForSeconds(1f);

        while (modoPruebaActivo)
        {
            SpawnNotaAleatoria();
            yield return new WaitForSeconds(2f);
        }
    }

    private void SpawnNotaAleatoria()
    {
        if (prefabsNotas == null || prefabsNotas.Length == 0 || contenedorPista == null) return;

        int indiceAleatorio = Random.Range(0, prefabsNotas.Length);
        GameObject prefabElegido = prefabsNotas[indiceAleatorio];

        NotaRitmo datosNota = prefabElegido.GetComponent<NotaRitmo>();
        if (datosNota == null) return;

        Vector3 posicionLocalSpawn = new Vector3(0f, 0f, zSpawn);

        switch (datosNota.tipoDeNota)
        {
            case NotaRitmo.TipoNota.Izquierda:
                posicionLocalSpawn.x = -0.7f;
                break;
            case NotaRitmo.TipoNota.Derecha:
                posicionLocalSpawn.x = 0.7f;
                break;
            case NotaRitmo.TipoNota.Arriba:
                posicionLocalSpawn.y = 0.7f;
                break;
            case NotaRitmo.TipoNota.Abajo:
                posicionLocalSpawn.y = -0.7f;
                break;
            case NotaRitmo.TipoNota.Reposo:
                break;
        }

        GameObject nuevaNota = Instantiate(prefabElegido, contenedorPista);

        nuevaNota.transform.localPosition = posicionLocalSpawn;
    }

    public void EmpezarPartidaDesdeMenu()
    {
        //placeholder para eliminar errores
    }

    public void ReiniciarNivelActual()
    {
        //placeholder para eliminar errores
    }

    public void VolverAlMenuPrincipal()
    {
        //placeholder para eliminar errores
    }

    public void AlternarPausa(bool pausa)
    {
        //placeholder para eliminar errores
    }
}