//
// Copyright (C) Valve Corporation. All rights reserved.
//

using UnityEngine;
using UnityEditor;
using System;

namespace Phonon
{

    //
    // PhononManagerInspector
    // Custom inspector for a PhononManager component.
    //

    [CustomEditor(typeof(PhononManager))]
    public class PhononManagerInspector : Editor
    {
        //
        // Draws the inspector.
        //
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Audio Engine
            PhononGUI.SectionHeader("Audio Engine Integration");
            string[] engines = { "Unity Audio" };
            var audioEngineProperty = serializedObject.FindProperty("audioEngine");
            audioEngineProperty.enumValueIndex = EditorGUILayout.Popup("Audio Engine", audioEngineProperty.enumValueIndex, engines);

            // Scene Settings
            PhononManager phononManager = ((PhononManager)target);
            PhononGUI.SectionHeader("Global Material Settings");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("materialPreset"));
            if (serializedObject.FindProperty("materialPreset").enumValueIndex < 11)
            {
                PhononMaterialValue actualValue = phononManager.materialValue;
                actualValue.CopyFrom(PhononMaterialPresetList.PresetValue(serializedObject.FindProperty("materialPreset").enumValueIndex));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("materialValue"));
            }

            PhononGUI.SectionHeader("Scene Export");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");

            bool exportOBJ = false;
            bool exportScene = false;

            if (GUILayout.Button("Export to OBJ"))
                exportOBJ = true;
            if (GUILayout.Button("Pre-Export Scene"))
                exportScene = true;

            if (exportOBJ || exportScene)
            {
                if (exportScene)
                    phononManager.ExportScene();
                if (exportOBJ)
                    phononManager.DumpScene();
            }
            EditorGUILayout.EndHorizontal();

            // Simulation Settings
            PhononGUI.SectionHeader("Simulation Settings");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationPreset"));
            if (serializedObject.FindProperty("simulationPreset").enumValueIndex < 3)
            {
                SimulationSettingsValue actualValue = phononManager.simulationValue;
                actualValue.CopyFrom(SimulationSettingsPresetList.PresetValue(serializedObject.FindProperty("simulationPreset").enumValueIndex));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationValue"));
                if (Application.isEditor && EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SimulationSettingsValue actualValue = phononManager.simulationValue;
                    IntPtr environment = phononManager.PhononManagerContainer().Environment().GetEnvironment();
                    if (environment != IntPtr.Zero)
                        PhononCore.iplSetNumBounces(environment, actualValue.RealtimeBounces);
                }
            }

            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }
    }
}