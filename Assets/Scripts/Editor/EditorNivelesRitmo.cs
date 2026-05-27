using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class EditorNivelesRitmo : EditorWindow
{
    private DatosCancionRitmo cancionActual;
    private MonitorClinico.NivelDificultad dificultadAEditar = MonitorClinico.NivelDificultad.Facil;

    private AudioSource reproductor;
    private bool estaGrabando = false;

    private bool usarCuantizacion = true;
    private int precisionCuantizacion = 2;

    [MenuItem("TFG Herramientas/Creador de Niveles de Ritmo")]
    public static void MostrarVentana()
    {
        GetWindow<EditorNivelesRitmo>("Editor de Ritmo");
    }

    void OnEnable()
    {
        GameObject obj = new GameObject("ReproductorEditorRitmo");
        obj.hideFlags = HideFlags.HideAndDontSave;
        reproductor = obj.AddComponent<AudioSource>();
    }

    void OnDisable()
    {
        if (reproductor != null) DestroyImmediate(reproductor.gameObject);
    }

    void OnGUI()
    {
        GUILayout.Label("Mesa de Mapeo y Cuantización", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        cancionActual = (DatosCancionRitmo)EditorGUILayout.ObjectField("Archivo de Canción", cancionActual, typeof(DatosCancionRitmo), false);

        if (cancionActual == null)
        {
            EditorGUILayout.HelpBox("Asigna un archivo de canción para empezar.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        GUILayout.Label("Sintonización Fina (Ajuste en Vivo)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        float nuevoBPM = EditorGUILayout.FloatField("BPM de la Canción", cancionActual.bpm);
        float nuevoOffset = EditorGUILayout.FloatField("Offset Inicial (segs)", cancionActual.offsetInicial);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(cancionActual, "Ajuste de Ritmo");
            cancionActual.bpm = nuevoBPM;
            cancionActual.offsetInicial = nuevoOffset;
            EditorUtility.SetDirty(cancionActual);
        }

        EditorGUILayout.Space();
        dificultadAEditar = (MonitorClinico.NivelDificultad)EditorGUILayout.EnumPopup("Dificultad a Editar", dificultadAEditar);

        List<DatosCancionRitmo.NotaGuardada> listaActual = ObtenerListaActual();

        if (dificultadAEditar == MonitorClinico.NivelDificultad.Normal || dificultadAEditar == MonitorClinico.NivelDificultad.Dificil)
        {
            if (GUILayout.Button($"Copiar notas de dificultad anterior a {dificultadAEditar}")) CopiarDificultadAnterior();
        }

        EditorGUILayout.Space();
        GUILayout.Label($"Notas en {dificultadAEditar}: {listaActual.Count}", EditorStyles.helpBox);

        EditorGUILayout.Space();
        GUILayout.Label("Ajustes de Sincronización (Imán de Ritmo)", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        usarCuantizacion = EditorGUILayout.Toggle("Acoplar notas al BPM", usarCuantizacion);
        if (usarCuantizacion)
        {
            precisionCuantizacion = EditorGUILayout.IntPopup("Precisión", precisionCuantizacion,
                new string[] { "1/1 (Negras)", "1/2 (Corcheas)", "1/4 (Semicorcheas)" },
                new int[] { 1, 2, 4 });
        }
        GUILayout.EndHorizontal();

        if (reproductor != null && reproductor.isPlaying && cancionActual.bpm > 0)
        {
            DibujarMetronomoVisual();
        }

        EditorGUILayout.Space();
        if (reproductor.clip != cancionActual.archivoAudio) reproductor.clip = cancionActual.archivoAudio;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(estaGrabando ? "⏹ DETENER" : "▶ REPRODUCIR Y GRABAR", GUILayout.Height(40)))
        {
            if (estaGrabando) DetenerGrabacion();
            else IniciarGrabacion();
        }
        if (GUILayout.Button(reproductor.isPlaying && !estaGrabando ? "⏹ PARAR TEST" : "👁 TESTEAR METRÓNOMO", GUILayout.Height(40)))
        {
            if (reproductor.isPlaying) reproductor.Stop();
            else reproductor.Play();
        }
        GUILayout.EndHorizontal();

        if (reproductor.isPlaying)
        {
            Repaint();

            if (estaGrabando)
            {
                EditorGUILayout.HelpBox("¡GRABANDO! Usa las flechas del teclado y la tecla Espacio para añadir notas.", MessageType.Warning);
                RegistrarTeclasEnTiempoReal();
            }

            Rect r = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(r, reproductor.time / reproductor.clip.length, $"Tiempo: {reproductor.time:F2}s");

            if (!reproductor.isPlaying && estaGrabando) DetenerGrabacion();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Borrar TODAS las notas de esta dificultad"))
        {
            if (EditorUtility.DisplayDialog("Confirmar", "¿Borrar todas las notas?", "Sí", "Cancelar"))
            {
                listaActual.Clear();
                EditorUtility.SetDirty(cancionActual);
            }
        }
    }

    private float AcoplarAlRitmo(float tiempoReal)
    {
        if (!usarCuantizacion || cancionActual.bpm <= 0) return tiempoReal;

        float offsetActivo = cancionActual.offsetInicial;
        if (cancionActual.desfasesExtra != null)
        {
            foreach (var desfase in cancionActual.desfasesExtra)
            {
                if (tiempoReal >= desfase.tiempoCancion) offsetActivo = desfase.nuevoOffset;
            }
        }

        float segundosPorBeat = 60f / cancionActual.bpm;
        float segundosPorSubdivision = segundosPorBeat / precisionCuantizacion;

        float tiempoAjustado = tiempoReal - offsetActivo;
        float subdivisiones = Mathf.Round(tiempoAjustado / segundosPorSubdivision);
        float tiempoCuantizado = offsetActivo + (subdivisiones * segundosPorSubdivision);

        return Mathf.Max(0, tiempoCuantizado);
    }

    private void DibujarMetronomoVisual()
    {
        float offsetActivo = cancionActual.offsetInicial;
        if (cancionActual.desfasesExtra != null)
        {
            foreach (var desfase in cancionActual.desfasesExtra)
            {
                if (reproductor.time >= desfase.tiempoCancion) offsetActivo = desfase.nuevoOffset;
            }
        }

        float segundosPorBeat = 60f / cancionActual.bpm;
        float beatsPasados = (reproductor.time - offsetActivo) / segundosPorBeat;
        float distanciaAlBeatExacto = Mathf.Abs(beatsPasados - Mathf.Round(beatsPasados));

        Color colorBPM = (distanciaAlBeatExacto < 0.15f) ? Color.green : new Color(0.3f, 0.3f, 0.3f);

        GUI.backgroundColor = colorBPM;
        GUIStyle estiloMetronomo = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 18
        };
        estiloMetronomo.normal.textColor = Color.white;

        GUILayout.Box("METRÓNOMO", estiloMetronomo, GUILayout.Height(30), GUILayout.ExpandWidth(true));
        GUI.backgroundColor = Color.white;
    }

    private void IniciarGrabacion()
    {
        if (cancionActual.archivoAudio == null) return;
        estaGrabando = true;
        reproductor.Play();
    }

    private void DetenerGrabacion()
    {
        estaGrabando = false;
        reproductor.Stop();

        List<DatosCancionRitmo.NotaGuardada> lista = ObtenerListaActual();
        lista = lista.GroupBy(n => n.tiempoAparicion).Select(g => g.First()).ToList();
        lista = lista.OrderBy(n => n.tiempoAparicion).ToList();

        EditorUtility.SetDirty(cancionActual);
        AssetDatabase.SaveAssets();
    }

    private void RegistrarTeclasEnTiempoReal()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            NotaRitmo.TipoNota? notaDetectada = null;

            if (e.keyCode == KeyCode.LeftArrow) notaDetectada = NotaRitmo.TipoNota.Izquierda;
            else if (e.keyCode == KeyCode.RightArrow) notaDetectada = NotaRitmo.TipoNota.Derecha;
            else if (e.keyCode == KeyCode.UpArrow) notaDetectada = NotaRitmo.TipoNota.Arriba;
            else if (e.keyCode == KeyCode.DownArrow) notaDetectada = NotaRitmo.TipoNota.Abajo;
            else if (e.keyCode == KeyCode.Space) notaDetectada = NotaRitmo.TipoNota.Reposo;

            if (notaDetectada != null)
            {
                float tiempoFinal = AcoplarAlRitmo(reproductor.time);

                ObtenerListaActual().Add(new DatosCancionRitmo.NotaGuardada
                {
                    tiempoAparicion = tiempoFinal,
                    tipoNota = notaDetectada.Value
                });

                Debug.Log($"Nota guardada: Tocado en {reproductor.time:F2}s -> Acoplado a {tiempoFinal:F2}s");
                e.Use();
            }
        }
    }

    private List<DatosCancionRitmo.NotaGuardada> ObtenerListaActual()
    {
        if (dificultadAEditar == MonitorClinico.NivelDificultad.Facil) return cancionActual.notasFacil;
        if (dificultadAEditar == MonitorClinico.NivelDificultad.Normal) return cancionActual.notasNormal;
        return cancionActual.notasDificil;
    }

    private void CopiarDificultadAnterior()
    {
        List<DatosCancionRitmo.NotaGuardada> origen = dificultadAEditar == MonitorClinico.NivelDificultad.Normal ? cancionActual.notasFacil : cancionActual.notasNormal;
        List<DatosCancionRitmo.NotaGuardada> destino = ObtenerListaActual();

        destino.Clear();
        foreach (var nota in origen)
        {
            destino.Add(new DatosCancionRitmo.NotaGuardada { tiempoAparicion = nota.tiempoAparicion, tipoNota = nota.tipoNota });
        }

        EditorUtility.SetDirty(cancionActual);
    }
}