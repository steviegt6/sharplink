using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Tomat.SharpLink.Compiler.Collections;

namespace Tomat.SharpLink.Compiler;

public class Compilation {
    private readonly MultiKeyDictionary<HlFunction, MethodDefinition, CompiledFunction> functions = new();
    private readonly MultiKeyDictionary<HlNative, MethodDefinition, CompiledFunction> natives = new();
    private readonly Dictionary<HlTypeWithAbsName, CompiledAbstract> abstracts = new();
    private readonly MultiKeyDictionary<HlTypeWithEnum, TypeDefinition, CompiledEnum> enums = new();
    private readonly MultiKeyDictionary<HlTypeWithFun, TypeDefinition, CompiledFun> funs = new();
    private readonly MultiKeyDictionary<HlTypeWithObj, TypeDefinition, CompiledObj> objs = new();
    private readonly MultiKeyDictionary<HlTypeWithVirtual, TypeDefinition, CompiledVirtual> virtuals = new();

#region Function
    public CompiledFunction GetFunction(HlFunction function) {
        return functions[function];
    }

    public CompiledFunction GetFunction(MethodDefinition method) {
        return functions[method];
    }

    public void AddFunction(CompiledFunction compiled) {
        if (compiled.Function is null)
            throw new NullReferenceException(nameof(compiled.Function));

        functions.Add(compiled.Function, compiled.Method, compiled);
    }
#endregion

#region Native
    public CompiledFunction GetNative(HlNative native) {
        return natives[native];
    }

    public CompiledFunction GetNative(MethodDefinition method) {
        return natives[method];
    }

    public void AddNative(CompiledFunction compiled) {
        if (compiled.Native is null)
            throw new NullReferenceException(nameof(compiled.Native));

        natives.Add(compiled.Native, compiled.Method, compiled);
    }
#endregion

#region Abstract
    public CompiledAbstract GetAbstract(HlTypeWithAbsName type) {
        return abstracts[type];
    }

    public void AddAbstract(CompiledAbstract compiled) {
        abstracts.Add(compiled.Abstract, compiled);
    }
#endregion

#region Enum
    public CompiledEnum GetEnum(HlTypeWithEnum type) {
        return enums[type];
    }

    public CompiledEnum GetEnum(TypeDefinition type) {
        return enums[type];
    }

    public void AddEnum(CompiledEnum compiled) {
        enums.Add(compiled.Enum, compiled.Type, compiled);
    }
#endregion

#region Fun
    public CompiledFun GetFun(HlTypeWithFun type) {
        return funs[type];
    }

    public CompiledFun GetFun(TypeDefinition type) {
        return funs[type];
    }

    public void AddFun(CompiledFun compiled) {
        funs.Add(compiled.Fun, compiled.Type, compiled);
    }
#endregion

#region Obj
    public CompiledObj GetObj(HlTypeWithObj type) {
        return objs[type];
    }

    public CompiledObj GetObj(TypeDefinition type) {
        return objs[type];
    }

    public void AddObj(CompiledObj compiled) {
        objs.Add(compiled.Obj, compiled.Type, compiled);
    }
#endregion

#region Virtual
    public CompiledVirtual GetVirtual(HlTypeWithVirtual type) {
        return virtuals[type];
    }

    public CompiledVirtual GetVirtual(TypeDefinition type) {
        return virtuals[type];
    }

    public void AddVirtual(CompiledVirtual compiled) {
        virtuals.Add(compiled.Virtual, compiled.Type, compiled);
    }
#endregion

    public List<FieldDefinition> GetAllFieldsFor(TypeDefinition type) {
        if (objs.TryGetValue(type, out var obj))
            return obj.AllFields;

        if (virtuals.TryGetValue(type, out var @virtual))
            return @virtual.AllFields;

        throw new KeyNotFoundException($"Could not find compiled object or virtual for type {type.FullName}");
    }

    public List<FieldDefinition> GetAllProtosFor(TypeDefinition type) {
        if (objs.TryGetValue(type, out var obj))
            return obj.AllProtos;

        throw new KeyNotFoundException($"Could not find compiled object for type {type.FullName}");
    }

    public TypeReference TypeReferenceFromHlTypeRef(HlTypeRef typeRef, AssemblyDefinition asmDef) {
        if (typeRef.Value is not { } type)
            throw new ArgumentException("Type reference was null.", nameof(typeRef));

        switch (type.Kind) {
            case HlTypeKind.Void:
                return asmDef.MainModule.ImportReference(typeof(HaxeVoid));

            case HlTypeKind.UI8:
                // return asmDef.MainModule.ImportReference(typeof(HaxeUI8));
                return asmDef.MainModule.TypeSystem.Byte;

            case HlTypeKind.UI16:
                // return asmDef.MainModule.ImportReference(typeof(HaxeUI16));
                return asmDef.MainModule.TypeSystem.UInt16;

            case HlTypeKind.I32:
                // return asmDef.MainModule.ImportReference(typeof(HaxeI32));
                return asmDef.MainModule.TypeSystem.Int32;

            case HlTypeKind.I64:
                // return asmDef.MainModule.ImportReference(typeof(HaxeI64));
                return asmDef.MainModule.TypeSystem.UInt32;

            case HlTypeKind.F32:
                // return asmDef.MainModule.ImportReference(typeof(HaxeF32));
                return asmDef.MainModule.TypeSystem.Single;

            case HlTypeKind.F64:
                // return asmDef.MainModule.ImportReference(typeof(HaxeF64));
                return asmDef.MainModule.TypeSystem.Double;

            case HlTypeKind.Bool:
                // return asmDef.MainModule.ImportReference(typeof(HaxeBool));
                return asmDef.MainModule.TypeSystem.Boolean;

            case HlTypeKind.Bytes:
                return asmDef.MainModule.ImportReference(typeof(HaxeBytes));

            case HlTypeKind.Dyn:
                return asmDef.MainModule.ImportReference(typeof(HaxeDyn));

            case HlTypeKind.Fun:
                return GetFun((HlTypeWithFun)type).Type;

            case HlTypeKind.Obj:
                return GetObj((HlTypeWithObj)type).Type;

            case HlTypeKind.Array:
                return asmDef.MainModule.ImportReference(typeof(HaxeArray));

            case HlTypeKind.Type:
                return asmDef.MainModule.ImportReference(typeof(HaxeType));

            case HlTypeKind.Ref:
                return asmDef.MainModule.ImportReference(typeof(HaxeRef<>)).MakeGenericInstanceType(TypeReferenceFromHlTypeRef(((HlTypeWithType)typeRef.Value).Type, asmDef));

            case HlTypeKind.Virtual:
                return GetVirtual((HlTypeWithVirtual)type).Type;

            case HlTypeKind.DynObj:
                throw new NotImplementedException();

            case HlTypeKind.Abstract:
                // TODO: Pointers definitely aren't the solution..., but...
                return asmDef.MainModule.TypeSystem.IntPtr;

            case HlTypeKind.Enum:
                return GetEnum((HlTypeWithEnum)type).Type;

            case HlTypeKind.Null:
                return asmDef.MainModule.ImportReference(typeof(HaxeNull<>)).MakeGenericInstanceType(TypeReferenceFromHlTypeRef(((HlTypeWithType)typeRef.Value).Type, asmDef));

            case HlTypeKind.Method:
                throw new NotImplementedException();

            case HlTypeKind.Struct:
                throw new NotImplementedException();

            case HlTypeKind.Packed:
                throw new NotImplementedException();

            default:
                throw new ArgumentOutOfRangeException(nameof(typeRef), "Type kind out range.");
        }
    }
}
