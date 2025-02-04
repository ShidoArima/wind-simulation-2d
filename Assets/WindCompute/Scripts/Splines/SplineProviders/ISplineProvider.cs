using UnityEngine.Splines;

namespace WindCompute.SplineProviders
{
    public interface ISplineProvider
    {
        public Spline GetSpline();
    }
}