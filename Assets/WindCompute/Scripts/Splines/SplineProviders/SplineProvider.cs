using UnityEngine;
using UnityEngine.Splines;

namespace WindCompute.SplineProviders
{
    public class SplineProvider : BaseSplineProvider
    {
        [SerializeField] private SplineContainer _container;

        public override Spline GetSpline()
        {
            return _container.Spline;
        }
    }
}