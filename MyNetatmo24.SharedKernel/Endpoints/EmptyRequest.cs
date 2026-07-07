namespace MyNetatmo24.SharedKernel.Endpoints;

public sealed class EmptyRequest
{
    /// <summary>
    /// a cached empty request instance.
    /// </summary>
    public static EmptyRequest Instance { get; } = new();

    //private ctor only used by above cached instance.
    private EmptyRequest() { }
}
