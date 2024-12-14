namespace MyTests.TestNSwag;

public class PetstoreClientV2
{
    public PetstoreClientV2(HttpClient httpClient)
    {
    }

    public async Task<List<Pet>> FindPetsByStatusAsync(string[] status)
    {
        return new List<Pet>();
    }
}