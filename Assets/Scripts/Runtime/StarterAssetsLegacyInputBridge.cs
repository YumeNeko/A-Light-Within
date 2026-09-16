using StarterAssets;
using UnityEngine;

namespace FragmentsOfHer.Player
{
    public class StarterAssetsLegacyInputBridge : MonoBehaviour
    {
        public bool enableKeyboardMouseFallback = true;
        public float mouseLookMultiplier = 1f;

        private StarterAssetsInputs starterInputs;

        private void Awake()
        {
            starterInputs = GetComponent<StarterAssetsInputs>();
        }

        private void Update()
        {
            if (!enableKeyboardMouseFallback || starterInputs == null)
            {
                return;
            }

            // 使用旧输入轴作为兜底，让本项目在没有完整 Input System 配置时也能移动。
            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseLookMultiplier;

            starterInputs.MoveInput(Vector2.ClampMagnitude(moveInput, 1f));
            starterInputs.LookInput(starterInputs.cursorInputForLook ? lookInput : Vector2.zero);
            starterInputs.JumpInput(Input.GetButtonDown("Jump"));
            starterInputs.SprintInput(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
    }
}
