using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Parallel.GPU
{
    public class CountingSortCore : IDisposable
    {
        private readonly IPrefixSum _prefixSum;
        private readonly ComputeShader _shader;
    
        private readonly int _clearBinCountsKernelThreadGroup;
        private readonly int _clearBinCountsKernelIndex;

        private readonly int _countKernelThreadGroup;
        private readonly int _countKernelIndex;

        private readonly int _sortKernelIndex;
        private readonly int _sortKernelThreadGroup;

        internal ComputeBuffer NumberCountsBuffer => _prefixSum.Numbers;
        internal ComputeBuffer NumberPrefixSumsBuffer => _prefixSum.PrefixSums;
        public ComputeBuffer SortedIndicesBuffer { get; }
        public ComputeBuffer NumbersBuffer { get; }

        public CountingSortCore(ComputeShader shader, IPrefixSum prefixSum, int itemCount, int binCount)
        {
            _prefixSum = prefixSum;
            _shader = shader;

            (_clearBinCountsKernelIndex, _clearBinCountsKernelThreadGroup) = shader.GetKernelAndThreadGroup("ClearBinCounts", binCount);
            (_countKernelIndex, _countKernelThreadGroup) = shader.GetKernelAndThreadGroup("Count", itemCount);
            (_sortKernelIndex, _sortKernelThreadGroup) = shader.GetKernelAndThreadGroup("Sort", itemCount);

            SortedIndicesBuffer = new ComputeBuffer(itemCount, UnsafeUtility.SizeOf<int>(), ComputeBufferType.Structured);
            NumbersBuffer = new ComputeBuffer(itemCount, UnsafeUtility.SizeOf<int>(), ComputeBufferType.Structured);

            shader.SetInt("ItemCount", itemCount);
            shader.SetInt("BinCount", binCount);

            // clear kernel
            shader.SetBuffer(_clearBinCountsKernelIndex, "NumberCounts", NumberCountsBuffer);

            // count kernel
            shader.SetBuffer(_countKernelIndex, "Numbers", NumbersBuffer);
            shader.SetBuffer(_countKernelIndex, "NumberCounts", NumberCountsBuffer);

            // sort kernel
            shader.SetBuffer(_sortKernelIndex, "Numbers", NumbersBuffer);
            shader.SetBuffer(_sortKernelIndex, "SortedIndices", SortedIndicesBuffer);
            shader.SetBuffer(_sortKernelIndex, "NumberPrefixSum", NumberPrefixSumsBuffer);
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
            SortedIndicesBuffer?.Dispose();
            NumbersBuffer?.Dispose();
        }
    }
}