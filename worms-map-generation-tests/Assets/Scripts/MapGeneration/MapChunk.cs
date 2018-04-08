public class MapChunk
{
// TODO: should these be structs or classes? Does it matter?
    public byte[] Chunk;
    public int Width;
    public int Height;
    public int XOffset;
    public int YOffset;

    public void Init(int width, int height, int x, int y)
    {
        Width = width;
        Height = height;
        XOffset = x;
        YOffset = y;
        Chunk = new byte[width * height];
    }

    public byte Get(int x, int y)
    {
        return Chunk[y * Width + x];
    }
}