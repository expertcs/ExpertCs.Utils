using System.Text.Json;
using System.Text.Json.Serialization;
using ExpertCs.Utils.Converters;

namespace ExpertCs.Utils.Test.Converters;

[TestFixture]
public class DictionaryToObjectConverterTests
{
    private DictionaryToObjectConverter _converter;
    private JsonSerializerOptions _options;

    [SetUp]
    public void Setup()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = { new JsonStringEnumConverter() }
        };
        _converter = new DictionaryToObjectConverter(_options);
    }

    [Test]
    public void Convert_SimpleDictionary_ReturnsCorrectObject()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            ["Name"] = "Test",
            ["Value"] = "42"
        };

        // Act
        var result = _converter.Convert<TestClass>(dictionary);

        // Assert
        Assert.That(result.Name, Is.EqualTo("Test"));
        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Convert_NestedDictionary_ReturnsCorrectObject()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            ["Name"] = "Test",
            ["Nested:Value"] = "42"
        };

        // Act
        var result = _converter.Convert<TestClassWithNested>(dictionary);

        // Assert
        Assert.That(result.Name, Is.EqualTo("Test"));
        Assert.That(result.Nested.Value, Is.EqualTo(42));
    }

    [Test]
    public void Convert_ArrayValues_ReturnsCorrectObject()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            ["Name"] = "Test",
            ["Values[0]"] = "1",
            ["Values[1]"] = "2",
            ["Values[2]"] = "3"
        };

        // Act
        var result = _converter.Convert<TestClassWithArray>(dictionary);

        // Assert
        Assert.That(result.Name, Is.EqualTo("Test"));
        Assert.That(result.Values, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Convert_EscapedStrings_ReturnsUnescapedValues()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            ["Path"] = "C:\\\\Folder\\\\File.txt",
            ["Text"] = "Line1\\nLine2"
        };

        // Act
        var result = _converter.Convert<TestClass>(dictionary);

        // Assert
        Assert.That(result.Path, Is.EqualTo("C:\\Folder\\File.txt"));
        Assert.That(result.Text, Is.EqualTo("Line1\nLine2"));
    }

    [Test]
    public void Convert_NullValue_ReturnsNullProperty()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            ["Name"] = "null"
        };

        // Act
        var result = _converter.Convert<TestClass>(dictionary);

        // Assert
        Assert.That(result.Name, Is.Null);
    }

    [Test]
    public void Convert_NullDictionary()
    {
        // Arrange
        Dictionary<string, string>? dictionary = null;

        var nullObject = _converter.Convert<TestClass>(dictionary);
        Assert.IsNull(nullObject);
    }

    [Test]
    public void Convert_InvalidPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            ["Invalid[Path"] = "value"
        };

        // Act & Assert
        Assert.That(() => _converter.Convert<TestClass>(dictionary), Throws.InvalidOperationException);
    }

    private class TestClass
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public string? Path { get; set; }
        public string? Text { get; set; }
    }

    private class TestClassWithNested
    {
        public string? Name { get; set; }
        public NestedClass Nested { get; set; } = new();
    }

    private class NestedClass
    {
        public int Value { get; set; }
    }

    private class TestClassWithArray
    {
        public string? Name { get; set; }
        public int[] Values { get; set; } = [];
    }
}