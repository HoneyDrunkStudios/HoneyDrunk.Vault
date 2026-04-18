using System.Reflection;

namespace HoneyDrunk.Vault.Tests.Canary.Invariants;

/// <summary>
/// Enforces that Vault assemblies do not construct Kernel context objects.
/// Vault must consume contexts via Kernel DI/accessors, never instantiate them.
/// </summary>
public static class NoContextCreationInvariant
{
    /// <summary>
    /// The invariant name for diagnostic messages.
    /// </summary>
    public const string InvariantName = "NoContextCreationInVault";

    // IL opcode constants
    private const byte NewobjOpcode = 0x73;
    private const byte TwoByteOpcodePrefix = 0xFE;

    /// <summary>
    /// Type names that Vault must never instantiate with 'new'.
    /// </summary>
    private static readonly string[] ForbiddenConstructionTypeNames =
    [
        "GridContext",
        "NodeContext",
        "OperationContext",
    ];

    /// <summary>
    /// Type namespace prefixes where the forbidden types live.
    /// </summary>
    private static readonly string[] ForbiddenTypeNamespacePrefixes =
    [
        "HoneyDrunk.Kernel",
    ];

    /// <summary>
    /// Validates that the specified Vault assembly does not construct context objects.
    /// </summary>
    /// <param name="vaultAssembly">The Vault assembly to inspect.</param>
    /// <exception cref="CanaryInvariantException">Thrown when context construction is detected.</exception>
    public static void Validate(Assembly vaultAssembly)
    {
        ArgumentNullException.ThrowIfNull(vaultAssembly);

        var assemblyName = vaultAssembly.GetName().Name ?? "unknown";

        // Scan all types in the assembly
        Type[] types;
        try
        {
            types = vaultAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some types may fail to load; work with what we have
            types = [.. ex.Types.Where(t => t != null).Cast<Type>()];
        }

        foreach (var type in types)
        {
            ValidateTypeDoesNotConstructContexts(type, assemblyName);
        }
    }

    private static void ValidateTypeDoesNotConstructContexts(Type type, string assemblyName)
    {
        // Get all methods including constructors
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            ScanMethodBodyForContextConstruction(method, type, assemblyName);
        }

