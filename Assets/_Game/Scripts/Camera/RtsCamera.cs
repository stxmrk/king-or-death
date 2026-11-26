using UnityEngine;

namespace KingOrDeath
{
    public class RtsCamera : MonoBehaviour
    {
        public Vector3 focus = new Vector3(-4, 0, 2);
        public float zoom = 34,
            yaw = 0,
            pitch = 55;

        void LateUpdate()
        {
            if (PauseMenu.IsOpen)
                return;
            float x = Input.GetAxisRaw("Horizontal"),
                z = Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.Q))
                yaw -= 45 * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.E))
                yaw += 45 * Time.unscaledDeltaTime;
            if (Input.GetMouseButton(2))
            {
                x = -Input.GetAxis("Mouse X") * 3;
                z = -Input.GetAxis("Mouse Y") * 3;
            }
            var rot = Quaternion.Euler(0, yaw, 0);
            focus += rot * new Vector3(x, 0, z) * Time.unscaledDeltaTime * zoom * .7f;
            focus.x = Mathf.Clamp(focus.x, -55, 55);
            focus.z = Mathf.Clamp(focus.z, -39, 39);
            if (!Hud.OverUI())
                zoom = Mathf.Clamp(zoom - Input.mouseScrollDelta.y * 2, 15, 62);
            if (Input.GetKeyDown(KeyCode.Home))
            {
                focus = new Vector3(-22, 0, 0);
                yaw = 0;
                zoom = 38;
            }
            if (Input.GetKeyDown(KeyCode.F1))
            {
                focus = new Vector3(-6, 0, 4);
                yaw = 0;
                zoom = 43;
            }
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = focus - transform.forward * 85;
            GetComponent<UnityEngine.Camera>().orthographicSize = zoom;
        }
    }
}
