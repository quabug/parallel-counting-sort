using System.Linq;
using Parallel.GPU;
using UnityEngine;

public class GPU : MonoBehaviour
{
    [SerializeField] private CountingSort _countingSort;
    [SerializeField] private int[] _numbers;
    [SerializeField] private int[] _sortedIndices;
    [SerializeField] private int[] _sortedNumbers;

    private void Reset()
    {
        var random = new System.Random(0);
        _numbers = Enumerable.Repeat(0, 1000).Select(_ => random.Next(0, 100)).ToArray();
    }

    private void Start()
    {
        _countingSort.SetCounts(itemCount: 1000, binCount: 100);
        _countingSort.NumbersBuffer.SetData(_numbers);
        _countingSort.Sort();
        _sortedIndices = new int[1000];
        _sortedNumbers = new int[1000];
        _countingSort.SortedIndicesBuffer.GetData(_sortedIndices);
        for (var i = 0; i < 1000; i++)
        {
            _sortedNumbers[_sortedIndices[i]] = _numbers[i];
        }
    }
}