using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Project.API.Tests.Support;

public sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;

    public string ApplicationName { get; set; } = "Project.API.Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
