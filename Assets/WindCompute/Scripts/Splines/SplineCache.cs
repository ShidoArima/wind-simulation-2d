using System;
using UnityEditor;
using UnityEngine;

namespace WindCompute
{
    [CreateAssetMenu(fileName = "SplineCache", menuName = "Spline/SplineCache")]
    public class SplineCache : ScriptableObject
    {
        [SerializeField] private SplineData _splineData;

        public Vector3[] Positions => _splineData.Positions;
        public Bounds    Bounds    => _splineData.Bounds;
        public int       Count     => _splineData.Count;

        public void Setup(Vector3[] positions, Bounds bounds)
        {
            _splineData.Setup(positions, bounds);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }

    [Serializable]
    public class SplineData
    {
        [SerializeField] private Vector3[] _positions;
        [SerializeField] private Bounds    _bounds;

        public Vector3[] Positions => _positions;
        public Bounds    Bounds    => _bounds;
        public int       Count     => _positions.Length;

        public void Setup(Vector3[] positions, Bounds bounds)
        {
            _positions = positions;
            _bounds    = bounds;
        }

        public void Clear()
        {
            _positions = Array.Empty<Vector3>();
            _bounds    = new Bounds();
        }
    }
}