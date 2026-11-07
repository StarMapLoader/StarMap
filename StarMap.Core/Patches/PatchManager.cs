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
        IDisposable RegisterInstancePatch<T, U>(PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchService.InstanceCallback<T, U> callback);
        IDisposable RegisterInstanceVoidPatch<T>(PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchService.InstanceVoidCallback<T> callback);
        IDisposable RegisterStaticPatch<U>(PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchService.StaticCallback<U> callback);
        IDisposable RegisterStaticVoidPatch(PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchService.StaticVoidCallback callback);
    }

    internal class PatchManager : IPatchManager
    {
        private static PatchManager? _instance;
        public static HarmonyMethod PrefixEntryPointVoid { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PrefixPatchEntryPointVoid), BindingFlags.Static | BindingFlags.Public));
        public static HarmonyMethod PostfixEntryPointVoid { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PostfixPatchEntryPointVoid), BindingFlags.Static | BindingFlags.Public));

        private readonly ILogger _logger;
        private readonly Harmony _harmony;

        private readonly Dictionary<MethodBase, MethodPatch> _patches = new();

        public PatchManager(ILogger logger)
        {
            _logger = logger;
            _harmony = new("com.starmap.core");

            _instance = this;
        }

        public IDisposable RegisterInstancePatch<T, U>(PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchService.InstanceCallback<T, U> callback)
        {
            return GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new InstanceCallback<T, U>(callback), _logger);
        }
        public IDisposable RegisterInstanceVoidPatch<T>(PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchService.InstanceVoidCallback<T> callback)
        {
            return GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new InstanceVoidCallback<T>(callback), _logger);
        }

        public IDisposable RegisterStaticPatch<U>(PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchService.StaticCallback<U> callback)
        {
            return GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new StaticCallback<U>(callback), _logger);
        }
        public IDisposable RegisterStaticVoidPatch(PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchService.StaticVoidCallback callback)
        {
            return GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new StaticVoidCallback(callback), _logger);
        }


        private MethodPatch GetOrCreateMethodPatch(MethodBase method)
        {
            if (!_patches.TryGetValue(method, out var methodPatch))
            {
                var newPatch = new MethodPatch(_harmony, method);
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
            //var stopwatch = Stopwatch.StartNew();

            var outcome = !_patches.TryGetValue(originalMethod, out var methodPatch)
                || methodPatch.Invoke(type, args, instance, ref result);

            //stopwatch.Stop();

            //Console.WriteLine($"{type} of {originalMethod.Name} took {stopwatch.Elapsed}");


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

        public static HarmonyMethod GetPrefixEntryPoint(Type resultType)
        {
            var method = typeof(PatchManager).GetMethod(nameof(PrefixPatchEntryPoint), BindingFlags.Static | BindingFlags.Public)!;
            return new HarmonyMethod(method.MakeGenericMethod(resultType));
        }

        public static HarmonyMethod GetPostfixEntryPoint(Type resultType)
        {
            var method = typeof(PatchManager).GetMethod(nameof(PostfixPatchEntryPoint), BindingFlags.Static | BindingFlags.Public)!;
            return new HarmonyMethod(method.MakeGenericMethod(resultType));
        }

        public static bool PrefixPatchEntryPoint<T>(
            MethodBase __originalMethod,
            object[] __args,
            object? __instance,
            ref T __result)
        {
            object? result = __result;
            var outcome = _instance?.PatchEntryPoint(HarmonyPatchType.Prefix, __originalMethod, __args, __instance, ref result) ?? true;
            __result = IPatchCallback.CastValue<T>(result, nameof(__result));
            return outcome;
        }

        public static void PostfixPatchEntryPointVoid(
            MethodBase __originalMethod,
            object[] __args,
            object? __instance)
        {
            object? result = null;

            _instance?.PatchEntryPoint(HarmonyPatchType.Postfix, __originalMethod, __args, __instance, ref result);
        }

        public static void PostfixPatchEntryPoint<T>(
            MethodBase __originalMethod,
            object[] __args,
            object? __instance,
            ref T __result)
        {
            object? result = __result;
            _instance?.PatchEntryPoint(HarmonyPatchType.Postfix, __originalMethod, __args, __instance, ref result);
            __result = IPatchCallback.CastValue<T>(result, nameof(__result));
        }

        public void Dispose()
        {
            foreach (var patch in _patches)
            {
                patch.Value.Dispose();
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

            private MethodInfo? _prefixPatch;
            private MethodInfo? _postfixPatch;

            public MethodPatch(Harmony harmony, MethodBase method)
            {
                _harmony = harmony;
                _method = method;
            }

            public IDisposable RegisterPatch(PatchType type, RuntimeMod? mod, IPatchCallback patchCallback, ILogger logger)
            {
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
                PrefixActions.Add(patch);

                if (_prefixPatch is null)
                {
                    var returnType = GetReturnType();
                    var prefix = returnType == typeof(void)
                        ? PrefixEntryPointVoid
                        : GetPrefixEntryPoint(returnType);
                    _harmony.Patch(_method, prefix: prefix);
                    _prefixPatch = prefix.method;
                }
            }

            private void AddPostfix(ModMethodPatch patch)
            {
                PostfixActions.Add(patch);

                if (_postfixPatch is null)
                {
                    var returnType = GetReturnType();
                    var postfix = returnType == typeof(void)
                        ? PatchManager.PostfixEntryPointVoid
                        : PatchManager.GetPostfixEntryPoint(returnType);
                    _harmony.Patch(_method, postfix: postfix);
                    _postfixPatch = postfix.method;
                }
            }

            private Type GetReturnType()
            {
                return _method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);
            }

            private void RemovePrefix(ModMethodPatch patch)
            {
                PrefixActions.Remove(patch);

                if (PrefixActions.Count == 0)
                {
                    _harmony.Unpatch(_method, _prefixPatch);
                    _prefixPatch = null;
                }
            }

            private void RemovePostfix(ModMethodPatch patch)
            {
                PostfixActions.Remove(patch);

                if (PostfixActions.Count == 0)
                {
                    _harmony.Unpatch(_method, _postfixPatch);
                    _postfixPatch = null;
                }
            }

            public void Dispose()
            {
                if (PrefixActions.Count > 0)
                {
                    _harmony.Unpatch(_method, _prefixPatch);
                    PrefixActions.Clear();
                }

                if (PostfixActions.Count > 0)
                {
                    _harmony.Unpatch(_method, _postfixPatch);
                    PostfixActions.Clear();
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

                _patch.UnregisterPatch(this);
            }
        }
    }
}
