using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class Compilation {
    private readonly Dictionary<HlFunction, CompiledFunction> functions = new();
    private readonly Dictionary<HlNative, CompiledFunction> natives = new();
    private readonly Dictionary<HlTypeWithAbsName, CompiledAbstract> abstracts = new();
    private readonly Dictionary<HlTypeWithEnum, CompiledEnum> enums = new();
    private readonly Dictionary<HlTypeWithFun, CompiledFun> funs = new();
    private readonly Dictionary<HlTypeWithObj, CompiledObj> objs = new();
    private readonly Dictionary<HlTypeWithVirtual, CompiledVirtual> virtuals = new();

    private readonly Dictionary<TypeDefinition, CompiledObj> compiledObjsByType = new();
    private readonly Dictionary<TypeDefinition, CompiledVirtual> compiledVirtualsByType = new();

    public CompiledFunction GetFunction(HlFunction function) {
        return functions[function];
    }

    public CompiledFunction GetNative(HlNative native) {
        return natives[native];
    }

    public CompiledAbstract GetAbstract(HlTypeWithAbsName abs) {
        return abstracts[abs];
    }

    public CompiledEnum GetEnum(HlTypeWithEnum @enum) {
        return enums[@enum];
    }

    public CompiledFun GetFun(HlTypeWithFun fun) {
        return funs[fun];
    }

    public CompiledObj GetObj(HlTypeWithObj obj) {
        return objs[obj];
    }

    public CompiledVirtual GetVirtual(HlTypeWithVirtual @virtual) {
        return virtuals[@virtual];
    }

    public void AddFunction(HlFunction function, CompiledFunction compiledFunction) {
        functions.Add(function, compiledFunction);
    }

    public void AddNative(HlNative native, CompiledFunction compiledNative) {
        natives.Add(native, compiledNative);
    }

    public void AddAbstract(HlTypeWithAbsName abs, CompiledAbstract compiledAbstract) {
        abstracts.Add(abs, compiledAbstract);
    }

    public void AddEnum(HlTypeWithEnum @enum, CompiledEnum compiledEnum) {
        enums.Add(@enum, compiledEnum);
    }

    public void AddFun(HlTypeWithFun fun, CompiledFun compiledFun) {
        funs.Add(fun, compiledFun);
    }

    public void AddObj(HlTypeWithObj obj, CompiledObj compiledObj) {
        objs.Add(obj, compiledObj);
        compiledObjsByType.Add(compiledObj.Type, compiledObj);
    }

    public void AddVirtual(HlTypeWithVirtual @virtual, CompiledVirtual compiledVirtual) {
        virtuals.Add(@virtual, compiledVirtual);
        compiledVirtualsByType.Add(compiledVirtual.Type, compiledVirtual);
    }

    public List<FieldDefinition> GetAllFieldsFor(TypeDefinition type) {
        if (compiledObjsByType.TryGetValue(type, out var compiledObj))
            return compiledObj.AllFields;

        compiledObj = objs.Values.FirstOrDefault(x => x.Type.FullName == type.FullName);

        if (compiledObj is not null) {
            compiledObjsByType.Add(type, compiledObj);
            return compiledObj.AllFields;
        }

        if (compiledVirtualsByType.TryGetValue(type, out var compiledVirtual))
            return compiledVirtual.AllFields;

        compiledVirtual = virtuals.Values.FirstOrDefault(x => x.Type.FullName == type.FullName);

        if (compiledVirtual is not null) {
            compiledVirtualsByType.Add(type, compiledVirtual);
            return compiledVirtual.AllFields;
        }

        throw new KeyNotFoundException($"Could not find compiled object or virtual for type {type.FullName}");
    }

    public List<FieldDefinition> GetAllProtosFor(TypeDefinition type) {
        if (compiledObjsByType.TryGetValue(type, out var compiledObj))
            return compiledObj.AllProtos;

        compiledObj = objs.Values.FirstOrDefault(x => x.Type.FullName == type.FullName);

        if (compiledObj is not null) {
            compiledObjsByType.Add(type, compiledObj);
            return compiledObj.AllProtos;
        }

        throw new KeyNotFoundException($"Could not find compiled object for type {type.FullName}");
    }
}
