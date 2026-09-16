using UnityEngine;

namespace FragmentsOfHer.Clean
{
    public class PlayerRespawnManager : MonoBehaviour
    {
        public Transform player;
        public Transform currentCheckpoint;
        public string currentCheckpointName = "心灯起点";
        public string currentAreaName = "回声庭院";
        public float voidY = -10f;
        public bool allowManualRespawn = true;

        private CharacterController characterController;
        private Rigidbody playerRigidbody;

        public static PlayerRespawnManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (player != null)
            {
                characterController = player.GetComponent<CharacterController>();
                playerRigidbody = player.GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            if (player.position.y < voidY || (allowManualRespawn && Input.GetKeyDown(KeyCode.R)))
            {
                RespawnPlayer();
            }
        }

        public void SetCheckpoint(Transform checkpoint, string checkpointName, string areaName)
        {
            if (checkpoint == null)
            {
                return;
            }

            currentCheckpoint = checkpoint;
            currentCheckpointName = checkpointName;
            currentAreaName = areaName;
        }

        public void RespawnPlayer()
        {
            if (Heartlight.HeartlightDirector.Instance != null)
            {
                Heartlight.HeartlightDirector.Instance.Respawn(true);
                return;
            }
            if (player == null || currentCheckpoint == null)
            {
                return;
            }

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            // 临时关闭 CharacterController，避免直接设置位置时被碰撞系统推开。
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            player.SetPositionAndRotation(currentCheckpoint.position, currentCheckpoint.rotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }
    }
}
