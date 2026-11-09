# Method patching

`IPatchService` registers prefix or postfix callbacks against a `MethodBase`. It supports static and instance methods, with or without return values. Callbacks receive the target method's arguments; instance callbacks also receive the target object, and callbacks for methods with return values can read or change the result by reference.

The patch registration methods are currently marked experimental in `StarMap.API`; C# projects will receive the `PatchHelperExperimental` diagnostic when they use them.

## Choose a callback type

| Target method | Registration method | Callback parameters |
| --- | --- | --- |
| Static or instance, returns `void`; callback omits the instance | `RegisterStaticVoidPatch` | `object[] args` |
| Static or instance, returns a value; callback omits the instance | `RegisterStaticPatch<TResult>` | `object[] args`, `ref TResult result` |
| Instance, returns `void` | `RegisterInstanceVoidPatch<TInstance>` | `object[] args`, `TInstance instance` |
| Instance, returns a value | `RegisterInstancePatch<TInstance, TResult>` | `object[] args`, `TInstance instance`, `ref TResult result` |

`TInstance` must be assignable from the target method's declaring type; instance callbacks cannot target static methods or constructors. `TResult` must match the target method's return type. The argument array contains the original method arguments in declaration order.

The dispatcher supports closed methods with ordinary boxable return types. It rejects open generic methods and methods returning by-reference, pointer, function-pointer, or byref-like types.

## Prefix example

A prefix runs before the target method. Return `true` to continue to the next prefix and then the original method. Returning `false` stops later prefixes and skips the original method.

For a target that returns a value, `result` starts with the type's default value. If the prefix returns `true`, the original method normally replaces that value. To provide a replacement result, set `result` and return `false`.

```csharp
using System;
using System.Reflection;
using StarMap.API;
using StarMap.API.Services;

public static class ExampleTarget
{
    public static void Run(int value)
    {
        Console.WriteLine($"Running with {value}");
    }
}

[StarMapMod]
public sealed class ExampleMod
{
    private readonly IPatchService _patches;

    public ExampleMod(IPatchService patches)
    {
        _patches = patches;
    }

    [StarMapBeforeMain]
    public void Initialize()
    {
        MethodInfo target = typeof(ExampleTarget).GetMethod(
            nameof(ExampleTarget.Run),
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException(
                typeof(ExampleTarget).FullName,
                nameof(ExampleTarget.Run));

        _patches.RegisterStaticVoidPatch(
            IPatchService.PatchType.Prefix,
            target,
            args =>
            {
                var value = (int)args[0];
                Console.WriteLine($"Before Run({value})");
                return value >= 0;
            });
    }
}
```

## Postfixes and return values

A postfix runs after the target method. Returning `false` stops later postfix callbacks registered through this `IPatchService` dispatcher; other Harmony postfixes still run. The original method has already run. For a method that returns a value, use a result callback and modify `result` by reference when needed. Its generic `TResult` must exactly match the target method's return type.

When selecting a target with reflection, choose the intended overload explicitly. Prefer lifecycle callbacks where they meet the need; game-internal patch targets can vary across KSA versions.
