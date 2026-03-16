using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestLibrary.Types;

public sealed class Chunks : IDisposable
{
    private const int InvalidIndex = -1;
    private const int LocalCapacity = 32;
    private const int PooledBlockCapacity = 1024;
    private const int AreaOfInterestNeighborCount = 9;

    private readonly float _inverseChunkSize;

    public int MapWidth { get; }
    public int MapHeight { get; }
    public int ChunkSize { get; }

    public int ChunkGridWidth { get; }
    public int ChunkGridHeight { get; }
    public int ChunkCount { get; }

    public ref struct IntegerCollector
    {
        private Span<int> _destination;

        public int WrittenCount { get; private set; }

        public IntegerCollector(Span<int> destination)
        {
            _destination = destination;
            WrittenCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value)
        {
            if ((uint)WrittenCount >= (uint)_destination.Length)
            {
                throw new InvalidOperationException("IntegerCollector destination span is full.");
            }

            _destination[WrittenCount] = value;
            WrittenCount++;
        }
    }

    private sealed class ActiveChunkSet
    {
        private readonly int[] _activeChunkIndices;
        private readonly int[] _activeChunkArrayIndexByChunkIndex;

        public int ActiveChunkCount { get; private set; }

        public ActiveChunkSet(int chunkCount)
        {
            _activeChunkIndices = new int[chunkCount];
            _activeChunkArrayIndexByChunkIndex = new int[chunkCount];

            for (int i = 0; i < chunkCount; i++)
            {
                _activeChunkArrayIndexByChunkIndex[i] = InvalidIndex;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Activate(int chunkIndex)
        {
            if (_activeChunkArrayIndexByChunkIndex[chunkIndex] != InvalidIndex)
            {
                return;
            }

            int writeIndex = ActiveChunkCount;
            _activeChunkIndices[writeIndex] = chunkIndex;
            _activeChunkArrayIndexByChunkIndex[chunkIndex] = writeIndex;
            ActiveChunkCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deactivate(int chunkIndex)
        {
            int removeIndex = _activeChunkArrayIndexByChunkIndex[chunkIndex];
            if (removeIndex == InvalidIndex)
            {
                return;
            }

            int lastIndex = ActiveChunkCount - 1;
            int lastChunkIndex = _activeChunkIndices[lastIndex];

            _activeChunkIndices[removeIndex] = lastChunkIndex;
            _activeChunkArrayIndexByChunkIndex[lastChunkIndex] = removeIndex;

            _activeChunkArrayIndexByChunkIndex[chunkIndex] = InvalidIndex;
            ActiveChunkCount--;
        }

        public ReadOnlySpan<int> AsSpan()
        {
            return _activeChunkIndices.AsSpan(0, ActiveChunkCount);
        }
    }

    private sealed class LargeMemoryBlockPool : IDisposable
    {
        private readonly int _blockCount;
        private readonly int[] _nextFreeBlockIndex;

        private int[] _blockData;
        private int _firstFreeBlockIndex;

        public LargeMemoryBlockPool(int blockCount)
        {
            if (blockCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockCount));
            }

            _blockCount = blockCount;
            _blockData = ArrayPool<int>.Shared.Rent(blockCount * PooledBlockCapacity);
            _nextFreeBlockIndex = new int[blockCount];

            for (int blockIndex = 0; blockIndex < blockCount - 1; blockIndex++)
            {
                _nextFreeBlockIndex[blockIndex] = blockIndex + 1;
            }

            _nextFreeBlockIndex[blockCount - 1] = InvalidIndex;
            _firstFreeBlockIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RentBlockIndex()
        {
            if (_firstFreeBlockIndex == InvalidIndex)
            {
                throw new InvalidOperationException("LargeMemoryBlockPool is exhausted.");
            }

            int rentedBlockIndex = _firstFreeBlockIndex;
            _firstFreeBlockIndex = _nextFreeBlockIndex[rentedBlockIndex];
            _nextFreeBlockIndex[rentedBlockIndex] = InvalidIndex;
            return rentedBlockIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReturnBlockIndex(int blockIndex)
        {
            _nextFreeBlockIndex[blockIndex] = _firstFreeBlockIndex;
            _firstFreeBlockIndex = blockIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<int> GetBlockSpan(int blockIndex)
        {
            return _blockData.AsSpan(blockIndex * PooledBlockCapacity, PooledBlockCapacity);
        }

        public void Dispose()
        {
            if (_blockData != null)
            {
                ArrayPool<int>.Shared.Return(_blockData, clearArray: false);
                _blockData = null!;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServerChunk
    {
        public int ChunkGridX;
        public int ChunkGridY;

        public int LocalPlayerCount;
        public int PooledPlayerCount;

        public int LocalEntityCount;
        public int PooledEntityCount;

        public int PooledPlayersBlockIndex;
        public int PooledEntitiesBlockIndex;

        public Buffer32 LocalPlayers;
        public Buffer32 LocalEntities;

        public int TotalPlayerCount => LocalPlayerCount + PooledPlayerCount;
        public int TotalEntityCount => LocalEntityCount + PooledEntityCount;

        public int Count => TotalPlayerCount + TotalEntityCount;

        public void Initialize(int chunkGridX, int chunkGridY)
        {
            ChunkGridX = chunkGridX;
            ChunkGridY = chunkGridY;

            LocalPlayerCount = 0;
            PooledPlayerCount = 0;

            LocalEntityCount = 0;
            PooledEntityCount = 0;

            PooledPlayersBlockIndex = InvalidIndex;
            PooledEntitiesBlockIndex = InvalidIndex;
        }
    }

    private readonly ActiveChunkSet _activeChunkSet;
    private readonly LargeMemoryBlockPool _largeMemoryBlockPool;
    private readonly ServerChunk[] _serverChunks;
    private readonly int[] _areaOfInterestNeighborChunkIndex;

    // Entity membership
    private readonly int[] _entityCurrentChunkIndex;
    private readonly int[] _entityCurrentLocalEntityIndex;
    private readonly int[] _entityCurrentPooledEntityIndex;

    // Player membership
    private readonly int[] _entityCurrentPlayerChunkIndex;
    private readonly int[] _entityCurrentLocalPlayerIndex;
    private readonly int[] _entityCurrentPooledPlayerIndex;

    public int Actions; // For testing purposes, counts the number of add/remove operations performed on chunks.
    public Chunks(
        MapSettings mapSettings,
        int playersCapacity,
        int entitiesCapacity)
    {

        if (mapSettings.MapWidth <= 0) throw new ArgumentOutOfRangeException(nameof(mapSettings.MapWidth));
        if (mapSettings.MapHeight <= 0) throw new ArgumentOutOfRangeException(nameof(mapSettings.MapHeight));
        if (mapSettings.ChunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(mapSettings.ChunkSize));
        if (playersCapacity < 0) throw new ArgumentOutOfRangeException(nameof(playersCapacity));
        if (entitiesCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(entitiesCapacity));

        // Players are also entities, and we need to store both groups in the entities arrays.
        entitiesCapacity = playersCapacity + entitiesCapacity;

        MapWidth = mapSettings.MapWidth;
        MapHeight = mapSettings.MapHeight;
        ChunkSize = mapSettings.ChunkSize;
        _inverseChunkSize = 1f / mapSettings.ChunkSize;

        ChunkGridWidth = (mapSettings.MapWidth + mapSettings.ChunkSize - 1) / mapSettings.ChunkSize;
        ChunkGridHeight = (mapSettings.MapHeight + mapSettings.ChunkSize - 1) / mapSettings.ChunkSize;
        ChunkCount = ChunkGridWidth * ChunkGridHeight;

        _activeChunkSet = new ActiveChunkSet(ChunkCount);
        _serverChunks = new ServerChunk[ChunkCount];
        _areaOfInterestNeighborChunkIndex = new int[ChunkCount * AreaOfInterestNeighborCount];

        _entityCurrentChunkIndex = new int[entitiesCapacity];
        _entityCurrentLocalEntityIndex = new int[entitiesCapacity];
        _entityCurrentPooledEntityIndex = new int[entitiesCapacity];

        _entityCurrentPlayerChunkIndex = new int[entitiesCapacity];
        _entityCurrentLocalPlayerIndex = new int[entitiesCapacity];
        _entityCurrentPooledPlayerIndex = new int[entitiesCapacity];

        for (int entityId = 0; entityId < entitiesCapacity; entityId++)
        {
            _entityCurrentChunkIndex[entityId] = InvalidIndex;
            _entityCurrentLocalEntityIndex[entityId] = InvalidIndex;
            _entityCurrentPooledEntityIndex[entityId] = InvalidIndex;

            _entityCurrentPlayerChunkIndex[entityId] = InvalidIndex;
            _entityCurrentLocalPlayerIndex[entityId] = InvalidIndex;
            _entityCurrentPooledPlayerIndex[entityId] = InvalidIndex;
        }

        for (int chunkGridY = 0; chunkGridY < ChunkGridHeight; chunkGridY++)
        {
            for (int chunkGridX = 0; chunkGridX < ChunkGridWidth; chunkGridX++)
            {
                int chunkIndex = chunkGridX + chunkGridY * ChunkGridWidth;
                _serverChunks[chunkIndex].Initialize(chunkGridX, chunkGridY);
            }
        }

        for (int chunkGridY = 0; chunkGridY < ChunkGridHeight; chunkGridY++)
        {
            for (int chunkGridX = 0; chunkGridX < ChunkGridWidth; chunkGridX++)
            {
                int chunkIndex = chunkGridX + chunkGridY * ChunkGridWidth;
                int baseIndex = chunkIndex * AreaOfInterestNeighborCount;

                int writeIndex = 0;
                for (int deltaY = -1; deltaY <= 1; deltaY++)
                {
                    for (int deltaX = -1; deltaX <= 1; deltaX++)
                    {
                        int neighborChunkGridX = chunkGridX + deltaX;
                        int neighborChunkGridY = chunkGridY + deltaY;

                        _areaOfInterestNeighborChunkIndex[baseIndex + writeIndex] =
                            neighborChunkGridX < 0 ||
                            neighborChunkGridY < 0 ||
                            neighborChunkGridX >= ChunkGridWidth ||
                            neighborChunkGridY >= ChunkGridHeight
                                ? InvalidIndex
                                : neighborChunkGridX + neighborChunkGridY * ChunkGridWidth;

                        writeIndex++;
                    }
                }
            }
        }

        int maximumSimultaneousPlayerPooledBlocks = Math.Min(ChunkCount, playersCapacity / (LocalCapacity + 1));
        int maximumSimultaneousEntityPooledBlocks = Math.Min(ChunkCount, entitiesCapacity / (LocalCapacity + 1));
        int pooledBlockSafetyMargin = 64;

        int pooledBlockCount =
            maximumSimultaneousPlayerPooledBlocks +
            maximumSimultaneousEntityPooledBlocks +
            pooledBlockSafetyMargin;

        if (pooledBlockCount <= 0)
        {
            pooledBlockCount = 1;
        }

        _largeMemoryBlockPool = new LargeMemoryBlockPool(pooledBlockCount);
    }

    public int AllEntitiesCount => _serverChunks.Sum(s => s.Count);
    public int MaxCountInSingleChunk => _serverChunks.Max(s => s.Count);
    public void Dispose()
    {
        _largeMemoryBlockPool.Dispose();
    }

    public ReadOnlySpan<int> ActiveChunkIndices => _activeChunkSet.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetChunkIndexFromChunkGridPosition(int chunkGridX, int chunkGridY)
    {
        return chunkGridX + chunkGridY * ChunkGridWidth;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetChunkIndexFromWorldPosition(float worldX, float worldY)
    {
        int chunkGridX = (int)(worldX * _inverseChunkSize);
        int chunkGridY = (int)(worldY * _inverseChunkSize);

        chunkGridX = Math.Clamp(chunkGridX, 0, ChunkGridWidth - 1);
        chunkGridY = Math.Clamp(chunkGridY, 0, ChunkGridHeight - 1);

        return chunkGridX + chunkGridY * ChunkGridWidth;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetChunkIndexFromWorldPosition(System.Numerics.Vector2 worldPosition)
    {
        return GetChunkIndexFromWorldPosition(worldPosition.X, worldPosition.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<int> GetAreaOfInterestNeighborChunkIndices(int chunkIndex)
    {
        return _areaOfInterestNeighborChunkIndex.AsSpan(chunkIndex * AreaOfInterestNeighborCount, AreaOfInterestNeighborCount);
    }

    public ref ServerChunk GetChunkReference(int chunkIndex)
    {
        return ref _serverChunks[chunkIndex];
    }

    // ============================================================
    // Entities
    // ============================================================

    public void AddEntityToChunk(int entityId, int chunkIndex)
    {
        ref ServerChunk serverChunk = ref _serverChunks[chunkIndex];

        if (serverChunk.LocalEntityCount < LocalCapacity)
        {
            int localIndex = serverChunk.LocalEntityCount;
            serverChunk.LocalEntities[localIndex] = entityId;
            serverChunk.LocalEntityCount++;

            _entityCurrentChunkIndex[entityId] = chunkIndex;
            _entityCurrentLocalEntityIndex[entityId] = localIndex;
            _entityCurrentPooledEntityIndex[entityId] = InvalidIndex;
        }
        else
        {
            if (serverChunk.PooledEntitiesBlockIndex == InvalidIndex)
            {
                serverChunk.PooledEntitiesBlockIndex = _largeMemoryBlockPool.RentBlockIndex();
            }

            if (serverChunk.PooledEntityCount >= PooledBlockCapacity)
            {
                throw new InvalidOperationException("Entity pooled block capacity exceeded for chunk.");
            }

            int pooledIndex = serverChunk.PooledEntityCount;
            Span<int> pooledEntities = _largeMemoryBlockPool.GetBlockSpan(serverChunk.PooledEntitiesBlockIndex);
            pooledEntities[pooledIndex] = entityId;
            serverChunk.PooledEntityCount++;

            _entityCurrentChunkIndex[entityId] = chunkIndex;
            _entityCurrentLocalEntityIndex[entityId] = InvalidIndex;
            _entityCurrentPooledEntityIndex[entityId] = pooledIndex;
        }
    }

    public void RemoveEntityFromCurrentChunk(int entityId)
    {
        int chunkIndex = _entityCurrentChunkIndex[entityId];
        if (chunkIndex == InvalidIndex)
        {
            return;
        }

        ref ServerChunk serverChunk = ref _serverChunks[chunkIndex];

        int localEntityIndex = _entityCurrentLocalEntityIndex[entityId];
        int pooledEntityIndex = _entityCurrentPooledEntityIndex[entityId];

        if (localEntityIndex != InvalidIndex)
        {
            RemoveEntityFromLocalStorage(entityId, chunkIndex, ref serverChunk, localEntityIndex);
        }
        else
        {
            RemoveEntityFromPooledStorage(entityId, chunkIndex, ref serverChunk, pooledEntityIndex);
        }

        _entityCurrentChunkIndex[entityId] = InvalidIndex;
        _entityCurrentLocalEntityIndex[entityId] = InvalidIndex;
        _entityCurrentPooledEntityIndex[entityId] = InvalidIndex;
    }

    public void MoveEntityToChunk(int entityId, int newChunkIndex)
    {
        Actions++;
        int oldChunkIndex = _entityCurrentChunkIndex[entityId];
        if (oldChunkIndex == newChunkIndex)
        {
            return;
        }

        RemoveEntityFromCurrentChunk(entityId);
        AddEntityToChunk(entityId, newChunkIndex);
    }

    // ============================================================
    // Players
    // ============================================================

    public void AddPlayerToChunk(int entityId, int chunkIndex)
    {
        ref ServerChunk serverChunk = ref _serverChunks[chunkIndex];
        bool chunkWasInactive = serverChunk.TotalPlayerCount == 0;

        if (serverChunk.LocalPlayerCount < LocalCapacity)
        {
            int localIndex = serverChunk.LocalPlayerCount;
            serverChunk.LocalPlayers[localIndex] = entityId;
            serverChunk.LocalPlayerCount++;

            _entityCurrentPlayerChunkIndex[entityId] = chunkIndex;
            _entityCurrentLocalPlayerIndex[entityId] = localIndex;
            _entityCurrentPooledPlayerIndex[entityId] = InvalidIndex;
        }
        else
        {
            if (serverChunk.PooledPlayersBlockIndex == InvalidIndex)
            {
                serverChunk.PooledPlayersBlockIndex = _largeMemoryBlockPool.RentBlockIndex();
            }

            if (serverChunk.PooledPlayerCount >= PooledBlockCapacity)
            {
                throw new InvalidOperationException("Player pooled block capacity exceeded for chunk.");
            }

            int pooledIndex = serverChunk.PooledPlayerCount;
            Span<int> pooledPlayers = _largeMemoryBlockPool.GetBlockSpan(serverChunk.PooledPlayersBlockIndex);
            pooledPlayers[pooledIndex] = entityId;
            serverChunk.PooledPlayerCount++;

            _entityCurrentPlayerChunkIndex[entityId] = chunkIndex;
            _entityCurrentLocalPlayerIndex[entityId] = InvalidIndex;
            _entityCurrentPooledPlayerIndex[entityId] = pooledIndex;
        }

        if (chunkWasInactive)
        {
            _activeChunkSet.Activate(chunkIndex);
        }
    }

    public void RemovePlayerFromCurrentChunk(int entityId)
    {
        int chunkIndex = _entityCurrentPlayerChunkIndex[entityId];
        if (chunkIndex == InvalidIndex)
        {
            return;
        }

        ref ServerChunk serverChunk = ref _serverChunks[chunkIndex];

        int localPlayerIndex = _entityCurrentLocalPlayerIndex[entityId];
        int pooledPlayerIndex = _entityCurrentPooledPlayerIndex[entityId];

        if (localPlayerIndex != InvalidIndex)
        {
            RemovePlayerFromLocalStorage(entityId, chunkIndex, ref serverChunk, localPlayerIndex);
        }
        else
        {
            RemovePlayerFromPooledStorage(entityId, chunkIndex, ref serverChunk, pooledPlayerIndex);
        }

        _entityCurrentPlayerChunkIndex[entityId] = InvalidIndex;
        _entityCurrentLocalPlayerIndex[entityId] = InvalidIndex;
        _entityCurrentPooledPlayerIndex[entityId] = InvalidIndex;

        if (serverChunk.TotalPlayerCount == 0)
        {
            _activeChunkSet.Deactivate(chunkIndex);
        }
    }

    public void MovePlayerToChunk(int entityId, int newChunkIndex)
    {
        Actions++;
        int oldChunkIndex = _entityCurrentPlayerChunkIndex[entityId];
        if (oldChunkIndex == newChunkIndex)
        {
            return;
        }

        RemovePlayerFromCurrentChunk(entityId);
        AddPlayerToChunk(entityId, newChunkIndex);
    }

    // ============================================================
    // Collect
    // ============================================================

    public void CollectChunkEntities(int chunkIndex, ref IntegerCollector collector)
    {
        ref ServerChunk serverChunk = ref _serverChunks[chunkIndex];

        for (int localIndex = 0; localIndex < serverChunk.LocalEntityCount; localIndex++)
        {
            collector.Add(serverChunk.LocalEntities[localIndex]);
        }

        if (serverChunk.PooledEntityCount > 0)
        {
            Span<int> pooledEntities = _largeMemoryBlockPool.GetBlockSpan(serverChunk.PooledEntitiesBlockIndex);

            for (int pooledIndex = 0; pooledIndex < serverChunk.PooledEntityCount; pooledIndex++)
            {
                collector.Add(pooledEntities[pooledIndex]);
            }
        }
    }

    public void CollectChunkPlayers(int chunkIndex, ref IntegerCollector collector)
    {
        ref ServerChunk serverChunk = ref _serverChunks[chunkIndex];

        for (int localIndex = 0; localIndex < serverChunk.LocalPlayerCount; localIndex++)
        {
            collector.Add(serverChunk.LocalPlayers[localIndex]);
        }

        if (serverChunk.PooledPlayerCount > 0)
        {
            Span<int> pooledPlayers = _largeMemoryBlockPool.GetBlockSpan(serverChunk.PooledPlayersBlockIndex);

            for (int pooledIndex = 0; pooledIndex < serverChunk.PooledPlayerCount; pooledIndex++)
            {
                collector.Add(pooledPlayers[pooledIndex]);
            }
        }
    }

    public void CollectAreaOfInterestEntities(int chunkIndex, ref IntegerCollector collector)
    {
        ReadOnlySpan<int> neighbors = GetAreaOfInterestNeighborChunkIndices(chunkIndex);

        for (int neighborIndex = 0; neighborIndex < neighbors.Length; neighborIndex++)
        {
            int neighborChunkIndex = neighbors[neighborIndex];
            if (neighborChunkIndex == InvalidIndex)
            {
                continue;
            }

            CollectChunkEntities(neighborChunkIndex, ref collector);
        }
    }

    public void CollectAreaOfInterestPlayers(int chunkIndex, ref IntegerCollector collector)
    {
        ReadOnlySpan<int> neighbors = GetAreaOfInterestNeighborChunkIndices(chunkIndex);

        for (int neighborIndex = 0; neighborIndex < neighbors.Length; neighborIndex++)
        {
            int neighborChunkIndex = neighbors[neighborIndex];
            if (neighborChunkIndex == InvalidIndex)
            {
                continue;
            }

            CollectChunkPlayers(neighborChunkIndex, ref collector);
        }
    }

    // ============================================================
    // Internal remove helpers - entities
    // ============================================================

    private void RemoveEntityFromLocalStorage(
        int removedEntityId,
        int chunkIndex,
        ref ServerChunk serverChunk,
        int removedLocalIndex)
    {
        if (serverChunk.PooledEntityCount > 0)
        {
            int lastPooledIndex = serverChunk.PooledEntityCount - 1;
            Span<int> pooledEntities = _largeMemoryBlockPool.GetBlockSpan(serverChunk.PooledEntitiesBlockIndex);

            int movedEntityId = pooledEntities[lastPooledIndex];
            serverChunk.LocalEntities[removedLocalIndex] = movedEntityId;
            serverChunk.PooledEntityCount--;

            _entityCurrentChunkIndex[movedEntityId] = chunkIndex;
            _entityCurrentLocalEntityIndex[movedEntityId] = removedLocalIndex;
            _entityCurrentPooledEntityIndex[movedEntityId] = InvalidIndex;

            if (serverChunk.PooledEntityCount == 0)
            {
                _largeMemoryBlockPool.ReturnBlockIndex(serverChunk.PooledEntitiesBlockIndex);
                serverChunk.PooledEntitiesBlockIndex = InvalidIndex;
            }
        }
        else
        {
            int lastLocalIndex = serverChunk.LocalEntityCount - 1;

            if (removedLocalIndex != lastLocalIndex)
            {
                int movedEntityId = serverChunk.LocalEntities[lastLocalIndex];
                serverChunk.LocalEntities[removedLocalIndex] = movedEntityId;

                _entityCurrentChunkIndex[movedEntityId] = chunkIndex;
                _entityCurrentLocalEntityIndex[movedEntityId] = removedLocalIndex;
                _entityCurrentPooledEntityIndex[movedEntityId] = InvalidIndex;
            }

            serverChunk.LocalEntityCount--;
        }
    }

    private void RemoveEntityFromPooledStorage(
        int removedEntityId,
        int chunkIndex,
        ref ServerChunk serverChunk,
        int removedPooledIndex)
    {
        int lastPooledIndex = serverChunk.PooledEntityCount - 1;
        Span<int> pooledEntities = _largeMemoryBlockPool.GetBlockSpan(serverChunk.PooledEntitiesBlockIndex);

        if (removedPooledIndex != lastPooledIndex)
        {
            int movedEntityId = pooledEntities[lastPooledIndex];
            pooledEntities[removedPooledIndex] = movedEntityId;

            _entityCurrentChunkIndex[movedEntityId] = chunkIndex;
            _entityCurrentLocalEntityIndex[movedEntityId] = InvalidIndex;
            _entityCurrentPooledEntityIndex[movedEntityId] = removedPooledIndex;
        }

        serverChunk.PooledEntityCount--;

        if (serverChunk.PooledEntityCount == 0)
        {
            _largeMemoryBlockPool.ReturnBlockIndex(serverChunk.PooledEntitiesBlockIndex);
            serverChunk.PooledEntitiesBlockIndex = InvalidIndex;
        }
    }

    // ============================================================
    // Internal remove helpers - players
    // ============================================================

    private void RemovePlayerFromLocalStorage(
        int removedEntityId,
        int chunkIndex,
        ref ServerChunk serverChunk,
        int removedLocalIndex)
    {
        if (serverChunk.PooledPlayerCount > 0)
        {
            int lastPooledIndex = serverChunk.PooledPlayerCount - 1;
            Span<int> pooledPlayers = _largeMemoryBlockPool.GetBlockSpan(serverChunk.PooledPlayersBlockIndex);

            int movedEntityId = pooledPlayers[lastPooledIndex];
            serverChunk.LocalPlayers[removedLocalIndex] = movedEntityId;
            serverChunk.PooledPlayerCount--;

            _entityCurrentPlayerChunkIndex[movedEntityId] = chunkIndex;
            _entityCurrentLocalPlayerIndex[movedEntityId] = removedLocalIndex;
            _entityCurrentPooledPlayerIndex[movedEntityId] = InvalidIndex;

            if (serverChunk.PooledPlayerCount == 0)
            {
                _largeMemoryBlockPool.ReturnBlockIndex(serverChunk.PooledPlayersBlockIndex);
                serverChunk.PooledPlayersBlockIndex = InvalidIndex;
            }
        }
        else
        {
            int lastLocalIndex = serverChunk.LocalPlayerCount - 1;

            if (removedLocalIndex != lastLocalIndex)
            {
                int movedEntityId = serverChunk.LocalPlayers[lastLocalIndex];
                serverChunk.LocalPlayers[removedLocalIndex] = movedEntityId;

                _entityCurrentPlayerChunkIndex[movedEntityId] = chunkIndex;
                _entityCurrentLocalPlayerIndex[movedEntityId] = removedLocalIndex;
                _entityCurrentPooledPlayerIndex[movedEntityId] = InvalidIndex;
            }

            serverChunk.LocalPlayerCount--;
        }
    }

    private void RemovePlayerFromPooledStorage(
        int removedEntityId,
        int chunkIndex,
        ref ServerChunk serverChunk,
        int removedPooledIndex)
    {
        int lastPooledIndex = serverChunk.PooledPlayerCount - 1;
        Span<int> pooledPlayers = _largeMemoryBlockPool.GetBlockSpan(serverChunk.PooledPlayersBlockIndex);

        if (removedPooledIndex != lastPooledIndex)
        {
            int movedEntityId = pooledPlayers[lastPooledIndex];
            pooledPlayers[removedPooledIndex] = movedEntityId;

            _entityCurrentPlayerChunkIndex[movedEntityId] = chunkIndex;
            _entityCurrentLocalPlayerIndex[movedEntityId] = InvalidIndex;
            _entityCurrentPooledPlayerIndex[movedEntityId] = removedPooledIndex;
        }

        serverChunk.PooledPlayerCount--;

        if (serverChunk.PooledPlayerCount == 0)
        {
            _largeMemoryBlockPool.ReturnBlockIndex(serverChunk.PooledPlayersBlockIndex);
            serverChunk.PooledPlayersBlockIndex = InvalidIndex;
        }
    }
}