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
        private static readonly ActivitySource _activitySource = new ActivitySource("KyleTestActivitySource");
        //private static readonly ActivitySource _activitySource = new ActivitySource(nameof(TodoItemsController));
        //private readonly ActivitySource _activitySource;

        public TodoItemsController(TodoContext context, ILogger<TodoItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            _logger.LogInformation("**************************************");
            _logger.LogInformation("Begin logging existing Activity Props:");
            _logger.LogInformation("Activity.Current.TraceId = " + Activity.Current.TraceId);
            _logger.LogInformation("Activity.Current.SpanId = " + Activity.Current.SpanId);
            _logger.LogInformation("Activity.Current.ParentId = " + Activity.Current.ParentId);
            _logger.LogInformation("Activity.Current.TraceStateString = " + Activity.Current.TraceStateString);
            _logger.LogInformation("**** Done Logging existing Activity Props.");

            Activity a = new Activity("ExampleActivityInTodoItemsController");
            a.Start();
            _logger.LogInformation("*********************************");
            _logger.LogInformation("Begin logging new Activity Props:");
            _logger.LogInformation("Activity.Current.TraceId = " + Activity.Current.TraceId);
            _logger.LogInformation("Activity.Current.SpanId = " + Activity.Current.SpanId);
            _logger.LogInformation("Activity.Current.ParentId = " + Activity.Current.ParentId);
            _logger.LogInformation("Activity.Current.TraceStateString = " + Activity.Current.TraceStateString);
            _logger.LogInformation("**** Done Logging new Activity Props.");
            Task.Delay(2000).Wait();
            a.Stop();
            
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
                
            using (var activity = _activitySource.StartActivity("KyleActivityTest", ActivityKind.Server))
            {
                _logger.LogInformation("Trying to manually start Child Span using ActivitySource.");
                if (activity?.IsAllDataRequested ?? false)
                {
                    _logger.LogInformation("Adding Tags and Events to Activity.");
                    activity?.AddTag("label1", "Is it working?");
                    activity?.AddTag("label2", "Are you sure?");
                    activity?.AddEvent(new ActivityEvent("event, equivalent of a log entry."));

                    _logger.LogInformation("*********************************");
                    _logger.LogInformation("Begin logging new Activity Props:");
                    _logger.LogInformation("Activity.Current.TraceId = " + Activity.Current.TraceId);
                    _logger.LogInformation("Activity.Current.SpanId = " + Activity.Current.SpanId);
                    _logger.LogInformation("Activity.Current.ParentId = " + Activity.Current.ParentId);
                    _logger.LogInformation("Activity.Current.TraceStateString = " + Activity.Current.TraceStateString);
                    _logger.LogInformation("**** Done Logging new Activity Props.");
                    Task.Delay(2000).Wait();
                }
                _logger.LogInformation("Ok, Activity created and properties logged, will this make it to Jaeger?.");
            } // Activity gets stopped automatically at end of this block during dispose.

            return await _context.TodoItems.ToListAsync();
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
