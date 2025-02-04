using System;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using WindCompute.SplineProviders;
using SplineUtility = UnityEngine.Splines.SplineUtility;

namespace WindCompute
{
    public class SplineCacheController : MonoBehaviour
    {
        [SerializeField] private BaseSplineProvider _splineProvider;
        [SerializeField] [MinMaxSlider(0, 1)] private Vector2 _range;
        [SerializeField] private float _density;
        [SerializeField] private float _height;
        [SerializeField] private SplineCache _splineCache;
        [SerializeField] private bool _preview;

        [Button("Generate")]
        private void Generate()
        {
            Populate();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (BuildPipeline.isBuildingPlayer)
                return;
#endif

            if (_splineProvider != null)
            {
                Populate();
            }
        }

        private void Populate()
        {
            var unitySpline = _splineProvider.GetSpline();
            var length = SplineUtility.CalculateLength(unitySpline, Matrix4x4.identity);
            length *= (_range.y - _range.x);

            int nodesCount = Mathf.FloorToInt(length * _density);

            var positions = new Vector3[nodesCount];
            var bounds = new Bounds();
            for (int i = 0; i < nodesCount; i++)
            {
                var normalPosition = (float) i / (nodesCount - 1);
                var t = Mathf.Lerp(_range.x, _range.y, normalPosition);
                var localPosition = unitySpline.GetPoint(t, _height);
                var upVector = unitySpline.GetUpVector(t);
                var angle = -Vector2.SignedAngle(upVector, Vector2.up) * Mathf.Deg2Rad;

                positions[i] = new Vector3(localPosition.x, localPosition.y, angle);

                if (i == 0)
                {
                    bounds = new Bounds(localPosition, Vector3.one);
                }
                else
                {
                    bounds.Encapsulate(localPosition);
                }
            }

            _splineCache.Setup(positions, bounds);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_preview)
                return;

            if (_splineProvider == null)
                return;

            var spline = _splineProvider.GetSpline();
            var startPosition = spline.GetPoint(_range.x);
            var endPosition = spline.GetPoint(_range.y);

            for (var i = 0; i < _splineCache.Positions.Length; i++)
            {
                var wPos = _splineProvider.transform.TransformPoint(_splineCache.Positions[i]);
                var angle = _splineCache.Positions[i].z;
                var position = wPos;
                var normal = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward) * Vector3.up;

                Gizmos.DrawLine(position, position + normal);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_splineProvider.transform.TransformPoint(startPosition), 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_splineProvider.transform.TransformPoint(endPosition), 0.5f);

            Gizmos.color = Color.white;
            var min = _splineProvider.transform.TransformPoint(_splineCache.Bounds.min);
            var max = _splineProvider.transform.TransformPoint(_splineCache.Bounds.max);
            var size = max - min;

            var rect = new Rect(min, size);
            var z = _splineCache.Bounds.center.z;

            Gizmos.DrawLineList(new ReadOnlySpan<Vector3>(
                new[]
                {
                    new Vector3(rect.xMin, rect.yMax, z),
                    new Vector3(rect.xMax, rect.yMax, z),
                    new Vector3(rect.xMax, rect.yMax, z),
                    new Vector3(rect.xMax, rect.yMin, z),
                    new Vector3(rect.xMax, rect.yMin, z),
                    new Vector3(rect.xMin, rect.yMin, z),
                    new Vector3(rect.xMin, rect.yMin, z),
                    new Vector3(rect.xMin, rect.yMax, z),
                }));
        }
    }
}