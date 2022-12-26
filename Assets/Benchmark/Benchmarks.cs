using System;
using NUnit.Framework;
using Parallel.CPU;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.PerformanceTesting;

[Category("benchmark")]
public class Benchmarks
{
    private static int[] _counts =
    {
        100, 1_000, 10_000, 100_000, 1_000_000//, 10_000_000, 100_000_000, 1_000_000_000
    };

    [Test, TestCaseSource(nameof(_counts)), Performance]
    public void Sort(int count)
    {
        var binCount = 10000;
        var randomNumbers = new NativeArray<int>(count, Allocator.Temp);
        var random = new Random(0);
        for (var i = 0; i < randomNumbers.Length; i++) randomNumbers[i] = random.Next(0, binCount);
        
        {
            var numbers = new NativeArray<int>(count, Allocator.TempJob);
            Measure.Method(() =>
                {
                    numbers.Sort();
                })
                .SetUp(() =>
                {
                    for (var i = 0; i < numbers.Length; i++) numbers[i] = randomNumbers[i];
                })
                .SampleGroup("baseline")
                .Run();
            numbers.Dispose();
        }
        
        {
            var numbers = new NativeArray<int>(count, Allocator.TempJob);
            Measure.Method(() =>
                {
                    numbers.SortJob().Schedule().Complete();
                })
                .SetUp(() =>
                {
                    for (var i = 0; i < numbers.Length; i++) numbers[i] = randomNumbers[i];
                })
                .SampleGroup("baseline-job")
                .Run();
            numbers.Dispose();
        }

        {
            var numbers = new NativeArray<int>(count, Allocator.TempJob);
            Measure.Method(() =>
                {
                    new QuickSortJob() { entries = numbers }.Schedule().Complete();
                })
                .SetUp(() =>
                {
                    for (var i = 0; i < numbers.Length; i++) numbers[i] = randomNumbers[i];
                })
                .SampleGroup("quicksort-job")
                .Run();
            numbers.Dispose();
        }

        {
            var numbers = new NativeArray<int>(count, Allocator.TempJob);
            var output = new NativeArray<int>(count, Allocator.TempJob);
            var binItems = new NumberBinItems { BinCount = binCount, Numbers = numbers };
            var sortable = new SortableData<int> { Input = numbers, Output = output };
            Measure.Method(() =>
                {
                    using var handle = sortable.CountingSort(ref binItems).Complete();
                })
                .SetUp(() =>
                {
                    for (var i = 0; i < numbers.Length; i++) numbers[i] = randomNumbers[i];
                })
                .SampleGroup("parallel-counting-sort")
                .Run();
            numbers.Dispose();
            output.Dispose();
        }

        randomNumbers.Dispose();
    }

    struct NumberBinItems : IBinItems
    {
        [ReadOnly] public NativeArray<int> Numbers;

        public int ItemCount => Numbers.Length;
        public int BinCount { get; set; }

        public int GetBinIndexByItemIndex(int index)
        {
            return Numbers[index];
        }
    }
}

// quick sort from https://coffeebraingames.wordpress.com/2020/05/24/nativearray-sortjob-is-fast-or-is-it/
[BurstCompile]
struct QuickSortJob : IJob {
    public NativeArray<int> entries;

    public void Execute() {
        if (this.entries.Length > 0) {
            Quicksort(0, this.entries.Length - 1);
        }
    }

    private void Quicksort(int left, int right) {
        int i = left;
        int j = right;
        int pivot = this.entries[(left + right) / 2];

        while (i <= j) {
            // Lesser
            while (this.entries[i] < pivot) {
                ++i;
            }

            // Greater
            while (this.entries[j] > pivot) {
                --j;
            }

            if (i <= j) {
                // Swap
                int temp = this.entries[i];
                this.entries[i] = this.entries[j];
                this.entries[j] = temp;

                ++i;
                --j;
            }
        }

        // Recurse
        if (left < j) {
            Quicksort(left, j);
        }

        if (i < right) {
            Quicksort(i, right);
        }
    }
}