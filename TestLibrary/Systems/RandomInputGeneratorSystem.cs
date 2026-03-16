using System;
using BIG.ECS;
using BIG.ECS.Tests.Components;
using TestLibrary.Types;

namespace TestLibrary.Systems
{
    public sealed class RandomInputGeneratorSystem : ISystem
    {
        private Query _query;
        private readonly Random _random = new Random();
        public void Initialize(EntityManager entityManager)
        {
            _query = new QueryBuilder(entityManager).With<Input>().Resolve();
        }

        public void Update(World world, in float deltaTime)
        {
            foreach (int entityId in _query)
            {
                var entity = world.EntityManager.GetEntity(entityId);
                ref Input input = ref entity.GetComponent<Input>();

                input.Value = new Vector2(
                    (float)(_random.NextDouble() * 2f - 1f),
                    (float)(_random.NextDouble() * 2f - 1f)
                );
            }
        }
    }
}
