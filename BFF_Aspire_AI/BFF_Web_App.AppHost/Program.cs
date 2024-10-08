using Aspire.Hosting;
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

//For auth in the web app UI
var tenantId     = builder.AddParameter("TenantId");
var clientId     = builder.AddParameter("ClientId");
var clientSecret = builder.AddParameter("ClientSecret",secret:true);

var localOaiKey             = "1234";
var localOaiDeploymentName  = "gpt-4o";
var oaiSimulatorPort        = 8000;

var env = builder.Environment;

var azaoai = builder.AddBicepTemplate(
    name: "AI",
    bicepFile: "../infra/ai.bicep")
    .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName);

var cloudEndpoint   = azaoai.GetOutput("endpoint");
var accountName     = azaoai.GetOutput("accountName");
var cloudKey        = azaoai.GetSecretOutput("accountKey");
var cloudDeployment = "gpt-4o";

var apimai = builder.AddBicepTemplate(
    name: "APIM",
    bicepFile: "../infra/apim.bicep")
    .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
    .WithParameter("apimResourceName", "apim")
    .WithParameter("apimSku", "Basicv2")
    .WithParameter("openAIAccountName", accountName);

var apimEndpoint = apimai.GetOutput("apimResourceGatewayURL");
var apimKey = apimai.GetSecretOutput("subscriptionKey");

builder.AddProject<Projects.BFF_Web_App>("bff-web-app")
    .WithEnvironment("TenantId", tenantId)
    .WithEnvironment("ClientId", clientId)
    .WithEnvironment("ClientSecret", clientSecret)
    .WithEnvironment("oaiKey", localOaiKey)
    .WithEnvironment("oaiDeploymentName", localOaiDeploymentName);

//Instance for mocked (lorem ipsum) responses
builder.AddDockerfile("aoai-simulator-generate", "../AOAI_API_Simulator")
    .WithHttpEndpoint(port: 8000, targetPort:oaiSimulatorPort)
    .WithEnvironment("SIMULATOR_MODE", "generate")
    .WithEnvironment("SIMULATOR_API_KEY", localOaiKey)
    .ExcludeFromManifest();

//Instance for real responses proxied through simulator and recorded
builder.AddDockerfile("aoai-simulator-record", "../AOAI_API_Simulator")
    .WithBindMount("recordings", "/app/.recording")
    .WithHttpEndpoint(port: 8001, targetPort: oaiSimulatorPort)
    .WithEnvironment("SIMULATOR_API_KEY", localOaiKey)
    .WithEnvironment("SIMULATOR_MODE", "record")
    //Switch between cloud and apim endpoints as needed through comments
    //.WithEnvironment("AZURE_OPENAI_ENDPOINT", cloudEndpoint)
    //.WithEnvironment("AZURE_OPENAI_KEY", cloudKey)
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", apimEndpoint)
    .WithEnvironment("AZURE_OPENAI_KEY", apimKey)
    .WithEnvironment("AZURE_OPENAI_DEPLOYMENT", cloudDeployment)
    .WithEnvironment("AZURE_OPENAI_EMBEDDING_DEPLOYMENT", cloudDeployment)
    .ExcludeFromManifest();

//Instance for replayed/offline responses
builder.AddDockerfile("aoai-simulator-replay", "../AOAI_API_Simulator")
    .WithBindMount("recordings", "/app/.recording")
    .WithHttpEndpoint(port: 8002, targetPort: oaiSimulatorPort)
    .WithEnvironment("SIMULATOR_API_KEY", localOaiKey)
    .WithEnvironment("SIMULATOR_MODE", "replay")
    .ExcludeFromManifest();

builder.Build().Run();
