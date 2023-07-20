﻿using Microsoft.Extensions.Configuration;
using OSIsoft.Data;
using OSIsoft.Data.Reflection;
using OSIsoft.Identity;
using System.Net.Http.Headers;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace StreamingUpdatesRestApi
{
    public static class Program
    {
        private static IConfiguration _configuration;
        private static Exception _toThrow;

        public static void Main()
        {
            Console.WriteLine("Beginning sample DotNet application for AVEVA DataHub StreamingUpdates.");
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task<bool> MainAsync(bool test = false)
        {
            #region Setup
            _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName)
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.test.json", optional: true)
                    .Build();

            // ==== Client constants ====
            string tenantId = _configuration["TenantId"];
            string namespaceId = _configuration["NamespaceId"];
            string resource = _configuration["Resource"];
            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];
        
            // ==== Ids ====
            const string TypeId = "Simple Sds Time Value Type";
            const string CommunityId = null;
            string signupId = null;

            // ==== Names ====
            const string SignupName = "PLACEHOLDER_SIGNUP_NAME";
            const string StreamNamePrefix = "stream_";

            // Change this to desired number of streams to create and update.
            const int NumOfStreamsToCreate = 3;
            const int NumOfStreamsToUpdate = 3;
            #endregion

            // Step 1
            // Obtain authentication handler for ADH using Client-credential clients.
            // Create Sds communication services.
            #region Step1
            Uri uriResource = new(resource);
            AuthenticationHandler authenticationHandler = new(uriResource, clientId, clientSecret);
            SdsService sdsService = new(new Uri(resource), authenticationHandler);
            ISdsMetadataService metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
            ISdsDataService dataService = sdsService.GetDataService(tenantId, namespaceId);
            #endregion

            using (HttpClient httpClient = new(authenticationHandler))
            {
                try
                {
                    // Add Community Id to Http Request Headers if necessary.
                    if (CommunityId != null)
                    {
                        httpClient.DefaultRequestHeaders.Add("Community-Id", CommunityId);
                    }

                    // Step 2
                    // Create a simple SDS Type.
                    #region Step2
                    Console.WriteLine("Creating a simple SDS Type");
                    SdsType type = SdsTypeBuilder.CreateSdsType<SdsSimpleType>();
                    type.Id = TypeId;
                    type = await metadataService.GetOrCreateTypeAsync(type).ConfigureAwait(false);
                    #endregion

                    // Step 3
                    // Create SDS Streams and populate list of stream Ids for creating signup.
                    #region Step3
                    Console.WriteLine("Creating SDS Streams and populate list of stream Ids");
                    List<string> streamIdList = new List<string>();

                    for (int i = 0; i < NumOfStreamsToCreate; i++)
                    {
                        SdsStream sdsStream = new SdsStream()
                        {
                            Id = StreamNamePrefix + i,
                            Name = StreamNamePrefix + i,
                            TypeId = type.Id,
                            Description = $"Stream one for ADH Streaming Updates"
                        };

                        sdsStream = await metadataService.GetOrCreateStreamAsync(sdsStream).ConfigureAwait(false);
                        streamIdList.Add(sdsStream.Id);
                    }
                    #endregion

                    // STREAMING UPDATES:
                    // Step 4
                    // Create an ADH Signup against the created resources (streams).
                    #region Step4
                    Console.WriteLine("Creating Signup.");
                    CreateSignupInput signupToCreate = new CreateSignupInput()
                    {
                        Name = SignupName,
                        ResourceType = ResourceType.Stream,
                        ResourceIds = streamIdList,
                    };

                    using StringContent signupToCreateString = new(JsonSerializer.Serialize(signupToCreate));
                    signupToCreateString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(new Uri($"{resource}/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups", UriKind.Absolute), signupToCreateString).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Get Signup Id from HttpResponse.
                    Signup? signup = JsonSerializer.Deserialize<Signup>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    signupId = signup.Id;
                    Console.WriteLine($"Signup {signupId} has been created and is {signup?.SignupState}.");
                    #endregion

                    // Add Delay if signup is not in active state.
                    // Thread.Sleep(30000);

                    // Step 5
                    // Make an API request to GetSignup to activate the signup.
                    #region Step5
                    Console.WriteLine($"Activating signup");
                    response = await httpClient.GetAsync(new Uri($"{resource}/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}", UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);

                    // Check signup state is active.
                    signup = JsonSerializer.Deserialize<Signup>(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    Console.WriteLine($"Signup is now {signup?.SignupState}.");

                    // Get Bookmark for GetUpdates Request from Headers.
                    string getUpdates = response.Headers.TryGetValues("Get-Updates", out var values) ? values.FirstOrDefault(): null;
                    #endregion

                    // TODO: Show resources

                    // Step 6
                    // Make updates to the Streams (post data to stream).
                    #region Step6
                    Console.WriteLine("Making updates to previously created streams.");
                    for (int i = 0; i < NumOfStreamsToUpdate; i++)
                    {
                        var streamId = StreamNamePrefix + i;
                        await dataService.InsertValuesAsync(streamId, GetData());
                    }
                    #endregion

                    // Step 7
                    // Make an API request to GetUpdates and ensure that data updates are received.
                    #region Step7

                    // 30 second delay to catch up to updates.
                    Console.WriteLine("Waiting for updates to process.");
                    Thread.Sleep(30000);

                    Console.WriteLine("Get Updates.");
                    response = await httpClient.GetAsync(new Uri(getUpdates, UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);
                    string updates = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine(updates);
                    #endregion

                    // Step 8
                    // Create a new SDS Stream and make an API Request to UpdateSignupResources to add the stream to signup.
                    #region Step8
                    SdsStream newSdsStream = new SdsStream()
                    {
                        Id = "newStream",
                        Name = "newStream",
                        TypeId = type.Id,
                        Description = $"New Stream for ADH Streaming Updates"
                    };

                    newSdsStream = await metadataService.GetOrCreateStreamAsync(newSdsStream).ConfigureAwait(false);

                    Console.WriteLine("Updating Signup Resources.");
                    SignupResourcesInput signupToUpdate = new SignupResourcesInput()
                    {
                        ResourcesToAdd = new List<string>() { newSdsStream.Id },
                        ResourcesToRemove = new List<string>() { }
                    };

                    using StringContent signupToUpdateString = new(JsonSerializer.Serialize(signupToUpdate));
                    signupToUpdateString.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await httpClient.PostAsync(new Uri($"{resource}/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}/resources", UriKind.Absolute), signupToUpdateString).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);
                    #endregion

                    // Add Delay if new resource isn't shown.
                    // Thread.Sleep(30000);

                    // Step 9
                    // Make an API request to GetSignupResources to view signup with updated resources.
                    #region Step9
                    Console.WriteLine("Get Signup Resources.");
                    response = await httpClient.GetAsync(new Uri($"{resource}/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}/resources", UriKind.Absolute)).ConfigureAwait(false);
                    CheckIfResponseWasSuccessful(response);
                    #endregion
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                    _toThrow = ex;
                    throw;
                }
                finally
                {
                    Console.WriteLine("Cleaning Up.");
                    for (int i = 0; i < NumOfStreamsToCreate; i++)
                    {
                        Console.WriteLine($"Deleting {StreamNamePrefix + i}");
                        RunInTryCatch(metadataService.DeleteStreamAsync, StreamNamePrefix + i);
                    }

                    Console.WriteLine("Deleting newStream.");
                    RunInTryCatch(metadataService.DeleteStreamAsync, "newStream");
                    Console.WriteLine("Deleting Type.");
                    RunInTryCatch(metadataService.DeleteTypeAsync, TypeId);
                    Console.WriteLine($"Deleting ADH Signup with id {signupId}");
                    RunInTryCatch(httpClient.DeleteAsync, $"{resource}/api/v1-preview/Tenants/{tenantId}/Namespaces/{namespaceId}/signups/{signupId}");
                }
            }

            if (test && _toThrow != null)
                throw _toThrow;
            return _toThrow == null;
        }

        /// <summary>
        /// Use this to run a method that you don't want to stop the program if there is an error
        /// </summary>
        /// <param name="methodToRun">The method to run.</param>
        /// <param name="value">The value to put into the method to run</param>
        private static void RunInTryCatch(Func<string, Task> methodToRun, string value)
        {
            try
            {
                methodToRun(value).Wait(10000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got error in {methodToRun.Method.Name} with value {value} but continued on:" + ex.Message);
                _toThrow ??= ex;
            }
        }

        private static void CheckIfResponseWasSuccessful(HttpResponseMessage response)
        {
            // If support is needed please know the Operation-ID header information for support purposes (it is included in the exception below automatically too)
            // string operationId = response.Headers.GetValues("Operation-Id").First();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(response.ToString());
            }
        }

        private static IList<SdsSimpleType> GetData()
        {
            List<SdsSimpleType> data = new()
            {
                new SdsSimpleType {Time = DateTimeOffset.Now, Value = 10},
                new SdsSimpleType {Time = DateTimeOffset.Now, Value = 20},
                new SdsSimpleType {Time = DateTimeOffset.Now, Value = 30},
            };

            return data;
        }
    }
}