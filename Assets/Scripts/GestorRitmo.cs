using UnityEngine;

public class GestorRitmo : MonoBehaviour
{
    public static GestorRitmo Instancia;

    [Header("Estado del Juego")]
    public bool juegoEmpezado = false;
    public bool enCuentaAtras = false;

    [Header("Referencias de Pista")]
    public Transform pistaDeRitmo;
    public Transform headAnchor;

    void Awake()
    {
        Instancia = this;
    }

    public void EmpezarPartidaDesdeMenu()
    {
        CentrarPistaEnMirada();
        juegoEmpezado = true;
    }

    public void VolverAlMenuPrincipal()
    {
        juegoEmpezado = false;
    }

    public void ReiniciarNivelActual()
    {
        VolverAlMenuPrincipal();
        EmpezarPartidaDesdeMenu();
    }

    public void AlternarPausa(bool estaEnPausa)
    {
        if (estaEnPausa) Time.timeScale = 0f;
        else Time.timeScale = 1f;
    }

    private void CentrarPistaEnMirada()
    {
        if (headAnchor == null || pistaDeRitmo == null) return;

        pistaDeRitmo.position = headAnchor.position;
        pistaDeRitmo.rotation = headAnchor.rotation;
    }
}