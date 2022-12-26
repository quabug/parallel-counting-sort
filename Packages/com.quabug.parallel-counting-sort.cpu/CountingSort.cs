using System;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Parallel.CPU
{
    public static class CountingSortExtension
    {
        public static Handle CountingSort<TBinItems, TSortableData>(
            this ref TSortableData sortableData,
            ref TBinItems binItems,
            int batchCount = 32
        )
            where TBinItems : struct, IBinItems
            where TSortableData : struct, ISortableData
        {
            return sortableData.CountingSort(ref binItems, new SingleThreadPrefixSum<IntNumber, int>(), batchCount);
        }

        public static Handle CountingSort<TBinItems, TSortableData, TPrefixSum>(
            this ref TSortableData sortableData,
            ref TBinItems binItems,
            TPrefixSum prefixSumJob,
            int batchCount = 32
        )
            where TBinItems : struct, IBinItems
            where TSortableData : struct, ISortableData
            where TPrefixSum : struct, IPrefixSum<int>
        {
            var itemBinIndices = new NativeArray<int>(binItems.ItemCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var binItemCounts = new NativeArray<int>(binItems.BinCount, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            var prefixSum = new NativeArray<int>(binItems.BinCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var itemToBinIndexJobHandle = new ItemToBinIndexJob<TBinItems>
            {
                BinItems = binItems,
                ItemBinIndices = itemBinIndices
            }.Schedule(itemBinIndices.Length, batchCount);

            var countJobHandle = new CountJob
            {
                ItemBinIndices = itemBinIndices,
                BinItemCounts = binItemCounts
            }.Schedule(itemBinIndices.Length, batchCount, itemToBinIndexJobHandle);

            var prefixSumJobHandle = prefixSumJob.CalculatePrefixSum(binItemCounts, prefixSum, countJobHandle);

            var countingSortJobHandle = new CountingSortJob<TSortableData>
            {
                BinPrefixSum = prefixSum,
                Data = sortableData,
                ItemBinIndices = itemBinIndices
            }.Schedule(itemBinIndices.Length, batchCount, prefixSumJobHandle);

            var handles = new NativeArray<JobHandle>(4, Allocator.Temp);
            handles[0] = itemToBinIndexJobHandle;
            handles[1] = countJobHandle;
            handles[2] = prefixSumJobHandle;
            handles[3] = countingSortJobHandle;
            var result = new Handle(
                jobHandle: JobHandle.CombineDependencies(handles), 
                inputItemBinIndices: itemBinIndices,
                binItemCounts: binItemCounts,
                itemPrefixSum: prefixSum
            );
            handles.Dispose();
            return result;
        }

        [BurstCompile]
        public struct ItemToBinIndexJob<T> : IJobParallelFor where T : struct, IBinItems
        {
            public T BinItems;
            [WriteOnly] public NativeArray<int> ItemBinIndices;

            public void Execute(int index)
            {
                ItemBinIndices[index] = BinItems.GetBinIndexByItemIndex(index);
            }
        }

        [BurstCompile]
        public struct CountJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> ItemBinIndices;
            [NativeDisableParallelForRestriction] public NativeArray<int> BinItemCounts;

            public unsafe void Execute(int index)
            {
                var binIndex = ItemBinIndices[index];
                ref var value = ref UnsafeUtility.ArrayElementAsRef<int>(BinItemCounts.GetUnsafePtr(), binIndex);
                Interlocked.Increment(ref value);
            }
        }

        [BurstCompile]
        public struct CountingSortJob<T> : IJobParallelFor where T : struct, ISortableData
        {
            public T Data;
            [ReadOnly] public NativeArray<int> ItemBinIndices;
            [NativeDisableParallelForRestriction] public NativeArray<int> BinPrefixSum;

            public unsafe void Execute(int index)
            {
                var binIndex = ItemBinIndices[index];
                var sortedIndex = Interlocked.Decrement(ref UnsafeUtility.ArrayElementAsRef<int>(BinPrefixSum.GetUnsafePtr(), binIndex));
                Data.Sort(index, sortedIndex);
            }
        }

        public readonly struct Handle : IDisposable
        {
            public JobHandle JobHandle { get; }
            public NativeArray<int> InputItemBinIndices { get; }
            public NativeArray<int> BinItemCounts { get; }
            public NativeArray<int> ItemPrefixSum { get; }

            public Handle(JobHandle jobHandle, NativeArray<int> inputItemBinIndices, NativeArray<int> binItemCounts, NativeArray<int> itemPrefixSum)
            {
                JobHandle = jobHandle;
                InputItemBinIndices = inputItemBinIndices;
                BinItemCounts = binItemCounts;
                ItemPrefixSum = itemPrefixSum;
            }

            public Handle Complete()
            {
                JobHandle.Complete();
                return this;
            }

            public void Dispose()
            {
                InputItemBinIndices.Dispose();
                BinItemCounts.Dispose();
                ItemPrefixSum.Dispose();
            }
        }
    
        public struct IntNumber : INumber<int>
        {
            public int Zero()
            {
                return 0;
            }

            public int Add(int lhs, int rhs)
            {
                return lhs + rhs;
            }
        }
    }
}