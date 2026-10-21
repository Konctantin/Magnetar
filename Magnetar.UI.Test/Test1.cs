using System.Text.RegularExpressions;

namespace Magnetar.UI.Test;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        var text = "player.buff(Some Name).remains > 5";
        var reg = new Regex(@"^\s*(?<negation>!)?\s*(?<condition>.*?)" +
    @"(?:\s*(?<operator>not\s+in|>=|<=|!=|>|<|=|(?<!\S)in(?=\s))" +
    @"\s*(?<value>.*))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var mat = reg.Matches(text);
        Assert.IsNotNull(mat);
    }
}
