using System;
using NUnit.Framework;
using Parallel.CPU;
using Unity.Collections;
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
        var randomNumbers = new NativeArray<int>(count, Allocator.Temp);
        var random = new Random(0);
        for (var i = 0; i < randomNumbers.Length; i++) randomNumbers[i] = random.Next(0, 1000);
        
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
            var output = new NativeArray<int>(count, Allocator.TempJob);
            var binItems = new NumberBinItems { BinCount = 1000, Numbers = numbers };
            var sortable = new SortableData<int> { Input = numbers, Output = output };
            var sort = new CountingSort(new SingleThreadPrefixSum<IntNumber, int>());
            Measure.Method(() =>
                {
                    using var handle = sort.Schedule(ref binItems, ref sortable);
                    handle.Complete();
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