using System.Xml.Linq;
using FluentAssertions;

namespace BT.Tests.Architecture;

public sealed class SolutionBoundaryTests
{
    [Fact]
    public void Every_solution_project_supports_and_maps_the_x64_platform()
    {
        var root = FindRepositoryRoot();
        var solution = XDocument.Load(Path.Combine(root, "BaseTemplate.slnx"));
        var projects = solution.Descendants("Project").ToArray();
        projects.Should().NotBeEmpty();

        foreach (var project in projects)
        {
            var path = project.Attribute("Path")!.Value;
            project.Elements("Platform").Should().Contain(mapping =>
                (string?)mapping.Attribute("Solution") == "*|x64"
                && (string?)mapping.Attribute("Project") == "x64",
                "{0} must match its references when Visual Studio builds the x64 solution", path);

            var definition = XDocument.Load(Path.Combine(root, path));
            var platforms = definition.Descendants("Platforms")
                .SelectMany(element => element.Value.Split(';')).ToArray();

            platforms.Should().Contain("AnyCPU", "{0} must support the default solution platform", path);
            platforms.Should().Contain("x64", "{0} must support the explicit x64 solution platform", path);
        }
    }

    [Fact]
    public void Blazor_is_the_only_frontend_and_references_only_shared_transport_contracts()
    {
        var root = FindRepositoryRoot();
        var frontend = Path.Combine(root, "src", "Frontend");
        var projects = Directory.GetFiles(frontend, "*.csproj", SearchOption.AllDirectories);
        var webProject = Path.Combine(frontend, "Web", "BT.UI.Blazor", "BT.UI.Blazor.csproj");
        projects.Should().BeEquivalentTo([webProject]);

        var definition = XDocument.Load(webProject);
        var references = definition.Descendants("ProjectReference")
            .Select(reference => Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(webProject)!, reference.Attribute("Include")!.Value
                    .Replace('\\', Path.DirectorySeparatorChar))))
            .ToArray();

        references.Should().BeEquivalentTo([
            Path.Combine(root, "src", "Shared", "BT.SharedKernel", "BT.SharedKernel.csproj")
        ]);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BaseTemplate.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the BaseTemplate solution.");
    }
}
