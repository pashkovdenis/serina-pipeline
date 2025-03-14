using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.Steps.Chat;
using Serina.Semantic.Ai.Pipelines.Streams;
using Serina.Semantic.Ai.Pipelines.Utils;
using Serina.Semantic.Ai.Pipelines.ValueObject;
using System.Text;
using System.Text.RegularExpressions;

namespace Serina.Semantic.Ai.Pipeline.Ollama.Sample
{
    internal class Program
    {
        private static IPipelineStream Stream = new PipelineChannelStream();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Example llama chat pipeline.");
            Console.WriteLine("Please enter ollama endpoint ex: http://127.0.0.1:11434");
            
            string endpoint;

            do
            {
                Console.Write("Enter an endpoint (format: http://ip:port): ");
                endpoint = Console.ReadLine();
            }
            while (!IsValidEndpoint(endpoint));

            Console.WriteLine($"Valid endpoint entered: {endpoint}");

            Console.Write("Enter model name to use : ");

            var modelName = Console.ReadLine();

            var pipeline = PipelineBuilder.New().New(new StreamingChatStep(Stream))
                                .WithKernel(new SemanticKernelOptions
                                {
                                    Models = new List<SemanticModelOption>
                                    {
                                         new SemanticModelOption
                                         {
                                              Endpoint = endpoint,
                                               Name = modelName,
                                               Key = "123456",EngineType = 1
                                         }
                                    }
                                }).AttachKernel().Build();

            var context = new PipelineContext
            {
                Response = new MessageResponse(),
                Id = Guid.NewGuid()
            };

            var history = new List<RequestMessage>()
            {
                 new RequestMessage("You are helpfull assistant answer in one sentence.", MessageRole.System, Guid.NewGuid(), Temperature: 0.5)
            };
             


            Console.WriteLine("Write questions...");

            while (true)
            {
                var message = Console.ReadLine();

                history.Add(new RequestMessage(message,
                    MessageRole.User, Guid.NewGuid(), Temperature: 0.5));


                context.RequestMessage = new RequestMessage("", MessageRole.System,
                    ChatId: context.Id,
                    History: history.ToArray());


                await pipeline.ExecuteStepAsync(context, default);

                var rspBot = new StringBuilder();

                await foreach (var response in Stream.ReadResponses(context.Id))
                {
                    rspBot.AppendLine(response.Content);

                    Console.Write(response.Content);
                   
                }

                history.Add(new RequestMessage(rspBot.ToString(),
                    MessageRole.Bot, Guid.NewGuid(), Temperature: 0.5));
                Console.WriteLine("");
            }
        }

        static bool IsValidEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return false;

            string pattern = @"^http://(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}):(\d+)$";
            Match match = Regex.Match(endpoint, pattern);

            if (!match.Success)
            {
                Console.WriteLine("Invalid format. Please use http://ip:port (e.g., http://192.168.1.1:8080)");
                return false;
            }

            // Validate each IP segment is 0-255
            string[] ipParts = match.Groups[1].Value.Split('.');
            foreach (string part in ipParts)
            {
                if (int.Parse(part) > 255)
                {
                    Console.WriteLine("Invalid IP address. Each octet should be between 0-255.");
                    return false;
                }
            }

            // Validate port range (1-65535)
            int port = int.Parse(match.Groups[2].Value);
            if (port < 1 || port > 65535)
            {
                Console.WriteLine("Invalid port number. It should be between 1-65535.");
                return false;
            }

            return true;
        }

    }






}
