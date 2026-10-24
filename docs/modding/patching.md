# Method patching

`IPatchService` registers prefix or postfix callbacks against a `MethodBase`. It supports static and instance methods, with or without return values. Callbacks receive the target method's arguments; instance callbacks also receive the target object, and callbacks for methods with return values can read or change the result by reference.

## Choose a callback type

| Target method | Registration method | Callback parameters |
| --- | --- | --- |
| Static, returns `void` | `RegisterStaticVoidPatch` | `object[] args` |
| Static, returns a value | `RegisterStaticPatch<TResult>` | `object[] args`, `ref TResult result` |
| Instance, returns `void` | `RegisterInstanceVoidPatch<TInstance>` | `object[] args`, `TInstance instance` |
| Instance, returns a value | `RegisterInstancePatch<TInstance, TResult>` | `object[] args`, `TInstance instance`, `ref TResult result` |

`TInstance` is the target method's instance type. `TResult` must match the target method's return type. The argument array contains the original method arguments in declaration order.

## Prefix example

A prefix runs before the target method. Return `true` to continue to the next prefix and then the original method. Returning `false` stops later prefixes and skips the original method.

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

A postfix runs after the target method. Returning `false` from a postfix stops later postfix callbacks; the original method has already run. For a method that returns a value, use a result callback and modify `result` by reference when needed. Its generic `TResult` must exactly match the target method's return type.

When selecting a target with reflection, choose the intended overload explicitly. Prefer lifecycle callbacks where they meet the need; game-internal patch targets can vary across KSA versions.
