using System.Reflection;

namespace HoneyDrunk.Vault.Tests.Canary.Invariants;

/// <summary>
/// Enforces that provider assemblies have no references to Kernel types.
/// Provider packages should only depend on HoneyDrunk.Vault abstractions,
/// never on Kernel directly.
/// </summary>
public static class ProviderBoundaryInvariant
{
    /// <summary>
    /// The invariant name for diagnostic messages.
    /// </summary>
    public const string InvariantName = "ProviderBoundaryStaysClean";

    /// <summary>
    /// Forbidden assembly name prefixes that providers must not reference.
    /// </summary>
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "HoneyDrunk.Kernel",
    ];

    /// <summary>
    /// Forbidden type namespace prefixes that providers must not use in public APIs.
    /// </summary>
    private static readonly string[] ForbiddenNamespacePrefixes =
    [
        "HoneyDrunk.Kernel",
    ];

    /// <summary>
    /// Validates that the specified provider assembly has no Kernel dependencies.
    /// </summary>
    /// <param name="providerAssembly">The provider assembly to inspect.</param>
    /// <exception cref="CanaryInvariantException">Thrown when Kernel references are found.</exception>
    public static void Validate(Assembly providerAssembly)
    {
        ArgumentNullException.ThrowIfNull(providerAssembly);

        var assemblyName = providerAssembly.GetName().Name ?? "unknown";

        // Check referenced assemblies
        ValidateReferencedAssemblies(providerAssembly, assemblyName);

        // Check public API for Kernel types
        ValidatePublicApi(providerAssembly, assemblyName);
    }

    private static void ValidateReferencedAssemblies(Assembly providerAssembly, string assemblyName)
    {
        var referencedAssemblies = providerAssembly.GetReferencedAssemblies();

        foreach (var refName in referencedAssemblies.Select(r => r.Name ?? string.Empty))
        {
            if (ForbiddenAssemblyPrefixes.Any(f => refName.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
            {
                var message = $"Provider assembly '{assemblyName}' references forbidden assembly '{refName}'. Provider packages must not depend on HoneyDrunk.Kernel or HoneyDrunk.Kernel.Abstractions.";

                throw new CanaryInvariantException(InvariantName, message);
            }
        }
    }

    private static void ValidatePublicApi(Assembly providerAssembly, string assemblyName)
    {
        var publicTypes = providerAssembly
            .GetExportedTypes()
            .Where(t => t.IsPublic || t.IsNestedPublic);

        foreach (var type in publicTypes)
        {
            // Check constructors
            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                ValidateMethodParameters(ctor, assemblyName, type.FullName ?? type.Name);
            }

            // Check public methods
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                ValidateMethodParameters(method, assemblyName, type.FullName ?? type.Name);
                ValidateReturnType(method, assemblyName, type.FullName ?? type.Name);
            }

            // Check public properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                ValidatePropertyType(prop, assemblyName, type.FullName ?? type.Name);
            }
        }
    }

    private static void ValidateMethodParameters(MethodBase method, string assemblyName, string typeName)
    {
        foreach (var param in method.GetParameters().Where(p => IsForbiddenType(p.ParameterType)))
        {
            var message = $"Provider assembly '{assemblyName}' has public API '{typeName}.{method.Name}' with parameter '{param.Name}' of forbidden type '{param.ParameterType.FullName}'. Provider packages must not accept Kernel types in public APIs.";

            throw new CanaryInvariantException(InvariantName, message);
        }
    }

    private static void ValidateReturnType(MethodInfo method, string assemblyName, string typeName)
    {
        if (IsForbiddenType(method.ReturnType))
        {
            var message = $"Provider assembly '{assemblyName}' has public API '{typeName}.{method.Name}' with forbidden return type '{method.ReturnType.FullName}'. Provider packages must not return Kernel types from public APIs.";

            throw new CanaryInvariantException(InvariantName, message);
        }
    }

    private static void ValidatePropertyType(PropertyInfo property, string assemblyName, string typeName)
    {
        if (IsForbiddenType(property.PropertyType))
        {
            var message = $"Provider assembly '{assemblyName}' has public property '{typeName}.{property.Name}' of forbidden type '{property.PropertyType.FullName}'. Provider packages must not expose Kernel types in public APIs.";

            throw new CanaryInvariantException(InvariantName, message);
        }
    }

    private static bool IsForbiddenType(Type type)
    {
        // Check the type's namespace
        var ns = type.Namespace ?? string.Empty;

        if (ForbiddenNamespacePrefixes.Any(f => ns.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check generic type arguments
        if (type.IsGenericType && type.GetGenericArguments().Any(IsForbiddenType))
        {
            return true;
        }

        // Check array element type
        if (type.IsArray && type.GetElementType() is { } elementType)
        {
            return IsForbiddenType(elementType);
        }

        return false;
    }
}
