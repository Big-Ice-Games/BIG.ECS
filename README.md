# BIG.ECS

Part of https://github.com/Big-Ice-Games public repositories.
Used in Spac Smuggler project.
Very simple but also limited implementation for ECS system with a simple TestLibrary and ConsoleTest.
Designed to run and test without additional dependencies.

## Limitations
- Maximum amount of component types that you can use is 63 due to the enum flags used to track components by their types.
- You can't have more than 1 World created in the same process due to Component nature.

ConsoleTest runs two examples that also shows potential optimization.
The only difference between them is that optimized example merge InputSystem, AccelerationSystem and RandomInputGeneratorSystem into OneUnifiedOptimizedSystem.

```csharp
static void Main(string[] args)
{
    RunWorldSimulation(false);
    RunWorldSimulation(true);

    Console.ReadLine();
}
```

Take into consideration that Release gonna run significantly faster than Debug.