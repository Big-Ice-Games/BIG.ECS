using BIG.ECS;
using TestLibrary.Types;

namespace TestLibrary.Components
{
    public struct Rigidbody2D : IComponent
    {
        public float Mass;
        public float Drag;
        public Vector2 Velocity;

        public Rigidbody2D(float mass, float drag)
        {
            Mass = mass;
            Drag = drag;
            Velocity = Vector2.Zero;
        }
    }
}
