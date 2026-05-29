using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class SpawnPlayer : MonoBehaviour
{
    [SerializeField] private Transform[] _spawnPoints;

    private NetworkManager _networkManager;
    private int _nextSpawnIndex;

    private void Awake()
    {
        _networkManager = GetComponent<NetworkManager>();
        _networkManager.NetworkConfig.ConnectionApproval = true;
        _networkManager.ConnectionApprovalCallback = ApproveConnection;
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (_networkManager.ConnectedClientsIds.Count == 0)
        {
            _nextSpawnIndex = 0;
        }

        if (_nextSpawnIndex >= _spawnPoints.Length || _spawnPoints[_nextSpawnIndex] == null)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Reason = "No player spawn point is available.";
            response.Pending = false;
            Debug.LogWarning($"Rejected client {request.ClientNetworkId}: {response.Reason}");
            return;
        }

        Transform spawnPoint = _spawnPoints[_nextSpawnIndex];
        _nextSpawnIndex++;

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = spawnPoint.position;
        response.Rotation = spawnPoint.rotation;
        response.Pending = false;

        Debug.Log($"Approved client {request.ClientNetworkId} at spawn point {spawnPoint.name}: {spawnPoint.position}");
    }
}
