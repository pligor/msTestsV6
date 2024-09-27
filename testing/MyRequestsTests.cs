using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;

namespace MyTests;

// dotnet test --filter FullyQualifiedName~MyRequestsTests

[TestClass]
public class MyRequestsTests
{

    [TestMethod]
    // [Ignore]
    public void Test1()
    {
        // TODO make it fail explicitly
        // Assert.Fail("It always fails for no good reason.");
    }

    private readonly HttpClient httpClient;

    public MyRequestsTests()
    {
      httpClient = new HttpClient();
    }

    [TestMethod]
    public async Task test_json_get_request_with_model() {
              // Arrange
      var requestUri = "https://jsonplaceholder.typicode.com/todos/1"; // Sample JSON endpoint

      // Act
      var response = await httpClient.GetAsync(requestUri);

      // Assert Status Code
      Assert.AreEqual(200, (int)response.StatusCode, "Expected status code 200");

      // Assert Body
      var responseBody = await response.Content.ReadFromJsonAsync<ResponseModel>();
      Assert.IsNotNull(responseBody, "Response body should not be null");

      // Assert specific fields in the JSON response
      Assert.AreEqual(1, responseBody.Id, "Expected Id to be 1");
      Assert.AreEqual("delectus aut autem", responseBody.Title, "Expected title to match");
      Assert.IsFalse(responseBody.Completed, "Expected completed to be false");
    }

    [TestMethod]
    [TestCategory("json_req")]
    public async Task test_json_get_request() {
      var requestUri = "https://jsonplaceholder.typicode.com/todos/1"; // Sample JSON endpoint

      // Act
      var response = await httpClient.GetAsync(requestUri);

      // Assert Status Code
      Assert.AreEqual(200, (int)response.StatusCode, "Expected status code 200");

      // Read and parse the response content as JSON
      var jsonString = await response.Content.ReadAsStringAsync();
      using var jsonDocument = JsonDocument.Parse(jsonString);

      // Assert specific fields in the JSON response
      var root = jsonDocument.RootElement;

      // Asserting values using property names
      Assert.AreEqual(1, root.GetProperty("id").GetInt32(), "Expected Id to be 1");
      Assert.AreEqual("delectus aut autem", root.GetProperty("title").GetString(), "Expected title to match");
      Assert.IsFalse(root.GetProperty("completed").GetBoolean(), "Expected completed to be false");
    }
}

  // Example response model based on expected JSON structure
  public class ResponseModel
  {
    public int UserId { get; set; }
    public int Id { get; set; }
    public string Title { get; set; }
    public bool Completed { get; set; }
  }