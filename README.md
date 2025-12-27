# GFramework

A comprehensive C# game development framework designed for Godot and general game development scenarios.

## Features

### Core Architecture

- **Dependency Injection**: Built-in IoC container for managing object lifecycles
- **Event System**: Type-safe event system for loose coupling
- **Property Binding**: Bindable properties for reactive programming
- **Logging Framework**: Structured logging with multiple log levels

### Game Development Features

- **Asset Management**: Centralized asset catalog system
- **Resource Factory**: Factory pattern for resource creation
- **Architecture Pattern**: Clean architecture with separation of concerns

### Godot Integration

- **Godot-Specific Extensions**: Extensions and utilities for Godot development
- **Node Extensions**: Helpful extensions for Godot Node classes
- **Godot Logger**: Specialized logging system for Godot applications

## Projects

### Core Projects

- **GFramework.Core**: Core framework functionality
- **GFramework.Game**: Game-specific abstractions and systems
- **GFramework.Godot**: Godot-specific implementations

### Source Generators

- **GFramework.SourceGenerators**: Code generators for automatic code generation
- **GFramework.Godot.SourceGenerators**: Godot-specific code generators
- **GFramework.SourceGenerators.Abstractions**: Abstractions for source generators
- **GFramework.Godot.SourceGenerators.Abstractions**: Godot-specific abstractions

## Getting Started

### Installation

1. Install the NuGet packages:

```bash
dotnet add package GeWuYou.GFramework.Core
dotnet add package GeWuYou.GFramework.Game
dotnet add package GeWuYou.GFramework.Godot
```

### Basic Usage

```csharp
// Create an architecture instance
var architecture = new MyArchitecture();

// Initialize the architecture
await architecture.InitializeAsync();

// Access services
var service = architecture.Container.Resolve<IMyService>();
```

### Godot Integration

```csharp
// Use Godot-specific features
[GodotLog]
public partial class MyGodotNode : Node
{
    // Auto-generated logger will be available
    private readonly ILogger _log = Log.GetLogger("MyGodotNode");
    
    public override void _Ready()
    {
        _log.Info("Node is ready!");
    }
}
```

## Architecture

The framework follows clean architecture principles with the following layers:

1. **Core Layer**: Fundamental abstractions and interfaces
2. **Application Layer**: Use cases and application services
3. **Infrastructure Layer**: External dependencies and implementations
4. **Presentation Layer**: UI and user interaction components

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting pull requests.

## Support

For support and questions, please open an issue in the repository.