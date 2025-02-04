using NaughtyAttributes.Editor;
using UnityEditor;
using UnityEngine;

namespace WindCompute.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WireComputeRenderer))]
    public class WireComputeEditor : NaughtyInspector
    {
        public void OnSceneGUI()
        {
            WireComputeRenderer wire = target as WireComputeRenderer;

            if (wire == null)
                return;

            var start = wire.StartPosition;
            var end = wire.EndPosition;

            EditorGUI.BeginChangeCheck();

            Handles.color = Color.green;
            Vector3 startPosition = Handles.FreeMoveHandle(start, 0.5f, Vector3.zero, Handles.SphereHandleCap);
            Handles.color = Color.red;
            Vector3 endPosition = Handles.FreeMoveHandle(end, 0.5f, Vector3.zero, Handles.SphereHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Look At Target Position");
                wire.SetStartPoint(startPosition);
                wire.SetEndPoint(endPosition);
                EditorUtility.SetDirty(wire);
            }
        }
    }
}