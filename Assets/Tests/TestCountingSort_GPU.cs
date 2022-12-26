using System.Linq;
using NUnit.Framework;
using Parallel.GPU;
using UnityEditor;
using UnityEngine;

[Category("gpu")]
public class TestCountingSort_GPU
{
    private (CountingSortCore sort, SingleThreadPrefixSum prefixSum, int[] items, int[] counts, int[] sums, int[] sorted) RandomNumbersAndSums(int seed)
    {
        var random = new System.Random(seed);
        var itemCount = random.Next(100, 1000);
        var binCount = random.Next(1, 100);
        var items = new int[itemCount];
        var counts = new int[binCount];
        var sums = new int[binCount];
        for (var i = 0; i < itemCount; i++)
        {
            var bin = random.Next(binCount);
            items[i] = bin;
            counts[bin]++;
        }
        
        Debug.Log($"itemCount = {itemCount}({counts.Sum()})");

        var sorted = items.OrderBy(i => i).ToArray();
        sums[0] = counts[0];
        for (var i = 1; i < binCount; i++) sums[i] = sums[i-1]+counts[i];
        var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.quabug.parallel-counting-sort.gpu/CountingSort.compute");
        var prefixSumShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.quabug.parallel-prefix-sum.gpu/SingleThreadPrefixSum_int.compute");
        var prefixSum = new SingleThreadPrefixSum(prefixSumShader, binCount, 4);
        var sort = new CountingSortCore(shader, prefixSum, itemCount, binCount);
        sort.NumbersBuffer.SetData(items);
        return (sort, prefixSum, items, counts, sums, sorted);
    }

    [Test]
    public void should_count_items([Random(0, 1000, 100)] int seed)
    {
        var (sort, prefixSum, items, counts, sums, sorted) = RandomNumbersAndSums(seed);
        sort.DispatchClearBinCounts();
        sort.DispatchCount();
        var binItemCounts = new int[counts.Length];
        sort.NumberCountsBuffer.GetData(binItemCounts);
        Assert.That(binItemCounts.Sum(n => n), Is.EqualTo(items.Length));
        Assert.That(counts, Is.EqualTo(binItemCounts));
        sort.Dispose();
        prefixSum.Dispose();
    }

    [Test]
    public void should_sum_prefix([Random(0, 1000, 100)] int seed)
    {
        var (sort, prefixSum, items, counts, sums, sorted) = RandomNumbersAndSums(seed);
        sort.DispatchClearBinCounts();
        sort.DispatchCount();
        sort.DispatchSum();
        var binPrefixSums = new int[sums.Length];
        sort.NumberPrefixSumsBuffer.GetData(binPrefixSums);
        Assert.That(sums, Is.EqualTo(binPrefixSums));
        sort.Dispose();
        prefixSum.Dispose();
    }

    [Test]
    public void should_sort([Random(0, 1000, 100)] int seed)
    {
        var (sort, prefixSum, items, counts, sums, sorted) = RandomNumbersAndSums(seed);
        sort.DispatchClearBinCounts();
        sort.DispatchCount();
        sort.DispatchSum();
        sort.DispatchSort();
        var sortedIndices = new int[sorted.Length];
        sort.SortedIndicesBuffer.GetData(sortedIndices);
        var sortedNumbers = new int[sorted.Length];
        for (var i = 0; i < sortedNumbers.Length; i++)
        {
            sortedNumbers[sortedIndices[i]] = items[i];
        }
        Assert.That(sorted, Is.EqualTo(sortedNumbers));
        sort.Dispose();
        prefixSum.Dispose();
    }

    [Test]
    public void should_sort_multiple_times([Random(0, 1000, 10)] int seed)
    {
        var (sort, prefixSum, items, counts, sums, sorted) = RandomNumbersAndSums(seed);
        for (var i = 0; i < 100; i++) sort.Dispatch();
        var sortedIndices = new int[sorted.Length];
        sort.SortedIndicesBuffer.GetData(sortedIndices);
        var sortedNumbers = new int[sorted.Length];
        for (var i = 0; i < sortedNumbers.Length; i++)
        {
            sortedNumbers[sortedIndices[i]] = items[i];
        }
        Assert.That(sorted, Is.EqualTo(sortedNumbers));
        sort.Dispose();
        prefixSum.Dispose();
    }
}