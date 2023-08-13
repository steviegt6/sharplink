using System.Collections.Generic;
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

    protected void LoadConstInt(int value) {
        switch (value) {
            case -1:
                IL.Emit(Ldc_I4_M1);
                break;

            case 0:
                IL.Emit(Ldc_I4_0);
                break;

            case 1:
                IL.Emit(Ldc_I4_1);
                break;

            case 2:
                IL.Emit(Ldc_I4_2);
                break;

            case 3:
                IL.Emit(Ldc_I4_3);
                break;

            case 4:
                IL.Emit(Ldc_I4_4);
                break;

            case 5:
                IL.Emit(Ldc_I4_5);
                break;

            case 6:
                IL.Emit(Ldc_I4_6);
                break;

            case 7:
                IL.Emit(Ldc_I4_7);
                break;

            case 8:
                IL.Emit(Ldc_I4_8);
                break;

            default:
                IL.Emit(Ldc_I4, value);
                break;
        }
    }

    private void EmitArgumentLoadAddress(int index) {
        switch (index) {
            case < 0xFF:
                IL.Emit(Ldarga_S, (byte)index);
                break;

            default:
                IL.Emit(Ldarga, index);
                break;
        }
    }

    private void EmitLocalLoadAddress(int index) {
        switch (index) {
            case < 0xFF:
                IL.Emit(Ldloca_S, (byte)index);
                break;

            default:
                IL.Emit(Ldloca, index);
                break;
        }
    }

    protected void EmitDynamicTypeConversion(ITypeReferenceProvider fromProvider, ITypeReferenceProvider toProvider) {
        var from = fromProvider.GetReference(context);
        var to = toProvider.GetReference(context);

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

            case "Tomat.SharpLink.HaxeBytes": {
                if (from.FullName != typeof(string).FullName)
                    return;

                IL.Emit(Newobj, Module.ImportReference(typeof(HaxeBytes).GetConstructor(new[] { typeof(string) })));
                break;
            }
        }
    }

    /*protected void EmitNumberConverter(ITypeReferenceProvider leftProvider, ITypeReferenceProvider rightProvider) {
        var left = leftProvider.GetReference(context);
        var right = rightProvider.GetReference(context);
        
        // UI8
        // UI16
        // I32
        // I64
        // F32
        // F64
        // Priority: u8 -> u16 -> i32 -> i64 -> f32 -> f64
        var targetNumber = Ldc_I4;
        if (left.FullName == typeof(byte).FullName || left.FullName == typeof(ushort).FullName || left.FullName == typeof(int).FullName)
            
    }*/

    protected void LoadLocalRegisterAddress(LocalRegister register) {
        if (register.IsParameter)
            EmitArgumentLoadAddress(register.AdjustedIndex);
        else
            EmitLocalLoadAddress(register.AdjustedIndex);
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
            EmitLocalStore(register.AdjustedIndex);
    }

    protected void LoadCachedInt(int key) {
        LoadConstInt(Hash.Code.Ints[key]);
    }

    protected void LoadCachedFloat(int key) {
        IL.Emit(Ldc_R8, Hash.Code.Floats[key]);
    }

    protected void LoadCachedString(int key) {
        IL.Emit(Ldstr, Hash.Code.Strings[key]);
    }
}
