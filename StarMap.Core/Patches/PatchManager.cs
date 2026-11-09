using HarmonyLib;
using StarMap.API.Services;
using StarMap.Core.ModRepository;
using StarMap.Core.Services;
using System.Reflection;
using static StarMap.API.Services.IPatchService;

namespace StarMap.Core.Patches
{
    internal interface IPatchManager : IDisposable
    {
        IDisposable RegisterInstancePatch<T, U>(PatchType patchType, RuntimeMod? mod, MethodBase? method, IPatchService.InstanceCallback<T, U> callback);
        IDisposable RegisterInstanceVoidPatch<T>(PatchType patchType, RuntimeMod? mod, MethodBase? method, IPatchService.InstanceVoidCallback<T> callback);
        IDisposable RegisterStaticPatch<U>(PatchType patchType, RuntimeMod? mod, MethodBase? method, IPatchService.StaticCallback<U> callback);
        IDisposable RegisterStaticVoidPatch(PatchType patchType, RuntimeMod? mod, MethodBase? method, IPatchService.StaticVoidCallback callback);
    }

    internal class PatchManager : IPatchManager
    {
        private static PatchManager? _instance;
        public static HarmonyMethod PrefixEntryPointVoid { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PrefixPatchEntryPointVoid), BindingFlags.Static | BindingFlags.Public));
        public static HarmonyMethod PrefixEntryPointResult { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PrefixPatchEntryPoint), BindingFlags.Static | BindingFlags.Public));
        public static HarmonyMethod PostfixEntryPointVoid { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PostfixPatchEntryPointVoid), BindingFlags.Static | BindingFlags.Public));
        public static HarmonyMethod PostfixEntryPointResult { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PostfixPatchEntryPoint), BindingFlags.Static | BindingFlags.Public));

        private readonly ILogger _logger;
        private readonly Harmony _harmony;

        private readonly Dictionary<MethodBase, MethodPatch> _patches = new();

        public PatchManager(ILogger logger)
        {
            _logger = logger;
            _harmony = new("com.starmap.core");

            _instance = this;
        }

        public IDisposable RegisterInstancePatch<T, U>(PatchType patchType, RuntimeMod? mod, MethodBase? method, IPatchService.InstanceCallback<T, U> callback)
        {
            if (method is null) return MissingTarget(mod);
            ValidateInstanceTarget<T>(method);
            return GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new InstanceCallback<T, U>(callback), _logger);
        }
        public IDisposable RegisterInstanceVoidPatch<T>(PatchType patchType, RuntimeMod? mod, MethodBase? method, IPatchService.InstanceVoidCallback<T> callback)
        {
            if (method is null) return MissingTarget(mod);
            ValidateInstanceTarget<T>(method);
            return GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new InstanceVoidCallback<T>(callback), _logger);
        }

        public IDisposable RegisterStaticPatch<U>(PatchType patchType, RuntimeMod? mod, MethodBase? method, IPatchService.StaticCallback<U> callback)
        {
            if (method is null) return MissingTarget(mod);
            return GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new StaticCallback<U>(callback), _logger);
        }
        public IDisposable RegisterStaticVoidPatch(PatchType patchType, RuntimeMod? mod, MethodBase? method, IPatchService.StaticVoidCallback callback)
        {
            if (method is null) return MissingTarget(mod);
            return GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new StaticVoidCallback(callback), _logger);
        }


        // A KSA update can remove a method StarMap hooks. Skipping the one callback keeps the loader
        // and every other mod running.
        private IDisposable MissingTarget(RuntimeMod? mod)
        {
            _logger.Warning($"StarMap - A hook target is missing from this KSA build; a callback of {mod?.ModId ?? "StarMap"} will not run");
            return new NoPatch();
        }

        private static void ValidateInstanceTarget<T>(MethodBase method)
        {
            var declaringType = method.DeclaringType;
            if (method.IsStatic || method is ConstructorInfo || declaringType is null)
            {
                throw new ArgumentException(
                    $"An instance callback requires an instance method with a declaring type; '{method}' is not a supported instance target.",
                    nameof(method));
            }

            if (!typeof(T).IsAssignableFrom(declaringType))
            {
                throw new ArgumentException(
                    $"Instance callback type '{typeof(T)}' cannot receive instances of target type '{declaringType}' for '{method}'.",
                    nameof(method));
            }
        }

        private sealed class NoPatch : IDisposable
        {
            public void Dispose() { }
        }

        private MethodPatch GetOrCreateMethodPatch(MethodBase method)
        {
            if (!_patches.TryGetValue(method, out var methodPatch))
            {
                var newPatch = new MethodPatch(_harmony, method, _logger);
                _patches.Add(method, newPatch);
                return newPatch;
            }

            if (methodPatch is not MethodPatch typedMethodPatch)
                throw new Exception();

            return typedMethodPatch;

        }

        private bool PatchEntryPoint(
            HarmonyPatchType type,
            MethodBase originalMethod,
            object[] args,
            object? instance,
            ref object? result)
        {
            var outcome = !_patches.TryGetValue(originalMethod, out var methodPatch)
                || methodPatch.Invoke(type, args, instance, ref result);

            return outcome;
        }

        public static bool PrefixPatchEntryPointVoid(
            MethodBase __originalMethod,
            object[] __args,
            object? __instance)
        {
            object? result = null;

            return _instance?.PatchEntryPoint(HarmonyPatchType.Prefix, __originalMethod, __args, __instance, ref result) ?? true;
        }

        public static bool PrefixPatchEntryPoint(
            MethodBase __originalMethod,
            object[] __args,
            object? __instance,
            ref object? __result)
        {
            var manager = _instance;
            if (manager is null)
                return true;

            return manager.PatchEntryPoint(HarmonyPatchType.Prefix, __originalMethod, __args, __instance, ref __result);
        }

        public static void PostfixPatchEntryPointVoid(
            MethodBase __originalMethod,
            object[] __args,
            object? __instance)
        {
            object? result = null;

            _instance?.PatchEntryPoint(HarmonyPatchType.Postfix, __originalMethod, __args, __instance, ref result);
        }

        public static void PostfixPatchEntryPoint(
            MethodBase __originalMethod,
            object[] __args,
            object? __instance,
            ref object? __result)
        {
            var manager = _instance;
            if (manager is null)
                return;

            manager.PatchEntryPoint(HarmonyPatchType.Postfix, __originalMethod, __args, __instance, ref __result);
        }

        public void Dispose()
        {
            try
            {
                foreach (var patch in _patches.Values.ToArray())
                {
                    try
                    {
                        patch.Dispose();
                    }
                    catch (Exception ex)
                    {
                        SafeWarning($"StarMap - Unexpected error while disposing Harmony hooks for '{patch.Method}': {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                SafeWarning($"StarMap - Unexpected error while enumerating Harmony hooks during disposal: {ex}");
            }
            finally
            {
                try
                {
                    _patches.Clear();
                }
                catch (Exception ex)
                {
                    SafeWarning($"StarMap - Unexpected error while clearing Harmony hooks during disposal: {ex}");
                }

                if (ReferenceEquals(_instance, this))
                    _instance = null;
            }
        }

        private void SafeWarning(string message)
        {
            try
            {
                _logger.Warning(message);
            }
            catch
            {
            }
        }

        #region Callbacks

        private interface IPatchCallback
        {
            bool Invoke(object[] args, object? instance, ref object? result);

            static V CastValue<V>(object? value, string valueName)
            {
                if (value is V typedValue)
                    return typedValue;

                if (value is null && default(V) is null)
                    return default!;

                throw new InvalidOperationException(
                    $"Patch value '{valueName}' is incompatible with callback type '{typeof(V)}'. Actual type: '{value?.GetType().ToString() ?? "null"}'.");
            }
        }

        private interface IVoidCallback : IPatchCallback { }
        private interface IResultCallback : IPatchCallback
        {
            Type ResultType { get; }
        }

        private class InstanceCallback<T, U>(IPatchService.InstanceCallback<T, U> callback) : IResultCallback
        {
            public Type ResultType => typeof(U);

            public bool Invoke(object[] args, object? instance, ref object? result)
            {
                var typedInstance = IPatchCallback.CastValue<T>(instance, nameof(instance));
                var typedResult = IPatchCallback.CastValue<U>(result, nameof(result));
                var callNext = callback(args, typedInstance, ref typedResult);

                result = typedResult;
                return callNext;
            }
        }

        private class InstanceVoidCallback<T>(IPatchService.InstanceVoidCallback<T> callback) : IVoidCallback
        {
            public bool Invoke(object[] args, object? instance, ref object? result)
            {
                var typedInstance = IPatchCallback.CastValue<T>(instance, nameof(instance));
                var callNext = callback(args, typedInstance);
                return callNext;
            }
        }

        private class StaticCallback<U>(IPatchService.StaticCallback<U> callback) : IResultCallback
        {
            public Type ResultType => typeof(U);

            public bool Invoke(object[] args, object? instance, ref object? result)
            {
                var typedResult = IPatchCallback.CastValue<U>(result, nameof(result));
                var callNext = callback(args, ref typedResult);

                result = typedResult;
                return callNext;
            }
        }

        private class StaticVoidCallback(IPatchService.StaticVoidCallback callback) : IVoidCallback
        {
            public bool Invoke(object[] args, object? instance, ref object? result)
            {
                var callNext = callback(args);
                return callNext;
            }
        }

        #endregion

        private class MethodPatch : IDisposable
        {
            public MethodBase Method => _method;

            private readonly Harmony _harmony;
            private readonly MethodBase _method;
            private readonly ILogger _logger;

            private MethodInfo? _prefixPatch;
            private MethodInfo? _postfixPatch;

            public MethodPatch(Harmony harmony, MethodBase method, ILogger logger)
            {
                _harmony = harmony;
                _method = method;
                _logger = logger;
            }

            public IDisposable RegisterPatch(PatchType type, RuntimeMod? mod, IPatchCallback patchCallback, ILogger logger)
            {
                ValidateTargetReturnType();

                if (patchCallback is IResultCallback resultCallback && GetReturnType() != resultCallback.ResultType)
                {
                    throw new ArgumentException(
                        $"Result callback type '{resultCallback.ResultType}' does not match the return type '{GetReturnType()}' of '{_method}'.",
                        nameof(patchCallback));
                }

                var patch = new ModMethodPatch(this, mod, patchCallback, type, logger);

                switch (type)
                {
                    case PatchType.Prefix:
                        AddPrefix(patch);
                        break;

                    case PatchType.Postfix:
                        AddPostfix(patch);
                        break;

                    default:
                        throw new InvalidOperationException("Unknown type " + type);
                }

                return patch;
            }

            public void UnregisterPatch(ModMethodPatch patch)
            {
                switch (patch.PatchType)
                {
                    case PatchType.Prefix:
                        RemovePrefix(patch);
                        break;

                    case PatchType.Postfix:
                        RemovePostfix(patch);
                        break;

                    default:
                        throw new InvalidOperationException("Unknown type " + patch.PatchType);
                }
            }


            public bool Invoke(HarmonyPatchType type, object[] args, object? instance, ref object? result)
            {
                var callNext = true;

                foreach (var patch in GetActions(type).ToArray())
                {
                    if (patch.IsDisposed)
                        continue;

                    if (!patch.Invoke(args, instance, ref result))
                    {
                        callNext = false;
                        break;
                    }
                }

                return callNext;
            }

            private List<ModMethodPatch> GetActions(HarmonyPatchType type)
            {
                return type switch
                {
                    HarmonyPatchType.Prefix => PrefixActions,
                    HarmonyPatchType.Postfix => PostfixActions,
                    _ => throw new Exception("Harmony patch type not supported")
                };
            }

            private void AddPrefix(ModMethodPatch patch)
            {
                if (_prefixPatch is null)
                {
                    var returnType = GetReturnType();
                    var prefix = returnType == typeof(void)
                        ? PrefixEntryPointVoid
                        : PrefixEntryPointResult;
                    _harmony.Patch(_method, prefix: prefix);
                    _prefixPatch = prefix.method;
                }

                PrefixActions.Add(patch);
            }

            private void AddPostfix(ModMethodPatch patch)
            {
                if (_postfixPatch is null)
                {
                    var returnType = GetReturnType();
                    var postfix = returnType == typeof(void)
                        ? PatchManager.PostfixEntryPointVoid
                        : PatchManager.PostfixEntryPointResult;
                    _harmony.Patch(_method, postfix: postfix);
                    _postfixPatch = postfix.method;
                }

                PostfixActions.Add(patch);
            }

            private Type GetReturnType()
            {
                return _method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);
            }

            private void ValidateTargetReturnType()
            {
                if (_method.ContainsGenericParameters)
                {
                    throw new NotSupportedException(
                        $"Open generic target '{_method}' cannot be patched through IPatchService. Supply a closed method instead.");
                }

                var returnType = GetReturnType();
                if (returnType.IsByRef || returnType.IsPointer || returnType.IsFunctionPointer || returnType.IsByRefLike)
                {
                    throw new NotSupportedException(
                        $"Target '{_method}' returns '{returnType}', which cannot be passed through the object-based Harmony result dispatcher.");
                }
            }

            private void RemovePrefix(ModMethodPatch patch)
            {
                PrefixActions.Remove(patch);

                if (PrefixActions.Count == 0)
                    TryUnpatch(ref _prefixPatch, "prefix");
            }

            private void RemovePostfix(ModMethodPatch patch)
            {
                PostfixActions.Remove(patch);

                if (PostfixActions.Count == 0)
                    TryUnpatch(ref _postfixPatch, "postfix");
            }

            public void Dispose()
            {
                PrefixActions.Clear();
                PostfixActions.Clear();
                TryUnpatch(ref _prefixPatch, "prefix");
                TryUnpatch(ref _postfixPatch, "postfix");
            }

            private bool TryUnpatch(ref MethodInfo? patch, string patchType)
            {
                var patchMethod = patch;
                if (patchMethod is null)
                    return true;

                try
                {
                    _harmony.Unpatch(_method, patchMethod);
                    patch = null;
                    return true;
                }
                catch (Exception ex)
                {
                    SafeWarning(
                        $"StarMap - Harmony could not remove its {patchType} for '{_method}'. " +
                        $"The callback has been disabled; the installed dispatcher is inert. " +
                        $"Harmony patches still registered: {GetPatchSummary()}. Error: {ex}");
                    return false;
                }
            }

            private string GetPatchSummary()
            {
                try
                {
                    var patchInfo = Harmony.GetPatchInfo(_method);
                    if (patchInfo is null)
                        return "(none)";

                    static string Describe(IEnumerable<Patch> patches)
                    {
                        var entries = patches.Select(patch => $"{patch.owner}: {patch.PatchMethod}").ToArray();
                        return entries.Length == 0 ? "(none)" : string.Join(" | ", entries);
                    }

                    return $"prefixes [{Describe(patchInfo.Prefixes)}]; postfixes [{Describe(patchInfo.Postfixes)}]";
                }
                catch (Exception ex)
                {
                    return $"(unavailable: {ex.Message})";
                }
            }

            private void SafeWarning(string message)
            {
                try
                {
                    _logger.Warning(message);
                }
                catch
                {
                }
            }

            public List<ModMethodPatch> PrefixActions { get; } = [];
            public List<ModMethodPatch> PostfixActions { get; } = [];
        }

        private class ModMethodPatch : IDisposable
        {
            public PatchType PatchType => _patchType;

            private readonly MethodPatch _patch;
            private readonly RuntimeMod? _mod;
            private readonly IPatchCallback _callback;
            private readonly PatchType _patchType;
            private readonly ILogger _logger;
            private int _disposed;

            public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

            public ModMethodPatch(MethodPatch methodPatch, RuntimeMod? mod, IPatchCallback callback, PatchType patchType, ILogger logger)
            {
                _patch = methodPatch;
                _mod = mod;
                _callback = callback;
                _patchType = patchType;
                _logger = logger;
            }

            public bool Invoke(object[] args, object? instance, ref object? result)
            {
                if (IsDisposed)
                    return true;

                try
                {
                    return _callback.Invoke(args, instance, ref result);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Mod '{_mod?.ModId ?? "StarMap"}' could not handle '{_patchType}' for method '{_patch.Method}' \n {ex.Message}");
                    return true;
                }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                try
                {
                    _patch.UnregisterPatch(this);
                }
                catch (Exception ex)
                {
                    try
                    {
                        _logger.Warning($"StarMap - Failed to unregister '{_patchType}' callback for '{_patch.Method}': {ex}");
                    }
                    catch
                    {
                    }
                }

            }
        }
    }
}
