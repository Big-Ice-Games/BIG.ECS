using BIG.ECS;
using BIG.ECS.Tests.Components;
using TestLibrary.Components;
using TestLibrary.Types;

namespace TestLibrary.Systems
{
    public sealed class AccelerationSystem : ISystem
    {
        private Query _query;
        private readonly MapSettings _settings;
        public AccelerationSystem(MapSettings settings)
        {
            _settings = settings;
        }

        public void Initialize(EntityManager entityManager)
        {
            _query = new QueryBuilder(entityManager).With<Acceleration>().With<Input>().With<Rigidbody2D>().With<Transform>().Resolve();
        }

        public void Update(World world, in float deltaTime)
        {
            foreach (int entityId in _query)
            {
                var entity = world.EntityManager.GetEntity(entityId);
                Rigidbody2D rigidbody = entity.GetComponent<Rigidbody2D>();
                ref Transform transform = ref entity.GetComponent<Transform>();
                
                var position = transform.Position;
                position += rigidbody.Velocity;

                SecureMapBorders(ref position);

                transform.Position = position;
            }
        }

        private void SecureMapBorders(ref Vector2 position)
        {
            if (position.X > _settings.MapWidth)
                position.X = _settings.MapWidth;
            else if (position.X < 0)
                position.X = 0;

            if (position.Y > _settings.MapHeight)
                position.Y = _settings.MapHeight;
            else if (position.Y < 0)
                position.Y = 0;
        }
    }
}
