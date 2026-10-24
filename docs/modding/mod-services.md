# Mod-provided services

`IModServiceRegistry` lets a provider mod expose an object through a shared service contract so dependent mods can use it. This is separate from StarMap's built-in services such as `IModMenuService` and `IMediationService`.

## Define and share a contract

Put the public interface in an assembly loaded by the provider. A small contracts assembly makes it possible to share the API without sharing the provider's implementation:

```csharp
namespace ProviderMod.Contracts;

public interface IGreetingService
{
	string Greet(string name);
}
```

Configure the provider to export this assembly and the consumer to import it. The consumer must also reference the contract assembly at compile time. See [sharing mod service contracts](dependencies.md#sharing-mod-service-contracts) for the dependency configuration.

## Register the service

Inject `IModServiceRegistry` into the provider's `[StarMapMod]` entry class and register the implementation:

```csharp
using ProviderMod.Contracts;
using StarMap.API;
using StarMap.API.Registries;

[StarMapMod]
public sealed class ProviderMod
{
	public ProviderMod(IModServiceRegistry services)
	{
		services.RegisterService<IGreetingService>(new GreetingService());
	}
}

internal sealed class GreetingService : IGreetingService
{
	public string Greet(string name) => $"Hello, {name}!";
}
```

Specify the contract type explicitly in `RegisterService<T>()`. Without the type argument, C# infers the implementation's concrete type, which consumers won't be able to retrieve as `IGreetingService`.

The contract type must belong to the registering mod's assembly load context. Register each contract type once; a second registration for the same contract fails.

## Retrieve the service

A consumer with a required dependency on the provider can retrieve the service from its entry class:

```csharp
using ProviderMod.Contracts;
using StarMap.API;
using StarMap.API.Registries;

[StarMapMod]
public sealed class ConsumerMod
{
	private readonly IGreetingService _greetings;

	public ConsumerMod(IModServiceRegistry services)
	{
		_greetings = services.Get<IGreetingService>();
	}
}
```

The provider must register before the consumer calls `Get<T>()`. A missing registration throws `InvalidOperationException`; declare the provider as a required dependency when the consumer needs the service during initialization. Only one provider can register a given contract type, and the registry does not support named or multiple implementations of the same contract.

The registry does not manage the service object's lifetime or dispose it. The provider owns its implementation and should release any resources it owns from its `[StarMapUnload]` callback. See [lifecycle callbacks](lifecycle.md).
