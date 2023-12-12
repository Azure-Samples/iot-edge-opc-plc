namespace OpcPlc.Certs;

using Opc.Ua;

/// <summary>
/// Defines type for <see cref="FlatDirectoryCertificateStore"/>.
/// </summary>
public sealed class FlatDirectoryCertificateStoreType : ICertificateStoreType
{
    /// <inheritdoc/>
    public ICertificateStore CreateStore()
    {
        return new FlatDirectoryCertificateStore();
    }

    /// <inheritdoc/>
    public bool SupportsStorePath(string storePath)
    {
        return !string.IsNullOrEmpty(storePath);
    }
}
