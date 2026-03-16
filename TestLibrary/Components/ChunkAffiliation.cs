namespace BIG.ECS.Tests.Components
{
    public struct ChunkAffiliation : IComponent
    {
        public int ChunkId;

        public ChunkAffiliation()
        {
            ChunkId = -1;
        }
    }
}
