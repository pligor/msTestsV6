namespace MyTests.test_rabbitmq;

using System.Threading.Tasks;
using static MyTests.libs.GenericHelper;
using NanoidDotNet;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using Microsoft.Playwright;
using System.Reactive;
using System.Net.Http;
using System.Net.Http.Headers;
using MyTests.libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;


// https://docs.google.com/spreadsheets/d/10ZQCt-X8-5xGl9kfCReTehVKg7EoALBOAlSCqSICFEY/edit?gid=0#gid=0

// Package required:
// dotnet add package RabbitMQ.Client

//dotnet test --filter FullyQualifiedName~TestRabbitMQ

[TestClass]
public class TestRabbitMQ {
  private readonly HttpClientHandler handler = new() {
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
  };

  public Task DisposeAsync() {
    // throw new NotImplementedException();
    return Task.CompletedTask;
  }

  public Task InitializeAsync() {
    // throw new NotImplementedException();
    return Task.CompletedTask;
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public async Task test14_AutoDeleteQueue() {
    /*
    	1.	Create the Queue:
      •	Declare an auto-delete queue.
      2.	Publish Messages:
      •	Publish some messages to the queue.
      3.	Consume Messages:
      •	Attach a consumer to the queue and start consuming messages.
      4.	Remove the Consumer:
      •	Unsubscribe the consumer from the queue.
      5.	Verify Queue Deletion:
      •	Use QueueDeclarePassive to assert that the queue has been deleted.
    */

    var queue_name = $"auto-delete-queue-{Nanoid.Generate(size: 5)}";
    var message = "Temporary Message";

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        // Step 1: Declare an auto-delete queue
        channel.QueueDeclare(queue: queue_name, durable: false, exclusive: false,
        autoDelete: true, //Queue which is automatically deleted when the last consumer unsubscribes
        arguments: null);

        // Step 2: Publish messages to the queue
        channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: null, body: Encoding.UTF8.GetBytes(message));
        await Task.Delay(200);
        Assert.AreEqual(1L, channel.MessageCount(queue_name));

        // Step 3: Attach a consumer to the queue
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) => {
          var body = ea.Body.ToArray();
          var receivedMessage = Encoding.UTF8.GetString(body);
          Assert.AreEqual(message, receivedMessage);
          channel.BasicAck(ea.DeliveryTag, false);
        };

        // Step 4: Consume the message
        string consumerTag = channel.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
        await Task.Delay(200);

        // Verify that the message was consumed
        Assert.AreEqual(0L, channel.MessageCount(queue_name));

