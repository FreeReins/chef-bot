using System.Reflection;
using ChefBot.Core.Managers.Contracts.Services;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>
/// Keeps the layers honest. Calls only go down: clients call managers, managers call engines and resource
/// accessors, engines call accessors (only ValidationEngine does today), and nothing calls sideways within a layer.
/// Dependencies are read from constructors, which is how every component gets the ones it uses.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Core = typeof(CoreRegistration).Assembly;

    private static IEnumerable<Type> In(string area) =>
        Core.GetTypes().Where(type => type.Namespace?.StartsWith($"ChefBot.Core.{area}", StringComparison.Ordinal) == true);

    private static bool IsManager(Type type) =>
        type.Namespace == "ChefBot.Core.Managers" && type.Name.EndsWith("Manager", StringComparison.Ordinal)
        || type.Namespace == "ChefBot.Core.Managers.Contracts.Services";

    private static bool IsEngine(Type type) => type.Namespace?.StartsWith("ChefBot.Core.Engines", StringComparison.Ordinal) == true
        && type.Name.EndsWith("Engine", StringComparison.Ordinal);

    private static bool IsAccessor(Type type) => type.Namespace == "ChefBot.Core.ResourceAccess.Contracts.Services" && type.IsInterface;

    private static IEnumerable<(Type Owner, Type Dependency)> Dependencies(IEnumerable<Type> owners) =>
        owners
            .Where(type => type.IsClass && !type.IsAbstract)
            .SelectMany(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => (type, parameter.ParameterType)));

    private static string Describe(IEnumerable<(Type Owner, Type Dependency)> found) =>
        string.Join("; ", found.Select(pair => $"{pair.Owner.Name} -> {pair.Dependency.Name}"));

    [Test]
    public async Task Managers_never_call_other_managers()
    {
        var sideways = Dependencies(In("Managers")).Where(pair => IsManager(pair.Dependency)).ToList();

        await Assert.That(Describe(sideways)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Engines_never_call_managers_or_other_engines()
    {
        var wrong = Dependencies(In("Engines")).Where(pair => IsManager(pair.Dependency) || IsEngine(pair.Dependency)).ToList();

        await Assert.That(Describe(wrong)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Only_the_validation_engine_reads_through_accessors()
    {
        var readers = Dependencies(In("Engines")).Where(pair => IsAccessor(pair.Dependency)).Select(pair => pair.Owner.Name).Distinct().ToList();

        await Assert.That(string.Join(",", readers)).IsEqualTo("ValidationEngine");
    }

    [Test]
    public async Task Accessors_call_only_their_resources()
    {
        var upward = Dependencies(In("ResourceAccess"))
            .Where(pair => IsManager(pair.Dependency) || IsEngine(pair.Dependency) || IsAccessor(pair.Dependency))
            .ToList();

        await Assert.That(Describe(upward)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Only_managers_commit_and_publish_messages()
    {
        var others = Dependencies(Core.GetTypes().Where(type => type.Namespace != "ChefBot.Core.Managers"))
            .Where(pair => pair.Dependency == typeof(IMessageAccess))
            .ToList();

        await Assert.That(Describe(others)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Utilities_depend_on_no_layer()
    {
        var wrong = Dependencies(In("Utilities"))
            .Where(pair => IsManager(pair.Dependency) || IsEngine(pair.Dependency) || IsAccessor(pair.Dependency))
            .ToList();

        await Assert.That(Describe(wrong)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Only_the_contracts_and_the_registration_are_public()
    {
        var exposed = Core.GetExportedTypes()
            .Where(type => type != typeof(CoreRegistration)
                && type.Namespace?.StartsWith("ChefBot.Core.Managers.Contracts", StringComparison.Ordinal) != true)
            .Select(type => type.FullName)
            .ToList();

        await Assert.That(string.Join(", ", exposed)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Every_manager_interface_has_one_implementation()
    {
        var unimplemented = typeof(IPlanManager).Assembly.GetTypes()
            .Where(type => type.IsInterface && type.Namespace == "ChefBot.Core.Managers.Contracts.Services")
            .Where(service => In("Managers").Count(type => type.IsClass && service.IsAssignableFrom(type)) != 1)
            .Select(service => service.Name)
            .ToList();

        await Assert.That(string.Join(",", unimplemented)).IsEqualTo(string.Empty);
    }
}
