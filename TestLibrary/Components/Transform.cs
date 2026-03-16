using TestLibrary.Types;

namespace BIG.ECS.Tests.Components
{
    public struct Transform : IComponent
    {
        public Vector2 Position;
        public float RotationDegrees;

        public Transform(Vector2 position)
        {
            Position = position;
            RotationDegrees = 0;
        }
    }
}
