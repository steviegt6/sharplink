using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public class FunctionEmitter {
    private readonly CompiledFunction function;
    private readonly AssemblyDefinition asmDef;
    private readonly HlCodeHash hash;
    private readonly Compilation compilation;

    private int currentOpcodeIndex;

    public HlOpcode[] Opcodes => function.Function!.Opcodes;

    public HlOpcode CurrentOpcode => Opcodes[currentOpcodeIndex];

    public FunctionEmitter(CompiledFunction function, AssemblyDefinition asmDef, HlCodeHash hash, Compilation compilation) {
        if (function.Function is null)
            throw new NullReferenceException(nameof(function.Function));

        this.function = function;
        this.asmDef = asmDef;
        this.hash = hash;
        this.compilation = compilation;
    }

    public void EmitToMethod(MethodDefinition method) {
        var locals = CreateMethodLocals();

        var body = method.Body = new MethodBody(method);
        foreach (var local in locals)
            method.Body.Variables.Add(local);

        var il = body.GetILProcessor();

        // Assign every parameter to a local variable corresponding to a
        // register.
        for (var i = 0; i < method.Parameters.Count; i++) {
            var param = method.Parameters[i];
            il.Emit(Ldarg, param);
            il.Emit(Stloc, locals[i]);
        }

        var markers = new Dictionary<int, Instruction>();
        for (var i = 0; i < Opcodes.Length; i++)
            markers[i] = il.Create(Nop);

        for (currentOpcodeIndex = 0;  currentOpcodeIndex < Opcodes.Length; currentOpcodeIndex++) {
            il.Append(markers[currentOpcodeIndex]);
            // GenerateInstruction(CurrentOpcode, locals, il, asmDef, i, markers, method);
        }
    }

#region Emission utilities
    /*private void PushCached<T>(ILProcessor il, int index) {
        if (typeof(T) == typeof(int) || typeof(T) == typeof(byte) || typeof(T) == typeof(ushort))
            il.Emit(Ldc_I4, hash.Code.Ints[index]);
        else if (typeof(T) == typeof(long))
            il.Emit(Ldc_I8, hash.Code.Ints[index]);
        else if (typeof(T) == typeof(float))
            il.Emit(Ldc_R4, (float)hash.Code.Floats[index]);
        else if (typeof(T) == typeof(double))
            il.Emit(Ldc_R8, hash.Code.Floats[index]);
        else if (typeof(T) == typeof(string))
            il.Emit(Ldstr, hash.Code.Strings[index]);
    }

    private void PushConverter<TSys, THaxe>(ILProcessor il, AssemblyDefinition asmDef) {
        il.Emit(Newobj, asmDef.MainModule.ImportReference(typeof(THaxe).GetConstructor(new[] { typeof(TSys) })));
    }

    private void LoadLocal(ILProcessor il, List<VariableDefinition> locals, int index) {
        il.Emit(Ldloc, locals[index]);
    }

    private void LoadLocalThatMayNeedToBeConvertedToHaxeDyn(ILProcessor il, List<VariableDefinition> locals, int index, TypeReference targetType, AssemblyDefinition asmDef) {
        LoadLocal(il, locals, index);

        var localVar = locals[index];

        if (targetType.FullName != "Tomat.SharpLink.HaxeDyn" || localVar.VariableType.FullName == "Tomat.SharpLink.HaxeDyn")
            return;

        if (localVar.VariableType.IsValueType)
            il.Emit(Box, localVar.VariableType);

        var haxeDynCtor = asmDef.MainModule.ImportReference(typeof(HaxeDyn).GetConstructor(new[] { typeof(object) }));
        il.Emit(Newobj, haxeDynCtor);
    }

    private void SetLocal(ILProcessor il, List<VariableDefinition> locals, int index) {
        il.Emit(Stloc, locals[index]);
    }

    private void LoadGlobal(ILProcessor il, int index, AssemblyDefinition asmDef) {
        var module = asmDef.MainModule.GetType("<Module>");
        var field = module.Fields.FirstOrDefault(field => field.Name == $"global{index}");
        il.Emit(Ldsfld, field);
    }

    private void SetGlobal(ILProcessor il, int index, AssemblyDefinition asmDef) {
        var module = asmDef.MainModule.GetType("<Module>");
        var field = module.Fields.FirstOrDefault(field => field.Name == $"global{index}");
        il.Emit(Stsfld, field);
    }

    private TypeReference GetTypeForLocal(List<VariableDefinition> locals, int index) {
        return locals[index].VariableType;
    }*/
#endregion

    private List<VariableDefinition> CreateMethodLocals() {
        var locals = new List<VariableDefinition>();

        // TODO: When I have time to clean up and optimize code, make it so we
        // don't lazily assign parameters to locals and treat them all like
        // regular hl registers.
        foreach (var local in function.Function!.LocalVariables) {
            var localType = compilation.TypeReferenceFromHlTypeRef(local, asmDef);
            locals.Add(new VariableDefinition(localType));
        }

        return locals;
    }

    private CompiledFunction CompiledFunctionFromFunctionIndex(int fIndex) {
        var corrected = hash.FunctionIndexes[fIndex];
        return corrected >= hash.Code.Functions.Count
            ? compilation.GetNative(hash.Code.Natives[corrected - hash.Code.Functions.Count])
            : compilation.GetFunction(hash.Code.Functions[corrected]);
    }
}
