using System;
using System.Collections;
using System.Collections.Generic;

namespace BIG.ECS
{
    public ref struct QueryBuilder
    {
        private ulong _withMask;
        private ulong _withoutMask;
        private readonly EntityManager _entityManager;

        public QueryBuilder(EntityManager entity)
        {
            _entityManager = entity;
            _withMask = 0;
            _withoutMask = 0;
        }

        public QueryBuilder With<T>() where T : struct, IComponent { _withMask |= 1UL << ComponentId<T>.Value; return this; }
        public QueryBuilder Without<T>() where T : struct, IComponent { _withoutMask |= 1UL << ComponentId<T>.Value; return this; }

        #region With and Withouts for multiple components

        public QueryBuilder With<T1, T2>()
          where T1 : struct, IComponent
          where T2 : struct, IComponent
          => With<T1>().With<T2>();

        public QueryBuilder With<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            => With<T1>().With<T2>().With<T3>();

        public QueryBuilder With<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            => With<T1>().With<T2>().With<T3>().With<T4>();

        public QueryBuilder With<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            => With<T1>().With<T2>().With<T3>().With<T4>().With<T5>();

        public QueryBuilder With<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            => With<T1>().With<T2>().With<T3>().With<T4>().With<T5>().With<T6>();

        public QueryBuilder With<T1, T2, T3, T4, T5, T6, T7>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            => With<T1>().With<T2>().With<T3>().With<T4>().With<T5>().With<T6>().With<T7>();

        public QueryBuilder With<T1, T2, T3, T4, T5, T6, T7, T8>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            => With<T1>().With<T2>().With<T3>().With<T4>().With<T5>().With<T6>().With<T7>().With<T8>();

        public QueryBuilder Without<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            => Without<T1>().Without<T2>();

        public QueryBuilder Without<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            => Without<T1>().Without<T2>().Without<T3>();

        public QueryBuilder Without<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            => Without<T1>().Without<T2>().Without<T3>().Without<T4>();

        public QueryBuilder Without<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            => Without<T1>().Without<T2>().Without<T3>().Without<T4>().Without<T5>();

        #endregion

        public Query Resolve()
        {
            // If we already have a query with the same masks, we can return it instead of creating a new one.
            if (_entityManager.TryToGetQuery(new ValueTuple<ulong, ulong>(_withMask, _withoutMask), out Query query))
                return query;

            return new(_entityManager, _withMask, _withoutMask);
        }
    }

    public readonly struct Query : IEnumerable<int>
    {
        private readonly ulong _with;
        private readonly ulong _without;
        public ValueTuple<ulong, ulong> Key => (_with, _without);

        private readonly HashSet<int> _cachedEntities = new HashSet<int>(4096);

        public Query(EntityManager entityManager, ulong with, ulong without)
        {
            _with = with;
            _without = without;
            entityManager.Resolve(this);
        }

        internal void FillCache(IEnumerator<int> entitiesEnumerator)
        {
            while (entitiesEnumerator.MoveNext())
            {
                _cachedEntities.Add(entitiesEnumerator.Current);
            }
        }

        /// <summary>
        /// Because Queries are cached, we need to validate entities when they are added, removed or modified.
        /// </summary>
        internal unsafe void ValidateEntityForQuery(in EntityRef entityRef)
        {
            bool validForQuery = ((entityRef.Entity->Components & _with) == _with &&
                                  (entityRef.Entity->Components & _without) == 0);
            if (validForQuery)
            {
                _cachedEntities.Add(entityRef.Id);
            }
            else
            {
                _cachedEntities.Remove(entityRef.Id);
            }
        }

        internal void OnEntityRemoved(int entityIndex)
        {
            _cachedEntities.Remove(entityIndex);
        }

        public IEnumerator<int> GetEnumerator() => _cachedEntities.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
