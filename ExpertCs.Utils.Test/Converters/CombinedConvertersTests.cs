using System.Text.Json;
using System.Text.Json.Serialization;
using ExpertCs.Utils.Converters;

namespace ExpertCs.Utils.Test.Converters;

[TestFixture]
public class CombinedConvertersTests
{
    [Test]
    public void Convert_RoundTrip_ReturnsEquivalentObject()
    {
        // Arrange
        var original = new
        {
            Name = "Test",
            Value = 42,
            Nested = new { Text = "Hello" },
            Array = new[] { 1, 2, 3 },
            NullValue = (string?)null,
            SpecialChars = "Line1\nLine2"
        };

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        var toDictConverter = new ObjectToDictionaryConverter(options);
        var toObjConverter = new DictionaryToObjectConverter(options);

        // Act
        var dictionary = toDictConverter.Convert(original);
        var result = toObjConverter.Convert<dynamic>(dictionary);

        // Assert
        Assert.That(result.Name, Is.EqualTo(original.Name));
        Assert.That(result.Value, Is.EqualTo(original.Value));
        Assert.That(result.Nested.Text, Is.EqualTo(original.Nested.Text));
        Assert.That((int[])result.Array, Is.EqualTo(original.Array));
        Assert.That(result.NullValue, Is.Null);
        Assert.That(result.SpecialChars, Is.EqualTo(original.SpecialChars));
    }
}