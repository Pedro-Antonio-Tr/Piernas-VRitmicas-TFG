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

    private AudioSource ObtenerReproductor()
    {
        if (reproductor == null)
        {
            GameObject obj = GameObject.Find("ReproductorEditorRitmo_Temp");
            if (obj == null)
            {
                obj = new GameObject("ReproductorEditorRitmo_Temp");
                obj.hideFlags = HideFlags.HideAndDontSave;
                reproductor = obj.AddComponent<AudioSource>();
            }
            else
            {
                reproductor = obj.GetComponent<AudioSource>();
            }
        }
        return reproductor;
    }

    void OnDisable()
    {
        if (estaGrabando)
        {
            Debug.LogWarning("Se interrumpió el editor. Forzando guardado de seguridad...");
            DetenerGrabacion(ObtenerReproductor());
        }
        if (reproductor != null) DestroyImmediate(reproductor.gameObject);
    }

    void Update()
    {
        if (Application.isPlaying && ObtenerReproductor().isPlaying)
        {
            Repaint();
        }
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

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("¡ATENCIÓN! Debes entrar en el MODO PLAY (▶️) para grabar sin lag de audio.", MessageType.Error);
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

        AudioSource player = ObtenerReproductor();
        if (player.isPlaying && cancionActual.bpm > 0)
        {
            DibujarMetronomoVisual(player);
        }

        EditorGUILayout.Space();
        if (player.clip != cancionActual.archivoAudio) player.clip = cancionActual.archivoAudio;

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(estaGrabando ? "⏹ DETENER Y GUARDAR" : "▶ REPRODUCIR Y GRABAR", GUILayout.Height(40)))
        {
            if (estaGrabando) DetenerGrabacion(player);
            else IniciarGrabacion(player);
        }
        if (GUILayout.Button(player.isPlaying && !estaGrabando ? "⏹ PARAR TEST" : "👁 TESTEAR METRÓNOMO", GUILayout.Height(40)))
        {
            if (player.isPlaying) player.Stop();
            else player.Play();
        }
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        if (player.isPlaying)
        {
            if (estaGrabando)
            {
                EditorGUILayout.HelpBox("¡GRABANDO! Usa las flechas del teclado y la tecla Espacio para añadir notas.", MessageType.Warning);
                RegistrarTeclasEnTiempoReal(player);
            }

            Rect r = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(r, player.time / player.clip.length, $"Tiempo: {player.time:F2}s");

            if (!player.isPlaying && estaGrabando) DetenerGrabacion(player);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Borrar TODAS las notas de esta dificultad"))
        {
            if (EditorUtility.DisplayDialog("Confirmar", "¿Borrar todas las notas?", "Sí", "Cancelar"))
            {
                listaActual.Clear();
                EditorUtility.SetDirty(cancionActual);
                AssetDatabase.SaveAssets();
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

    private void DibujarMetronomoVisual(AudioSource player)
    {
        float offsetActivo = cancionActual.offsetInicial;
        if (cancionActual.desfasesExtra != null)
        {
            foreach (var desfase in cancionActual.desfasesExtra)
            {
                if (player.time >= desfase.tiempoCancion) offsetActivo = desfase.nuevoOffset;
            }
        }

        float segundosPorBeat = 60f / cancionActual.bpm;
        float beatsPasados = (player.time - offsetActivo) / segundosPorBeat;
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

    private void IniciarGrabacion(AudioSource player)
    {
        if (cancionActual.archivoAudio == null) return;
        estaGrabando = true;
        player.Play();
    }

    private void DetenerGrabacion(AudioSource player)
    {
        estaGrabando = false;
        if (player != null) player.Stop();

        List<DatosCancionRitmo.NotaGuardada> lista = ObtenerListaActual();

        var listaLimpia = lista.GroupBy(n => n.tiempoAparicion).Select(g => g.First()).ToList();
        listaLimpia = listaLimpia.OrderBy(n => n.tiempoAparicion).ToList();

        if (dificultadAEditar == MonitorClinico.NivelDificultad.Facil) cancionActual.notasFacil = listaLimpia;
        else if (dificultadAEditar == MonitorClinico.NivelDificultad.Normal) cancionActual.notasNormal = listaLimpia;
        else if (dificultadAEditar == MonitorClinico.NivelDificultad.Dificil) cancionActual.notasDificil = listaLimpia;

        EditorUtility.SetDirty(cancionActual);
        AssetDatabase.SaveAssets();

        Debug.Log("<color=cyan>[Editor Ritmo]</color> Notas ordenadas y guardadas permanentemente en el disco.");
    }

    private void RegistrarTeclasEnTiempoReal(AudioSource player)
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
                float tiempoFinal = AcoplarAlRitmo(player.time);

                ObtenerListaActual().Add(new DatosCancionRitmo.NotaGuardada
                {
                    tiempoAparicion = tiempoFinal,
                    tipoNota = notaDetectada.Value
                });

                EditorUtility.SetDirty(cancionActual);

                Debug.Log($"Nota guardada: Acoplada a {tiempoFinal:F2}s");
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
        AssetDatabase.SaveAssets();
    }
}