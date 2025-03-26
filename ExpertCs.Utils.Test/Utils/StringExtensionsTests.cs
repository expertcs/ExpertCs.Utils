using System.Globalization;

namespace ExpertCs.Utils.Test.Utils;

[TestFixture]
public class StringExtensionsTests
{
    private class TestClass
    {
        public DateTime DateProp { get; set; }
        public int IntField { get; set; }
        public string? StrProp { get; set; }
        public decimal[]? DArray { get; set; }
    }

    [Test]
    public void FormatWithExpressions_SimpleProperties_ReturnsFormattedString()
    {
        // Arrange
        var testObj = new TestClass
        {
            DateProp = new DateTime(2023, 1, 1),
            IntField = 5,
            StrProp = "-test text-",
            DArray = new decimal[] { 2m, 4.3m, 7m }
        };

        string template = "p1={IntField}, p2={StrProp}, p3={DateProp:g}";

        // Act
        string result = testObj.GetInterpolatedString(template);

        // Assert
        Assert.That(result, Is.EqualTo("p1=5, p2=-test text-, p3=01.01.2023 00:00"));
    }

    [Test]
    public void FormatWithExpressions_WithFormatting_ReturnsFormattedString()
    {
        // Arrange
        var testObj = new TestClass
        {
            IntField = 5,
            DateProp = new DateTime(2023, 1, 1)
        };

        string template = "p1={IntField,10:n2}, p4={(DateProp.Year+5):n2}";

        // Act
        string result = testObj.GetInterpolatedString(template, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo("p1=      5.00, p4=2,028.00"));
    }

    [Test]
    public void FormatWithExpressions_WithArrayIndex_ReturnsFormattedString()
    {
        // Arrange
        var testObj = new TestClass
        {
            DArray = new decimal[] { 2m, 4.3m, 7m }
        };

        string template = "p8={DArray[1]:n2}";

        // Act
        string result = testObj.GetInterpolatedString(template, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo("p8=4.30"));
    }

    [Test]
    public void FormatWithExpressions_WithExpression_ReturnsFormattedString()
    {
        // Arrange
        var testObj = new TestClass
        {
            IntField = 5
        };

        string template = "10={7+3}, p7={GetType().Name}";

        // Act
        string result = testObj.GetInterpolatedString(template);

        // Assert
        Assert.That(result, Is.EqualTo("10=10, p7=TestClass"));
    }

    [Test]
    public void FormatWithExpressions_NullObject_UsesEmptyObject()
    {
        // Arrange
        string template = "p1={GetType().Name}";

        // Act
        string result = ((object?)null).GetInterpolatedString(template);

        // Assert
        Assert.That(result, Is.EqualTo("p1=Object"));
    }

    [Test]
    public void FormatWithExpressions_InvalidExpression_ThrowsFormatException()
    {
        // Arrange
        var testObj = new TestClass();
        string template = "p1={InvalidProperty}";

        // Act & Assert
        var ex = Assert.Throws<FormatException>(() => testObj.GetInterpolatedString(template));
        Assert.That(ex.Message, Does.Contain("Error expression='InvalidProperty'"));
    }
}