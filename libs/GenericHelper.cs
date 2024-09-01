namespace MyTests.libs;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

class GenericHelper
{
  public static List<int> Slice(List<int> list, int? start = null, int? end = null, int step = 1) {
      if (step == 0)
          throw new ArgumentException("Step cannot be zero.", nameof(step));

      // Handle null and negative indices
      int startIndex = start ?? (step > 0 ? 0 : list.Count - 1);
      int endIndex = end ?? (step > 0 ? list.Count : 0);
      // Console.WriteLine((startIndex, endIndex));

      if (startIndex < 0) startIndex = list.Count + startIndex;
      if (endIndex < 0) endIndex = list.Count + endIndex;
      // Console.WriteLine((startIndex, endIndex));

      // Ensure indices are within bounds
      startIndex = Math.Max(0, Math.Min(list.Count, startIndex));
      endIndex = Math.Max(0, Math.Min(list.Count, endIndex));
      // Console.WriteLine((startIndex, endIndex));

      List<int> result = new List<int>();

      if (step > 0)
      {
          for (int i = startIndex; i < endIndex; i += step)
          {
              result.Add(list[i]);
          }
      }
      else
      {
          for (int i = startIndex; i >= endIndex; i += step)
          {
              result.Add(list[i]);
          }
      }

      return result;
  }

  
  public static void print(object ss) {
    Console.WriteLine(ss);
  }
  
  public static void assert(bool condition, string message="Assertion failed") {
    if (!condition) {
        throw new Exception(message);
    }
  }

  public static bool is_valid_uuid(string input) {
    string pattern = @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
    return Regex.IsMatch(input, pattern);
  }

  public static bool is_valid_json_schema(JObject jobj, string schema_filename, out List<string> js_schema_error_messages) {
     //var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    var baseDirectory = AppContext.BaseDirectory;

    // Construct the path to the JSON file
    var json_schema_path = Path.Combine(baseDirectory, "json_schemas", schema_filename);
    assert(File.Exists(json_schema_path));
    var js_schema_str = File.ReadAllText(json_schema_path);

    var js_schema = JSchema.Parse(js_schema_str);

    //res.Data is a JObject
    bool is_valid = ((JObject)jobj).IsValid(js_schema, out IList<string> errorMessages);

    // js_schema_error_messages = [.. errorMessages]; //dotnet8 specific
    js_schema_error_messages = new List<string>(errorMessages);

    // errorMessages.ToList().ForEach(Console.WriteLine);

    return is_valid;

    // Console.WriteLine(res.Data.ToString());
    // Console.WriteLine(is_valid);
    // Console.WriteLine(js_schema_str);
  }

/**
  * Wait for a condition to be true
  * @param condition: The condition to wait for
  * @param timeout: The maximum time to wait for the condition to be true
  * @param interval: The interval to check the condition
  * @return: True if the condition is true before the timeout, False otherwise
*/
  public async static Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan timeout, int interval = 100) {
    var timeoutTask = Task.Delay(timeout);
    while (!condition()) {
      if (await Task.WhenAny(Task.Delay(interval), timeoutTask) == timeoutTask) {
        return false;
      }
    }
    return true;
  }
}