using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace MyTests.libs;

/**
  * Helper class for interacting with RabbitMQ
  You NEED to run this command in terminal:
  `rabbitmq-plugins enable rabbitmq_management`

  RabbitMQ Management HTTP API
  */

public static class RabbitMqHelper
{
  private static readonly HttpClient client = new HttpClient();

  public static void SetupDeadLetterExchangeAndQueue(string exchange_name = "my_custom_dlx",
    string dead_letter_queue = "my_custom_dead_letter_queue", string routing_key = "my_custom_dead_letter_routing_key")
  {
    var factory = new ConnectionFactory() { HostName = "localhost" };
    using (var connection = factory.CreateConnection())
    using (var channel = connection.CreateModel())
    {
      // Declare the dead-letter exchange with a custom name
      channel.ExchangeDeclare(exchange: exchange_name, type: "direct");

      // Declare the dead-letter queue with a custom name
      channel.QueueDeclare(queue: dead_letter_queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

      // Bind the dead-letter queue to the dead-letter exchange with a custom routing key
      channel.QueueBind(queue: dead_letter_queue, exchange: exchange_name, routingKey: routing_key);

      Assert.IsTrue(channel.MessageCount(dead_letter_queue) == 0, "Messages in the dead-letter queue should be empty initially");
    }
  }

  static RabbitMqHelper()
  {
    var byteArray = new UTF8Encoding().GetBytes("guest:guest"); // Default username and password
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
  }

  public static async Task<int> GetUnacknowledgedMessageCount(string queueName)
  {
    var response = await client.GetAsync($"http://localhost:15672/api/queues/%2f/{queueName}");
    response.EnsureSuccessStatusCode();

    var responseString = await response.Content.ReadAsStringAsync();
    dynamic queueInfo = JObject.Parse(responseString);

    // Console.WriteLine($"Messages info: {queueInfo}");
    /*
    {
      "garbage_collection": {
        "max_heap_size": -1,
        "min_bin_vheap_size": -1,
        "min_heap_size": -1,
        "fullsweep_after": -1,
        "minor_gcs": -1
      },
      "consumer_details": [],
      "arguments": {},
      "auto_delete": false,
      "deliveries": [],
      "durable": false,
      "exclusive": false,
      "incoming": [],
      "name": "queue-NIgx9",
      "node": "rabbit@localhost",
      "state": "running",
      "type": "classic",
      "vhost": "/"
    }
    */
    //TODO it seems that "messages_unacknowledged" is not present in the response

    return (int)queueInfo.messages_unacknowledged;
  }

  public static async Task<int> GetReadyMessageCount(string queueName)
  {
    var response = await client.GetAsync($"http://localhost:15672/api/queues/%2f/{queueName}");
    response.EnsureSuccessStatusCode();

    var responseString = await response.Content.ReadAsStringAsync();
    dynamic queueInfo = JObject.Parse(responseString);

    return (int)queueInfo.messages_ready;
  }
}
