using Serilog;
using tema3;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log")
    .CreateLogger();

Log.Information("Serilog initialized");

var window = new SilkWindow();
window.Start();