using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Parallel.GPU
{
    public class CountingSort : IDisposable
    {
        private readonly IPrefixSum _prefixSum;
        private readonly ComputeShader _shader;
    
        private readonly int _clearBinCountsKernelThreadGroup;
        private readonly int _clearBinCountsKernelIndex;

        private readonly int _countKernelThreadGroup;
        private readonly int _countKernelIndex;

        private readonly int _sortKernelIndex;
        private readonly int _sortKernelThreadGroup;

        internal ComputeBuffer BinItemCountsBuffer => _prefixSum.Numbers;
        internal ComputeBuffer BinPrefixSumsBuffer => _prefixSum.PrefixSums;
        internal ComputeBuffer SortedItemBinIndicesBuffer { get; }

        public CountingSort(ComputeShader shader, IPrefixSum prefixSum, ComputeBuffer itemBinIndicesBuffer, int binCount)
        {
            _prefixSum = prefixSum;
            _shader = shader;
            var itemCount = itemBinIndicesBuffer.count;

            (_clearBinCountsKernelIndex, _clearBinCountsKernelThreadGroup) = shader.GetKernelAndThreadGroup("ClearBinCounts", binCount);
            (_countKernelIndex, _countKernelThreadGroup) = shader.GetKernelAndThreadGroup("Count", itemCount);
            (_sortKernelIndex, _sortKernelThreadGroup) = shader.GetKernelAndThreadGroup("Sort", itemCount);

            SortedItemBinIndicesBuffer = new ComputeBuffer(itemCount, UnsafeUtility.SizeOf<int>(), ComputeBufferType.Structured);

            shader.SetInt("ItemCount", itemCount);

            // clear kernel
            shader.SetBuffer(_clearBinCountsKernelIndex, "BinItemCounts", BinItemCountsBuffer);

            // count kernel
            shader.SetBuffer(_countKernelIndex, "ItemBinIndices", itemBinIndicesBuffer);
            shader.SetBuffer(_countKernelIndex, "BinItemCounts", BinItemCountsBuffer);

            // sort kernel
            shader.SetBuffer(_sortKernelIndex, "ItemBinIndices", itemBinIndicesBuffer);
            shader.SetBuffer(_sortKernelIndex, "SortedItemBinIndices", SortedItemBinIndicesBuffer);
            shader.SetBuffer(_sortKernelIndex, "BinPrefixSum", BinPrefixSumsBuffer);
        }

        public void Dispatch()
        {
            DispatchClearBinCounts();
            DispatchCount();
            DispatchSum();
            DispatchSort();
        }

        internal void DispatchClearBinCounts()
        {
            _shader.Dispatch(_clearBinCountsKernelIndex, _clearBinCountsKernelThreadGroup, 1, 1);
        }

        internal void DispatchCount()
        {
            _shader.Dispatch(_countKernelIndex, _countKernelThreadGroup, 1, 1);
        }

        internal void DispatchSum()
        {
            _prefixSum.Dispatch();
        }

        internal void DispatchSort()
        {
            _shader.Dispatch(_sortKernelIndex, _sortKernelThreadGroup, 1, 1);
        }

        public void Dispose()
        {
            BinItemCountsBuffer?.Dispose();
            BinPrefixSumsBuffer?.Dispose();
            SortedItemBinIndicesBuffer?.Dispose();
        }
    }
}