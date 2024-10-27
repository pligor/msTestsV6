# Do a smoke check of the framework with this command
`dotnet test --filter FullyQualifiedName~MyTests.MyRequestsTests.test_json_get_request_with_model`

# Specify dotnet version

List available SDKs:
`dotnet --list-sdks`

To use specific version of dotnet you need to create global.json file via this command:
`dotnet new globaljson --sdk-version 6.0.425`

# Install `MSTest` framework

`dotnet add package MSTest.TestAdapter`
`dotnet add package MSTest.TestFramework`
add specific version if you need to: `--version 3.0.2`

# Here we are using `MSTest` test framework

Execute all tests:
`dotnet test`

Filter by Class name:
`dotnet test --filter FullyQualifiedName~UnitTest1`

Filter by method:
`dotnet test --filter FullyQualifiedName~MyTests.MyRequestsTests.test_json_get_request_with_model`

Filter by partial name of the method:
`dotnet test --filter FullyQualifiedName~TestGraphQL.test04`
here all methods starting with "test04" will be included in the test run

Filter by TestCategory:
`dotnet test --filter "TestCategory=json_req"`

Basic HTML report
`dotnet test --filter FullyQualifiedName~TestGraphQL.test12 --logger html`

Add libraries for GraphQL:
`dotnet add package GraphQL.Client`
`dotnet add package GraphQL.Client.Serializer.Newtonsoft`
`dotnet add package Newtonsoft.Json.Schema`

Add library for Generating unique short ids:
`dotnet add package Nanoid`

Add library for RabbitMQ:
`dotnet add package RabbitMQ.Client`
List Queues in terminal:
`rabbitmqctl list_queues name messages`

# Test Categories (tags/labels)

Example:

```
  [TestMethod]
  [TestCategory("high_prio")]
  [TestCategory("rabbitmq")]
  [TestCategory("login")]
  public void Test1()
```

Execute tests based on Test categories:
`dotnet test --filter "TestCategory=rabbitmq"`
`dotnet test --filter "(TestCategory=PriorityHigh)&(TestCategory=FeatureLogin)"`
Use logical operators: `& (AND), | (OR), and ! (NOT)`


# HOW TO GENERATE GRAPHQL CLIENTS FROM SCHEMA ########################################################

## Install StrawberryShake globally
`dotnet tool install StrawberryShake.Tools --global`
or
`dotnet tool update StrawberryShake.Tools --global`

## Setup project to support Depedency injection as it is mandatory by StrawberryShake
`dotnet add package Microsoft.Extensions.DependencyInjection`
`dotnet add package Microsoft.Extensions.Http`
`dotnet add package StrawberryShake`
`dotnet add package StrawberryShake.Transport.Http`

# Ensure that StrawberryShake does NOT re-generate the clients upon every build
Remove this package: StrawberryShake.CodeGeneration.CSharp.Analyzers
`dotnet remove package StrawberryShake.CodeGeneration.CSharp.Analyzers`

## how to initialize 
`dotnet graphql init https://spacex-production.up.railway.app/graphql`
where the link is the graphql endpoint directly.

## next times to update the schema
`dotnet graphql update`

## Generate client
`dotnet graphql generate -n MyGraphQLClient.SpaceX -o Generated`
-n is the namespace that you want the client generated in
-o  is the output folder
################################################################################################


# MINIMAL SETUP For Research Portal ########################################################

git clone <https://github.com/reportportal/reportportal.git>
docker compose -p reportportal up -d
Visit: <http://localhost:8080>

- Default Super Admin: superadmin
- Default Super Admin pass (from yml file of docker): erebus
• Default Plain User Username: default
• Default Plain User Password: 1q2w3e

https://reportportal.io/docs/log-data-in-reportportal/test-framework-integration/Net/VSTest/
https://github.com/reportportal/agent-net-vstest#readme

Add libraries for Research Portal:
`dotnet add package ReportPortal.VSTest.TestLogger`
`dotnet add package ReportPortal.Shared`

create file: ReportPortal.config.json
```
{
  "$schema": "https://raw.githubusercontent.com/reportportal/agent-net-vstest/develop/src/ReportPortal.VSTest.TestLogger/ReportPortal.config.schema",
  "enabled": true,
  "server": {
    "url": "http://localhost:8080/api/v1",
    "project": "superadmin_personal",
    "apiKey": "superadmin-my-tests-mstest_wIKB5FatRmaLzzy2mxD0gIImjk5LevCgaDY8swqordTMe_Kwfl9Dv0kuI47I9qk_"
  },
  "launch": {
    "name": "Georgioss MSTest Tests",
    "description": "A description of your launch",
    "debugMode": false,
    "attributes": ["mstest"]
  }
}
```

To integrate ReportPortal with your MSTest tests, you only need to make changes to your existing test classes:
Include these:

  ```
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using ReportPortal.Shared;
  using System;
  using System.Reflection;
  ```

Add inside the Test Class:
    ```
    // private readonly ITestOutputHelper _out;
    private readonly ITestOutputHelper_out;

    public DummyTests(ITestOutputHelper output)
    {
        _out = output.WithReportPortal(); // Integrates ReportPortal with test output
    }
    ```

    and log like so:
    ```
    _out.WriteLine($"Testing addition: {a} + {b} = {result}");
    Context.Current.Log.Info($"Addition result calculated: {result}");
    ```

