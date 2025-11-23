namespace HoneyDrunk.Vault.Models;

/// <summary>
/// Represents the scope for vault operations, such as environment, tenant, or node.
/// </summary>
public sealed record VaultScope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultScope"/> class.
    /// </summary>
    /// <param name="environment">The environment name (e.g., Development, Production).</param>
    public VaultScope(string environment)
        : this(environment, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultScope"/> class.
    /// </summary>
    /// <param name="environment">The environment name (e.g., Development, Production).</param>
    /// <param name="tenant">The optional tenant identifier.</param>
    /// <param name="node">The optional node identifier.</param>
    public VaultScope(string environment, string? tenant, string? node)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("Environment cannot be null or whitespace.", nameof(environment));
        }

        Environment = environment;
        Tenant = tenant;
        Node = node;
    }

    /// <summary>
    /// Gets the environment name.
    /// </summary>
    public string Environment { get; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string? Tenant { get; }

    /// <summary>
    /// Gets the node identifier.
    /// </summary>
    public string? Node { get; }
}
