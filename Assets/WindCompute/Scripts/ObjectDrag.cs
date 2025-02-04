using UnityEngine;
using UnityEngine.InputSystem;

namespace WindCompute
{
    public class ObjectDrag : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private Vector3 _offset;
        private bool _isDragging;

        private void OnMouseDown()
        {
            _offset = transform.position - GetPointerWorldPosition();
            _isDragging = true;
        }

        private void OnMouseUp()
        {
            _isDragging = false;
        }

        private void Update()
        {
            if (!_isDragging)
                return;

            var pointerPos = GetPointerWorldPosition();
            transform.position = pointerPos + _offset;
        }

        private Vector3 GetPointerWorldPosition()
        {
            var pointerScreen = Mouse.current.position.value;
            var pointerWorld = _camera.ScreenToWorldPoint(pointerScreen);
            return pointerWorld;
        }
    }
}