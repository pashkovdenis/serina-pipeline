using Microsoft.KernelMemory;
namespace Serina.Semantic.Ai.Pipelines.Utils
{
    public class MemoryRegister
    {
        private static Dictionary<string, IKernelMemory> _memoryKernels = new Dictionary<string, IKernelMemory>();

        public static bool Exists(string name) => _memoryKernels.ContainsKey(name);

        public static IKernelMemory Get(string name) => _memoryKernels[name];

        public static void Add(string name, IKernelMemory memory) => _memoryKernels.Add(name, memory);
    }
}
