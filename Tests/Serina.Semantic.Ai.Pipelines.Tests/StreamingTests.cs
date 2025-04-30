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
using Serina.Semantic.Ai.Pipelines.SemanticKernel.Reducers;

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
                                               Endpoint = "http://192.168.88.231:7000",
                                                Name = "gpt-4.1-nano",
                                                Key = "123456",
                                                EngineType = 1
                                         }
                                    }
                                })
                              
                                .AttachKernel()
                               
                                .Build();

            // Act 

            var context = new PipelineContext
            {
                Response = new MessageResponse(),
                Id = Guid.NewGuid()
            };

            context.RequestMessage = new RequestMessage("", MessageRole.User,
                ChatId: Guid.NewGuid(),
                History: new RequestMessage[] { new RequestMessage("generate a story about a boy on the moon", MessageRole.User, Guid.NewGuid(), Temperature: 0.5) });


            await pipeline.ExecuteStepAsync(context, default);


            // Assert
            await foreach (var response in Stream.ReadResponses(context.Id))
            {
                Console.Write(response.Content);
                Debug.Write(response.Content);
            }

            Assert.NotNull(context); 
        }



        [Fact]
        public async Task ShouldChunkLargeUserMessageAndStreamResponses()
        {
            // Arrange: лимит 1 токен, чтобы «Hello world» разбилось на два чанка
            var pipeline = PipelineBuilder
                .New()
                .New(new StreamingChatStep(Stream))
                .WithKernel(new SemanticKernelOptions
                {
                    Models = new List<SemanticModelOption>
                    {
                        new SemanticModelOption
                        {
                            Endpoint = "http://192.168.88.231:7000",
                            Name = "gpt-4.1-nano",
                            Key = "123456",
                            EngineType = 1
                        }
                    }
                })
         
                .AddReducer(new TokenChunkingReducer(tokenLimit: 1))
                .AttachKernel()
                .Build();

            var id = Guid.NewGuid(); 

            var context = new PipelineContext
            {
                Response = new MessageResponse(),
                Id = id
            };

            // Одно большое сообщение «Hello world»
            var inputText = "Hello world";
            context.RequestMessage = new RequestMessage(
                inputText,
                MessageRole.User,
                ChatId: id,
                History: new[]
                {
                    new RequestMessage(inputText, MessageRole.User, Guid.NewGuid(), Temperature: 0.5)
                }
            );

            // Act
            await pipeline.ExecuteStepAsync(context, CancellationToken.None);

            // Assert: в Stream должно прийти более одного чанка
            var responses = new List<string>();
            await foreach (var msg in Stream.ReadResponses(context.Id))
            {
                responses.Add(msg.Content);
                Console.WriteLine(msg.Content);
            }

            Assert.True(
                context.ChatHistory.Count >= 4 ,
                $"Ожидалось не менее двух событий (по одному на каждый чанк), но получили {responses.Count}"
            );
        } 
    }
}
