using System;
using System.Collections;
using System.Collections.Generic;

namespace BIG.ECS
{
    /// <summary>
    /// Base reference structure used by <see cref="EntityManager"/> to handle entities.
    /// </summary>
    public readonly unsafe ref struct EntityRef
    {
        public readonly int Id;
        public readonly Entity* Entity;

        internal EntityRef(int id, Entity* entity)
        {
            Entity = entity;
            Id = id;
        }

        public ref T GetComponent<T>() where T : struct, IComponent
        {
            if (!Entity->Components.HasFlag<T>())
                throw new InvalidOperationException($"Entity {Id} does not have component of type {typeof(T).Name}.");
            return ref Components<T>.Get(Id);
        }
    }

    public sealed class EntityManager : IEnumerable<int>
    {
        private int _maxEntityId;
        private readonly Entity[] _entities;
        private readonly Stack<int> _availableSlots;
        private readonly Dictionary<(ulong with, ulong without), Query> _queryCache = new();

        public EntityManager(int entitiesCapacity)
        {
            _entities = new Entity[entitiesCapacity];
            _availableSlots = new Stack<int>(entitiesCapacity);

            for (int i = entitiesCapacity - 1; i > 0; i--)
                _availableSlots.Push(i);
        }

        public QueryBuilder Query() => new QueryBuilder(this);
        internal bool TryToGetQuery(ValueTuple<ulong, ulong> key, out Query query) => _queryCache.TryGetValue(key, out query);

        #region Optimized Create entity functions

        public EntityRef CreateEntity()
        {
            var entity = PrivateCreateEntity();
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1>(T1 c1)
            where T1 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2>(T1 c1, T2 c2)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2, T3>(T1 c1, T2 c2, T3 c3)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c3);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2, T3, T4>(T1 c1, T2 c2, T3 c3, T4 c4)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c3);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c4);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2, T3, T4, T5>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c3);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c4);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c5);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2, T3, T4, T5, T6>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c3);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c4);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c5);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c6);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2, T3, T4, T5, T6, T7>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c3);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c4);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c5);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c6);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c7);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c3);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c4);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c5);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c6);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c7);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c8);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c3);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c4);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c5);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c6);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c7);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c8);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c9);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        public EntityRef CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
        {
            var entity = PrivateCreateEntity();
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c1);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c2);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c3);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c4);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c5);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c6);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c7);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c8);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c9);
            AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess(entity, c10);
            ValidateEntityInCachedQueries(in entity);
            return entity;
        }

        private unsafe EntityRef PrivateCreateEntity()
        {
            lock (_entities)
            {
                if (_availableSlots.Count > 0)
                {
                    int index = _availableSlots.Pop();
                    _entities[index] = new Entity()
                    {
                        State = EntityState.IsCreating
                    };

                    if (index >= _maxEntityId) _maxEntityId = index + 1; // Remember the highest index used.

                    fixed (Entity* entity = &_entities[index])
                    {
                        var result = new EntityRef(index, entity);
                        return result;
                    }
                }
            }

            throw new IndexOutOfRangeException("ECS capacity reached.");
        }

        private unsafe void AddComponentWithoutValidationUsedOnlyWithInEntityCreationProcess<T>(EntityRef entity, T component) where T : struct, IComponent
        {
            entity.Entity->Components.AddFlag<T>();
            Components<T>.Set(entity.Id, component);
        }
        #endregion
        public unsafe void AddComponent<T>(EntityRef entity, T component) where T : struct, IComponent
        {
            entity.Entity->Components.AddFlag<T>();
            Components<T>.Set(entity.Id, component);
            ValidateEntityInCachedQueries(in entity);
        }

        public void UpdateComponent<T>(EntityRef entity, T component) where T : struct, IComponent => Components<T>.Set(entity.Id, component);

        public unsafe void RemoveComponent<T>(EntityRef entity, T component) where T : struct, IComponent
        {
            entity.Entity->Components.RemoveFlag<T>();
            Components<T>.Set(entity.Id, default);
            ValidateEntityInCachedQueries(in entity);
        }

        private void ValidateEntityInCachedQueries(in EntityRef entity)
        {
            foreach (var query in _queryCache)
            {
                query.Value.ValidateEntityForQuery(entity);
            }
        }

        public unsafe EntityRef GetEntity(int id)
        {
            if (id < 0 || id >= _maxEntityId) throw new IndexOutOfRangeException($"Entity ID {id} is out of range.");
            if (_entities[id].State == EntityState.Empty) throw new InvalidOperationException($"Entity with ID {id} does not exist.");

            fixed (Entity* entity = &_entities[id])
            {
                return new EntityRef(id, entity);
            }
        }

        public void DestroyEntity(int id)
        {
            if (id < 0 || id >= _maxEntityId) throw new IndexOutOfRangeException($"Entity ID {id} is out of range.");
            if (_entities[id].State != EntityState.Alive) throw new InvalidOperationException($"Entity with ID {id} is not alive.");
            _entities[id].State = EntityState.IsDestroying;

            // Remove entity from all queries
            foreach (KeyValuePair<(ulong with, ulong without), Query> cache in _queryCache)
            {
                cache.Value.OnEntityRemoved(id);
            }
        }

        /// <summary>
        /// Create all entities that were marked as IsCreating and change their state to Alive.
        /// Destroy all entities that were marked as IsDestroying and change their state to Empty.
        /// </summary>
        internal void EndFrame()
        {
            for (int i = 0; i < _maxEntityId; i++)
            {
                if (_entities[i].State == EntityState.IsCreating)
                {
                    _entities[i].State = EntityState.Alive;
                }
                else if (_entities[i].State == EntityState.IsDestroying)
                {
                    _entities[i].State = EntityState.Empty;
                    _availableSlots.Push(i); // Recycle the slot
                }
            }

            // Maintain the highest entity ID
            if (_entities[_maxEntityId].State == EntityState.Empty)
            {
                for (int i = _maxEntityId - 1; i >= 0; i--)
                {
                    if (_entities[i].State == EntityState.Alive)
                    {
                        _maxEntityId = i + 1;
                        break;
                    }
                }
            }
        }

        internal void Resolve(Query query)
        {
            query.FillCache(QueryEntities(query.Key.Item1, query.Key.Item2).GetEnumerator());
            _queryCache[query.Key] = query;
        }

        private IEnumerable<int> QueryEntities(ulong withFlags, ulong withoutFlags)
        {
            for (int i = 0; i < _maxEntityId; i++)
            {
                var entity = _entities[i];
                if (entity.State == EntityState.Alive &&
                    (entity.Components & withFlags) == withFlags &&
                    (entity.Components & withoutFlags) == 0)
                {
                    yield return i;
                }
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < _maxEntityId; i++)
                if (_entities[i].State == EntityState.Alive)
                    yield return i;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
