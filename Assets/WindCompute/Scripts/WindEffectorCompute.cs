using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace WindCompute
{
    [ExecuteInEditMode]
    public class WindEffectorCompute : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private RenderTexture _windTexture;
        [SerializeField] private RenderTexture _displacementTexture;
        [SerializeField] private ComputeShader _windCompute;
        [SerializeField] private int _maxEffectorSize;
        
        [InfoBox("Wind Params " +
                 "x - Speed " +
                 "y - Frequency " +
                 "z - Amplitude " +
                 "w - Displacement Power")]
        [SerializeField]
        private Vector4 _windParams1;
        
        [InfoBox("Wind Params " +
                 "x - Damping " +
                 "y - Turbulence Frequency " +
                 "z - Turbulence Size " +
                 "w - Max Displacement")]
        [SerializeField]
        private Vector4 _windParams2;

        public Vector4 WindParams1
        {
            get => _windParams1;
            set => _windParams1 = value;
        }

        public Vector4 WindParams2
        {
            get => _windParams2;
            set => _windParams2 = value;
        }

        [SerializeField] private List<TargetEffector> _targetEffectors;

        private Vector4[] _bufferEffectors;

        private static readonly int
            EffectorMatrixId = Shader.PropertyToID("_EffectorMatrix"),
            EffectorMatrixInverseId = Shader.PropertyToID("_EffectorInverseMatrix"),
            EffectorBufferId = Shader.PropertyToID("_EffectorBuffer"),
            EffectorSizeId = Shader.PropertyToID("_EffectorSize"),
            WindMapId = Shader.PropertyToID("_WindMap"),
            DisplacementMapId = Shader.PropertyToID("_DisplacementMap"),
            DimensionId = Shader.PropertyToID("_Dimension"),
            WindParams1Id = Shader.PropertyToID("_WindParams1"),
            WindParams2Id = Shader.PropertyToID("_WindParams2"),
            TimeId = Shader.PropertyToID("_Time");

        private ComputeBuffer
            _effectorBuffer,
            _windBuffer;

        private void OnEnable()
        {
            _bufferEffectors = new Vector4[_maxEffectorSize];
            _effectorBuffer = new ComputeBuffer(_maxEffectorSize, 4 * 4);
        }

        private void OnDisable()
        {
            _effectorBuffer.Release();
            _effectorBuffer = null;
        }

        public void AddEffector(Transform target, float radius)
        {
            _targetEffectors.Add(new TargetEffector(target, radius));
        }

        public void RemoveEffector(Transform target)
        {
            for (int i = _targetEffectors.Count - 1; i >= 0; i--)
            {
                var effector = _targetEffectors[i];

                if (effector.target == target)
                {
                    _targetEffectors.RemoveAt(i);
                    return;
                }
            }
        }

        private void Update()
        {
            var viewportMatrix = GetViewportMatrix();
            Shader.SetGlobalMatrix(EffectorMatrixId, viewportMatrix);

            var activeEffectors = UpdateEffectors(viewportMatrix);
            var dimension = _windTexture.width;

            _effectorBuffer.SetData(_bufferEffectors, 0, 0, activeEffectors);

            _windCompute.SetBuffer(0, EffectorBufferId, _effectorBuffer);
            _windCompute.SetTexture(0, WindMapId, _windTexture);
            _windCompute.SetTexture(0, DisplacementMapId, _displacementTexture);
            _windCompute.SetVector(WindParams1Id, _windParams1);
            _windCompute.SetVector(WindParams2Id, _windParams2);

            _windCompute.SetInt(DimensionId, dimension);
            _windCompute.SetInt(EffectorSizeId, activeEffectors);
            _windCompute.SetVector(TimeId, new Vector4(Time.time, Time.deltaTime));
            _windCompute.SetMatrix(EffectorMatrixInverseId, viewportMatrix.inverse);

            int groups = Mathf.CeilToInt(dimension / 8f);
            _windCompute.Dispatch(0, groups, groups, 1);
        }

        private int UpdateEffectors(Matrix4x4 viewportMatrix)
        {
            int effectorLenght = _targetEffectors.Count;
            int activeEffectors = 0;
            for (int i = 0; i < effectorLenght; i++)
            {
                if (activeEffectors >= _maxEffectorSize)
                    return activeEffectors;

                var effector = _targetEffectors[i];
                var position = effector.target.position;
                var effectorPosition = viewportMatrix * new Vector4(position.x, position.y, position.z, 1);
                if (effectorPosition.x > 1 || effectorPosition.x < 0 || effectorPosition.y > 1 || effectorPosition.y < 0)
                    continue;

                Vector2 radius = viewportMatrix * new Vector4(effector.radius, effector.radius, effector.radius, 0);
                var effectorRadius = radius.magnitude;

                _bufferEffectors[activeEffectors] = new Vector4(effectorPosition.x, effectorPosition.y, effectorPosition.z, effectorRadius);
                activeEffectors++;
            }

            return activeEffectors;
        }

        private Matrix4x4 GetViewportMatrix()
        {
            Matrix4x4 vp = _camera.projectionMatrix * _camera.worldToCameraMatrix;
            var w = vp.m33;

            Matrix4x4 ndcMatrix = new Matrix4x4(
                new Vector4(1 / w, 0, 0, 0),
                new Vector4(0, 1 / w, 0, 0),
                new Vector4(0, 0, 1 / w, 0),
                new Vector4(0, 0, 0, 1f / w));

            Matrix4x4 correctionMatrix = new Matrix4x4(
                new Vector4(0.5f, 0, 0, 0),
                new Vector4(0, 0.5f, 0, 0),
                new Vector4(0, 0, 0.5f, 0),
                new Vector4(0.5f, 0.5f, 0.5f, 1f));

            return correctionMatrix * ndcMatrix * vp;
        }
    }

    [Serializable]
    public class TargetEffector
    {
        public Transform target;
        public float radius;

        public TargetEffector(Transform target, float radius)
        {
            this.target = target;
            this.radius = radius;
        }
    }
}