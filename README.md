# AVEVA Data Hub StreamingUpdates Service .NET REST API Sample

## Version
Developed against DotNet 6.0

## Requirements
The [.NET Core CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/) is referenced in this sample, and should be installed to run the sample from the command line.

## Definitions
* ADH: AVEVA DataHub
* SDS: Sequential Data Store

## About this repository

The sample code in this repository demonstrates REST API calls to ADH for creating a Signup to receive resource updates. Signups allow clients to subscribe resources (for example, streams) and get updates for those resources.

The sample will perform the following procedures:
1. Obtain an OAuth token for ADH using a client-credentials client
1. Create a simple SDS Type
1. Create a SDS Stream
1. Create an ADH Signup against the created resources (streams)
1. Make an API request to GetSignup to activate the signup
1. Make an API request to GetSignupResources to view accessible and inaccessible resources in the signup
1. Make updates to the Streams (post data to stream)
1. Make an API request to GetUpdates and ensure that data updates are received
1. Create a new SDS Stream and make an API request to UpdateSignupResources to include the new stream
1. Make an API request to GetSignupResources to view signup with updated resources
1. Cleanup signup, streams, and type

## Configuring the sample

The sample is configured using the file appsettings.placeholder.json.  Before editing, rename this file to `appsettings.json`. This repository's `.gitignore` rules should prevent the file from ever being checked in to any fork or branch, to ensure credentials are not compromised. 

AVEVA Data Hub is secured by obtaining tokens from its identity endpoint. Client credentials clients provide a client application identifier and an associated secret (or key) that are authenticated against the token endpoint. You must replace the placeholders in your `appsettings.json` file with the authentication-related values from your tenant and a client-credentials client created in your ADH tenant.

```json
{
  "NamespaceId": "PLACEHOLDER_REPLACE_WITH_NAMESPACE_ID",
  "TenantId": "PLACEHOLDER_REPLACE_WITH_TENANT_ID",
  "Resource": "https://uswe.datahub.connect.aveva.com",
  "ClientId": "PLACEHOLDER_REPLACE_WITH_CLIENT_ID",
  "ClientSecret": "PLACEHOLDER_REPLACE_WITH_CLIENT_SECRET",
}
```

Within the sample, there are configurable options for number of streams to create, number of streams to update, adding a community id to the HTTP request headers, and the signup name. Note, a community id or changing the configurable options is not required to use the sample.

## Running the sample

To run this example from the command line once the `appsettings.json` is configured, run

```shell
cd StreamingUpdatesRestApi
dotnet restore
dotnet run
```
---

## Running the automated test

To test the sample, run

```shell
cd StreamingUpdatesRestApiTest
dotnet restore
dotnet test
```

---

Tested against DotNet 6.0.  
For the ADH Signups samples page [ReadMe]()  
For the main ADH samples page [ReadMe](https://github.com/osisoft/OSI-Samples-OCS)  
For the main AVEVA samples page [ReadMe](https://github.com/osisoft/OSI-Samples)