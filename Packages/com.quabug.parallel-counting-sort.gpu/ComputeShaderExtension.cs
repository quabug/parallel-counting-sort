using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Parallel.GPU
{
    public static class ComputeShaderExtension
    {
        public static ComputeBuffer CreateComputeBuffer<T>(this ref NativeArray<T> array) where T : unmanaged
        {
            var buffer = new ComputeBuffer(array.Length, UnsafeUtility.SizeOf<T>(), ComputeBufferType.Structured);
            buffer.SetData(array);
            return buffer;
        }

        public static (int kernelIndex, int threadGroups) GetKernelAndThreadGroup(this ComputeShader shader, string kernelName, int size)
        {
            var kernelIndex = shader.FindKernel(kernelName);
            shader.GetKernelThreadGroupSizes(kernelIndex, out var threadGroupSize, out _, out _);
            var threadGroups = (int) ((size + (threadGroupSize - 1)) / threadGroupSize);
            return (kernelIndex, threadGroups);
        }
    }
}