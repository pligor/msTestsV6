using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Dynamic;

// using ReportPortal.Shared;
// using ReportPortal.Shared.Execution.Logging;


namespace MyTests.testing {

    [TestClass]
    [Ignore("Skipping this class because it has on purpose various failures which are useful for ReportPortal integration")]
    // [Collection("Overrided Collection Name for DummyTests")]
    public class DummyTests {
        // private readonly ITestOutputHelper _out;
        // public DummyTests(ITestOutputHelper output)
        // {
        //     _out = output.WithReportPortal(); // Integrates ReportPortal with test output
        // }

        // private dynamic _out;
        // public DummyTests() {
        //     //_out = new ExpandoObject();

        //     //_out.WriteLine = (Action<string>)(param => 
        //     {
        //         Console.WriteLine($"Method called with: {param}");
        //     });
        // }

        [TestMethod]
        [TestCategory("dummy")]
        public void Addition_SimpleValuesShouldCalculate() {
            // Arrange
            var a = 5;
            var b = 8;

            // Act
            var result = a + b;

            // Log some useful information for debugging
            // //_out.WriteLine($"Testing addition: {a} + {b} = {result}");

            // Context.Current.Log.Info($"Addition result calculated: {result}");

            //This logs information directly to ReportPortal, allowing for better tracking and analysis of the test results.

            // Assert
            Assert.AreEqual(13, result);
        }

        [TestMethod]
        [TestCategory("dummy")]
        public void Test_FlakyBehavior() {
            // Create a random number generator
            Random random = new Random();

            // Generate a random number between 1 and 10
            int randomNumber = random.Next(1, 11);

            // Define a threshold for passing the test
            int threshold = 3; // ~30% chance to fail

            // Output the random number for logging purposes
            // //_out.WriteLine($"Generated random number: {randomNumber}");
            // Context.Current.Log.Info($"Generated random number: {randomNumber}");

            // The test will fail if the random number is below the threshold
            Assert.IsTrue(randomNumber > threshold, $"Test failed with random number: {randomNumber}");
        }

        [TestMethod]
        [TestCategory("dummy")]
        public void Test_KnownIssue_Fails() {
            var expected = 10;
            var actual = 5; // Known issue, actual does not match expected.

            // Log this as a known issue
            // Context.Current.Log.Info("This failure is linked to known issue JIRA-123");

            // Assert with failure
            Assert.AreEqual(expected, actual);
        }


        // ARITHMETIC
        [TestMethod]
        public void DivisionByZero_ShouldFail() {
            // Arrange
            int divisor = 0;
            int dividend = 10;

            // Act & Log
            //_out.WriteLine($"Attempting to divide {dividend} by {divisor}");
            // Context.Current.Log.Info("Known issue: Division by zero.");

            // Assert
            Assert.ThrowsException<DivideByZeroException>(() => { var result = dividend / divisor; });
            var result = dividend / divisor;
        }

        [TestMethod]
        public void Overflow_ShouldFail() {
            // Arrange
            int maxInt = int.MaxValue;
            int increment = 1;

            // Act & Log
            //_out.WriteLine($"Attempting to increment {maxInt} by {increment}");
            // Context.Current.Log.Info("Known issue: Integer overflow.");

            // Assert
            // Assert.ThrowsException<OverflowException>(() => { checked { var result = maxInt + increment; } });
            checked {
                var result = maxInt + increment;
            }
        }

        [TestMethod]
        public void InvalidOperation_ShouldFail() {
            // Arrange
            int[] numbers = new int[] { 1, 2, 3 };
            int index = 3;

            // Act & Log
            //_out.WriteLine($"Attempting to access index {index} in array of length {numbers.Length}");
            // Context.Current.Log.Info("Known issue: Index out of bounds.");

            // Assert
            Assert.ThrowsException<IndexOutOfRangeException>(() => { var result = numbers[index]; });
            var result = numbers[index];
        }

        // STRING MANIPULATION
        [TestMethod]
        public void NullReference_ShouldFail() {
            // Arrange
            string input = null;

            // Act & Log
            //_out.WriteLine("Attempting to call method on null string.");
            // Context.Current.Log.Info("Known issue: Null reference exception.");

            // Assert
            Assert.ThrowsException<NullReferenceException>(() => input.ToUpper());
            // input.ToUpper();
        }

        [TestMethod]
        public void InvalidCast_ShouldFail() {
            // Arrange
            object number = 123;

            // Act & Log
            //_out.WriteLine("Attempting to cast an integer to a string.");
            // Context.Current.Log.Info("Known issue: Invalid cast exception.");

            // Assert
            Assert.ThrowsException<InvalidCastException>(() => { var text = (string)number; });
            var text = (string)number;
        }

        [TestMethod]
        public void StringFormat_ShouldFail() {
            // Arrange
            string format = "{0} is {1} years old";
            object[] args = new object[] { "Alice" };

            // Act & Log
            //_out.WriteLine("Attempting to format string with missing argument.");
            // Context.Current.Log.Info("Known issue: String format exception.");

            // Assert
            Assert.ThrowsException<FormatException>(() => string.Format(format, args));
            string.Format(format, args);
        }

        // FILE ERRORS
        [TestMethod]
        public void FileNotFound_ShouldFail() {
            // Arrange
            string filePath = "nonexistent_file.txt";

            // Act & Log
            //_out.WriteLine($"Attempting to open file at {filePath}");
            // Context.Current.Log.Info("Known issue: File not found exception.");

            // Assert
            Assert.ThrowsException<FileNotFoundException>(() => File.OpenRead(filePath));
            File.OpenRead(filePath);
        }

        [TestMethod]
        public void UnauthorizedAccess_ShouldFail() {
            // Arrange
            string filePath = "/protected_path/protected_file.txt";

            // Act & Log
            //_out.WriteLine($"Attempting to write to file at {filePath}");
            // Context.Current.Log.Info("Known issue: Unauthorized access exception.");

            // Assert
            Assert.ThrowsException<UnauthorizedAccessException>(() => File.WriteAllText(filePath, "data"));
            File.WriteAllText(filePath, "data");
        }

        [TestMethod]
        public void DirectoryNotFound_ShouldFail() {
            // Arrange
            string directoryPath = "/nonexistent_directory/";

            // Act & Log
            //_out.WriteLine($"Attempting to access directory at {directoryPath}");
            // Context.Current.Log.Info("Known issue: Directory not found exception.");

            // Assert
            Assert.ThrowsException<DirectoryNotFoundException>(() => Directory.GetFiles(directoryPath));
            Directory.GetFiles(directoryPath);
        }
    }
}
