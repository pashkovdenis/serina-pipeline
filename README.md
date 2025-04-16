# Semantic Kernel Pipeline NuGet Package

Samples: https://github.com/pashkovdenis/serinasamples

## Updates

Added Autogen support, added example app and Autogen step!

## Overview
The **Semantic Kernel Pipeline** is a .NET 8 NuGet package that enables flexible AI-driven workflows. It supports **memory plugins, service selection, streaming responses, history reducers, and summarizers**, making it an ideal solution for conversational AI, intelligent data retrieval, and custom AI workflows.

## Features
- **AI Pipeline Creation** – Define custom AI processing steps.
- **Memory Plugins** – Store and retrieve historical data.
- **Service Selection** – Use multiple AI services with dynamic selection.
- **Streaming Support** – Process responses as a stream.
- **History Reducers & Summarizers** – Efficiently manage conversation history.
- **Media Processing** – Handle text, images, and other inputs.
- **Extensible Plugins** – Add custom plugins for specialized tasks.

## Installation

Install the package via NuGet:

```sh
Install-Package SemanticKernel.Pipeline
```

Or add it to your `.csproj` file:

```xml
<PackageReference Include="SemanticKernel.Pipeline" Version="1.0.0" />
```

## Quick Start

### 1. Create a Pipeline

```csharp
var pipeline = new PipelineBuilder()
    .AddStep(new SimpleChatStep())
    .AddStep(new HistoryReducer())
    .AddStep(new Summarizer())
    .Build();
```

### 2. Process a Request

```csharp
var result = await pipeline.ProcessAsync("Tell me a joke");
Console.WriteLine(result);
```

### 3. Streaming Responses

```csharp
await foreach (var chunk in pipeline.ProcessStreamAsync("Generate a story"))
{
    Console.Write(chunk);
}
```

### 4. Using a Custom Plugin

```csharp
public class CustomPlugin : IPipelinePlugin
{
    public async Task<string> ExecuteAsync(string input)
    {
        return $"Processed: {input}";
    }
}

var pipeline = new PipelineBuilder()
    .AddStep(new CustomPlugin())
    .Build();
```

## Advanced Usage

### Service Selection

Use **RandomServiceSelector** to dynamically pick AI models:

```csharp
var selector = new RandomServiceSelector(["gpt-4", "claude-3"]);
var pipeline = new PipelineBuilder()
    .UseServiceSelector(selector)
    .AddStep(new SimpleChatStep())
    .Build();
```

### Memory Integration

Store conversation history and recall past interactions:

```csharp
var memory = new InMemoryStorage();
var pipeline = new PipelineBuilder()
    .UseMemory(memory)
    .AddStep(new HistoryReducer())
    .Build();
```

### Summarization & History Reduction

Summarize long conversations to optimize memory:

```csharp
var pipeline = new PipelineBuilder()
    .AddStep(new Summarizer())
    .AddStep(new HistoryReducer())
    .Build();
```

### Processing Images with Vision Models

Supports multimodal AI pipelines (text + images):

```csharp
var visionStep = new AIImageProcessor("llama3.2-vision");
var pipeline = new PipelineBuilder()
    .AddStep(visionStep)
    .Build();

var result = await pipeline.ProcessAsync(imageInput);
```

## Extensibility

Create your own **steps**, **filters**, and **plugins** by implementing `IPipelinePlugin`.

```csharp
public class MyCustomStep : IPipelinePlugin
{
    public async Task<string> ExecuteAsync(string input)
    {
        return input.ToUpper();
    }
}
```

## Contributing

Contributions are welcome! Feel free to submit issues, feature requests, or pull requests.

## License

This project is licensed under the MIT License. See `LICENSE` for details.

