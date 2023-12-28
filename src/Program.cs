namespace OpcPlc;

public static class Program
{
    public static OpcPlcServer OpcPlcServer { get; set; }

    /// <summary>
    /// Synchronous main method of the app.
    /// </summary>
    public static void Main(string[] args)
    {
        OpcPlcServer = new OpcPlcServer();

        // Start OPC UA server.
        OpcPlcServer.StartAsync(args).Wait();
    }
}
