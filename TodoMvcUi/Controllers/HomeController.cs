using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TodoMvcUi.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;

// RabbitMQ
using RabbitMQ.Client;

// Messaging Utils
using Utils.Messaging;

namespace TodoMvcUi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly MessageSender _messageSender;

        private static readonly HttpClient client = new HttpClient();

        // Create ActivitySource to capture my manual Spans - this ActivitySource is Added to the OpenTelemetry
        // Service declaration in Startup.cs
        //private static readonly ActivitySource _activitySource = new ActivitySource("RabbitMQ");

        public HomeController(ILogger<HomeController> logger, MessageSender messageSender)
        {
            _logger = logger;
            _messageSender = messageSender;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> ToDo()
        {

            // Grab the ToDoItems from the API and write JSON to Console Log as it comes (minified, and all lower-case)
            var stringTask = client.GetStringAsync("http://host.docker.internal:5000/api/TodoItems");
            var msg = await stringTask;
            _logger.LogDebug("Incoming JSON from API: " + msg);

            // Deserialize the json string into POCO object
            // These options are important!
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Match json since my API is sending all lower-case
                WriteIndented = true // For Debugging, printing will print this nicely when serialized back into JSON
            };
            
            // Deserialize my json into List of TodoItems
            List<TodoItem> todoItems = JsonSerializer.Deserialize<List<TodoItem>>(msg, options);
            _logger.LogDebug("Deserialized into TodoItem List Successfully!");
            
            // Reserialize my List of TodoItems into json (to pretty-print, and to pass to View)
            var modelJson = JsonSerializer.Serialize(todoItems, options);
            _logger.LogDebug("Re-serialized Pretty-Print JSON: " + modelJson);
            
            // Return View with TodoItems
            ViewData["TodoItems"] = modelJson;
            return View(todoItems);
        }

        public async Task<IActionResult> ToDoPost(string name, bool isComplete)
        {
            _logger.LogDebug($"Name from Form Post = {name}");
            _logger.LogDebug($"IsComplete from Form Post = {isComplete}");

            // POST new TodoItem to API
            var todoItemDTO = new TodoItemDTO();
            todoItemDTO.Name = name;
            todoItemDTO.IsComplete = isComplete;
            var jsonTodoItem = JsonSerializer.Serialize(todoItemDTO);
            _logger.LogDebug($"Serialized JSON for API POST = {jsonTodoItem}");
            var httpContent = new StringContent(jsonTodoItem, Encoding.UTF8, "application/json");
            var postResponse = client.PostAsync("http://host.docker.internal:5000/api/TodoItems", httpContent).Result;
            _logger.LogDebug($"postResponse = {postResponse.Content.ReadAsStringAsync().Result}");

            // Grab the latest list of TodoItems - I don't need this anymore b/c of my redirect, but keep it, who cares?!? :)
            var stringTask = client.GetStringAsync("http://host.docker.internal:5000/api/TodoItems");
            var msg = await stringTask;
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Match json since my API is sending all lower-case
                WriteIndented = true // For Debugging, printing will print this nicely when serialized back into JSON
            };
            List<TodoItem> todoItems = JsonSerializer.Deserialize<List<TodoItem>>(msg, options);
            var modelJson = JsonSerializer.Serialize(todoItems, options);

            // Send Message using RabbitMQ
            _messageSender.SendMessage();
            
            // OLD - this is all wrapped in the Utils.Messaging.MessageSender Class
            // Send a message to RabbitMQ, and wrap it in a Span
            // Create Child Span - it will automatically detect Activity.Current as its parent
            /*
            using (var activity = _activitySource.StartActivity("RabbitMQ Publish", ActivityKind.Producer))
            {
                if (activity?.IsAllDataRequested ?? false)
                {
                    // Adding Tags and Events to new Child Activity
                    activity?.AddTag("rabbit.producer.tag.1", "Is it working?");
                    activity?.AddTag("rabbit.producer.tag.2", "Yes");
                    activity?.AddEvent(new ActivityEvent("This is the event body - kinda equivalent to a log entry."));

                    // Debug Logging
                    
                    _logger.LogInformation("----- Begin logging new Activity Props -----");
                    _logger.LogInformation($"Activity.Current.TraceId = {Activity.Current.TraceId}");
                    _logger.LogInformation($"Activity.Current.SpanId = {Activity.Current.SpanId}");
                    _logger.LogInformation($"Activity.Current.ParentId = {Activity.Current.ParentId}");
                    _logger.LogInformation("----- Done Logging new Activity Props -----");
                    

                    // Do Work
                    sendRabbitMqMsg();
                }
            }
            */
            
            ViewData["TodoItems"] = modelJson;
            return LocalRedirect("/Home/ToDo");
        }

        // Not used, but interesting...
        private static T GetJsonGenericType<T>(string jsonString)
        {
            var generatedType = JsonSerializer.Deserialize<T>(jsonString);
            return (T)Convert.ChangeType(generatedType, typeof(T));
        }

        private async Task<String> getAllTodoItemsAsyc()
        {
            // Grab the ToDoItems from the API and write JSON to Console Log as it comes (minified, and all lower-case)
            var stringTask = client.GetStringAsync("http://host.docker.internal:5000/api/TodoItems");
            var msg = await stringTask;

            _logger.LogDebug("Incoming JSON from API: " + msg);

            return msg;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // OLD - this is all wrapped in the Utils.Messaging.MessageSender Class
        /*
        private void sendRabbitMqMsg()
        {
            // Create RabbitMQ connection to docker-compose service "rabbitmq"
            var factory = new ConnectionFactory() { HostName = "rabbitmq" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);

                // Create traceparent header
                IBasicProperties props = channel.CreateBasicProperties();
                props.Headers = new Dictionary<string, object>();
                props.Headers.Add("traceparent", Activity.Current.Id);

                channel.BasicPublish(exchange: "",
                                    routingKey: "hello",
                                    basicProperties: props,
                                    body: body);
                
            }
        }
        */
    }
}
