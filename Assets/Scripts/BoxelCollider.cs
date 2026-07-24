using Jitter2.Collision.Shapes;

using Prowl.Runtime;

namespace Voxels;

/// <summary>
/// Collider that attaches an arbitrary set of pre-built Jitter <see cref="RigidBodyShape"/>s (e.g. one
/// <see cref="BoxShape"/> per solid voxel, wrapped in a <see cref="TransformedShape"/>), rather than
/// one <see cref="BoxCollider"/> component per voxel. A single component attaching thousands of shapes
/// to one rigidbody is far cheaper than thousands of MonoBehaviours each with their own attach/detach
/// and per-frame transform tracking.
/// </summary>
public class BoxelCollider : Collider
{
    private RigidBodyShape[] shapes = [];

    /// <summary>
    /// Sets the shapes to attach and rebuilds the collider.
    /// </summary>
    public void Set(RigidBodyShape[] shapes)
    {
        this.shapes = shapes;
        OnValidate();
    }

    public override RigidBodyShape[] CreateShapes() => shapes;
}
