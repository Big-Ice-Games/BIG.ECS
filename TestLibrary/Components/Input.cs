using TestLibrary.Types;

namespace BIG.ECS.Tests.Components
{
    public struct Input : IComponent
    {
        public Vector2 Value;

        public Input(Vector2 value)
        {
            Value = value;
        }
    }
}
