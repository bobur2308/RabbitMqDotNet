using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMq.WebApi.Data;
using RabbitMq.WebApi.Models;

namespace RabbitMq.WebApi.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    
    public RabbitMqConsumer(
        IConfiguration configuration, 
        IServiceProvider serviceProvider,
        ILogger<RabbitMqConsumer> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Consumer starting...");
        await InitializeRabbitMqAsync();
        await base.StartAsync(cancellationToken);
    }
    
    private async Task InitializeRabbitMqAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };
        
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        
        var queueName = _configuration["RabbitMQ:QueueName"] ?? "hello-queue";
        
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
        
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        
        _logger.LogInformation("RabbitMQ connected. Queue: {QueueName}", queueName);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogError("Channel is null");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        var queueName = _configuration["RabbitMQ:QueueName"] ?? "hello-queue";
        
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            _logger.LogInformation("Received: {Message}", message);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var messageEntity = new Message
                {
                    Content = Encoding.UTF8.GetString(body),
                    ReceivedAt = DateTime.UtcNow
                };
                
                await dbContext.Messages.AddAsync(messageEntity);
                await dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Saved to DB: {Message}", messageEntity.Content);
                
                await _channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await _channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };
        
        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
        
        await Task.CompletedTask;
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync();
            
        if (_connection != null)
            await _connection.CloseAsync();
            
        _logger.LogInformation("RabbitMQ Consumer stopped");
        await base.StopAsync(cancellationToken);
    }
    
    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}