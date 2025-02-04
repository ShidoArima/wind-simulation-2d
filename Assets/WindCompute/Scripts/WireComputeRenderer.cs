using System.Runtime.InteropServices;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace WindCompute
{
    [ExecuteInEditMode]
    public class WireComputeRenderer : MonoBehaviour
    {
        [SerializeField] private ComputeShader _wireCompute;
        [SerializeField] private Material _wireMaterial;
        [SerializeField] private Texture _windTexture;
        [SerializeField] private Gradient _color;

        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField] private bool _endFixed = true;
        [SerializeField] private int _segmentsCount;
        [SerializeField] private int _iterations = 1;
        [SerializeField] private float _width = 1;

        [SerializeField] private Vector2 _gravity;
        [SerializeField] private float _windForce = 1;
        [SerializeField] private float _stiffness = 1;
        [SerializeField] private Bounds _bounds;

        [Header("Bake")] 
        [SerializeField] private float _restDistance;
        [SerializeField] private Vector3[] _bakedPositions;
        [SerializeField] private bool _baked;

        private Point[] _points;
        private Edge[] _edges;

        private Vector3[] _vertices;
        private int[] _tris;

        private int _groupsCount;
        private Vector3 _startPosition;
        private Vector3 _endPosition;

        private ComputeBuffer _pointBuffer, _edgeBuffer, _forcesBuffer;
        GraphicsBuffer _verticesBuffer, _trisBuffer;
        private MaterialPropertyBlock _propertyBlock;

        public Vector3 StartPosition => _startPoint.position;
        public Vector3 EndPosition => _endPoint.position;

        private bool _isVisible;
        private readonly Plane[] _cameraPlanes = new Plane[6];


        private static readonly int
            ForcesId = Shader.PropertyToID("_ForcesBuffer"),
            PointsId = Shader.PropertyToID("_PointBuffer"),
            EdgesId = Shader.PropertyToID("_EdgeBuffer"),
            VerticesId = Shader.PropertyToID("_VerticesBuffer"),
            EffectorMapId = Shader.PropertyToID("_EffectorMap"),
            PointsCountId = Shader.PropertyToID("_PointCount"),
            EdgeCountId = Shader.PropertyToID("_EdgeCount"),
            GravityId = Shader.PropertyToID("_Gravity"),
            DeltaTimeId = Shader.PropertyToID("_DeltaTime"),
            StiffnessId = Shader.PropertyToID("_Stiffness"),
            WidthId = Shader.PropertyToID("_Width"),
            ColorId = Shader.PropertyToID("_Color"),
            EffectorForceId = Shader.PropertyToID("_EffectorForce"),
            WorldPositionId = Shader.PropertyToID("_WorldPosition");

        private static readonly int PointsCount = Shader.PropertyToID("_PointsCount");

        public void SetStartPoint(Vector3 position)
        {
            _startPoint.position = position;
        }

        public void SetEndPoint(Vector3 position)
        {
            _endPoint.position = position;
        }

        [Button("Bake")]
        public void Bake()
        {
            _baked = true;

            _pointBuffer.GetData(_points);
            _bakedPositions = new Vector3[_points.Length];
            for (int i = 0; i < _points.Length; i++)
            {
                _bakedPositions[i] = _points[i].position;
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        [Button("Reset")]
        public void ResetBake()
        {
            _baked = false;
            _bakedPositions = new Vector3[] { };
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        private void OnEnable()
        {
            GeneratePoints();

            if (Application.isPlaying)
            {
                UnityEngine.Camera.onPreCull += OnPreCullAny;
            }
            else
            {
                Simulate();
            }
        }

        private void OnDisable()
        {
            Dispose();

            if (Application.isPlaying)
            {
                UnityEngine.Camera.onPreCull -= OnPreCullAny;
            }
        }

        private void Dispose()
        {
            _pointBuffer?.Release();
            _edgeBuffer?.Release();
            _forcesBuffer?.Release();
            _verticesBuffer?.Release();
            _trisBuffer?.Release();

            _pointBuffer = null;
            _edgeBuffer = null;
            _forcesBuffer = null;
            _verticesBuffer = null;
            _trisBuffer = null;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Vector2.Distance(_startPosition, _startPoint.position) > 0.01f || Vector2.Distance(_endPosition, _endPoint.position) > 0.01f)
            {
                GeneratePoints();
            }
#endif

            Render();
        }

        private void FixedUpdate()
        {
            if (!_isVisible)
                return;

            Simulate();
            _isVisible = false;
        }

        private void Simulate()
        {
            SetProperties();
            AccumulateForces();

            Step();
            Solve();
            Populate();
        }

        private void SetProperties()
        {
            _wireCompute.SetFloat(EffectorForceId, _windForce);
            _wireCompute.SetInt(PointsCountId, _points.Length);
            _wireCompute.SetInt(EdgeCountId, _edges.Length);
            _wireCompute.SetFloat(StiffnessId, _stiffness);
            _wireCompute.SetFloat(WidthId, _width);
            _wireCompute.SetVector(WorldPositionId, transform.position);

            _wireCompute.SetFloat(DeltaTimeId, Time.fixedDeltaTime);
            _wireCompute.SetVector(GravityId, _gravity);
        }

        private void AccumulateForces()
        {
            _wireCompute.SetBuffer(0, ForcesId, _forcesBuffer);
            _wireCompute.SetBuffer(0, PointsId, _pointBuffer);
            _wireCompute.SetTexture(0, EffectorMapId, _windTexture);

            _wireCompute.Dispatch(0, _groupsCount, 1, 1);
        }

        private void Step()
        {
            _wireCompute.SetBuffer(1, ForcesId, _forcesBuffer);
            _wireCompute.SetBuffer(1, PointsId, _pointBuffer);

            _wireCompute.Dispatch(1, _groupsCount, 1, 1);
        }

        private void Solve()
        {
            _wireCompute.SetBuffer(2, PointsId, _pointBuffer);
            _wireCompute.SetBuffer(2, EdgesId, _edgeBuffer);

            int iterations = Mathf.CeilToInt(_iterations / 8f);
            _wireCompute.Dispatch(2, iterations, 1, 1);
        }

        private void Populate()
        {
            _wireCompute.SetBuffer(3, VerticesId, _verticesBuffer);
            _wireCompute.SetBuffer(3, PointsId, _pointBuffer);
            _wireCompute.SetBuffer(3, EdgesId, _edgeBuffer);

            var count = Mathf.CeilToInt(_edgeBuffer.count / 8f);

            _wireCompute.Dispatch(3, count, 1, 1);
        }

        private void Render()
        {
            RenderParams renderParams = new RenderParams(_wireMaterial)
            {
                worldBounds = _bounds,
                matProps = _propertyBlock
            };

            renderParams.matProps.SetBuffer(VerticesId, _verticesBuffer);
            renderParams.matProps.SetFloat(PointsCount, _points.Length);
            renderParams.matProps.SetColor(ColorId, _color.Evaluate(0));

            Graphics.RenderPrimitivesIndexed(renderParams, MeshTopology.Triangles, _trisBuffer, _trisBuffer.count);
        }

        [Button("Generate")]
        private void GeneratePoints()
        {
            if (_baked)
            {
                var length = _bakedPositions.Length;
                _points = new Point[length];

                _vertices = new Vector3[_points.Length * 2];
                _tris = GetTris(_points.Length, 0);
                _bounds = new Bounds(transform.position, Vector3.zero);

                _startPosition = _bakedPositions[0];
                _endPosition = _bakedPositions[^1];

                for (int i = 0; i < length; i++)
                {
                    bool isStable = i == 0 || (i == length - 1 && _endFixed);
                    _points[i] = new Point(_bakedPositions[i], isStable);

                    var index = i * 2;
                    _vertices[index] = _points[i].position;
                    _vertices[index + 1] = _points[i].position;
                    _bounds.Encapsulate(new Vector3(_points[i].position.x, _points[i].position.y, transform.position.z));
                }
            }
            else
            {
                _startPosition = _startPoint.position;
                _endPosition = _endPoint.position;
                var totalDistance = Vector2.Distance(_startPosition, _endPosition);

                _restDistance = totalDistance / (_segmentsCount + 1);
                _points = new Point[2 + _segmentsCount];

                _vertices = new Vector3[_points.Length * 2];
                _tris = GetTris(_points.Length, 0);
                _bounds = new Bounds(transform.position, Vector3.zero);

                _points[0] = new Point(_startPosition, true);
                _points[^1] = new Point(_endPosition, _endFixed);

                for (int i = 1; i < _points.Length - 1; i++)
                {
                    float t = (float) i / (_points.Length - 1);
                    var position = Vector2.Lerp(_startPoint.position, _endPoint.position, t);
                    _points[i] = new Point(position, false);

                    var index = i * 2;
                    _vertices[index] = position;
                    _vertices[index + 1] = position;
                    _bounds.Encapsulate(new Vector3(position.x, position.y, transform.position.z));
                }
            }

            _edges = new Edge[_points.Length - 1];

            for (int i = 0; i < _points.Length - 1; i++)
            {
                _edges[i] = new Edge(i, i + 1, _restDistance);
            }

            _groupsCount = Mathf.CeilToInt(_points.Length / 8f);

            Dispose();
            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _pointBuffer = new ComputeBuffer(_points.Length, 4 * 5);
            _edgeBuffer = new ComputeBuffer(_edges.Length, 4 * 3);
            _forcesBuffer = new ComputeBuffer(_points.Length, 4 * 2);

            _trisBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _tris.Length, sizeof(int));
            _verticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _vertices.Length, 3 * sizeof(float));

            _verticesBuffer.SetData(_vertices);
            _trisBuffer.SetData(_tris);
            _pointBuffer.SetData(_points);
            _edgeBuffer.SetData(_edges);
        }

        private int[] GetTris(int size, int offset)
        {
            int[] tris = new int[6 * (size - 1)];
            for (int i = 0; i < size - 1; i++)
            {
                int vertOffset = 2 * i + offset;
                int triOffset = i * 6;

                tris[triOffset] = vertOffset;
                tris[triOffset + 1] = vertOffset + 2;
                tris[triOffset + 2] = vertOffset + 1;

                tris[triOffset + 3] = vertOffset + 1;
                tris[triOffset + 4] = vertOffset + 2;
                tris[triOffset + 5] = vertOffset + 3;
            }

            return tris;
        }

        private void OnPreCullAny(UnityEngine.Camera currentCamera)
        {
            GeometryUtility.CalculateFrustumPlanes(currentCamera, _cameraPlanes);
            if (GeometryUtility.TestPlanesAABB(_cameraPlanes, _bounds))
            {
                _isVisible = true;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public Vector2 position;
            public Vector2 oldPosition;
            public int stable;

            public Point(Vector2 position, bool stable)
            {
                this.position = position;
                oldPosition = position;
                this.stable = stable ? 1 : 0;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Edge
        {
            public int a;
            public int b;
            public float length;

            public Edge(int a, int b, float length)
            {
                this.a = a;
                this.b = b;
                this.length = length;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying && isActiveAndEnabled)
                Simulate();

            // for (int i = 0; i < _positions.Length - 1; i++)
            // {
            //     Gizmos.color = Color.green;
            //     Gizmos.DrawLine(_positions[i], _positions[i + 1]);
            // }
        }
    }
}