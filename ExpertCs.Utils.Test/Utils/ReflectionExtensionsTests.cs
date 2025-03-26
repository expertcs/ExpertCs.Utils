namespace ExpertCs.Utils.Test.Utils;

[TestFixture]
public class ReflectionExtensionsTests
{
    private class TestClass
    {
        public string Name { get; set; } = "Test";
        public NestedClass Nested { get; set; } = new NestedClass();
    }

    private class NestedClass
    {
        public int Value { get; set; } = 42;
        public DeepNestedClass Deep { get; set; } = new DeepNestedClass();
    }

    private class DeepNestedClass
    {
        public DateTime Date { get; set; } = new DateTime(2023, 1, 1);
    }

    [Test]
    public void GetRuntimePropertyValue_WithNullObject_ReturnsNull()
    {
        // Arrange
        object? obj = null;

        // Act
        var result = obj.GetRuntimePropertyValue("Any.Property");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetRuntimePropertyValue_WithTopLevelProperty_ReturnsValue()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var result = obj.GetRuntimePropertyValue("Name");

        // Assert
        Assert.That(result, Is.EqualTo("Test"));
    }

    [Test]
    public void GetRuntimePropertyValue_WithNestedProperty_ReturnsValue()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var result = obj.GetRuntimePropertyValue("Nested.Value");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetRuntimePropertyValue_WithDeepNestedProperty_ReturnsValue()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var result = obj.GetRuntimePropertyValue("Nested.Deep.Date");

        // Assert
        Assert.That(result, Is.EqualTo(new DateTime(2023, 1, 1)));
    }

    [Test]
    public void GetRuntimePropertyValue_WithInvalidProperty_ReturnsNull()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var result = obj.GetRuntimePropertyValue("Invalid.Property");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetRuntimePropertyValue_WithEmptyPropertyPath_ReturnsOriginalObject()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var result = obj.GetRuntimePropertyValue("");

        // Assert
        Assert.That(result, Is.EqualTo(obj));
    }

    [Test]
    public void GetRuntimePropertyValue_WithWhitespacePropertyPath_ReturnsOriginalObject()
    {
        // Arrange
        var obj = new TestClass();

        // Act
        var result = obj.GetRuntimePropertyValue("   ");

        // Assert
        Assert.That(result, Is.EqualTo(obj));
    }
}