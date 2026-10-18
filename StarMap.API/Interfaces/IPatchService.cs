using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StarMap.API.Interfaces
{

    public interface IPatchService
    {
        enum PatchType
        {
            Prefix,
            Postfix
        }


        delegate bool InstanceCallback<T, U>(object[] args, T instance, ref U result);
        [Experimental("PatchHelperExperimental")]
        void RegisterInstancePatch<T, U>(PatchType type, MethodBase method, InstanceCallback<T, U> callback);

        delegate bool InstanceVoidCallback<T>(object[] args, T instance);
        [Experimental("PatchHelperExperimental")]
        void RegisterInstanceVoidPatch<T>(PatchType type, MethodBase method, InstanceVoidCallback<T> callback);

        delegate bool StaticCallback<U>(object[] args, ref U result);
        [Experimental("PatchHelperExperimental")]
        void RegisterStaticPatch<U>(PatchType type, MethodBase method, StaticCallback<U> callback);

        delegate bool StaticVoidCallback(object[] args);
        [Experimental("PatchHelperExperimental")]
        void RegisterStaticVoidPatch(PatchType type, MethodBase method, StaticVoidCallback callback);
    }
}
