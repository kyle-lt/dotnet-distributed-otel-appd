using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly ILogger<TodoItemsController> _logger;
        private readonly TodoContext _context;
        //private readonly MessageReceiver _messageReceiver;
        
        // Create ActivitySource to capture my manual Spans - this ActivitySource is Added to the OpenTelemetry
        // Service declaration in Startup.cs
        //private static readonly ActivitySource _activitySource = new ActivitySource("ManualActivitySource");
        //private static readonly ActivitySource _activitySource = new ActivitySource(nameof(TodoItemsController));

        public TodoItemsController(TodoContext context, ILogger<TodoItemsController> logger)
        {
            _context = context;
            _logger = logger;

        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            return await _context.TodoItems.ToListAsync();

            /* DEBUGGING
            // Create Child Span - it will automatically detect Activity.Current as its parent
            using (var activity = _activitySource.StartActivity("ChildActivityTest", ActivityKind.Server))
            {
                if (activity?.IsAllDataRequested ?? false)
                {
                    // Adding Tags and Events to new Child Activity
                    activity?.AddTag("child.tag.1", "Is it working?");
                    activity?.AddTag("child.tag.2", "Yes");
                    activity?.AddEvent(new ActivityEvent("This is the event body - kinda equivalent to a log entry."));

                    // Debug Logging
                    
                    _logger.LogInformation("----- Begin logging new Activity Props -----");
                    _logger.LogInformation($"Activity.Current.TraceId = {Activity.Current.TraceId}");
                    _logger.LogInformation($"Activity.Current.SpanId = {Activity.Current.SpanId}");
                    _logger.LogInformation($"Activity.Current.ParentId = {Activity.Current.ParentId}");
                    _logger.LogInformation("----- Done Logging new Activity Props -----");
                    
                    // Simulate Work Being Done
                    Task.Delay(2000).Wait();
                }
            } // Activity gets stopped automatically at end of this block during dispose.
            */

            // -- Moved to the bottom, after return since just comments --
            // Manually create Trace Provider using SDK - don't need this since I'm using the
            // Dependency Injection method in Startup.cs, but good to know anyway...
            // Note, the syntax for this may be changing in the future to something more like
            /*
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource("MyCompany.MyProduct.MyLibrary")
                .AddConsoleExporter()
                .Build();
            */
            /*
            using var tracerProvider = Sdk.CreateTracerProvider(builder => builder
                .AddActivitySource("KyleTestActivitySource")
                .UseConsoleExporter()
                .UseJaegerExporter(jaeger =>
                {
                    jaeger.ServiceName = "dotnet-distrubuted-otel-appd.TodoApi";
                    jaeger.AgentHost = "host.docker.internal";
                    jaeger.AgentPort = 6831;
                })
            );
            */            
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return NotFound();
            }

            return todoItem;
        }

        // PUT: api/TodoItems/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodoItem(long id, TodoItem todoItem)
        {
            if (id != todoItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(todoItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TodoItems
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            //return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<TodoItem>> DeleteTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return todoItem;
        }

        private bool TodoItemExists(long id)
        {
            return _context.TodoItems.Any(e => e.Id == id);
        }
    }
}
