using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyTests.test_nswag;

// dotnet test --logger "console;verbosity=detailed" --filter FullyQualifiedName~NSwagTests

[TestClass]
public class NSwagTests
{

    [TestMethod]
    [TestCategory("nswag")]
    // [Ignore]
    public void Test1()
    {
        // TODO make it fail explicitly
        // Assert.Fail("It always fails for no good reason.");
    }

}
