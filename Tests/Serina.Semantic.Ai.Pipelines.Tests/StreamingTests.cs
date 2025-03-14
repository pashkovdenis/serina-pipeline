using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;

using System.Diagnostics;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.Extensions.DependencyInjection;
using Serina.Semantic.Ai.Pipelines.Interfaces;
using Serina.Semantic.Ai.Pipelines.Streams;
using Serina.Semantic.Ai.Pipelines.Steps.Chat;
using Serina.Semantic.Ai.Pipelines.SemanticKernel;
using Serina.Semantic.Ai.Pipelines.Utils;
using Serina.Semantic.Ai.Pipelines.Models;
using Serina.Semantic.Ai.Pipelines.ValueObject;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070

namespace PipelineTests
{
    public sealed class StreamingTests
    {
        private readonly IPipelineStream Stream = new PipelineChannelStream();


        [Fact]
        public async Task ShouldWriteCompleteStreamOutput()
        {
            // Arrange       
           var pipeline = PipelineBuilder.New().New(new StreamingChatStep(Stream))
                                .WithKernel(new SemanticKernelOptions
                                {
                                    Models = new List<SemanticModelOption>
                                    {
                                         new SemanticModelOption
                                         {
                                              Endpoint = "http://192.168.88.105:11434",
                                               Name = "mistral",
                                               Key = "123456",EngineType = 1
                                         }
                                    }
                                })
                               //  .AddFilter(new TranslateFilter(_translateOptions.Value, _translateStorage))
                                //.WithModelSelectStrategy(new RandomServiceSelector())
                                //.AddFilter(new TextChunkerFilter()) 
                                
                                // .WithPlugin(new SamplePLugin(), "GetTime")
                                
                                //.WithPlugin(new LightsPlugin(),"Lights")
                                // .WithCache("http://192.168.88.105:11434", "nomic-embed-text")
                                .AttachKernel()
                                // .AddOllamaChatCompletion("http://192.168.88.105:11434", "mistral")
                                .Build();

            // Act 

            var context = new PipelineContext
            {
                Response = new MessageResponse(),
                Id = Guid.NewGuid()
            };

            context.RequestMessage = new RequestMessage("", MessageRole.User,
                ChatId: Guid.NewGuid(),
                History: new RequestMessage[] { new RequestMessage("Write short story about moon travel", MessageRole.User, Guid.NewGuid(), Temperature: 0.5) });


            await pipeline.ExecuteStepAsync(context, default);


            // Assert
            await foreach (var response in Stream.ReadResponses(context.Id))
            {
                Console.Write(response.Content);
                Debug.Write(response.Content);
            }

            Assert.NotNull(context); 
        }








    }
}