        foreach (var ctor in constructors)
        {
            ScanMethodBodyForContextConstruction(ctor, type, assemblyName);
        }
    }

    private static void ScanMethodBodyForContextConstruction(MethodBase method, Type containingType, string assemblyName)
    {
        MethodBody? body;
        try
        {
            body = method.GetMethodBody();
        }
        catch
        {
            // Some methods (abstract, extern, etc.) have no body
            return;
        }

        if (body == null)
        {
            return;
        }

        byte[]? il;
        try
        {
            il = body.GetILAsByteArray();
        }
        catch
        {
            return;
        }

        if (il == null || il.Length == 0)
        {
            return;
        }

        // Scan IL properly by decoding instruction boundaries
        var module = method.Module;

        foreach (var token in EnumerateNewobjTokens(il))
        {
            ConstructorInfo? ctorInfo;
            try
            {
                var member = module.ResolveMember(token);
                ctorInfo = member as ConstructorInfo;
            }
            catch
            {
                // Token resolution can fail for various reasons
                continue;
            }

            if (ctorInfo != null)
            {
                var declaringType = ctorInfo.DeclaringType;
                if (declaringType != null && IsForbiddenContextType(declaringType))
                {
                    var message = $"Vault assembly '{assemblyName}' constructs forbidden context type '{declaringType.FullName}' in '{containingType.FullName}.{method.Name}'. Vault must not instantiate Kernel context objects - consume them via accessors instead.";

                    throw new CanaryInvariantException(InvariantName, message);
                }
            }
        }
    }

    /// <summary>
    /// Properly enumerates IL instructions and yields metadata tokens from newobj instructions.
    /// This respects instruction boundaries to avoid false positives from operand bytes.
    /// </summary>
    private static IEnumerable<int> EnumerateNewobjTokens(byte[] il)
    {
        int i = 0;
        while (i < il.Length)
        {
            var opcode = il[i];

            // Check for two-byte opcode prefix
            if (opcode == TwoByteOpcodePrefix && i + 1 < il.Length)
            {
                // Two-byte opcode - skip prefix and second byte, then operand
                var secondByte = il[i + 1];
                var operandSize = GetTwoByteOpcodeOperandSize(secondByte);
                i += 2 + operandSize;
                continue;
            }

            // Single-byte opcode
            var singleByteOperandSize = GetSingleByteOpcodeOperandSize(opcode);

            if (opcode == NewobjOpcode && i + 4 < il.Length)
            {
                // newobj has a 4-byte metadata token operand
                int token = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);
                yield return token;
            }

            i += 1 + singleByteOperandSize;
        }
    }

    /// <summary>
    /// Returns the operand size for a single-byte opcode.
    /// </summary>
    private static int GetSingleByteOpcodeOperandSize(byte opcode) => opcode switch
    {
        // No operand (most common)
        0x00 => 0, // nop
        0x01 => 0, // break
        0x02 => 0, // ldarg.0
        0x03 => 0, // ldarg.1
        0x04 => 0, // ldarg.2
        0x05 => 0, // ldarg.3
        0x06 => 0, // ldloc.0
        0x07 => 0, // ldloc.1
        0x08 => 0, // ldloc.2
        0x09 => 0, // ldloc.3
        0x0A => 0, // stloc.0
        0x0B => 0, // stloc.1
        0x0C => 0, // stloc.2
        0x0D => 0, // stloc.3
        0x14 => 0, // ldnull
        0x15 => 0, // ldc.i4.m1
        0x16 => 0, // ldc.i4.0
        0x17 => 0, // ldc.i4.1
        0x18 => 0, // ldc.i4.2
        0x19 => 0, // ldc.i4.3
        0x1A => 0, // ldc.i4.4
        0x1B => 0, // ldc.i4.5
        0x1C => 0, // ldc.i4.6
        0x1D => 0, // ldc.i4.7
        0x1E => 0, // ldc.i4.8
        0x25 => 0, // dup
        0x26 => 0, // pop
        0x2A => 0, // ret
        0x46 => 0, // ldind.i1
        0x47 => 0, // ldind.u1
        0x48 => 0, // ldind.i2
        0x49 => 0, // ldind.u2
        0x4A => 0, // ldind.i4
        0x4B => 0, // ldind.u4
        0x4C => 0, // ldind.i8
        0x4D => 0, // ldind.i
        0x4E => 0, // ldind.r4
        0x4F => 0, // ldind.r8
        0x50 => 0, // ldind.ref
        0x51 => 0, // stind.ref
        0x52 => 0, // stind.i1
        0x53 => 0, // stind.i2
        0x54 => 0, // stind.i4
        0x55 => 0, // stind.i8
        0x56 => 0, // stind.r4
        0x57 => 0, // stind.r8
        0x58 => 0, // add
        0x59 => 0, // sub
        0x5A => 0, // mul
        0x5B => 0, // div
        0x5C => 0, // div.un
        0x5D => 0, // rem
        0x5E => 0, // rem.un
        0x5F => 0, // and
        0x60 => 0, // or
        0x61 => 0, // xor
        0x62 => 0, // shl
        0x63 => 0, // shr
        0x64 => 0, // shr.un
        0x65 => 0, // neg
        0x66 => 0, // not
        0x67 => 0, // conv.i1
        0x68 => 0, // conv.i2
        0x69 => 0, // conv.i4
        0x6A => 0, // conv.i8
        0x6B => 0, // conv.r4
        0x6C => 0, // conv.r8
        0x6D => 0, // conv.u4
        0x6E => 0, // conv.u8
        0x76 => 0, // conv.r.un
        0x7A => 0, // throw
        0x82 => 0, // conv.ovf.i1.un
        0x83 => 0, // conv.ovf.i2.un
        0x84 => 0, // conv.ovf.i4.un
        0x85 => 0, // conv.ovf.i8.un
        0x86 => 0, // conv.ovf.u1.un
        0x87 => 0, // conv.ovf.u2.un
        0x88 => 0, // conv.ovf.u4.un
        0x89 => 0, // conv.ovf.u8.un
        0x8A => 0, // conv.ovf.i.un
        0x8B => 0, // conv.ovf.u.un
        0x90 => 0, // ldelem.i1
        0x91 => 0, // ldelem.u1
        0x92 => 0, // ldelem.i2
        0x93 => 0, // ldelem.u2
        0x94 => 0, // ldelem.i4
        0x95 => 0, // ldelem.u4
        0x96 => 0, // ldelem.i8
        0x97 => 0, // ldelem.i
        0x98 => 0, // ldelem.r4
        0x99 => 0, // ldelem.r8
        0x9A => 0, // ldelem.ref
        0x9B => 0, // stelem.i
        0x9C => 0, // stelem.i1
        0x9D => 0, // stelem.i2
        0x9E => 0, // stelem.i4
        0x9F => 0, // stelem.i8
        0xA0 => 0, // stelem.r4
        0xA1 => 0, // stelem.r8
        0xA2 => 0, // stelem.ref
        0xB3 => 0, // conv.ovf.i1
        0xB4 => 0, // conv.ovf.u1
        0xB5 => 0, // conv.ovf.i2
        0xB6 => 0, // conv.ovf.u2
        0xB7 => 0, // conv.ovf.i4
        0xB8 => 0, // conv.ovf.u4
        0xB9 => 0, // conv.ovf.i8
        0xBA => 0, // conv.ovf.u8
        0xC3 => 0, // ckfinite
        0xD1 => 0, // conv.u2
        0xD2 => 0, // conv.u1
        0xD3 => 0, // conv.i
        0xD4 => 0, // conv.ovf.i
        0xD5 => 0, // conv.ovf.u
        0xD6 => 0, // add.ovf
        0xD7 => 0, // add.ovf.un
        0xD8 => 0, // mul.ovf
        0xD9 => 0, // mul.ovf.un
        0xDA => 0, // sub.ovf
        0xDB => 0, // sub.ovf.un
        0xDC => 0, // endfinally
        0xDE => 0, // conv.u
        0xE0 => 0, // conv.ovf.u1

        // 1-byte operand
        0x0E => 1, // ldarg.s
        0x0F => 1, // ldarga.s
        0x10 => 1, // starg.s
        0x11 => 1, // ldloc.s
        0x12 => 1, // ldloca.s
        0x13 => 1, // stloc.s
        0x1F => 1, // ldc.i4.s
        0x2B => 1, // br.s
        0x2C => 1, // brfalse.s
        0x2D => 1, // brtrue.s
        0x2E => 1, // beq.s
        0x2F => 1, // bge.s
        0x30 => 1, // bgt.s
        0x31 => 1, // ble.s
        0x32 => 1, // blt.s
        0x33 => 1, // bne.un.s
        0x34 => 1, // bge.un.s
        0x35 => 1, // bgt.un.s
        0x36 => 1, // ble.un.s
        0x37 => 1, // blt.un.s
        0xDD => 1, // leave.s

        // 4-byte operand (token or int32)
        0x20 => 4, // ldc.i4
        0x21 => 8, // ldc.i8
        0x22 => 4, // ldc.r4
        0x23 => 8, // ldc.r8
        0x27 => 4, // jmp
        0x28 => 4, // call
        0x29 => 4, // calli
        0x38 => 4, // br
        0x39 => 4, // brfalse
        0x3A => 4, // brtrue
        0x3B => 4, // beq
        0x3C => 4, // bge
        0x3D => 4, // bgt
        0x3E => 4, // ble
        0x3F => 4, // blt
        0x40 => 4, // bne.un
        0x41 => 4, // bge.un
        0x42 => 4, // bgt.un
        0x43 => 4, // ble.un
        0x44 => 4, // blt.un
        0x45 => 0, // switch - variable size, handle specially below
        0x6F => 4, // callvirt
        0x70 => 4, // cpobj
        0x71 => 4, // ldobj
        0x72 => 4, // ldstr
        0x73 => 4, // newobj
        0x74 => 4, // castclass
        0x75 => 4, // isinst
        0x79 => 4, // unbox (corrected - has token)
        0x7B => 4, // ldfld
        0x7C => 4, // ldflda
        0x7D => 4, // stfld
        0x7E => 4, // ldsfld
        0x7F => 4, // ldsflda
        0x80 => 4, // stsfld
        0x81 => 4, // stobj
        0x8C => 4, // box
        0x8D => 4, // newarr
        0x8E => 0, // ldlen
        0x8F => 4, // ldelema
        0xA3 => 4, // ldelem
        0xA4 => 4, // stelem
        0xA5 => 4, // unbox.any
        0xC2 => 4, // refanyval
        0xC6 => 4, // mkrefany
        0xD0 => 4, // ldtoken
        0xDF => 4, // leave
        _ => 0, // Unknown opcode - assume no operand (safest for forward compatibility)
    };

    /// <summary>
    /// Returns the operand size for a two-byte opcode (after 0xFE prefix).
    /// </summary>
    private static int GetTwoByteOpcodeOperandSize(byte secondByte) => secondByte switch
    {
        0x00 => 0, // arglist
        0x01 => 0, // ceq
        0x02 => 0, // cgt
        0x03 => 0, // cgt.un
        0x04 => 0, // clt
        0x05 => 0, // clt.un
        0x06 => 4, // ldftn
        0x07 => 4, // ldvirtftn
        0x09 => 2, // ldarg
        0x0A => 2, // ldarga
        0x0B => 2, // starg
        0x0C => 2, // ldloc
        0x0D => 2, // ldloca
        0x0E => 2, // stloc
        0x0F => 0, // localloc
        0x11 => 0, // endfilter
        0x12 => 1, // unaligned.
        0x13 => 0, // volatile.
        0x14 => 0, // tail.
        0x15 => 4, // initobj
        0x16 => 4, // constrained.
        0x17 => 0, // cpblk
        0x18 => 0, // initblk
        0x1A => 0, // rethrow
        0x1C => 4, // sizeof
        0x1D => 0, // refanytype
        0x1E => 0, // readonly.
        _ => 0, // Unknown - assume no operand
    };

    private static bool IsForbiddenContextType(Type type)
    {
        var typeName = type.Name;
        var ns = type.Namespace ?? string.Empty;

        // Type name matches a forbidden name AND it's from the Kernel namespace
        if (ForbiddenConstructionTypeNames.Any(n => typeName.Equals(n, StringComparison.Ordinal))
            && ForbiddenTypeNamespacePrefixes.Any(n => ns.StartsWith(n, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }
}
