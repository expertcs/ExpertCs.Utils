using System;
using ExpertCs.Utils;
using NUnit.Framework;

namespace ExpertCs.Utils.Test.Utils;

[TestFixture]
public class ExceptionExtensionsTests
{
    [Test]
    public void InvokeIgnoreException_Generic_WithFunc_NoException_ReturnsResult()
    {
        // Arrange
        var func = new Func<int>(() => 42);

        // Act
        var result = func.InvokeIgnoreException<int, Exception>();

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void InvokeIgnoreException_Generic_WithFunc_HandlesSpecificException_ReturnsDefault()
    {
        // Arrange
        var func = new Func<int>(() => throw new InvalidOperationException());

        // Act
        var result = func.InvokeIgnoreException<int, InvalidOperationException>();

        // Assert
        Assert.That(result, Is.EqualTo(default(int)));
    }

    [Test]
    public void InvokeIgnoreException_Generic_WithFunc_HandlesSpecificException_ReturnsCustomValue()
    {
        // Arrange
        var func = new Func<int>(() => throw new InvalidOperationException());
        var handler = new Func<InvalidOperationException, int>(ex => -1);

        // Act
        var result = func.InvokeIgnoreException(handler);

        // Assert
        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void InvokeIgnoreException_Generic_WithFunc_ThrowsUnhandledException()
    {
        // Arrange
        var func = new Func<int>(() => throw new ArgumentException());

        // Act & Assert
        Assert.That(() => func.InvokeIgnoreException<int, InvalidOperationException>(),
            Throws.Exception.TypeOf<ArgumentException>());
    }

    [Test]
    public void InvokeIgnoreException_NonGeneric_WithFunc_HandlesException_ReturnsDefault()
    {
        // Arrange
        var func = new Func<string>(() => throw new Exception("Test"));

        // Act
        var result = func.InvokeIgnoreException();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void InvokeIgnoreException_NonGeneric_WithFunc_HandlesException_ReturnsCustomValue()
    {
        // Arrange
        var func = new Func<string>(() => throw new Exception("Test"));
        var handler = new Func<Exception, string>(ex => "Handled");

        // Act
        var result = func.InvokeIgnoreException(handler);

        // Assert
        Assert.That(result, Is.EqualTo("Handled"));
    }

    [Test]
    public void InvokeIgnoreException_Generic_WithAction_HandlesSpecificException()
    {
        // Arrange
        var executed = false;
        var action = new Action(() =>
        {
            executed = true;
            throw new InvalidOperationException();
        });

        // Act
        action.InvokeIgnoreException<InvalidOperationException>();

        // Assert
        Assert.That(executed, Is.True);
    }

    [Test]
    public void InvokeIgnoreException_Generic_WithAction_ThrowsUnhandledException()
    {
        // Arrange
        var action = new Action(() => throw new ArgumentException());

        // Act & Assert
        Assert.That(() => action.InvokeIgnoreException<InvalidOperationException>(),
            Throws.Exception.TypeOf<ArgumentException>());
    }

    [Test]
    public void InvokeIgnoreException_NonGeneric_WithAction_HandlesException()
    {
        // Arrange
        var executed = false;
        var action = new Action(() =>
        {
            executed = true;
            throw new Exception("Test");
        });

        // Act
        action.InvokeIgnoreException();

        // Assert
        Assert.That(executed, Is.True);
    }
}