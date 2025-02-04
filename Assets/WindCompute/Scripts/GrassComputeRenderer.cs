using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace WindCompute
{
    [ExecuteInEditMode]
    public class GrassComputeRenderer : MonoBehaviour
    {
        [SerializeField] private ComputeShader _grassCompute;
        [SerializeField] private Texture _effectorTexture;
        [SerializeField] private Vector3 _scale;
        [SerializeField] private Vector4 _heightRange;
        [SerializeField] private Vector4 _windParams;

        [SerializeField] private Mesh _meshInstance;
        [SerializeField] private Material _grassMaterial;

        [SerializeField] private SplineCache _splineCache;

        private MaterialPropertyBlock _propertyBlock;

        private ComputeBuffer
            _positionBuffer,
            _effectorBuffer,
            _windBuffer,
            _transformBuffer,
            _argsBuffer;

        private static readonly int
            PositionsId = Shader.PropertyToID("_GrassPositionBuffer"),
            TransformId = Shader.PropertyToID("_GrassMatrixBuffer"),
            ObjectToWorldId = Shader.PropertyToID("_ObjectToWorld"),
            EffectorMapId = Shader.PropertyToID("_EffectorMap"),
            WindId = Shader.PropertyToID("_WindBuffer"),
            DimensionId = Shader.PropertyToID("_Dimension"),
            HeightRangeId = Shader.PropertyToID("_HeightRange"),
            WindParams = Shader.PropertyToID("_WindParams"),
            ScaleId = Shader.PropertyToID("_Scale"),
            TimeId = Shader.PropertyToID("_Time");

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (BuildPipeline.isBuildingPlayer)
                return;
#endif

            InitializeBuffers();

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (BuildPipeline.isBuildingPlayer)
                return;
#endif

            Clear();
        }

        private void Update()
        {
            Compute();
            DrawIndirect();
        }

        private void Clear()
        {
            _positionBuffer.Release();
            _transformBuffer.Release();
            _argsBuffer.Release();
            _windBuffer.Release();

            _positionBuffer = null;
            _transformBuffer = null;
            _argsBuffer = null;
            _windBuffer = null;

            _propertyBlock.Clear();
        }

        [Button("Refresh")]
        private void Refresh()
        {
            InitializeBuffers();
            Compute();
            DrawIndirect();
        }

        private void InitializeBuffers()
        {
            _positionBuffer = new ComputeBuffer(_splineCache.Count, 4 * 3);
            _transformBuffer = new ComputeBuffer(_splineCache.Count, 4 * 16);
            _windBuffer = new ComputeBuffer(_splineCache.Count, 4);
            _argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

            uint[] args = {0, 0, 0, 0, 0};
            args[0] = _meshInstance.GetIndexCount(0);
            args[1] = (uint) _transformBuffer.count;
            args[2] = _meshInstance.GetIndexStart(0);
            args[3] = _meshInstance.GetBaseVertex(0);
            _argsBuffer.SetData(args);

            _positionBuffer.SetData(_splineCache.Positions);
            _windBuffer.SetData(new float[_splineCache.Count]);
        }

        private void Compute()
        {
            _grassCompute.SetBuffer(0, PositionsId, _positionBuffer);
            _grassCompute.SetBuffer(0, TransformId, _transformBuffer);
            _grassCompute.SetBuffer(0, WindId, _windBuffer);
            _grassCompute.SetTexture(0, EffectorMapId, _effectorTexture);
            _grassCompute.SetMatrix(ObjectToWorldId, transform.localToWorldMatrix);

            _grassCompute.SetInt(DimensionId, _splineCache.Count);

            _grassCompute.SetFloat(TimeId, Time.time);
            _grassCompute.SetVector(ScaleId, _scale);
            _grassCompute.SetVector(WindParams, _windParams);
            _grassCompute.SetVector(HeightRangeId, _heightRange);

            int groups = Mathf.CeilToInt(_splineCache.Count / 64f);
            _grassCompute.Dispatch(0, groups, 1, 1);
        }

        private void DrawIndirect()
        {
            _propertyBlock.SetBuffer(TransformId, _transformBuffer);
            _propertyBlock.SetBuffer(WindId, _windBuffer);
            Profiler.BeginSample("Draw Grass");

            var min = transform.TransformPoint(_splineCache.Bounds.min);
            var max = transform.TransformPoint(_splineCache.Bounds.max);
            var size = max - min;

            var bounds = new Bounds(min + size * 0.5f, new Vector3(size.x, size.y, 1));
            Graphics.DrawMeshInstancedIndirect(_meshInstance, 0, _grassMaterial, bounds, _argsBuffer, 0, _propertyBlock);
            Profiler.EndSample();
        }
    }
}