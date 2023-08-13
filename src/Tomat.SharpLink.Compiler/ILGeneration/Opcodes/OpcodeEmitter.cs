﻿using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public abstract class OpcodeEmitter {
    private readonly EmissionContext context;

    public HlOpcode Opcode =>  context.Opcode;

    public MethodDefinition Method => context.Method;

    public List<VariableDefinition> Locals => context.Locals;

    public Dictionary<int, JumpMarker> Markers => context.Markers;

    public ILProcessor IL => context.IL;

    public int Index => context.Index;

    public HlCodeHash Hash => context.Hash;

    public Compilation Compilation => context.Compilation;

    public AssemblyDefinition Assembly => context.Assembly;

    public ModuleDefinition Module => Assembly.MainModule;

    protected OpcodeEmitter(EmissionContext context) {
        this.context = context;
    }

    public abstract void Emit(FunctionEmitter emitter);

    protected LocalRegister CreateLocalRegister(int index) {
        return new LocalRegister(index, Method, Locals);
    }

    private void EmitArgumentLoad(int index) {
        switch (index) {
            case 0:
                IL.Emit(Ldarg_0);
                break;

            case 1:
                IL.Emit(Ldarg_1);
                break;

            case 2:
                IL.Emit(Ldarg_2);
                break;

            case 3:
                IL.Emit(Ldarg_3);
                break;

            case < 0xFF:
                IL.Emit(Ldarg_S, (byte)index);
                break;

            default:
                IL.Emit(Ldarg, index);
                break;
        }
    }

    private void EmitArgumentStore(int index) {
        switch (index) {
            case < 0xFF:
                IL.Emit(Starg_S, (byte)index);
                break;

            default:
                IL.Emit(Starg, index);
                break;
        }
    }

    private void EmitLocalLoad(int index) {
        switch (index) {
            case 0:
                IL.Emit(Ldloc_0);
                break;

            case 1:
                IL.Emit(Ldloc_1);
                break;

            case 2:
                IL.Emit(Ldloc_2);
                break;

            case 3:
                IL.Emit(Ldloc_3);
                break;

            case < 0xFF:
                IL.Emit(Ldloc_S, (byte)index);
                break;

            default:
                IL.Emit(Ldloc, index);
                break;
        }
    }

    private void EmitLocalStore(int index) {
        switch (index) {
            case 0:
                IL.Emit(Stloc_0);
                break;

            case 1:
                IL.Emit(Stloc_1);
                break;

            case 2:
                IL.Emit(Stloc_2);
                break;

            case 3:
                IL.Emit(Stloc_3);
                break;

            case < 0xFF:
                IL.Emit(Stloc_S, (byte)index);
                break;

            default:
                IL.Emit(Stloc, index);
                break;
        }
    }

    protected void EmitDynamicTypeConversion(ITypeReferenceProvider fromProvider, ITypeReferenceProvider toProvider) {
        var from = fromProvider.GetReference(Method, Locals);
        var to = toProvider.GetReference(Method, Locals);

        // Never going to need conversion between the same types.
        if (from.FullName == to.FullName)
            return;

        switch (to.FullName) {
            case "Tomat.SharpLink.HaxeDyn": {
                var haxeDynCtor = Module.ImportReference(typeof(HaxeDyn).GetConstructor(new[] { typeof(object) }));

                if (from.IsValueType)
                    IL.Emit(Box);

                IL.Emit(Newobj, haxeDynCtor);
                break;
            }
        }
    }

    protected void LoadLocalRegister(LocalRegister register) {
        if (register.IsParameter)
            EmitArgumentLoad(register.AdjustedIndex);
        else
            EmitLocalLoad(register.AdjustedIndex);
    }

    protected void StoreLocalRegister(LocalRegister register) {
        if (register.IsParameter)
            EmitArgumentStore(register.AdjustedIndex);
        else
            EmitLocalLoad(register.AdjustedIndex);
    }
}
