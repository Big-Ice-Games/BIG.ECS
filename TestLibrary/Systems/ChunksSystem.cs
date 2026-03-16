using System.Buffers;
using BIG.ECS;
using BIG.ECS.Tests.Components;

namespace TestLibrary.Systems
{
    public sealed class ChunksSystem : ISystem
    {
        private readonly Chunks _chunks;
        private Query _playersQuery;
        private Query _entitiesQuery;

        public ChunksSystem(Chunks chunks)
        {
            _chunks = chunks;
        }

        public void Initialize(EntityManager entityManager)
        {
            _playersQuery = new QueryBuilder(entityManager)
                .With<ChunkAffiliation>()
                .With<Transform>()
                .With<PlayerData>()
                .Resolve();

            _entitiesQuery = new QueryBuilder(entityManager)
                .With<ChunkAffiliation>()
                .With<Transform>()
                .Without<PlayerData>() // Removing players from query.
                .Resolve();

        }

        public void Update(World world, in float deltaTime)
        {
            UpdatePlayersChunksAffiliation(world);
            UpdateEntitiesChunksAffiliation(world);
            Synchronize(world);
        }

        private void UpdatePlayersChunksAffiliation(World world)
        {
            foreach (int entityId in _playersQuery)
            {
                var entity = world.EntityManager.GetEntity(entityId);
                ref ChunkAffiliation chunkAffiliation = ref entity.GetComponent<ChunkAffiliation>();
                var transform = entity.GetComponent<Transform>();

                int chunkFromPosition = _chunks.GetChunkIndexFromWorldPosition(transform.Position.X, transform.Position.Y);

                // Assign first chunk to player, if not assigned yet
                if (chunkAffiliation.ChunkId == -1)
                {
                    chunkAffiliation.ChunkId = chunkFromPosition;
                    _chunks.AddPlayerToChunk(entityId, chunkFromPosition);
                    continue;
                }

                if (chunkFromPosition != chunkAffiliation.ChunkId)
                {
                    _chunks.MovePlayerToChunk(entityId, chunkFromPosition);
                    chunkAffiliation.ChunkId = chunkFromPosition;
                }
            }
        }
        private void UpdateEntitiesChunksAffiliation(World world)
        {
            foreach (int entityId in _entitiesQuery)
            {
                var entity = world.EntityManager.GetEntity(entityId);
                ref ChunkAffiliation chunkAffiliation = ref entity.GetComponent<ChunkAffiliation>();
                var transform = entity.GetComponent<Transform>();

                int chunkFromPosition = _chunks.GetChunkIndexFromWorldPosition(transform.Position.X, transform.Position.Y);

                // Assign first chunk to entity, if not assigned yet
                if (chunkAffiliation.ChunkId == -1)
                {
                    chunkAffiliation.ChunkId = chunkFromPosition;
                    _chunks.AddEntityToChunk(entityId, chunkFromPosition);
                    continue;
                }


                if (chunkFromPosition != chunkAffiliation.ChunkId)
                {
                    _chunks.MoveEntityToChunk(entityId, chunkFromPosition);
                    chunkAffiliation.ChunkId = chunkFromPosition;
                }
            }
        }
        private void Synchronize(World world)
        {
            var activeChunks = _chunks.ActiveChunkIndices;
            for (int i = 0; i < activeChunks.Length; i++)
            {
                var activeChunkId = activeChunks[i];
                if (activeChunkId == -1) continue;
                SynchronizeChunk(world, activeChunkId);
            }
        }

        //private readonly EntityUpdateData[] _synchronizationModels = new EntityUpdateData[1024];
        private void SynchronizeChunk(World world, int chunkId)
        {
            int[] playerIdentifierBuffer = ArrayPool<int>.Shared.Rent(1024);
            int[] entityIdentifierBuffer = ArrayPool<int>.Shared.Rent(4096);

            // We can create a pool of EntityUpdateData and share from it.
            // EntityUpdateData synchronizationModels

            try
            {
                var entityCollector = new Chunks.IntegerCollector(entityIdentifierBuffer);
                _chunks.CollectAreaOfInterestEntities(chunkId, ref entityCollector);
                int entityCount = entityCollector.WrittenCount;

                for (int i = 0; i < entityCount; i++)
                {
                    int id = entityIdentifierBuffer[i];
                    // grab entity data and prepare synchronization model for it, e.g.:
                    // _synchronizationModels[i] = new EntityUpdateData(id, GetSynchronizationModel(world, id));
                }

                var playerCollector = new Chunks.IntegerCollector(playerIdentifierBuffer);
                _chunks.CollectChunkPlayers(chunkId, ref playerCollector);
                int playerCount = playerCollector.WrittenCount;

                // Now we have all entities that should be synchronized for the chunk and all players that should receive the synchronization data.
                // ReadOnlySpan<int> players = playerIdentifierBuffer.AsSpan(0, playerCount);
                // PushToAllPlayersAround(in players, in networkRequest);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(playerIdentifierBuffer, clearArray: false);
                ArrayPool<int>.Shared.Return(entityIdentifierBuffer, clearArray: false);
            }
        }
    }
}
