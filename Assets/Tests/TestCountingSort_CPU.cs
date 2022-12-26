using System.Linq;
using NUnit.Framework;
using Parallel.CPU;
using Unity.Collections;
using Random = System.Random;

public class TestCountingSort_CPU
{
    private CountingSort _sort;

    [SetUp]
    public void SetUp()
    {
        _sort = new CountingSort(new SingleThreadPrefixSum<IntNumber, int>());
    }

    [Test]
    public void should_sort_small_scale_of_numbers()
    {
        var numbers = new int[] { 3, 1, 2, 5, 4, 3, 2, 1, 1 };
        var sorted = numbers.OrderBy(n => n).ToArray();
        using var numbersInput = new NativeArray<int>(numbers, Allocator.TempJob);
        using var numbersOutput = new NativeArray<int>(numbers.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var items = new NumberBinItems() { Numbers = numbersInput };
        var data = new SortableData<int>() { Input = numbersInput, Output = numbersOutput };
        using var result = _sort.Schedule(ref items, ref data).Complete();
        Assert.That(result.InputItemBinIndices.ToArray(), Is.EqualTo(numbers));
        Assert.That(result.BinItemCounts.ToArray(), Is.EqualTo(new[] { 0, 3, 2, 2, 1, 1, 0, 0, 0 }));
        Assert.That(result.ItemPrefixSum.ToArray(), Is.EqualTo(new[] { 0, 0, 3, 5, 7, 8, 9, 9, 9 }));
        Assert.That(sorted, Is.EqualTo(numbersOutput.ToArray()));
    }

    [Test]
    public void should_sort_numbers([Random(int.MinValue, int.MaxValue, 100)] int seed)
    {
        var random = new Random(seed);
        var count = random.Next(100, 1000);
        var numbers = Enumerable.Range(0, count).Select(_ => random.Next(count)).ToArray();
        var sorted = numbers.OrderBy(n => n).ToArray();
        var numberCounts = new int[numbers.Length];
        foreach (var n in numbers) numberCounts[n]++;
        var numberSums = new int[numbers.Length];
        for (var i = 0; i < numberCounts.Length - 1; i++) numberSums[i + 1] = numberSums[i] + numberCounts[i];

        using var numbersInput = new NativeArray<int>(numbers, Allocator.TempJob);
        using var numbersOutput = new NativeArray<int>(numbers.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var items = new NumberBinItems() { Numbers = numbersInput };
        var data = new SortableData<int>() { Input = numbersInput, Output = numbersOutput };
        using var result = _sort.Schedule(ref items, ref data).Complete();
        Assert.That(result.InputItemBinIndices.ToArray(), Is.EqualTo(numbers));
        Assert.That(result.BinItemCounts.ToArray(), Is.EqualTo(numberCounts));
        Assert.That(result.ItemPrefixSum.ToArray(), Is.EqualTo(numberSums));
        Assert.That(sorted, Is.EqualTo(numbersOutput.ToArray()));
    }

    struct NumberBinItems : IBinItems
    {
        [ReadOnly] public NativeArray<int> Numbers;

        public int ItemCount => Numbers.Length;
        public int BinCount => Numbers.Length;

        public int GetBinIndexByItemIndex(int index)
        {
            return Numbers[index];
        }
    }

    struct IntNumber : INumber<int>
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