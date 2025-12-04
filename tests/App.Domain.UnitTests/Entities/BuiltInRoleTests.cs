using FluentAssertions;
using App.Domain.Entities;
using App.Domain.Exceptions;

namespace App.Domain.UnitTests.ValueObjects;

public class BuiltInRoleTests
{
    [Test]
    [TestCase("super_admin")]
    [TestCase("admin")]
    [TestCase("editor")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = BuiltInRole.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    [TestCase("super_admin", "Super Admin")]
    [TestCase("admin", "Admin")]
    [TestCase("editor", "Editor")]
    [Parallelizable(ParallelScope.All)]
    public void ToStringShouldMatchLabel(string developerName, string label)
    {
        var type = BuiltInRole.From(developerName);
        type.DefaultLabel.Should().Be(label);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = BuiltInRole.SuperAdmin;

        type.Should().Be("super_admin");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (BuiltInRole)"super_admin";

        type.Should().Be(BuiltInRole.SuperAdmin);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => BuiltInRole.From("BadValue"))
            .Should()
            .Throw<UnsupportedTemplateTypeException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        BuiltInRole.Permissions.Count().Should().Be(3);
    }
}
