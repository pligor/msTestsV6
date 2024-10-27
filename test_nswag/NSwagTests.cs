/*using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyTests.test_nswag;

// dotnet test --logger "console;verbosity=detailed" --filter FullyQualifiedName~NSwagTests

[TestClass]
public class NSwagTests
{

    private PetstoreClient _client;

    [TestInitialize]
    public void Setup()
    {
        // Initialize the client with the base URL for the Petstore API
        _client = new PetstoreClient("https://petstore.swagger.io/v2");
    }

    [TestMethod]
    public async Task GetPetsByStatus_ShouldReturnResults()
    {
        // Act: Call the API to find pets with "available" status
        var pets = await _client.FindPetsByStatusAsync(new[] { "available" });

        // Assert: Check if the results are not null and contain at least one pet
        Assert.IsNotNull(pets, "Expected pets list to be not null");
        Assert.IsTrue(pets.Count > 0, "Expected at least one pet with 'available' status");
    }

}
*/