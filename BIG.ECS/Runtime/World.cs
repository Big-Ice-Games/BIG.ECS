using System;
using System.Collections.Generic;

namespace BIG.ECS
{
    /// <summary>
    /// ECS world is the main class that holds all entities and systems.
    /// It is responsible for updating all systems and managing the lifecycle of entities.
    /// --- IMPORTANT ---
    /// You CAN'T use more than one world in the same process.
    /// Components are static and based on the entity index (id).
    /// Multiple worlds will cause conflicts between components.
    /// </summary>
    public sealed class World
    {
        public  readonly EntityManager EntityManager;
        private readonly IList<ISystem> _systems;
        private static   World? _instance;

        public static EntityManager GetEntityManager => _instance?.EntityManager ?? throw new Exception("World is not created. You need to create a world before accessing its EntityManager.");

        public static World Create(int entitiesCapacity, IList<ISystem> systems)
        {
            if (_instance != null)
                throw new Exception("World already exists. " +
                                    "You should never create more than one world because it shares Components<T> static classes, and it will cause conflicts between them." +
                                    "If you want to create a new one you need to call Destroy() first.");

            _instance = new World(entitiesCapacity, systems);
            return _instance;
        }

        World(int entitiesCapacity, IList<ISystem> systems)
        {
            EntityManager = new EntityManager(entitiesCapacity);
            _systems = systems;
            foreach (ISystem system in _systems)
                system.Initialize(EntityManager);
        }

        /// <summary>
        /// Update world by calling update function of all systems. This method should be called every frame to update the world and all its systems.
        /// It will also call <see cref="EntityManager.EndFrame"/> at the end of the update to clear all entities that are marked for destruction and to reset the state of the entity manager for the next frame.
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <exception cref="Exception"></exception>
        public static void Update(in float deltaTime)
        {
            if (_instance == null) throw new Exception("World is not created. You need to create a world before updating it.");

            foreach (var system in _instance._systems)
                system.Update(_instance, deltaTime);

            _instance.EntityManager.EndFrame();
        }

        /// <summary>
        /// Set current instance of the world to null. This is used to destroy the world and allow creating a new one. You should call this method when you want to create a new world after destroying the old one.
        /// </summary>
        public static void Destroy()
        {
            _instance = null;
        }
    }
}
