# 🖥 EventConsumers (Console Application)

`EventConsumers` is a lightweight console application included in the solution.
Its purpose is to **consume events from RabbitMQ** that are published by the main API (`EventIngestion.Api`).

### ✔ What it does

* Connects to RabbitMQ
* Listens to the main queue (`events.q`)
* Prints received JSON events into the console
* Acknowledges messages when processed
* Shows errors if message processing fails

### ✔ Why the console app exists

This project demonstrates how external systems (or internal microservices) would consume events produced by the ingestion service.
It serves as a **simple example consumer**, making it easy to verify:

* events are correctly published
* routing keys work
* retry mechanism works
* DLQ handling works
* message structure is correct

### ✔ How to run it

1. Make sure RabbitMQ is running
2. Open terminal
3. Navigate to the `EventConsumers` folder
4. Run:

```sh
dotnet run
```

You will see logs like:

```
[Consumer] Message received from events.q:
{ "ActorId": "player_123", "Amount": 25.50, "Currency": "GEL", ... }
```

### ✔ Does it need to be deployed?

No — the console app is only for **local testing and demonstration**.
In a real system, this would be replaced by real microservices or workers.

---
