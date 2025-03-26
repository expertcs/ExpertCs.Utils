using System.Globalization;

namespace ExpertCs.Utils.Test.Utils;

public class TestClass1
{
    public int Id;
    public string? Name;
}

public class StringHelperTest
{
    [TestCase(1, "HeLLo")]
    [TestCase(100500, "wOrlD")]
    public void TestFormatWithExpressions(int Id, string Name)
    {
        var a = new TestClass1 { Id = Id, Name = Name };

        Test2(
            a,
            "{Id} {Name}",
            $"{Id} {Name}");

        Test2(
            a,
            "{Name} {Id} {Name} {Name}",
            $"{Name} {Id} {Name} {Name}");

        Test2(
            a,
            "{GetType()} {Name} {Name} {Id} {Name.Length:n2}",
            $"{a.GetType()} {Name} {Name} {Id} {Name.Length:n2}");
    }

    private void Test2(object obj, string temp, string expect)
    {
        Console.Write(expect);
        Console.Write(" - ");
        var actual = obj.FormatWithExpressions(temp);
        Console.WriteLine(actual);
        Assert.That(actual, Is.EqualTo(expect));
    }

    [Test]
    public void TestExample()
    {
        var s = "p1={IntField,10:n2}, p2={StrProp}, p3={IntField}, p4={(DateProp.Year+5):n2}, p5={DateProp:g}, 10={7+3}, p7={GetType().Name}, p8={DArray[1]:p2}";

        var c = new
        {
            DateProp = new DateTime(2025, 03, 26, 3, 5, 10),
            IntField = 5,
            StrProp = "-test text-",
            DArray = new decimal[] { 2m, 4.3m, 7m }
        };

        var actual = c.FormatWithExpressions(s, CultureInfo.GetCultureInfo("ru"));
        var expect = "p1=      5,00, p2=-test text-, p3=5, p4=2 030,00, p5=26.03.2025 03:05, 10=10, p7=<>f__AnonymousType0`4, p8=430,00 %";
        Assert.That(actual, Is.EqualTo(expect));


        actual = c.FormatWithExpressions(s, CultureInfo.GetCultureInfo("en"));
        expect = "p1=      5.00, p2=-test text-, p3=5, p4=2,030.00, p5=3/26/2025 3:05 AM, 10=10, p7=<>f__AnonymousType0`4, p8=430.00%";
        Assert.That(actual, Is.EqualTo(expect));
    }
}
