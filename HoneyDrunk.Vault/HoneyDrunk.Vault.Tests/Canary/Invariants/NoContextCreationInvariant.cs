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

        // Scan IL for 'newobj' instructions (0x73) that target forbidden constructors
        // This is a simplified IL scanner - it looks for newobj opcodes and resolves their metadata tokens
        var module = method.Module;

        for (int i = 0; i < il.Length; i++)
        {
            // newobj opcode is 0x73
            if (il[i] == 0x73 && i + 4 < il.Length)
            {
                // Next 4 bytes are the metadata token (little-endian)
                int token = il[i + 1] | (il[i + 2] << 8) | (il[i + 3] << 16) | (il[i + 4] << 24);

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

                i += 4; // Skip the token bytes
            }
        }
    }

    private static bool IsForbiddenContextType(Type type)
    {
        var typeName = type.Name;
        var ns = type.Namespace ?? string.Empty;

        // Check if type name matches forbidden context types
        foreach (var forbiddenName in ForbiddenConstructionTypeNames)
        {
            if (typeName.Equals(forbiddenName, StringComparison.Ordinal))
            {
                // Verify it's from the Kernel namespace
                foreach (var forbiddenNs in ForbiddenTypeNamespacePrefixes)
                {
                    if (ns.StartsWith(forbiddenNs, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
