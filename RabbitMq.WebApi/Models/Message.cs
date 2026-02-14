namespace RabbitMq.WebApi.Models;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}