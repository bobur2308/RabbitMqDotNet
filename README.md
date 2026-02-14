# RabbitMQ with .NET Demo

Simple message queue implementation using RabbitMQ and .NET 8.

## Technologies
- .NET 8 Web API
- RabbitMQ (Message Broker)
- PostgreSQL
- Entity Framework Core
- Docker

## Features
- Message Publisher (API endpoint)
- Background Consumer Service
- PostgreSQL persistence
- RabbitMQ Management UI

## How to Run
1. Start RabbitMQ: `docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management`
2. Update `appsettings.json` with your PostgreSQL credentials
3. Run migrations: `dotnet ef database update`
4. Start the app: `dotnet run`

## API Endpoints
- POST `/api/message/send` - Send message to queue
- GET `/api/message/all` - Get all messages from DB
- GET `/api/message/count` - Get message count
