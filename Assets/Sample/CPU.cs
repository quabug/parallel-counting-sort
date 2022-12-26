using System.Linq;
using Parallel.CPU;
using Unity.Collections;
using UnityEngine;

public class CPU : MonoBehaviour
{
    [SerializeField] private int[] _numbers;
    [SerializeField] private int[] _sortedNumbers;

    private void Reset()
    {
        var random = new System.Random(0);
        _numbers = Enumerable.Repeat(0, 1000).Select(_ => random.Next(0, 100)).ToArray();
    }

    public void Start()
    {
        using var values = new NativeArray<int>(_numbers, Allocator.TempJob);
        using var sorted = new NativeArray<int>(_numbers.Length, Allocator.TempJob);
        var sortable = new SortableData<int> { Input = values, Output = sorted };
        var bins = new SimpleBinItems { Values = values, BinCount = 100 };
        using var handle = sortable.CountingSort(ref bins).Complete();
        _sortedNumbers = sorted.ToArray();

    }
    
    struct SimpleBinItems : IBinItems
    {
        public NativeArray<int> Values { get; set; }
        public int BinCount { get; set; }
        public int ItemCount => Values.Length;
        public int GetBinIndexByItemIndex(int index)
        {
            return Values[index];
        }
    }
}