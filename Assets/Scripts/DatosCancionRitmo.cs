using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NuevaCancion", menuName = "Ritmo/Nueva Cancion")]
public class DatosCancionRitmo : ScriptableObject
{
    [Header("Datos de la Pista")]
    public string nombreCancion = "Sin Nombre";
    public string artista = "Desconocido";
    public AudioClip archivoAudio;

    [Header("Configuración de Ritmo (Metrónomo)")]
    public float bpm = 120f;
    [Tooltip("Retardo en segundos hasta el primer beat de la canción")]
    public float offsetInicial = 0f;

    [System.Serializable]
    public class PuntoSincronizacion
    {
        [Tooltip("Segundo exacto de la canción donde empieza este desfase")]
        public float tiempoCancion;
        [Tooltip("Nuevo offset en segundos para acoplar el ritmo a partir de aquí")]
        public float nuevoOffset;
    }

    [Tooltip("Usa esto solo si la canción se desincroniza a la mitad o tiene pausas raras")]
    public List<PuntoSincronizacion> desfasesExtra = new List<PuntoSincronizacion>();

    [System.Serializable]
    public class NotaGuardada
    {
        public float tiempoAparicion; 
        public NotaRitmo.TipoNota tipoNota;
    }

    [Header("Mapas de Notas por Dificultad")]
    public List<NotaGuardada> notasFacil = new List<NotaGuardada>();
    public List<NotaGuardada> notasNormal = new List<NotaGuardada>();
    public List<NotaGuardada> notasDificil = new List<NotaGuardada>();
}