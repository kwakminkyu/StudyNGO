using Unity.Netcode;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    [SerializeField] private float _resetYPosition = -3f;

    private Rigidbody _ballRigidbody;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private void Awake()
    {
        _ballRigidbody = GetComponent<Rigidbody>();
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (!IsServer || _resetYPosition >= 0f || transform.position.y > _resetYPosition)
        {
            return;
        }

        ResetBall();
    }

    private void ResetBall()
    {
        if (_ballRigidbody != null)
        {
            _ballRigidbody.linearVelocity = Vector3.zero;
            _ballRigidbody.angularVelocity = Vector3.zero;
            _ballRigidbody.position = _initialPosition;
            _ballRigidbody.rotation = _initialRotation;
            return;
        }

        transform.SetPositionAndRotation(_initialPosition, _initialRotation);
    }
}
