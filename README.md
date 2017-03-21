# Getting Started With DynamoDB with C# (.NET Core)
This is a very simple .NET Core console app with DynamoDB. It verifies or creates a DynamoDB table, waits for that table to become active, then adds one document to the table and retrieves it.

The item stored in the database is modeled on the audio state item created in [the Alexa Audio Player sample](https://github.com/alexa/skill-sample-nodejs-audio-player).

# Nuget Package
[AWSSDK Amazon DynamoDB ](https://www.nuget.org/packages/AWSSDK.DynamoDBv2/)

Search or type <code>Install-Package AWSSDK.DynamoDBv2</code> into your Package Manager Console

Add the following namespaces

```csharp
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
```

# Set Your AWS Credentials
```csharp
BasicAWSCredentials(accessKey, secretKey);
var client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
```

# Verify a Table

```csharp
var tableResponse = await client.ListTablesAsync();
if (!tableResponse.TableNames.Contains(tableName))
{
    // Create our table if it doesn't exist
}
```

# Create a New Table
```csharp
await client.CreateTableAsync(new CreateTableRequest
{
    TableName = tableName,
    ProvisionedThroughput = new ProvisionedThroughput
    {
        ReadCapacityUnits = 3,
        WriteCapacityUnits = 1
    },
    KeySchema = new List&lt;KeySchemaElement&gt;
    {
        new KeySchemaElement
        {
            AttributeName = hashKey,
            KeyType = KeyType.HASH
        }
    },
    AttributeDefinitions = new List&lt;AttributeDefinition&gt;
    {
        new AttributeDefinition {
            AttributeName = hashKey,
            AttributeType =ScalarAttributeType.S
        }
    }
});
```

# Wait for New Table to Create / Become Active
```csharp
bool isTableAvailable = false;
while (!isTableAvailable) {
    Thread.Sleep(5000);
    var tableStatus = await client.DescribeTableAsync(tableName);
    isTableAvailable = tableStatus.Table.TableStatus == "ACTIVE";
}
```

# Set Your Context
```csharp
var context = new DynamoDBContext(client);
```

# Create an Object and Save It
```csharp
// Create our audio state object
AlexaAudioState currentState = new AlexaAudioState
{
    UserId = "someAwesomeUser",
    State = new StateMap()
    {
        EnqueuedToken = "awesomeAudioPart2",
        Index = 0,
        Loop = true,
        OffsetInMS = 123456,
        PlaybackFinished = false,
        PlaybackIndexChanged = false,
        playOrder = new List<int> { 0, 1 },
        Shuffle = false,
        State = "PLAY_MODE",
        Token = "awesomeAudioPart1"
    }
};

// Save that object into our DynamoDB
await context.SaveAsync<AlexaAudioState>(currentState);
```

# Retrieve a Document
```csharp
List<ScanCondition> conditions = new List<ScanCondition>();
conditions.Add(new ScanCondition("UserId", ScanOperator.Equal, currentState.UserId));
var allDocs = await context.ScanAsync<AlexaAudioState>(conditions).GetRemainingAsync();
var savedState = allDocs.FirstOrDefault();

```

# Delete a Table
```csharp
context.Dispose();
await client.DeleteTableAsync(new DeleteTableRequest() { TableName = tableName });

```

