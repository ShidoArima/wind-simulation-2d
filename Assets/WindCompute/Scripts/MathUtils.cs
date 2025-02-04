using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.U2D;
using Spline = UnityEngine.U2D.Spline;

namespace WindCompute
{
    public static class MathUtils
    {
        public static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - (2 * t));
        }

        /// <summary>
        /// Returns the local position along the spline based on progress 0 - 1.
        /// Good for lerping an object along the spline.
        /// </summary>
        /// <param name="spline"></param>
        /// <param name="progress">Value from 0 - 1</param>
        /// <returns></returns>
        public static Vector2 GetPoint(this Spline spline, float progress, float height = 0)
        {
            var length = spline.GetPointCount();
            var i      = Mathf.Clamp(Mathf.CeilToInt((length - 1) * progress), 0, length - 1);

            var t = progress * (length - 1) % 1f;
            if (i == length - 1 && progress >= 1f)
                t = 1;

            var prevIndex = Mathf.Max(i - 1, 0);

            Vector3 p0 = spline.GetPosition(prevIndex);
            Vector3 p1 = spline.GetPosition(i);
            Vector3 rt = p0 + spline.GetRightTangent(prevIndex);
            Vector3 lt = p1 + spline.GetLeftTangent(i);

            Vector2 position = BezierUtility.BezierPoint(rt, p0, p1, lt, t);
            var     normal   = GetNormal(rt, p0, p1, lt, t);

            return position + normal * height;
        }

        public static Vector2 GetPoint(this UnityEngine.Splines.Spline spline, float t, float height = 0)
        {
            var pos = spline.EvaluatePosition(t);
            var up  = height == 0 ? new float3(0) : spline.EvaluateUpVector(t);
            return new Vector2(pos.x, pos.y) + new Vector2(up.x, up.y) * height;
        }

        public static Vector2 GetUpVector(this UnityEngine.Splines.Spline spline, float t)
        {
            var up = spline.EvaluateUpVector(t);
            return new Vector2(up.x, up.y);
        }

        public static Vector2 GetNormal(Vector3 rt, Vector3 p0, Vector3 p1, Vector3 lt, float t)
        {
            var tangent = 3 * (Mathf.Pow(1 - t, 2) * (rt - p0) + 2 * (1 - t) * t * (lt - rt) + Mathf.Pow(t, 2) * (p1 - lt));

            return Vector2.Perpendicular(tangent).normalized;
        }

        public static UnityEngine.Splines.Spline ToUnitySpline(this Spline spline)
        {
            var              length = spline.GetPointCount();
            List<BezierKnot> knots  = new List<BezierKnot>();
            for (int i = 0; i < length; i++)
            {
                Vector3 p1 = spline.GetPosition(i);
                Vector3 rt = spline.GetRightTangent(i);
                Vector3 lt = spline.GetLeftTangent(i);

                float angle           = Mathf.Atan2(rt.y, rt.x);
                var   rotation        = quaternion.RotateZ(angle);
                var   inverseRotation = quaternion.RotateZ(-angle);
                knots.Add(new BezierKnot(p1, math.rotate(inverseRotation, lt), math.rotate(inverseRotation, rt), rotation));
            }

            var unitySpline = new UnityEngine.Splines.Spline(knots);
            return unitySpline;
        }
        
        public static Vector3 Interpolate(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p2;
        }

        public static bool TryLineIntersection(Vector2 linePoint1, Vector2 lineVec1, Vector2 linePoint2, Vector2 lineVec2, out Vector2 intersection)
        {
            Vector3 lineVec3      = linePoint2 - linePoint1;
            Vector3 crossVec1And2 = Vector3.Cross(lineVec1, lineVec2);
            Vector3 crossVec3And2 = Vector3.Cross(lineVec3, lineVec2);

            //is coplanar, and not parallel
            if (crossVec1And2.sqrMagnitude > 0.05f)
            {
                var s = Vector3.Dot(crossVec3And2, crossVec1And2) / crossVec1And2.sqrMagnitude;
                intersection = linePoint1 + (lineVec1 * s);
                return true;
            }

            intersection = Vector3.zero;
            return false;
        }
    }
}