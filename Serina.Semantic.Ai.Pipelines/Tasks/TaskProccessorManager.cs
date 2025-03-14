namespace Serina.Semantic.Ai.Pipelines.Tasks
{
    public class TaskProcessorManager<T>
    {
        private readonly List<TaskProcessor<T>> _processors;

        public TaskProcessorManager(List<TaskProcessor<T>> taskProccessors) => _processors = taskProccessors;


        public async Task RunOnAvailableProcessorAsync(T args)
        {
            while (true)
            {
                var availableProcessor = _processors.FirstOrDefault(p => !p.IsBusy);

                if (availableProcessor != null)
                {
                    await availableProcessor.RunAsync(args);
                    return;
                }

                // Wait briefly to avoid busy waiting
                await Task.Delay(50);
            }
        }
    }

}