Finally you execute
`dotnet test ...`
and you see the launches here: <http://localhost:8080/ui/?#superadmin_personal/launches/all>
################################################################################################

# How to use NSwag to generate C# clients from OpenAPI specs ########################################################

0) REMOVE the package from inside the project if it exists:
`dotnet remove package NSwag.MSBuild`

1) Install NSwag as a global tool:
`dotnet tool install -g NSwag.ConsoleCore`

2) Create the nswag.json file:

```json
{
  "runtime": "Net80",
  "defaultVariables": null,
  "documentGenerator": {
    "fromDocument": {
      "url": "https://petstore.swagger.io/v2/swagger.json",
      "output": null,
      "newLineBehavior": "Auto"
    }
  },
  "codeGenerators": {
    "openApiToCSharpClient": {
      "clientBaseClass": null,
      "configurationClass": null,
      "generateClientClasses": true,
      "suppressClientClassesOutput": false,
      "generateClientInterfaces": false,
      "suppressClientInterfacesOutput": false,
      "clientBaseInterface": null,
      "injectHttpClient": true,
      "disposeHttpClient": false,
      "protectedMethods": [],
      "generateExceptionClasses": true,
      "exceptionClass": "ApiException",
      "wrapDtoExceptions": false,
      "useHttpClientCreationMethod": true,
      "httpClientType": "System.Net.Http.HttpClient",
      "useHttpRequestMessageCreationMethod": false,
      "useBaseUrl": true,
      "generateBaseUrlProperty": true,
      "generateSyncMethods": true,
      "generatePrepareRequestAndProcessResponseAsAsyncMethods": false,
      "exposeJsonSerializerSettings": false,
      "clientClassAccessModifier": "public",
      "typeAccessModifier": "public",
      "propertySetterAccessModifier": "",
      "generateNativeRecords": false,
      "generateContractsOutput": false,
      "contractsNamespace": null,
      "contractsOutputFilePath": null,
      "parameterDateTimeFormat": "s",
      "parameterDateFormat": "yyyy-MM-dd",
      "generateUpdateJsonSerializerSettingsMethod": true,
      "useRequestAndResponseSerializationSettings": false,
      "serializeTypeInformation": false,
      "queryNullValue": "",
      "className": "PetstoreClient",
      "operationGenerationMode": "SingleClientFromOperationId",
      "additionalNamespaceUsages": [],
      "additionalContractNamespaceUsages": [],
      "generateOptionalParameters": true,
      "generateJsonMethods": false,
      "enforceFlagEnums": false,
      "parameterArrayType": "System.Collections.Generic.IEnumerable",
      "parameterDictionaryType": "System.Collections.Generic.IDictionary",
      "responseArrayType": "System.Collections.Generic.ICollection",
      "responseDictionaryType": "System.Collections.Generic.IDictionary",
      "wrapResponses": false,
      "wrapResponseMethods": [],
      "generateResponseClasses": true,
      "responseClass": "SwaggerResponse",
      "namespace": "MyTests.TestNSwag",
      "requiredPropertiesMustBeDefined": true,
      "dateType": "System.DateTimeOffset",
      "jsonConverters": null,
      "anyType": "object",
      "dateTimeType": "System.DateTimeOffset",
      "timeType": "System.TimeSpan",
      "timeSpanType": "System.TimeSpan",
      "arrayType": "System.Collections.Generic.ICollection",
      "arrayInstanceType": "System.Collections.ObjectModel.Collection",
      "dictionaryType": "System.Collections.Generic.IDictionary",
      "dictionaryInstanceType": "System.Collections.Generic.Dictionary",
      "arrayBaseType": "System.Collections.ObjectModel.Collection",
      "dictionaryBaseType": "System.Collections.Generic.Dictionary",
      "classStyle": "Poco",
      "jsonLibrary": "SystemTextJson",
      "generateDefaultValues": true,
      "generateDataAnnotations": true,
      "excludedTypeNames": [],
      "excludedParameterNames": [],
      "handleReferences": false,
      "generateImmutableArrayProperties": false,
      "generateImmutableDictionaryProperties": false,
      "jsonSerializerSettingsTransformationMethod": null,
      "inlineNamedArrays": false,
      "inlineNamedDictionaries": false,
      "inlineNamedTuples": true,
      "inlineNamedAny": false,
      "generateDtoTypes": true,
      "generateOptionalPropertiesAsNullable": true,
      "generateNullableReferenceTypes": true,
      "templateDirectory": null,
      "serviceHost": null,
      "serviceSchemes": null,
      "output": "TestNSwag/generated/PetstoreClient.cs",
      "newLineBehavior": "Auto"
    }
  }
}
```

3) Generate the client:
`nswag run nswag.json`

4) Use the generated client in your tests like so:

```csharp
        var httpClient = new HttpClient { BaseAddress = new Uri("https://petstore.swagger.io/v2") };
        var client = new PetstoreClient(httpClient);
        var pets = await _client.FindPetsByStatusAsync([Anonymous.Available]);
        Assert.IsTrue(pets.Count > 0);
```
