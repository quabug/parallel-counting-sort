using System;
using UnityEngine;

namespace Parallel.GPU
{
    [CreateAssetMenu(fileName = "CountingSort", menuName = "Parallel/GPU-CountingSort", order = 0)]
    public class CountingSort : ScriptableObject
    {
        [SerializeField] private int _itemCount = 512;
        [SerializeField] private int _binCount = 64;
        [SerializeField] private ComputeShader _countingSortShader;
        [SerializeField] private PrefixSum _prefixSum;

        private Lazy<CountingSortCore> _countingSort;

        public ComputeBuffer SortedIndicesBuffer => _countingSort.Value.SortedIndicesBuffer;
        public ComputeBuffer NumbersBuffer => _countingSort.Value.NumbersBuffer;

        public CountingSort()
        {
            _countingSort = new Lazy<CountingSortCore>(() =>
                new CountingSortCore(_countingSortShader, _prefixSum, _itemCount, _binCount));
        }

        public void SetItemCount(int itemCount)
        {
            SetCounts(itemCount, _binCount);
        }

        public void SetBinCount(int binCount)
        {
            SetCounts(_itemCount, binCount);
        }

        public void SetCounts(int itemCount, int binCount)
        {
            OnDestroy();
            _itemCount = itemCount;
            _binCount = binCount;
            _countingSort = new Lazy<CountingSortCore>(() =>
                new CountingSortCore(_countingSortShader, _prefixSum, _itemCount, _binCount));
        }

        public void Sort()
        {
            _countingSort.Value.Dispatch();
        }

        private void OnDestroy()
        {
            if (_countingSort.IsValueCreated)
                _countingSort.Value.Dispose();
        }
    }
}