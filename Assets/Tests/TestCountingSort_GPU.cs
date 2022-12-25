using System.Linq;
using NUnit.Framework;
using Parallel.GPU;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

public class TestCountingSort_GPU
{
    private (CountingSort sort, int[] items, int[] counts, int[] sums, int[] sorted, ComputeBuffer numbersBuffer) RandomNumbersAndSums(int seed)
    {
        var random = new System.Random(seed);
        var itemCount = random.Next(100, 1000);
        var binCount = random.Next(1, 100);
        var items = new int[itemCount];
        var counts = new int[binCount];
        var sums = new int[binCount];
        var sorted = new int[itemCount];
        for (var i = 0; i < itemCount; i++)
        {
            var bin = random.Next(binCount);
            items[i] = bin;
            counts[bin]++;
        }
        
        Debug.Log($"itemCount = {itemCount}({counts.Sum()})");

        foreach (var (origin, newIndex) in items
                     .Select((item, index) => (item, index))
                     .OrderBy(t => t.item)
                     .Select((t, i) => (t.index, sorted: i)))
        {
            sorted[origin] = newIndex;
        }

        sums[0] = counts[0];
        for (var i = 1; i < binCount; i++) sums[i] = sums[i-1]+counts[i];
        var buffer = new ComputeBuffer(items.Length, UnsafeUtility.SizeOf<int>(), ComputeBufferType.Structured);
        buffer.SetData(items);
        var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.quabug.parallel-counting-sort.gpu/CountingSort.compute");
        var prefixSum = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.quabug.parallel-prefix-sum.gpu/SingleThreadPrefixSum_int.compute");
        var sort = new CountingSort(shader, new SingleThreadPrefixSum(prefixSum, binCount, 4), buffer, binCount);
        return (sort, items, counts, sums, sorted, buffer);
    }

    [Test]
    public void should_count_items([Random(0, 1000, 100)] int seed)
    {
        var (sort, items, counts, sums, sorted, buffer) = RandomNumbersAndSums(seed);
        sort.DispatchClearBinCounts();
        sort.DispatchCount();
        var binItemCounts = new int[counts.Length];
        sort.BinItemCountsBuffer.GetData(binItemCounts);
        Assert.That(binItemCounts.Sum(n => n), Is.EqualTo(items.Length));
        Assert.That(counts, Is.EquivalentTo(binItemCounts));
        sort.Dispose();
        buffer.Dispose();
    }

    [Test]
    public void should_sum_prefix([Random(0, 1000, 100)] int seed)
    {
        var (sort, items, counts, sums, sorted, buffer) = RandomNumbersAndSums(seed);
        sort.DispatchClearBinCounts();
        sort.DispatchCount();
        sort.DispatchSum();
        var binPrefixSums = new int[sums.Length];
        sort.BinPrefixSumsBuffer.GetData(binPrefixSums);
        Assert.That(sums, Is.EquivalentTo(binPrefixSums));
        sort.Dispose();
        buffer.Dispose();
    }

    [Test]
    public void should_sort([Random(0, 1000, 100)] int seed)
    {
        var (sort, items, counts, sums, sorted, buffer) = RandomNumbersAndSums(seed);
        sort.DispatchClearBinCounts();
        sort.DispatchCount();
        sort.DispatchSum();
        sort.DispatchSort();
        var sortedItems = new int[sorted.Length];
        sort.SortedItemBinIndicesBuffer.GetData(sortedItems);
        Assert.That(sorted, Is.EquivalentTo(sortedItems));
        sort.Dispose();
        buffer.Dispose();
    }

    [Test]
    public void should_sort_multiple_times([Random(0, 1000, 10)] int seed)
    {
        var (sort, items, counts, sums, sorted, buffer) = RandomNumbersAndSums(seed);
        for (var i = 0; i < 100; i++) sort.Dispatch();
        var sortedItems = new int[sorted.Length];
        sort.SortedItemBinIndicesBuffer.GetData(sortedItems);
        Assert.That(sorted, Is.EquivalentTo(sortedItems));
        sort.Dispose();
        buffer.Dispose();
    }
}