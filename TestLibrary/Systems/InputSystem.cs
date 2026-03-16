using BIG.ECS;
using System;
using BIG.ECS.Tests.Components;
using TestLibrary.Components;

namespace TestLibrary.Systems
{
    public sealed class InputSystem : ISystem
    {
        private Query _query;
        public void Initialize(EntityManager entityManager)
        {
            _query = new QueryBuilder(entityManager).With<Acceleration>().With<Input>().With<Transform>().Resolve();
        }

        public void Update(World world, in float deltaTime)
        {
            foreach (int entityId in _query)
            {
                var entity = world.EntityManager.GetEntity(entityId);
                Input input = entity.GetComponent<Input>();
                Acceleration acceleration = entity.GetComponent<Acceleration>();
                ref Rigidbody2D rigidbody = ref entity.GetComponent<Rigidbody2D>();

                rigidbody.Velocity = input.Value * acceleration.Speed * deltaTime;
            }
        }
    }
}
