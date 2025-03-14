namespace Serina.Semantic.Ai.Pipelines.Tasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class TaskProcessor<T>
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        public bool IsBusy => _semaphore.CurrentCount == 0;

        private Func<T, Task> _task;

        public TaskProcessor(Func<T, Task> task)
        {
            _task = task;
        }

        public async Task RunAsync(T args)
        {
            await _semaphore.WaitAsync();
            try
            {
                await _task(args);
            }
            finally
            {
                _semaphore.Release();
            }
        }


    }

}
