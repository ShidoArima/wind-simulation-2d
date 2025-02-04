using UnityEngine;
using UnityEngine.Splines;

namespace WindCompute.SplineProviders
{
    public abstract class BaseSplineProvider : MonoBehaviour, ISplineProvider
    {
        public abstract Spline GetSpline();
    }
}