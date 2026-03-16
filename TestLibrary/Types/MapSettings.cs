namespace TestLibrary.Types
{
    public sealed class MapSettings
    {
        public int MapWidth;
        public int MapHeight;
        public int ChunkSize;

        public MapSettings(int mapWidth, int mapHeight, int chunkSize)
        {
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            ChunkSize = chunkSize;
        }
    }
}
