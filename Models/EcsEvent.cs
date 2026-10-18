namespace SecLog.ETL.Models;

public class EcsEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public EcsEventDetails Event { get; set; } = new();
    public EcsHost Host { get; set; } = new();
    public EcsProcess? Process { get; set; }
}

public class EcsEventDetails
{
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = "Windows";
}

public class EcsHost
{
    public string Name { get; set; } = Environment.MachineName;
}

public class EcsProcess
{
    public string? Executable { get; set; }
    public string? CommandLine { get; set; }
    public string? ParentExecutable { get; set; }
}