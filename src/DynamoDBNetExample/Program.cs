using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DynamoDBNetExample
{
    public class Program
    {
        private const string accessKey = "[add your access key here]";
        private const string secretKey = "[add your secret key here]";

        public static void Main(string[] args)
        {

            Task t = MainAsync(args);
            t.Wait();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static async Task MainAsync(string[] args)
        {
            string tableName = "AlexaAudioStates";
            string hashKey = "UserId";

            Console.WriteLine("Creating credentials and initializing DynamoDB client");
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);

            Console.WriteLine("Verify table => " + tableName);
            var tableResponse = await client.ListTablesAsync();
            if (!tableResponse.TableNames.Contains(tableName))
            {
                Console.WriteLine("Table not found, creating table => " + tableName);
                await client.CreateTableAsync(new CreateTableRequest
                {
                    TableName = tableName,
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 3,
                        WriteCapacityUnits = 1
                    },
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement
                        {
                            AttributeName = hashKey,
                            KeyType = KeyType.HASH
                        }
                    },
                    AttributeDefinitions = new List<AttributeDefinition>
                    {
                        new AttributeDefinition { AttributeName = hashKey, AttributeType=ScalarAttributeType.S }
                    }
                });
                
                bool isTableAvailable = false;
                while (!isTableAvailable) {
                    Console.WriteLine("Waiting for table to be active...");
                    Thread.Sleep(5000);
                    var tableStatus = await client.DescribeTableAsync(tableName);
                    isTableAvailable = tableStatus.Table.TableStatus == "ACTIVE";
                }
            }

            Console.WriteLine("Set a local DB context");
            var context = new DynamoDBContext(client);

            Console.WriteLine("Create an AlexaAudioState object to save");
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

            Console.WriteLine("Save an AlexaAudioState object");
            await context.SaveAsync<AlexaAudioState>(currentState);

            Console.WriteLine("Getting an AlexaAudioState object");
            List<ScanCondition> conditions = new List<ScanCondition>();
            conditions.Add(new ScanCondition("UserId", ScanOperator.Equal, currentState.UserId));
            var allDocs = await context.ScanAsync<AlexaAudioState>(conditions).GetRemainingAsync();
            var savedState = allDocs.FirstOrDefault();

            Console.WriteLine("Verifying object...");
            if (JsonConvert.SerializeObject(savedState) == JsonConvert.SerializeObject(currentState))
                Console.WriteLine("Object verified");
            else
                Console.WriteLine("oops, something went wrong");

            //Console.WriteLine("Delete table => " + tableName);
            //context.Dispose();
            //await client.DeleteTableAsync(new DeleteTableRequest() { TableName = tableName });
        }

    }
}
