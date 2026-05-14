using UnityEngine;

public class DetectorPiernaVR : MonoBehaviour
{
    public enum PosturaPierna
    {
        Reposo,       
        InclinadaIzq, 
        InclinadaDer,
        Extendida,   
        Levantada     
    }

    public PosturaPierna posturaActual = PosturaPierna.Reposo;
    public Transform mandoPierna; 

    void Update()
    {
        if (mandoPierna == null) return;

        // Leeremos la rotación para adivinar la postura. 
        CalcularPosturaBasica();
    }

    private void CalcularPosturaBasica()
    {
        // Esta lógica la afinaremos luego con los datos reales del mando
        // De momento es un esqueleto preparado.
    }
}