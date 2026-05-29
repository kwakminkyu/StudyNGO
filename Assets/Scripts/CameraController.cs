using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector3 _backViewOffset = new Vector3(0f, 4f, -8f);
    [SerializeField] private Vector3 _lookAtOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float _followSpeed = 10f;

    private Transform _target;
    private Quaternion _viewRotationOffset = Quaternion.identity;

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        Quaternion viewRotation = _target.rotation * _viewRotationOffset;
        Vector3 targetPosition = _target.position + viewRotation * _backViewOffset;
        Vector3 lookAtPosition = _target.position + viewRotation * _lookAtOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            _followSpeed * Time.deltaTime);

        transform.LookAt(lookAtPosition);
    }

    public void SetTarget(Transform target, bool useOppositeView = false)
    {
        _target = target;
        _viewRotationOffset = useOppositeView
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.identity;
    }
}
