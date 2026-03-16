namespace BIG.ECS.Tests.Components
{
    public struct Acceleration : IComponent
    {
        public float Speed;
        public float RotationSpeed;

        public Acceleration(float speed, float rotationSpeed)
        {
            Speed = speed;
            RotationSpeed = rotationSpeed;
        }
    }
}
