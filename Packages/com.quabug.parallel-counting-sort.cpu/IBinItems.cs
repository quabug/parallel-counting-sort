namespace Parallel.CPU
{
    public interface IBinItems
    {
        public int ItemCount { get; }
        public int BinCount { get; }
        public int GetBinIndexByItemIndex(int index);
    }
}