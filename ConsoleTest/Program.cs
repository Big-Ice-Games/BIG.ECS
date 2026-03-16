using BIG.ECS;
using BIG.ECS.Tests.Components;
using System.Diagnostics;
using TestLibrary.Components;
using TestLibrary.Systems;
using TestLibrary.Types;

namespace ConsoleTest
{
    internal class Program
    {
        const int MAP_WIDTH = 10000;
        const int MAP_HEIGHT = 10000;
        const int CHUNK_SIZE = 50;
        const int PLAYERS_CAPACITY = 1000;
        const int ENTITIES_CAPACITY = 50000;
        const int WORLD_CAPACITY = (PLAYERS_CAPACITY + ENTITIES_CAPACITY);

        private static IList<ISystem> GetSystems(int mapWidth, int mapHeight, int chunkSize, int playersCapacity, int entitiesCapacity)
        {
            MapSettings mapSettings = new MapSettings(mapWidth, mapHeight, chunkSize);
            Chunks chunks = new Chunks(mapSettings, playersCapacity, entitiesCapacity);

            return new List<ISystem>()
            {
                new RandomInputGeneratorSystem(),
                new InputSystem(),
                new AccelerationSystem(mapSettings),
                new ChunksSystem(chunks)
            };
        }

        private static IList<ISystem> GetOptimizedSystems(int mapWidth, int mapHeight, int chunkSize, int playersCapacity, int entitiesCapacity)
        {
            MapSettings mapSettings = new MapSettings(mapWidth, mapHeight, chunkSize);
            Chunks chunks = new Chunks(mapSettings, playersCapacity, entitiesCapacity);

            return new List<ISystem>()
            {
                new OneUnifiedOptimizedSystem(mapSettings),
                new ChunksSystem(chunks)
            };
        }


        static void Main(string[] args)
        {
            RunWorldSimulation(false);
            RunWorldSimulation(true);

            Console.ReadLine();
        }

        private static void RunWorldSimulation(bool optimized)
        {
            Console.WriteLine($"Preparing test for ECS world. Optimized ({optimized})");
            Console.WriteLine($"World capacity: {WORLD_CAPACITY}");
            PrepareWorld(optimized);
            Console.WriteLine("World created.");

            for (int k = 0; k < 3; k++)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                // 1 min of a fake gameplay.
                for (int i = 0; i < 60 * 60; i++)
                {
                    World.Update(0.166f);
                }

                var ms = stopWatch.ElapsedMilliseconds;
                Console.WriteLine($"Test {k} finished.");
                Console.WriteLine($"1 minute of ECS world simulation executed in {(ms/1000f)}s");

                Console.WriteLine();
                Console.WriteLine("---------------------------------------------------");
                Console.WriteLine();
            }
        }

        private static void PrepareWorld(bool optimized)
        {
            World.Destroy();
            World.Create(WORLD_CAPACITY,
                optimized
                    ? GetOptimizedSystems(MAP_WIDTH, MAP_HEIGHT, CHUNK_SIZE, PLAYERS_CAPACITY, ENTITIES_CAPACITY)
                    : GetSystems(MAP_WIDTH, MAP_HEIGHT, CHUNK_SIZE, PLAYERS_CAPACITY, ENTITIES_CAPACITY));

            for (int i = 0; i < PLAYERS_CAPACITY - 1; i++)
            {
                Vector2 randomPosition = new Vector2(Random.Shared.Next(100, 9000), Random.Shared.Next(100, 9000));
                World.GetEntityManager.CreateEntity(
                    new Acceleration(10, 10),
                    new Input(Vector2.Zero),
                    new Rigidbody2D(1, 0.01f),
                    new Transform(randomPosition),
                    new ChunkAffiliation(),
                    new PlayerData());
            }


            for (int i = 0; i < ENTITIES_CAPACITY - 1; i++)
            {
                Vector2 randomPosition = new Vector2(Random.Shared.Next(100, 9000), Random.Shared.Next(100, 9000));
                //Console.WriteLine(randomPosition);
                World.GetEntityManager.CreateEntity(
                    new Acceleration(10, 10),
                    new Input(Vector2.Zero),
                    new Rigidbody2D(1, 0.01f),
                    new Transform(randomPosition),
                    new ChunkAffiliation());
            }
        }
    }
}