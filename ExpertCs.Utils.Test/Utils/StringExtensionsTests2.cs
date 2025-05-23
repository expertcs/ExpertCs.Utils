﻿using System.Globalization;

namespace ExpertCs.Utils.Test.Utils;

public class StringExtensionsTests2
{
    public class TestClass1
    {
        public int Id;
        public string? Name;
    }

    [TestCase(1, "HeLLo")]
    [TestCase(100500, "wOrlD")]
    public void TestFormatWithExpressions(int Id, string Name)
    {
        var a = new TestClass1 { Id = Id, Name = Name };

        Test2(
            a,
            "{Id} {Name}",
            $"{a.Id} {a.Name}");

        Test2(
            a,
            "{Name} {Id} {Name} {Name}",
            $"{a.Name} {a.Id} {a.Name} {a.Name}");

        Test2(
            a,
            "{GetType()} {Name} {Name} {Id} {Name.Length:n2}",
            $"{a.GetType()} {a.Name} {a.Name} {a.Id} {a.Name.Length:n2}");
    }

    private void Test2(object obj, string temp, string expect)
    {
        Console.Write(expect);
        Console.Write(" - ");
        var actual = obj.GetInterpolatedString(temp);
        Console.WriteLine(actual);
        Assert.That(actual, Is.EqualTo(expect));
    }

    [Test]
    public void TestExample()
    {
        var s = "p1={IntField,10:n2}, p2={StrProp}, p3={IntField}, p4={(DateProp.Year+5):n2}, p5={DateProp:g}, 10={7+3}, p8={DArray[1]:p2}";

        var c = new
        {
            DateProp = new DateTime(2025, 03, 26, 3, 5, 10),
            IntField = 5,
            StrProp = "-test text-",
            DArray = new decimal[] { 2m, 4.3m, 7m }
        };

        var actual = c.GetInterpolatedString(s, CultureInfo.GetCultureInfo("ru"));
        var expect = "p1=      5,00, p2=-test text-, p3=5, p4=2 030,00, p5=26.03.2025 03:05, 10=10, p8=430,00 %";
        AssertNoEsc(actual, expect);

        actual = c.GetInterpolatedString(s, CultureInfo.GetCultureInfo("en"));
        expect = "p1=      5.00, p2=-test text-, p3=5, p4=2,030.00, p5=3/26/2025 3:05 AM, 10=10, p8=430.00%";
        AssertNoEsc(actual, expect);
        
        actual = c.GetInterpolatedString(s, CultureInfo.InvariantCulture);
        expect = "p1=      5.00, p2=-test text-, p3=5, p4=2,030.00, p5=03/26/2025 03:05, 10=10, p8=430.00 %";
        AssertNoEsc(actual, expect);
    }

    private static string NoEsc(string value)
    {
        new[] { '\u202F', '\u00A0' }.ToList().ForEach(s => value = value.Replace(s, ' '));
        return value;
    }

    private static void AssertNoEsc(string actual, string expect)
    {
        Assert.That(NoEsc(actual), Is.EqualTo(NoEsc(expect)));
    }


}