        // Step 5: Unsubscribe the consumer
        // HERE IT IS: The queue should be deleted after the consumer is detached
        channel.BasicCancel(consumerTag);
        await Task.Delay(200);
      }

      print("Verify the queue is deleted after the consumer is detached");
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        try {
          channel.QueueDeclarePassive(queue_name);
          Assert.Fail("The queue should be deleted and not exist.");
        } catch (OperationInterruptedException ex) {
          // Expected exception as the queue should be deleted
          // Assert. Contains("NOT_FOUND - no queue", ex.Message);
          StringAssert.Contains(ex.Message, "NOT_FOUND - no queue");
        }
      }
    } finally {
      // Clean up in case the test fails before deletion
      delete_queue(queue_name);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public async Task test13_exclusive_queues() {
    /*
    Exclusive queue
    Create queue with one message
    Try to consume it in another connection, get an expected RabbitMQ.Client.Exceptions.OperationInterruptedException
    
    Non-Exclusive Queue
    Create queue with one message
    Try to consume it in another connection and it works just fine
    We then delete it explicitly
    */

    var message = "Single Message";

    var queue_non_exclusive = $"queue-non-exclusive-{Nanoid.Generate(size: 5)}";
    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        Assert.IsTrue(connection.IsOpen);
        channel.QueueDeclare(queue: queue_non_exclusive, durable: true,
        exclusive: false, //NON EXCLUSIVE
        autoDelete: false, arguments: null);

        channel.BasicPublish(exchange: "", routingKey: queue_non_exclusive, basicProperties: null, body: Encoding.UTF8.GetBytes(message));
        await Task.Delay(200);
        Assert.AreEqual(1L, channel.MessageCount(queue_non_exclusive));
      }

      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        Assert.IsTrue(connection.IsOpen);
        var queueDeclareOk = channel.QueueDeclarePassive(queue_non_exclusive);
        Assert.AreEqual(1L, queueDeclareOk.MessageCount);

        await Task.Delay(100);

        var result = channel.BasicGet(queue_non_exclusive, autoAck: true);
        Assert.IsNotNull(result);
        Assert.AreEqual(message, Encoding.UTF8.GetString(result.Body.ToArray()));
      }
    } finally {
      delete_queue(queue_non_exclusive);
    }

    var queue_exclusive = $"queue-exclusive-{Nanoid.Generate(size: 5)}";

    var factory2 = new ConnectionFactory() { HostName = "localhost" };
    using (var connection = factory2.CreateConnection())
    using (var channel = connection.CreateModel()) {
      Assert.IsTrue(connection.IsOpen);
      channel.QueueDeclare(queue: queue_exclusive, durable: true,
      exclusive: true, // EXCLUSIVE
      autoDelete: false, arguments: null);

      channel.BasicPublish(exchange: "", routingKey: queue_exclusive, basicProperties: null, body: Encoding.UTF8.GetBytes(message));
      await Task.Delay(200);
      Assert.AreEqual(1L, channel.MessageCount(queue_exclusive));
    }

    print("Due to being exclusive the queue should be deleted after the connection is closed");

    using (var connection = factory2.CreateConnection())
    using (var channel = connection.CreateModel()) {
      Assert.IsTrue(connection.IsOpen);
      try {
        var queueDeclareOk = channel.QueueDeclarePassive(queue_exclusive);
        Assert.Fail("The queue should be deleted and not exist.");
      } catch (OperationInterruptedException ex) {
        print($"Queue with name {queue_exclusive} does not exist");
        // Assert. Contains("NOT_FOUND - no queue", ex.Message);
        StringAssert.Contains(ex.Message, "NOT_FOUND - no queue");
      }
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public async Task test12_MessagePriorityQueue() {
    /*
    Verify that messages with higher priority are consumed before messages with lower priority,
    even if they were enqueued later.
    */

    // Setup the main queue with priority support
    var queue_name = $"priority-queue-{Nanoid.Generate(size: 5)}";

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        var arguments = new Dictionary<string, object>
        {
          { "x-max-priority", 10 } // Enable priority for the queue
        };

        channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
        Assert.AreEqual(0L, channel.MessageCount(queue_name));

        // Publish messages with different priorities
        var messageProperties = channel.CreateBasicProperties();

        // Message with priority 1
        messageProperties.Priority = 1;
        channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: messageProperties, body: Encoding.UTF8.GetBytes("Message 1"));

        // Message with priority 5
        messageProperties.Priority = 5;
        channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: messageProperties, body: Encoding.UTF8.GetBytes("Message 2"));

        // Message with priority 1
        messageProperties.Priority = 1;
        channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: messageProperties, body: Encoding.UTF8.GetBytes("Message 3"));

        // Message with priority 5
        messageProperties.Priority = 5;
        channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: messageProperties, body: Encoding.UTF8.GetBytes("Message 4"));

        // Message with priority 1
        messageProperties.Priority = 1;
        channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: messageProperties, body: Encoding.UTF8.GetBytes("Message 5"));

        await Task.Delay(200);
        Assert.AreEqual(5L, channel.MessageCount(queue_name));

        // Set up the consumer to verify the order of message consumption
        var consumer = new EventingBasicConsumer(channel);
        var consumedMessages = new List<string>();
        consumer.Received += (model, ea) => {
          Thread.Sleep(300); // Simulate message processing
          var message = Encoding.UTF8.GetString(ea.Body.ToArray());
          consumedMessages.Add(message);
          channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);

        // Allow some time for the consumer to process the messages
        await Task.Delay(3000);

        // Verify the consumption order based on priority
        var expectedOrder = new List<string> { "Message 2", "Message 4", "Message 1", "Message 3", "Message 5" };
        CollectionAssert.AreEqual(expectedOrder, consumedMessages);
      }
    } finally {
      delete_queue(queue_name);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public async Task test11_MessageTTLToDeadLetterQueue() {
    /*
    Message Expiry due to TTL:
    When a message exceeds its TTL (Time-To-Live), it will be moved to the dead-letter exchange (DLX) if configured.
    */

    var dlx_exchange = "dlx_exchange";
    var dlx_queue = $"dlx_queue-{Nanoid.Generate(size: 5)}";
    var dlx_routing_key = "dead-letter";

    RabbitMqHelper.SetupDeadLetterExchangeAndQueue(exchange_name: dlx_exchange, dead_letter_queue: dlx_queue, routing_key: dlx_routing_key);
    await Task.Delay(200);

    // Main queue
    var queue_name = $"queue-{Nanoid.Generate(size: 5)}";
    var ttl = 3000; //msec

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        var arguments = new Dictionary<string, object>
        {
          { "x-dead-letter-exchange", dlx_exchange },
          { "x-dead-letter-routing-key", dlx_routing_key },
          { "x-message-ttl", ttl } // Message TTL of 5 seconds
        };

        channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
        Assert.AreEqual(0L, channel.MessageCount(queue_name));

        for (int ii = 1; ii <= 5; ii++) {
          string message = $"Expiring Message {ii}";
          var body = Encoding.UTF8.GetBytes(message);
          channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: null, body: body);
          print($"Published message {ii}");
        }

        await Task.Delay(200);
        Assert.AreEqual(5L, channel.MessageCount(queue_name));

        // Wait for TTL to expire
        await Task.Delay(ttl + 1000); // Wait longer than the TTL to ensure messages expire

        // Verify messages are moved to the dead-letter queue
        Assert.AreEqual(5L, channel.MessageCount(dlx_queue));
      }
    } finally {
      delete_queue(queue_name);
      delete_queue(dlx_queue);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public async Task test10_rejecting_messages_to_dead_letter_queue() {
    /*
    Message Rejection without Requeue:
    When a consumer receives a message and rejects it (using BasicReject or BasicNack) with the requeue parameter set to false,
      the message will be moved to the dead-letter exchange (DLX) if configured.
    This can happen due to a processing error or a specific business rule that determines the message cannot be processed successfully.
    */

    var dlx_exchange = "dlx_exchange";
    var dlx_queue = $"dlx_queue-{Nanoid.Generate(size: 5)}";
    var dlx_routing_key = "dead-letter";

    RabbitMqHelper.SetupDeadLetterExchangeAndQueue(exchange_name: dlx_exchange, dead_letter_queue: dlx_queue, routing_key: "dead-letter");
    await Task.Delay(200);

    //main queue
    var queue_name = $"queue-{Nanoid.Generate(size: 5)}";

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        var arguments = new Dictionary<string, object>
        {
          { "x-dead-letter-exchange", dlx_exchange },
          { "x-dead-letter-routing-key", dlx_routing_key },
          { "x-max-length", 10 } // optional: limit queue length
        };

        channel.QueueDeclare(queue: queue_name, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
        Assert.AreEqual(0L, channel.MessageCount(queue_name));

        for (int ii = 1; ii <= 5; ii++) {
          string message = $"Doomed Message {ii}";
          var body = Encoding.UTF8.GetBytes(message);
          channel.BasicPublish(exchange: "", routingKey: queue_name, basicProperties: null, body: body);
          print($"Published message {ii}");
        }

        await Task.Delay(200);
        Assert.AreEqual(5L, channel.MessageCount(queue_name));

        print("Setting up the consumer to reject every other message");
        var consumer = new EventingBasicConsumer(channel);
        int messageCount = 0;
        consumer.Received += (model, ea) => {
          Thread.Sleep(500); // Simulate message processing

          messageCount++;
          if (messageCount % 2 == 0) {
            // Reject the message without requeueing
            print($"Rejecting message {messageCount}");
            channel.BasicReject(ea.DeliveryTag, requeue: false);
          } else {
            // Acknowledge the message
            print($"Acknowledging message {messageCount}");
            channel.BasicAck(ea.DeliveryTag, multiple: false);
          }
        };
        string consumerTag = channel.BasicConsume(queue: queue_name, autoAck: false, consumer: consumer);
        print("Consumer setup complete with consumer tag " + consumerTag);

        var result = await WaitForConditionAsync(() => {
          return messageCount >= 5;
        }, timeout: TimeSpan.FromSeconds(10));
        Assert.IsTrue(result, "Expected 5 messages to be consumed within 10 seconds");

        await Task.Delay(200);

        Assert.AreEqual(2L, channel.MessageCount(dlx_queue));
      }
    } finally {
      delete_queue(queue_name);
      delete_queue(dlx_queue);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public async Task test09_MessagePersistenceAfterServerRestart() {
    var QueueName = $"queue-{Nanoid.Generate(size: 5)}";

    try {

      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        // Step 1: Declare a durable queue
        channel.QueueDeclare(queue: QueueName,
        durable: true,
        exclusive: false, autoDelete: false, arguments: null);

        // Ensure queue is empty
        // channel.QueuePurge(QueueName);
        Assert.AreEqual(0L, channel.MessageCount(QueueName));

        // Step 2: Publish durable messages
        for (int ii = 1; ii <= 5; ii++) {
          string message = $"Durable Message {ii}";
          var body = Encoding.UTF8.GetBytes(message);
          var properties = channel.CreateBasicProperties();
          properties.Persistent = true; // Mark message as persistent
          channel.BasicPublish(exchange: "", routingKey: QueueName, basicProperties: properties, body: body);
        }

        await Task.Delay(300);

        // Verify 5 messages are in the queue
        // channel.QueueDeclarePassive(QueueName).MessageCount;
        Assert.AreEqual(5L, channel.MessageCount(QueueName));
      }

      // Step 3: Stop RabbitMQ Server
      await TerminalHelper.RunCommand("brew services stop rabbitmq");
      // Verify RabbitMQ has stopped
      var status = await TerminalHelper.RunCommand("brew services list | grep rabbitmq | awk '{print $2}'");
      Assert.AreEqual("none", status.Trim());

      await Task.Delay(1000);

      // Step 4: Start RabbitMQ Server
      await TerminalHelper.RunCommand("brew services start rabbitmq");
      // Verify RabbitMQ has started
      status = await TerminalHelper.RunCommand("brew services list | grep rabbitmq | awk '{print $2}'");
      Assert.AreEqual("started", status.Trim());

      await Task.Delay(5000); //yes we need a large delay here to allow the rabbitmq service to really initialize

      // Step 5: Verify messages persist after restart
      //using the same factory is ok but nevertheless we will use a diffent one here
      var factory2 = new ConnectionFactory() { HostName = "localhost" };
      using (var connection2 = factory2.CreateConnection())
      using (var channel2 = connection2.CreateModel()) {
        //channel.QueueDeclarePassive(QueueName).MessageCount;
        Assert.AreEqual(5L, channel2.MessageCount(QueueName));

        // Clean up the queue by consuming the messages
        for (int i = 1; i <= 5; i++) {
          var result = channel2.BasicGet(QueueName, autoAck: true);
          Assert.IsNotNull(result);
          Assert.AreEqual($"Durable Message {i}", Encoding.UTF8.GetString(result.Body.ToArray()));
        }

        await Task.Delay(500);

        // Verify the queue is empty
        Assert.AreEqual(0L, channel2.MessageCount(QueueName));
      }
    } finally {
      delete_queue(QueueName);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public async Task test08_acknowledgement() {
    var queue_name = $"queue-{Nanoid.Generate(size: 5)}";

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        var qq = channel.QueueDeclare(queue: queue_name,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        Assert.AreEqual(0L, qq.MessageCount);

        string original_message = $"Hello Team. This is a test message: {Nanoid.Generate(size: 10)}";
        var body = Encoding.UTF8.GetBytes(original_message);

        channel.BasicPublish(exchange: "",
                            routingKey: queue_name,
                            basicProperties: null,
                            body: body);

        // Allow time for message to be processed
        await Task.Delay(200);

        // Verify message count is 1
        var queueDeclareOk = channel.QueueDeclarePassive(queue_name);
        Assert.AreEqual(1L, queueDeclareOk.MessageCount);

        // Consume the message without acknowledging
        var result = channel.BasicGet(queue_name, autoAck: false);
        await Task.Delay(200);

        Assert.IsNotNull(result); // Ensure a message was fetched
        var messageBody = Encoding.UTF8.GetString(result.Body.ToArray());
        Assert.AreEqual(original_message, messageBody);  // Confirm the message content

        // NOTE TWO THINGS:
        // 1. The message is consumed and you cannot peek the queue without consuming the message. In other words you cannot BasicGet the same message again.
        // 2. The MessageCount gives the number of messages in the queue that are ready to be consumed. Regardless whether they were acknowledged or not

        // Acknowledge the message
        channel.BasicAck(result.DeliveryTag, false);
        await Task.Delay(200);

        // Verify message count is now 0
        Assert.AreEqual(0L, channel.QueueDeclarePassive(queue_name).MessageCount);
      }
    } finally {
      delete_queue(queue_name);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public void test07_purge_queue() {
    var queueName = $"queue-{Nanoid.Generate(size: 5)}";
    long messagesTotal = 10;

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        // Declare the queue
        channel.QueueDeclare(queue: queueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        // Publish messages
        for (int i = 0; i < messagesTotal; i++) {
          string message = $"Message {i + 1}";
          var body = Encoding.UTF8.GetBytes(message);
          channel.BasicPublish(exchange: "",
                               routingKey: queueName,
                               basicProperties: null,
                               body: body);
        }

        Thread.Sleep(200);

        // Assert message count
        var queueDeclareOk = channel.QueueDeclarePassive(queueName);
        Assert.AreEqual(messagesTotal, queueDeclareOk.MessageCount);

        Thread.Sleep(200);

        // Purge the queue
        channel.QueuePurge(queueName);

        Thread.Sleep(200);

        // Assert message count is zero after purge
        queueDeclareOk = channel.QueueDeclarePassive(queueName);
        Assert.AreEqual(0L, queueDeclareOk.MessageCount);
      }
    } finally {
      delete_queue(queueName);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public void test06_stop_consuming_and_republishing() {
    var queueName = $"queue-{Nanoid.Generate(size: 5)}";

    try {
      // Setup RabbitMQ connection and channel
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using var connection = factory.CreateConnection();
      using var channel = connection.CreateModel();

      channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

      // Publish several messages to the queue
      for (int ii = 0; ii < 5; ii++) {
        var message = $"Test message {ii}";
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
      }

      // Allow some time for messages to be processed by the broker
      Thread.Sleep(100);

      var initialMessageCount = channel.QueueDeclarePassive(queueName).MessageCount;
      Console.WriteLine("Initial messages in Queue: " + initialMessageCount);

      // Set up the consumer
      var consumer = new EventingBasicConsumer(channel);
      string consumerTag = channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

      // Counter to keep track of consumed messages
      int messagesConsumed = 0;

      consumer.Received += (model, ea) => {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        messagesConsumed++;

        // Simulate work
        Console.WriteLine($"Processing message {messagesConsumed}: {message}");
        Thread.Sleep(300);  // simulate message processing delay

        // Acknowledge the message
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        Console.WriteLine($"Acknowledged message {messagesConsumed}");
      };

      print("Allow some time for message processing with consumer being active");
      Thread.Sleep(4000);

      // Cancel the consumer
      //This can cancel ONLY the message that are published from now onwards, not any messages that were published in the past
      channel.BasicCancel(consumerTag);
      Console.WriteLine("Consumer cancelled.");

      // Allow some time for cancellation to complete
      Thread.Sleep(1000);

      print("Sending new messages after cancellation");

      for (int jj = 5; jj < 10; jj++) {
        var message = $"Test message {jj}";
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
      }
      print("Allow some time for message processing with consumer being cancelled");
      Thread.Sleep(3000);

      // Check the number of remaining messages in the queue after cancellation
      var remainingMessages = channel.QueueDeclarePassive(queueName).MessageCount;
      var channelMessages = channel.MessageCount(queueName);

      // Assert that not all messages were consumed
      Console.WriteLine("Messages consumed: " + messagesConsumed);
      Console.WriteLine("Remaining messages in Queue: " + remainingMessages);

      Assert.IsTrue(messagesConsumed == 5, $"Messages consumed is currently: {messagesConsumed}");
      Assert.IsTrue(remainingMessages == 5, $"Remaining messages is currently: {remainingMessages}");
      Assert.AreEqual(remainingMessages, channelMessages);

    } finally {
      // Clean up
      delete_queue(queueName);
    }
  }

  [DataTestMethod]
  [DataRow(3, 500, 4)]
  [DataRow(10, 200, 3)]
  [TestCategory("rabbitmq")]
  public async Task test05_assert_fifo_logic(int messages_total, int avg_processing_time, int max_wait_time) {
    var uniqueQueueName = $"queue-{Nanoid.Generate(size: 5)}";

    // var messages_total = 3;
    // var avg_processing_time = 500; //ms
    // var max_wait_time = 4; //seconds

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        var arguments = new Dictionary<string, object> {
          { "x-max-length", 10 }
        };

        // Declare a queue with a maximum of 5 messages
        channel.QueueDeclare(queue: uniqueQueueName,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: arguments);

        var fifo_counter = 1;

        // Create a consumer and attach it to the queue
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) => {
          var body = ea.Body.ToArray();
          var message = Encoding.UTF8.GetString(body);

          int extractedNumber = int.Parse(System.Text.RegularExpressions.Regex.Match(message, @"\d+").Value);

          Console.WriteLine($"Received {message} with number {extractedNumber}");

          // Simulate work
          Thread.Sleep(avg_processing_time);

          // Acknowledge the message
          channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

          if (fifo_counter == extractedNumber) {
            fifo_counter += 1;
          }
        };

        // basically it starts the listener
        channel.BasicConsume(queue: uniqueQueueName,
                            autoAck: false,  // Manually acknowledge messages
                            consumer: consumer);

        // Publish messages
        for (int ii = 0; ii < messages_total; ii++) {
          string message = $"Message {ii + 1}";
          var body = Encoding.UTF8.GetBytes(message);
          channel.BasicPublish(exchange: "",
                              routingKey: uniqueQueueName,
                              basicProperties: null,
                              body: body);
          Console.WriteLine(" [x] Sent {0}", message);
        }

        // Keep the application running to listen for messages
        // Console.WriteLine(" Press [enter] to exit.");
        // Console.ReadLine();

        var result = await WaitForConditionAsync(() => {
          return fifo_counter >= messages_total;
        }, timeout: TimeSpan.FromSeconds(max_wait_time));

        Assert.IsTrue(result, $"Expected {messages_total} messages to be consumed from the queue within less than 5 seconds");

        Thread.Sleep(200); //wait a bit for queue to process things

        var queueDeclareOk = channel.QueueDeclarePassive(uniqueQueueName);
        Assert.AreEqual(0L, queueDeclareOk.MessageCount);
        // all messages should have been consumed by now
      }
    } finally {
      delete_queue(uniqueQueueName);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public void test04_insert_six_items_to_a_queue_of_max_five() {
    var uniqueQueueName = $"queue-{Nanoid.Generate(size: 5)}";

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        // Declare a queue with a maximum of 5 items
        var arguments = new Dictionary<string, object>
        {
          { "x-max-length", 5 }
        };

        var qq = channel.QueueDeclare(queue: uniqueQueueName,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: arguments);

        Assert.AreEqual(0L, qq.MessageCount);

        // Create and publish 5 messages
        for (int ii = 0; ii < 5; ii++) {
          string original_message = $"Hello Team. This is test message {ii + 1}: {Nanoid.Generate(size: 10)}";
          var body = Encoding.UTF8.GetBytes(original_message);

          channel.BasicPublish(exchange: "",
                              routingKey: uniqueQueueName,
                              basicProperties: null,
                              body: body);
        }

        // Allow some time for messages to be processed by the broker
        Thread.Sleep(100);

        // Check the message count after publishing 5 messages
        var queueDeclareOk = channel.QueueDeclarePassive(uniqueQueueName);
        Assert.AreEqual(5L, queueDeclareOk.MessageCount);


        //Inserting a 6th item
        string sixth_message = $"Hello Team. This is test message 6: {Nanoid.Generate(size: 10)}";
        var sixth_body = Encoding.UTF8.GetBytes(sixth_message);
        channel.BasicPublish(exchange: "",
                            routingKey: uniqueQueueName,
                            basicProperties: null,
                            body: sixth_body);

        // Allow some time for messages to be processed by the broker
        Thread.Sleep(200);

        // Check the message count after pushing the 6th message is still 5
        Assert.AreEqual(5L, channel.QueueDeclarePassive(uniqueQueueName).MessageCount);

        // Verify that the older message was discarded and the last 5 messages are present
        for (int ii = 1; ii < 6; ii++) {
          var result = channel.BasicGet(uniqueQueueName, autoAck: false);
          Assert.IsNotNull(result);  // Confirm a message was fetched

          var message_body = Encoding.UTF8.GetString(result.Body.ToArray());
          // Assert. StartsWith($"Hello Team. This is test message {ii + 1}", message_body);
          StringAssert.StartsWith(message_body, $"Hello Team. This is test message {ii + 1}");
        }
      }
    } finally {
      delete_queue(uniqueQueueName);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public void test03_insert_five_items() {
    var uniqueQueueName = $"queue-{Nanoid.Generate(size: 5)}";

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        // Declare a queue with a maximum of 5 items
        var arguments = new Dictionary<string, object>
        {
          { "x-max-length", 5 }
        };

        var qq = channel.QueueDeclare(queue: uniqueQueueName,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: arguments);

        Assert.AreEqual(0L, qq.MessageCount);

        // Create and publish 5 messages
        for (int ii = 0; ii < 5; ii++) {
          string original_message = $"Hello Team. This is test message {ii + 1}: {Nanoid.Generate(size: 10)}";
          var body = Encoding.UTF8.GetBytes(original_message);

          channel.BasicPublish(exchange: "",
                              routingKey: uniqueQueueName,
                              basicProperties: null,
                              body: body);
        }

        // Allow some time for messages to be processed by the broker
        Thread.Sleep(100);

        // Check the message count after publishing 5 messages
        var queueDeclareOk = channel.QueueDeclarePassive(uniqueQueueName);
        Assert.AreEqual(5L, queueDeclareOk.MessageCount);

        // Retrieve and verify the messages
        for (int ii = 0; ii < 5; ii++) {
          var result = channel.BasicGet(uniqueQueueName, autoAck: false);
          Assert.IsNotNull(result);  // Confirm a message was fetched

          var message_body = Encoding.UTF8.GetString(result.Body.ToArray());
          // Assert. StartsWith($"Hello Team. This is test message {ii + 1}", message_body);
          StringAssert.StartsWith(message_body, $"Hello Team. This is test message {ii + 1}");
        }
      }
    } finally {
      delete_queue(uniqueQueueName);
    }
  }

  [TestMethod]
  [TestCategory("rabbitmq")]
  public void test02_insert_one_item() {
    var uniqueQueueName = $"queue-{Nanoid.Generate(size: 5)}";

    try {
      var factory = new ConnectionFactory() { HostName = "localhost" };
      using (var connection = factory.CreateConnection())
      using (var channel = connection.CreateModel()) {
        var qq = channel.QueueDeclare(queue: uniqueQueueName,
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        Assert.AreEqual(0L, qq.MessageCount);

        string original_message = $"Hello Team. This is a test message: {Nanoid.Generate(size: 10)}";
        var body = Encoding.UTF8.GetBytes(original_message);

        channel.BasicPublish(exchange: "",
                            routingKey: uniqueQueueName,
                            basicProperties: null,
                            body: body);


        //this did NOT work as it is explained. It seems as if the message is consumed by the channel.BasicGet
        //autoAck: false: The message is retrieved and stays in the queue until you explicitly acknowledge it using channel.BasicAck.
        //This allows you to confirm the message processing only after certain conditions are met, ensuring the message is not lost if the consumer fails before processing it.
        // var result = channel.BasicGet(uniqueQueueName, autoAck: false);
        // Assert.IsNotNull(result);  // Confirm a message was fetched
        // print(result.GetType());

        //necessary step otherwise race condition
        Thread.Sleep(100);

        var queueDeclareOk = channel.QueueDeclarePassive(uniqueQueueName);
        Assert.AreEqual(1L, queueDeclareOk.MessageCount);

        var result = channel.BasicGet(uniqueQueueName, autoAck: false);
        var message_body = Encoding.UTF8.GetString(result.Body.ToArray());
        // var message_body = Encoding.UTF8.GetString(result.Body.Span); //this works also
        Assert.AreEqual(original_message, message_body);  // Confirm a message was fetched 
      }
    } finally {
      delete_queue(uniqueQueueName);
    }
  }

  // To cross-check the size of the queue from terminal: rabbitmqctl list_queues name messages
  [TestMethod]
  [TestCategory("rabbitmq")]
  public void test01_initially_queue_empty() {
    var uniqueQueueName = $"queue-{Nanoid.Generate(size: 5)}";

    var factory = new ConnectionFactory() { HostName = "localhost" };

    using (var connection = factory.CreateConnection())
    using (var channel = connection.CreateModel()) {
      var qq = channel.QueueDeclare(queue: uniqueQueueName,
                           durable: false,
                           exclusive: false,
                           autoDelete: false,
                           arguments: null);
      try {
        // Declare a queue in a way that it is not created if it does not exist
        var result = channel.QueueDeclarePassive(uniqueQueueName);

        Assert.AreEqual(qq.GetType(), result.GetType()); //RabbitMQ.Client.QueueDeclareOk

        Assert.AreEqual(0L, result.MessageCount);

        // Assert.AreEqual(0, result.MessageCount);
        // print($"Queue 'hello' has {result.MessageCount} messages.");
      } catch (OperationInterruptedException ex) {
        Assert.Fail($"Queue with name {uniqueQueueName} does not exist: {ex.Message}");
      }
    }

    delete_queue(uniqueQueueName);
  }

  [TestMethod]
  // [Fact(Timeout = 15000)]
  [TestCategory("rabbitmq")]
  public void test00_smoke_check_rabbitmq_server() {
    var queueName = "hello";

    // Create a connection to the RabbitMQ server
    var factory = new ConnectionFactory() { HostName = "localhost" };

    using (var connection = factory.CreateConnection())
    using (var channel = connection.CreateModel()) {
      /*
      1. **`queue`**: Name of the queue to declare or use.
      2. **`durable`**: Determines if the queue survives a broker restart (true for persistence).
      3. **`exclusive`**: Restricts queue access to the declaring connection; deleted on connection closure.
      4. **`autoDelete`**: Automatically deletes the queue when the last consumer unsubscribes.
      5. **`arguments`**: Additional settings for queue configuration, such as TTL or max length.
      */
      // Declare a queue
      channel.QueueDeclare(queue: queueName,
                           durable: false,
                           exclusive: false,
                           autoDelete: false,
                           arguments: null);
    }

    delete_queue(queueName);
  }

  /*
  # Delete all queues
  for queue in $(rabbitmqctl list_queues -q name); do
    rabbitmqctl delete_queue $queue
  done
  */
  //* Delete a queue manually: rabbitmqctl delete_queue queue_name
  private void delete_queue(string queue_name) {
    var factory = new ConnectionFactory() { HostName = "localhost" };
    using (var connection = factory.CreateConnection())
    using (var channel = connection.CreateModel()) {
      uint num = channel.QueueDelete(queue: queue_name);
      Assert.IsTrue(num >= 0);
      // Console.WriteLine($"Queue {queue_name} deleted. and return value is: {num}");
    }
  }

  /*
    // Destructor
    ~HelloPlaywrightTest()
    {
      // Cleanup code

    }
    */
}