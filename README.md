# DopamineDetoxFunction

## Overview
DopamineDetoxFunction is an Azure Functions application that provides backend services for the Dopamine Detox application, helping users manage their social media consumption. These functions reach out to an API to scrape Twitter/X results (https://github.com/rroethle7474/social-media-detox-pythonapi) and YouTube results and save them to the Database via the .NET API (https://github.com/rroethle7474/DopamineDetoxAPI). This is setup to be run in Azure (.NET 8, v4 Azure Functions)

To run locally, create a local.settings.json file from the template.settings.json file and then update the following settings: 

-AzureSignalRConnectionString: Only needed locally if you want to test out notifications being received on the front end app locally. Otherwise, a SignalR service can be set up within Azure for use here).

-YouTubeApiKey: Create a YouTubeAPI key to allow for searching: (details found here: https://developers.google.com/youtube/v3/getting-started)

-SearchApiUrl: Poorly named, but this is the python API used to scrape Twitter/X results (https://github.com/rroethle7474/social-media-detox-pythonapi)

-DopamineDetox:BaseUrl: Another poorly named variable. This is the .NET API used for retrieving YouTube results (https://github.com/rroethle7474/DopamineDetoxAPI) and retrieving data from the Database.

-ConnectionString: ConnectionString used for SQL database from the dbo tables found here (https://github.com/rroethle7474/ProjectDb/tree/main/dbo/Tables)

## HTTP Trigger Functions

The application includes several HTTP trigger functions that handle real-time communication with clients:

### 1. Negotiate Function

```csharp
[Function("negotiate")]
public async Task<HttpResponseData> Negotiate(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
    [SignalRConnectionInfoInput(HubName = "socialmedia")] string connectionInfo)
```

This function handles the SignalR connection negotiation process:
- Triggered by HTTP POST requests to the `/api/negotiate` endpoint
- Requires no authentication (AuthorizationLevel.Anonymous)
- Returns connection information needed for clients to establish a SignalR connection
- Essential for setting up real-time communication between the server and clients

### 2. SendUpdate Function

```csharp
[Function("SendUpdate")]
[SignalROutput(HubName = "socialmedia")]
public SignalRMessageAction SendUpdate(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
```

This function broadcasts updates to connected clients:
- Triggered by HTTP GET requests to the `/api/SendUpdate` endpoint
- Uses the `SignalROutput` binding to send messages to all connected clients
- Returns a `SignalRMessageAction` that:
  - Specifies the event name ("dataUpdated") that clients should listen for
  - Includes data about the current update time and next scheduled update
- Clients can subscribe to the "dataUpdated" event to receive these notifications in real-time

## Social Media Retriever Functions

The application includes several functions that retrieve and process social media data: (Please note that the Timer functions have been commented out)

### 1. WeeklyCleanupSocialMediaData

```csharp
[Function("WeeklyCleanupSocialMediaData")]
public async Task<IActionResult> WeeklyCleanupSocialMediaData(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
```

This function performs weekly cleanup of social media data:
- Clears caches and old data
- Sends weekly reports to MVP users with their search results
- Adds a weekly search result report to track that the process has run

### 2. YouTube Data Functions

#### GetYTDefaultSearchResults

```csharp
[Function("GetYTDefaultSearchResults")]
public async Task<IActionResult> GetYTDefaultSearchResults(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
```

This function retrieves default YouTube search results based on predefined topics stored in the database.

#### GetYTSocialMediaData

```csharp
[Function("GetYTSocialMediaData")]
public async Task<IActionResult> GetYTSocialMediaData(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
```

This function retrieves YouTube data based on user-defined search terms stored in the database.

#### GetYTSocialMediaChannelData

```csharp
[Function("GetYTSocialMediaChannelData")]
public async Task<IActionResult> GetYTSocialMediaChannelData(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
```

This function retrieves YouTube data from specific channels stored in the database.

### 3. Twitter/X Data Functions

#### GetXDefaultSearchResults

```csharp
[Function("GetXDefaultSearchResults")]
public async Task<IActionResult> GetXDefaultSearchResults(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
```

This function retrieves default X (Twitter) search results based on predefined topics stored in the database.

#### GetXSocialMediaData

```csharp
[Function("GetXSocialMediaData")]
public async Task<IActionResult> GetXSocialMediaData(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
```

This function retrieves X (Twitter) data based on user-defined search terms stored in the database.

#### GetXSocialMediaChannelData

```csharp
[Function("GetXSocialMediaChannelData")]
public async Task<IActionResult> GetXSocialMediaChannelData(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
```

This function retrieves X (Twitter) data from specific accounts/channels stored in the database.

### 4. Comprehensive Data Retrieval

#### SocialMediaDataHttpTrigger

```csharp
[Function("SocialMediaDataHttpTrigger")]
[SignalROutput(HubName = "socialmedia")]
public async Task<SignalRMessageAction> GetSocialMediaDataHttpTrigger(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
```

This function performs a comprehensive retrieval of all social media data:
- Resets data cache
- Retrieves YouTube default, user, and channel data
- Retrieves X (Twitter) default, user, and channel data 
- Broadcasts updates to connected clients via SignalR with detailed success/error information

### 5. Daily Quote Function

#### AddDailyQuoteHttpTrigger

```csharp
[Function("AddDailyQuoteHttpTrigger")]
[SignalROutput(HubName = "socialmedia")]
public async Task<SignalRMessageAction> AddDailyQuoteHttpTrigger(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
```

This function creates a daily motivational quote:
- Adds a new quote to the system
- Notifies connected clients via SignalR that new data is available

## SignalR Integration

The application uses Azure SignalR Service to enable real-time communication:

1. **Connection Setup**:
   - Clients first call the `negotiate` endpoint to establish a connection
   - The server provides necessary connection tokens and endpoints

2. **Message Broadcasting**:
   - The server broadcasts updates using `SignalRMessageAction`
   - Example: `return new SignalRMessageAction("dataUpdated", new[] { message });`
   - The first parameter ("dataUpdated") is the event name clients subscribe to
   - The second parameter contains the data payload sent to clients

3. **Client Implementation**:
   - Clients connect to SignalR using the connection info from the negotiate endpoint
   - They subscribe to specific events (e.g., "dataUpdated")
   - When the server broadcasts an event, all subscribed clients receive the update in real-time

## Configuration

The application uses Azure SignalR Service, which requires a connection string configured in the application settings:

```json
"AzureSignalRConnectionString": "Endpoint=https://your-signalr-service.service.signalr.net;AccessKey=your-access-key;Version=1.0;"
```