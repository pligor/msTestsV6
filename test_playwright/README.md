ALWAYS execute these two steps for the first time:

Add the Playwright package to your project by running (even if already found in csproj):
`dotnet add package Microsoft.Playwright`

### Step 1: Install Playwright CLI as a .NET Global Tool

The `dotnet playwright` command is part of the Playwright .NET tool, which needs to be installed separately as a global tool. Install it using the following command:

```bash
dotnet tool install --global Microsoft.Playwright.CLI
```

This command installs the Playwright CLI globally, making the `dotnet playwright` command available from anywhere on your system.

### Step 2: Ensure the Global Tool Path is in Your System PATH

After installing the Playwright CLI tool, ensure that the path to global tools is included in your system PATH environment variable. You can verify this by running:

```bash
dotnet tool list -g
```

Ensure that `Microsoft.Playwright.CLI` appears in the list. If it's listed but not working, you may need to restart your terminal or add the tool's path to your PATH variable manually.

### Step 3: Install Playwright Browsers

Now, run the `dotnet playwright install` command again to install the required browser binaries:

```bash
dotnet playwright install
```

AND if this does not work
Just do a `playwright install` instead. Because playwright.exe (found e.g. in C:\Users\SFP7ZGX\.dotnet\tools) should be already in PATH
BUT execute it within the root folder of a dotnet project like this one.

If you still have errors you might need to open PowerShell and execute:  `.\bin\Debug\net6.0\playwright.ps1 install`


### Step 4: Verify the Installation

To confirm that everything is set up correctly, try running a simple Playwright test again to ensure the browsers are available and working as expected.

These steps should resolve the issue, allowing you to proceed with running Playwright tests in C#. Let me know if you encounter any other issues!
