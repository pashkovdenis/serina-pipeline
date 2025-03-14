using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serina.Semantic.Ai.Pipelines.Tests
{
	using Microsoft.SemanticKernel;
	using Serina.Pipeline.App.SemanticKernel;
	using Serina.Pipeline.App.SemanticKernel.ServiceSelectors;
	using Serina.Pipeline.App.Filters;
	using System.ComponentModel;
	using Serina.Pipeline.Domain.Models;
	using Serina.Pipeline.App.Steps.Chat;
	using System.Text.Json.Serialization;
	using Serina.Pipeline.App.Interfaces;
	using Serina.Pipeline.App.Utils;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070

	namespace PipelineTests
	{
		public class PipelineTests
		{
			[Fact]
			public async Task Chat_Sample_Test()
			{
				var builder = PipelineBuilder.New();

				var name = "ChatPipe";

				IPipelineStep pipeline;

				if (PipelineRegistry.Exists(name))
				{
					pipeline = PipelineRegistry.Get(name);
				}
				else
				{

					pipeline = builder.New(new SimpleChatStep())
									  .WithKernel(new SemanticKernelOptions
									  {
										  Models = new List<SemanticModelOption>
										  {
										 new SemanticModelOption
										 {
											  Endpoint = "http://192.168.88.105:11434",
											   Name = "mistral",
											   Key = "123456",
											   EngineType = 1
										 }
										  }
									  }, serviceSelector: new RandomServiceSelector())
									  // .AddFilter(new TranslateFilter(_translateOptions.Value, _translateStorage))
									  .WithModelSelectStrategy(new RandomServiceSelector())
									  //.AddFilter(new TextChunkerFilter())
									  .WithName(name)
									  .WithPlugin(new SamplePLugin(), "GetTime")
									  //.WithPlugin(new LightsPlugin(),"Lights")
									  //.WithCache("http://192.168.88.105:11434", "nomic-embed-text")
									  .AttachKernel()
									  // .AddOllamaChatCompletion("http://192.168.88.105:11434", "mistral")
									  .Build();
				}


				// Act 

				var context = new PipelineContext
				{
					RequestMessage = new RequestMessage("You are helpfull assistant", Serina.Pipeline.Domain.ValueObject.MessageRole.System, Guid.NewGuid()),
					Response = new MessageResponse()

				};

				context.RequestMessage = new RequestMessage("Tell me what time is it?", Serina.Pipeline.Domain.ValueObject.MessageRole.User, Guid.NewGuid(), Temperature: 0.5, ServiceId: "mistral");

				var s2 = await pipeline.ExecuteStepAsync(context, default);

				// Assert 

				Assert.NotNull(s2);

			}

		}

		/// <summary>
		/// Represent a simple plugin for the semantic kernel
		/// </summary>
		public class SamplePLugin
		{

			[KernelFunction("get_time")]
			[Description("Retrieves the current time")]
			[return: Description("The current time")]
			public string GetTime() => DateTime.UtcNow.ToString("R");

		}

		public class LightsPlugin
		{
			// Mock data for the lights
			private readonly List<LightModel> lights = new()
   {
	  new LightModel { Id = 1, Name = "Table Lamp", IsOn = false, Brightness = 100, Hex = "FF0000" },
	  new LightModel { Id = 2, Name = "Porch light", IsOn = false, Brightness = 50, Hex = "00FF00" },
	  new LightModel { Id = 3, Name = "Chandelier", IsOn = true, Brightness = 75, Hex = "0000FF" }
   };

			[KernelFunction("get_lights")]
			[Description("Gets a list of lights and their current state")]
			[return: Description("An array of lights")]
			public async Task<List<LightModel>> GetLightsAsync()
			{
				return lights;
			}

			[KernelFunction("get_state")]
			[Description("Gets the state of a particular light")]
			[return: Description("The state of the light")]
			public async Task<LightModel?> GetStateAsync([Description("The ID of the light")] int id)
			{
				// Get the state of the light with the specified ID
				return lights.FirstOrDefault(light => light.Id == id);
			}

			[KernelFunction("change_state")]
			[Description("Changes the state of the light")]
			[return: Description("The updated state of the light; will return null if the light does not exist")]
			public async Task<LightModel?> ChangeStateAsync(int id, LightModel LightModel)
			{
				var light = lights.FirstOrDefault(light => light.Id == id);

				if (light == null)
				{
					return null;
				}

				// Update the light with the new state
				light.IsOn = LightModel.IsOn;
				light.Brightness = LightModel.Brightness;
				light.Hex = LightModel.Hex;

				return light;
			}
		}






		public class LightModel
		{
			[JsonPropertyName("id")]
			public int Id { get; set; }

			[JsonPropertyName("name")]
			public string Name { get; set; }

			[JsonPropertyName("is_on")]
			public bool? IsOn { get; set; }

			[JsonPropertyName("brightness")]
			public byte? Brightness { get; set; }

			[JsonPropertyName("hex")]
			public string? Hex { get; set; }
		}
	}
}
