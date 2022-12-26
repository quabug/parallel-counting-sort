using Unity.Collections;

namespace Parallel.CPU
{
    public struct SortableData<T> : ISortableData where T : unmanaged
    {
        [ReadOnly] public NativeArray<T> Input;
        [NativeDisableParallelForRestriction] public NativeArray<T> Output;

        public void Sort(int from, int to)
        {
            Output[to] = Input[from];
        }
    }
}