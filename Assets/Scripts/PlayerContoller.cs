using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerContoller : NetworkBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private Rigidbody _playerRigidbody;
    private Vector2 _moveInput;
    private bool _useOppositeCameraView;

    private void Awake()
    {
        _playerRigidbody = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _playerRigidbody.position = transform.position;
            _playerRigidbody.rotation = transform.rotation;
        }

        if (!IsOwner)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera.TryGetComponent(out CameraController cameraController))
        {
            _useOppositeCameraView = OwnerClientId != NetworkManager.ServerClientId;
            cameraController.SetTarget(transform, _useOppositeCameraView);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        Vector3 velocity = _playerRigidbody.linearVelocity;
        velocity.x = _moveInput.x * _moveSpeed;
        velocity.z = _moveInput.y * _moveSpeed;
        _playerRigidbody.linearVelocity = velocity;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner)
        {
            return;
        }

        Vector2 input = context.ReadValue<Vector2>();
        input = GetCameraRelativeInput(input);

        if (IsServer)
        {
            _moveInput = input;
            return;
        }

        SubmitMoveInputRpc(input);
    }

    [Rpc(SendTo.Server)]
    private void SubmitMoveInputRpc(Vector2 input)
    {
        _moveInput = input;
    }

    private Vector2 GetCameraRelativeInput(Vector2 input)
    {
        if (!_useOppositeCameraView)
        {
            return input;
        }

        return -input;
    }
}
