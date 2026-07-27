using System;

using Prowl.Runtime;
using Prowl.Vector;

public class CameraController : MonoBehaviour
{
    public bool EnableMovement = false;
    public float Sensitivity = 0.15f;
    public float MinPitch = -89f;
    public float MaxPitch = 89f;

    private float _yaw;
    private float _pitch;

    public override void Start()
    {
        Float3 euler = Transform.LocalEulerAngles;
        _pitch = euler.X;
        _yaw = euler.Y;
    }

    public override void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Input.UnlockCursor();
        }
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            Input.LockCursor();
        }

        if (!Input.CursorLocked) return;
        // if (!Input.GetMouseButton(0)) return;

        // Look rotation
        Float2 delta = Input.MouseDelta;
        _yaw += delta.X * Sensitivity;
        _pitch = Math.Clamp(_pitch + delta.Y * Sensitivity, MinPitch, MaxPitch);
        Transform.LocalEulerAngles = new Float3(_pitch, _yaw, 0f);
        
        if (!EnableMovement) return;

        // movement
        float forward = 0;
        if (Input.GetKeyDown(KeyCode.W)) forward = 1;
        if (Input.GetKeyDown(KeyCode.S)) forward = -1;
        float right = 0;
        if (Input.GetKeyDown(KeyCode.D)) right = 1;
        if (Input.GetKeyDown(KeyCode.A)) right = -1;
        float up = 0;
        if (Input.GetKeyDown(KeyCode.E)) right = 1;
        if (Input.GetKeyDown(KeyCode.Q)) right = -1;
        Transform.Position += Transform.Forward * forward * Time.DeltaTime;
        Transform.Position += Transform.Right * right * Time.DeltaTime;
        Transform.Position += Transform.Up * up * Time.DeltaTime;
    }
}
