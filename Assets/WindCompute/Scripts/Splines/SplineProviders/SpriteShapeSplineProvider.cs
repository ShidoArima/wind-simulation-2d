using UnityEngine;
using UnityEngine.U2D;
using Spline = UnityEngine.Splines.Spline;

namespace WindCompute.SplineProviders
{
    public class SpriteShapeSplineProvider : BaseSplineProvider
    {
        [SerializeField] private SpriteShapeController _spriteShapeController;

        public override Spline GetSpline()
        {
            return _spriteShapeController.spline.ToUnitySpline();
        }
    }
}