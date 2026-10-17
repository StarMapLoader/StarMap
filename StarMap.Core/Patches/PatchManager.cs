using HarmonyLib;
using StarMap.API.Interfaces;
using StarMap.Core.ModRepository;
using System.Diagnostics;
using System.Reflection;

namespace StarMap.Core.Patches
{
    internal interface IPatchManager
    {
        void RegisterInstancePatch<T, U>(IPatchHelper.PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchHelper.InstanceCallback<T, U> callback);
        void RegisterInstanceVoidPatch<T>(IPatchHelper.PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchHelper.InstanceVoidCallback<T> callback);
        void RegisterStaticPatch<U>(IPatchHelper.PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchHelper.StaticCallback<U> callback);
        void RegisterStaticVoidPatch(IPatchHelper.PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchHelper.StaticVoidCallback callback);
    }

    internal class PatchManager : IPatchManager
    {
        private static PatchManager? _instance;
        public static HarmonyMethod PrefixEntryPoint { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PrefixPatchEntryPoint), BindingFlags.Static | BindingFlags.Public));
        public static HarmonyMethod PostfixEntryPoint { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PostfixPatchEntryPoint), BindingFlags.Static | BindingFlags.Public));
        public static HarmonyMethod PrefixEntryPointVoid { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PrefixPatchEntryPointVoid), BindingFlags.Static | BindingFlags.Public));
        public static HarmonyMethod PostfixEntryPointVoid { get; } = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PostfixPatchEntryPointVoid), BindingFlags.Static | BindingFlags.Public));

        private readonly Harmony _harmony;

        private readonly Dictionary<MethodBase, MethodPatch> _patches = new();


        public PatchManager()
        {
            _harmony = new("com.starmap.core");

            _instance = this;
        }

        public void RegisterInstancePatch<T, U>(IPatchHelper.PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchHelper.InstanceCallback<T, U> callback)
        {
            GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new InstanceCallback<T, U>(callback));
        }
        public void RegisterInstanceVoidPatch<T>(IPatchHelper.PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchHelper.InstanceVoidCallback<T> callback)
        {
            GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new InstanceVoidCallback<T>(callback));
        }

        public void RegisterStaticPatch<U>(IPatchHelper.PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchHelper.StaticCallback<U> callback)
        {
            GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new StaticCallback<U>(callback));
        }

        public void RegisterStaticVoidPatch(IPatchHelper.PatchType patchType, RuntimeMod? mod, MethodBase method, IPatchHelper.StaticVoidCallback callback)
        {
            GetOrCreateMethodPatch(method).RegisterPatch(patchType, mod, new StaticVoidCallback(callback));
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

        public static bool PrefixPatchEntryPoint(
            MethodBase __originalMethod,
            object[] __args,
            object? __instance,
            ref object? __result)
        {
            return _instance?.PatchEntryPoint(HarmonyPatchType.Prefix, __originalMethod, __args, __instance, ref __result) ?? true;
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
            _instance?.PatchEntryPoint(HarmonyPatchType.Postfix, __originalMethod, __args, __instance, ref __result);
        }
    }

    internal interface IPatchCallback
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

    internal interface IVoidCallback : IPatchCallback { }
    internal interface IResultCallback : IPatchCallback { }

    internal class InstanceCallback<T, U>(IPatchHelper.InstanceCallback<T, U> callback) : IResultCallback
    {
        public bool Invoke(object[] args, object? instance, ref object? result)
        {
            var typedInstance = IPatchCallback.CastValue<T>(instance, nameof(instance));
            var typedResult = IPatchCallback.CastValue<U>(result, nameof(result));
            var callNext = callback(args, typedInstance, ref typedResult);

            result = typedResult;
            return callNext;
        }
    }

    internal class InstanceVoidCallback<T>(IPatchHelper.InstanceVoidCallback<T> callback) : IVoidCallback
    {
        public bool Invoke(object[] args, object? instance, ref object? result)
        {
            var typedInstance = IPatchCallback.CastValue<T>(instance, nameof(instance));
            var callNext = callback(args, typedInstance);
            return callNext;
        }
    }

    internal class StaticCallback<U>(IPatchHelper.StaticCallback<U> callback) : IResultCallback
    {
        public bool Invoke(object[] args, object? instance, ref object? result)
        {
            var typedResult = IPatchCallback.CastValue<U>(result, nameof(result));
            var callNext = callback(args, ref typedResult);

            result = typedResult;
            return callNext;
        }
    }

    internal class StaticVoidCallback(IPatchHelper.StaticVoidCallback callback) : IVoidCallback
    {
        public bool Invoke(object[] args, object? instance, ref object? result)
        {
            var callNext = callback(args);
            return callNext;
        }
    }



    internal class MethodPatch
    {
        private readonly Harmony _harmony;
        private readonly MethodBase _method;

        private MethodInfo? _prefixPatch;
        private MethodInfo? _postfixPatch;

        public MethodPatch(Harmony harmony, MethodBase method)
        {
            _harmony = harmony;
            _method = method;
        }

        public void RegisterPatch(IPatchHelper.PatchType type, RuntimeMod? mod, IPatchCallback patchCallback)
        {
            switch (type)
            {
                case IPatchHelper.PatchType.Prefix:
                    AddPrefix(mod, patchCallback);
                    break;

                case IPatchHelper.PatchType.Postfix:
                    AddPostfix(mod, patchCallback);
                    break;

                default:
                    throw new InvalidOperationException("Unknown type " + type);
            };
        }

        public void UnregisterPatch(IPatchHelper.PatchType type, RuntimeMod? mod)
        {
            switch (type)
            {
                case IPatchHelper.PatchType.Prefix:
                    RemovePrefix(mod);
                    break;

                case IPatchHelper.PatchType.Postfix:
                    RemovePostfix(mod);
                    break;

                default:
                    throw new InvalidOperationException("Unknown type " + type);
            }
            ;
        }


        public bool Invoke(HarmonyPatchType type, object[] args, object? instance, ref object? result)
        {
            var callNext = true;

            foreach (var (_, callback) in GetActions(type))
            {
                if (callback.Invoke(args, instance, ref result))
                {
                    callNext = false;
                    break;
                }
            }

            return callNext;
        }

        private List<(RuntimeMod?, IPatchCallback)> GetActions(HarmonyPatchType type)
        {
            return type switch
            {
                HarmonyPatchType.Prefix => PrefixActions,
                HarmonyPatchType.Postfix => PostfixActions,
                _ => throw new Exception("Harmony patch type not supported")
            };
        }

        private void AddPrefix(RuntimeMod? mod, IPatchCallback callback)
        {
            if (PrefixActions.FindIndex((m) => m.mod == mod) > -1)
                return;

            var wasEmpty = PrefixActions.Count == 0;

            PrefixActions.Add((mod, callback));

            if (_prefixPatch is null)
            {
                if (callback is IVoidCallback)
                {
                    _prefixPatch = _harmony.Patch(_method, prefix: PatchManager.PrefixEntryPointVoid);
                }

                if (callback is IResultCallback)
                {
                    _prefixPatch = _harmony.Patch(_method, prefix: PatchManager.PrefixEntryPoint);
                }
            }
        }

        private void AddPostfix(RuntimeMod? mod, IPatchCallback callback)
        {
            if (PostfixActions.FindIndex((m) => m.mod == mod) > -1)
                return;

            var wasEmpty = PostfixActions.Count == 0;

            PostfixActions.Add((mod, callback));

            if (_postfixPatch is null)
            {
                if (callback is IVoidCallback)
                {
                    _postfixPatch = _harmony.Patch(_method, postfix: PatchManager.PostfixEntryPointVoid);
                }

                if (callback is IResultCallback)
                {
                    _postfixPatch = _harmony.Patch(_method, postfix: PatchManager.PostfixEntryPoint);
                }
            }
        }

        private void RemovePrefix(RuntimeMod? mod)
        {
            PrefixActions.RemoveAt(PrefixActions.FindIndex((m) => m.mod == mod));

            if (PrefixActions.Count == 0)
            {
                _harmony.Unpatch(_method, _prefixPatch);
            }
        }

        private void RemovePostfix(RuntimeMod? mod)
        {
            PostfixActions.RemoveAt(PostfixActions.FindIndex((m) => m.mod == mod));

            if (PostfixActions.Count == 0)
            {
                _harmony.Unpatch(_method, _postfixPatch);
            }
        }

        public List<(RuntimeMod? mod, IPatchCallback callback)> PrefixActions { get; } = [];
        public List<(RuntimeMod? mod, IPatchCallback callback)> PostfixActions { get; } = [];
    }
}
