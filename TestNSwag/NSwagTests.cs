using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyTests.TestNSwag;

//dotnet test --logger "console;verbosity=detailed" --filter FullyQualifiedName~NSwagTests

[TestClass]
public class NSwagTests
{
    private PetstoreClient? _client;

    [TestInitialize]
    public void Setup()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("https://petstore.swagger.io/v2") };
        _client = new PetstoreClient(httpClient);
    }

    [TestMethod]
    public async Task GetPetsByStatus_ShouldReturnResults()
    {
        Assert.IsNotNull(_client, "Client is not initialized");
        var statusParameter = Anonymous.Available;
        var pets = await _client.FindPetsByStatusAsync([statusParameter]);

        Assert.IsNotNull(pets, "Expected pets list to be not null");
        Assert.IsTrue(pets.Count > 0, "Expected at least one pet with 'available' status");
    }
}