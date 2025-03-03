﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HybridCLR.Generators.MethodBridge
{
    internal abstract class PlatformAdaptorBase : IPlatformAdaptor
    {
        public abstract bool IsArch32 { get; }

        public abstract TypeInfo PointerType { get; }

        protected abstract Dictionary<Type, TypeInfo> CacheTypes { get; }

        protected abstract TypeInfo CreateValueType(Type type, bool returnValue);

        public abstract void GenerateCall(MethodBridgeSig method, List<string> lines);

        public abstract void GenerateInvoke(MethodBridgeSig method, List<string> lines);

        public abstract IEnumerable<MethodBridgeSig> GetPreserveMethods();

        private static Dictionary<Type, (int, int)> _typeSizeCache64 = new Dictionary<Type, (int, int)>();

        private static Dictionary<Type, (int, int)> _typeSizeCache32 = new Dictionary<Type, (int, int)>();

        private static ValueTypeSizeAligmentCalculator s_calculator64 = new ValueTypeSizeAligmentCalculator(false);

        private static ValueTypeSizeAligmentCalculator s_calculator32 = new ValueTypeSizeAligmentCalculator(true);

        public static (int Size, int Aligment) ComputeSizeAndAligmentOfArch64(Type t)
        {
            if (_typeSizeCache64.TryGetValue(t, out var sizeAndAligment))
            {
                return sizeAndAligment;
            }
            // all this just to invoke one opcode with no arguments!
            var method = new DynamicMethod("ComputeSizeOfImpl", typeof(int), Type.EmptyTypes, typeof(PlatformAdaptorBase), false);
            var gen = method.GetILGenerator();
            gen.Emit(OpCodes.Sizeof, t);
            gen.Emit(OpCodes.Ret);
            int clrSize = ((Func<int>)method.CreateDelegate(typeof(Func<int>)))();

            sizeAndAligment = s_calculator64.SizeAndAligmentOf(t);
            int customSize = sizeAndAligment.Item1;
            if (customSize != clrSize)
            {
                s_calculator64.SizeAndAligmentOf(t);
                Debug.LogError($"type:{t} size calculate error. HybridCLR Comput:{sizeAndAligment} CLR:{clrSize}");
            }
            _typeSizeCache64.Add(t, sizeAndAligment);
            return sizeAndAligment;
        }

        protected static (int Size, int Aligment) ComputeSizeAndAligmentOfArch32(Type t)
        {
            if (_typeSizeCache32.TryGetValue(t, out var sa))
            {
                return sa;
            }
            // all this just to invoke one opcode with no arguments!
            sa = s_calculator32.SizeAndAligmentOf(t);
            _typeSizeCache32.Add(t, sa);
            return sa;
        }

        public TypeInfo CreateTypeInfo(Type type, bool returnValue)
        {
            if (type.IsByRef)
            {
                return PointerType;
            }
            if (type == typeof(void))
            {
                return TypeInfo.s_void;
            }
            if (!type.IsValueType)
            {
                return PointerType;
            }
            if (CacheTypes.TryGetValue(type, out var cache))
            {
                return cache;
            }
            if (type.IsEnum)
            {
                return CreateTypeInfo(type.GetEnumUnderlyingType(), returnValue);
            }
            var ti = CreateValueType(type, returnValue);
            // s_typeInfoCaches.Add(type, ti);
            return ti;
        }

        protected static TypeInfo CreateGeneralValueType(Type type, int size, int aligment)
        {
            Debug.Assert(size % aligment == 0);
            switch (aligment)
            {
                case 1: return new TypeInfo(type, ParamOrReturnType.STRUCTURE_ALIGN1, size);
                case 2: return new TypeInfo(type, ParamOrReturnType.STRUCTURE_ALIGN2, size);
                case 4: return new TypeInfo(type, ParamOrReturnType.STRUCTURE_ALIGN4, size);
                case 8: return new TypeInfo(type, ParamOrReturnType.STRUCTURE_ALIGN8, size);
                default: throw new NotSupportedException($"type:{type} not support aligment:{aligment}");
            }
        }

        public void GenCallStub(List<MethodBridgeSig> methods, List<string> lines)
        {
            lines.Add($@"
NativeCallMethod hybridclr::interpreter::g_callStub[] = 
{{
");

            foreach (var method in methods)
            {
                lines.Add($"\t{{\"{method.CreateInvokeSigName()}\", (Il2CppMethodPointer)__Native2ManagedCall_{method.CreateInvokeSigName()}, (Il2CppMethodPointer)__Native2ManagedCall_AdjustorThunk_{method.CreateCallSigName()}, __Managed2NativeCall_{method.CreateInvokeSigName()}}},");
            }

            lines.Add($"\t{{nullptr, nullptr}},");
            lines.Add("};");
        }

        public void GenInvokeStub(List<MethodBridgeSig> methods, List<string> lines)
        {
            lines.Add($@"
NativeInvokeMethod hybridclr::interpreter::g_invokeStub[] = 
{{
");

            foreach (var method in methods)
            {
                lines.Add($"\t{{\"{method.CreateInvokeSigName()}\", __Invoke_instance_{method.CreateInvokeSigName()}, __Invoke_static_{method.CreateInvokeSigName()}}},");
            }

            lines.Add($"\t{{nullptr, nullptr, nullptr}},");
            lines.Add("};");
        }
    }
}
