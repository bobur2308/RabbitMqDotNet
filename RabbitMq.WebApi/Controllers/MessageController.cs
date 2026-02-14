using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMq.WebApi.Data;
using RabbitMq.WebApi.DTOs;
using RabbitMq.WebApi.Services;

namespace RabbitMq.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MessageController> _logger;
    
    public MessageController(
        RabbitMqPublisher publisher, 
        AppDbContext dbContext,
        ILogger<MessageController> logger)
    {
        _publisher = publisher;
        _dbContext = dbContext;
        _logger = logger;
    }
    
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            await _publisher.PublishMessageAsync(request.Message);
            _logger.LogInformation("Message published via API: {Message}", request.Message);
            
            return Ok(new 
            { 
                status = "success",
                message = "Message sent to RabbitMQ queue", 
                content = request.Message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
    
    [HttpGet("all")]
    public async Task<IActionResult> GetAllMessages()
    {
        var messages = await _dbContext.Messages
            .OrderByDescending(m => m.ReceivedAt)
            .Take(50)
            .ToListAsync();
            
        return Ok(new
        {
            count = messages.Count,
            messages = messages
        });
    }
    
    [HttpGet("count")]
    public async Task<IActionResult> GetMessageCount()
    {
        var count = await _dbContext.Messages.CountAsync();
        return Ok(new 
        { 
            totalMessages = count,
            timestamp = DateTime.UtcNow
        });
    }
}