using System;
using System.Runtime.CompilerServices;
using BIG.ECS;
using BIG.ECS.Tests.Components;
using TestLibrary.Components;
using TestLibrary.Types;

namespace TestLibrary.Systems
{
    public sealed class OneUnifiedOptimizedSystem : ISystem
    {
        private Query _query;
        private readonly Random _random = new Random();
        private readonly MapSettings _settings;
        public OneUnifiedOptimizedSystem(MapSettings settings)
        {
            _settings = settings;
        }

        public void Initialize(EntityManager entityManager)
        {
            _query = new QueryBuilder(entityManager).With<Acceleration>().With<Input>().With<Transform>().Resolve();
        }

        public void Update(World world, in float deltaTime)
        {
            foreach (int entityId in _query)
            {
                var entity = world.EntityManager.GetEntity(entityId);
                var acceleration = entity.GetComponent<Acceleration>();
                ref Input input = ref entity.GetComponent<Input>();
                ref Transform transform = ref entity.GetComponent<Transform>();
                ref Rigidbody2D rigidbody = ref entity.GetComponent<Rigidbody2D>();

                input.Value = new Vector2(
                    (float)(_random.NextDouble() * 2f - 1f),
                    (float)(_random.NextDouble() * 2f - 1f)
                );

                rigidbody.Velocity = input.Value * acceleration.Speed * deltaTime;

                var position = transform.Position;
                position += rigidbody.Velocity;

                SecureMapBorders(ref position);

                transform.Position = position;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
