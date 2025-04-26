using System.Text.Json;
using System.Text.Json.Serialization;
using ExpertCs.Utils.Converters;

namespace ExpertCs.Utils.Test.Converters;

[TestFixture]
public class ObjectToDictionaryConverterTests
{
    private ObjectToDictionaryConverter _converter;
    private JsonSerializerOptions _options;

    [SetUp]
    public void Setup()
    {
        _options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        _converter = new ObjectToDictionaryConverter(_options);
    }

    [Test]
    public void Convert_SimpleObject_ReturnsCorrectDictionary()
    {
        // Arrange
        var obj = new { Name = "Test", Value = 42 };

        // Act
        var result = _converter.Convert(obj);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result["Name"], Is.EqualTo("Test"));
        Assert.That(result["Value"], Is.EqualTo("42"));
    }

    [Test]
    public void Convert_NestedObject_ReturnsCorrectDictionary()
    {
        // Arrange
        var obj = new { 
            Name = "Test", 
            Nested = new { Value = 42 } 
        };

        // Act
        var result = _converter.Convert(obj);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result["Name"], Is.EqualTo("Test"));
        Assert.That(result["Nested:Value"], Is.EqualTo("42"));
    }

    [Test]
    public void Convert_ArrayProperty_ReturnsCorrectDictionary()
    {
        // Arrange
        var obj = new { 
            Name = "Test", 
            Values = new[] { 1, 2, 3 } 
        };

        // Act
        var result = _converter.Convert(obj);

        // Assert
        Assert.That(result.Count, Is.EqualTo(4));
        Assert.That(result["Name"], Is.EqualTo("Test"));
        Assert.That(result["Values[0]"], Is.EqualTo("1"));
        Assert.That(result["Values[1]"], Is.EqualTo("2"));
        Assert.That(result["Values[2]"], Is.EqualTo("3"));
    }

    [Test]
    public void Convert_NullValue_ReturnsNullString()
    {
        // Arrange
        var obj = new { Name = (string?)null };

        // Act
        var result = _converter.Convert(obj);

        // Assert
        Assert.That(result.Any(), Is.False);
    }

    [Test]
    public void Convert_EscapingCharacters_ReturnsEscapedStrings()
    {
        // Arrange
        var obj = new { 
            Path = "C:\\Folder\\File.txt", 
            Text = "Line1\nLine2" 
        };

        // Act
        var result = _converter.Convert(obj);

        // Assert
        Assert.That(result["Path"], Is.EqualTo("C:\\Folder\\File.txt"));
        Assert.That(result["Text"], Is.EqualTo("Line1\nLine2"));
    }

    [Test]
    public void Convert_NullObject()
    {
        // Arrange
        object? obj = null;
        var d = _converter.Convert(obj);
        Assert.That(d.Any(), Is.False);
    }
}