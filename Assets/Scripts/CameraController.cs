using System;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;
using Prowl.Runtime;
using Prowl.Vector;
using Voxels;

public class CameraController : MonoBehaviour
{
    public bool EnableMovement = false;
    public float Sensitivity = 0.15f;
    public float MinPitch = -89f;
    public float MaxPitch = 89f;

    private float _yaw;
    private float _pitch;

    public float health = 0.5f;

    public override void OnGui(Paper paper)
    {
        using (paper.Column("col").Enter())
        {
            using (paper.Box("health_bg")
                .BackgroundColor(Color.Red)
                .Width(UnitValue.Pixels(200))
                .Height(UnitValue.Pixels(20))
                .Enter())
            {
                paper.Draw((c, _) =>
                {
                    c.RectFilled(0, 0, 200, 20, Color.Gray);
                    c.RectFilled(0, 0, 200 * health, 20, Color.Green);
                });
            }
        }

        paper.Draw((c, r) =>
        {
            c.CircleFilled(r.Center.X, r.Center.Y, 2, Color.Gray);
        });
    }

    public override void Start()
    {
        Float3 euler = Transform.LocalEulerAngles;
        _pitch = euler.X;
        _yaw = euler.Y;
    }

    public override void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Scene.Physics.Raycast(Transform.Position, Transform.Forward, 100, out var hit))
            {
                var chunk = hit.Rigidbody.GetComponent<MoreOptimizedChunk>();

                var hitPos = hit.Point - (hit.Normal * 0.5f);

                var voxelCoords = new Int3(
                    Maths.FloorToInt(hitPos.X - chunk.Transform.Position.X),
                    Maths.FloorToInt(hitPos.Y - chunk.Transform.Position.Y),
                    Maths.FloorToInt(hitPos.Z - chunk.Transform.Position.Z)
                );

                Debug.Log("Editing block at " + voxelCoords);

                chunk.UpdateBlock(voxelCoords.X, voxelCoords.Y, voxelCoords.Z, 0);
                health += 0.1f;
            }
        }

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
