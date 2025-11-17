
# **Event Ingestion Service — README**

A lightweight integration service that receives external JSON events, validates them, maps them into an internal format, stores them in a database, and publishes them to RabbitMQ.

---

## **📌 How to Run the Project**

### **1. Requirements**

* .NET 9 SDK
* SQL Server (local or remote)
* RabbitMQ (Docker or local installation)

### **2. Database Setup**

Connection string is in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=EventIngestionDb;Trusted_Connection=True;"
}
```

When the API starts:

* Database will be created automatically
* Default mapping rules will be inserted (if empty)

### **3. Running the API**

Use any of the following:

```
dotnet run
```

or run via Visual Studio.

Swagger UI will be available at:

```
https://localhost:<port>/swagger
```

### **4. Running RabbitMQ (optional via Docker)**

```bash
docker run -d --hostname rabbit --name rabbitmq \
-p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Dashboard:
[http://localhost:15672](http://localhost:15672)
User/Pass: guest / guest

---

## **📌 Required Fields (and why they exist)**

The incoming external JSON **must contain**:

| Field (after mapping) | Purpose                                                 |
| --------------------- | ------------------------------------------------------- |
| **ActorId**           | Identifies the player/user making the action            |
| **Amount**            | Action amount (decimal)                                 |
| **OccurredAt**        | When the event actually happened                        |
| **Currency**          | Needed for correct processing (defaults to `GEL`)       |
| **EventType**         | Defines the event type (Deposit, BetPlaced, Withdrawal) |

These fields are required because all internal processing (DB model + RabbitMQ routing) depends on them.

---

## **📌 Default Mapping Logic**

The system accepts **any external JSON field names** and converts them using configurable mapping rules stored in the database.

### Example:

External JSON:

```json
{
  "usr": "player_123",
  "amt": "25.50",
  "curr": "GEL",
  "ts": "2025-11-14T12:30:00Z",
  "etype": "BetPlaced"
}
```

Mapping Rules Table:

| ExternalName | InternalName |
| ------------ | ------------ |
| usr          | ActorId      |
| amt          | Amount       |
| curr         | Currency     |
| ts           | OccurredAt   |
| etype        | EventType    |

If a field does **not** exist in mapping rules, the API automatically converts it like this:

```
externalName → PascalCase internal name
```

Example:

```
"sessionid" → "Sessionid"
```

All unknown fields are added to `ExtraFields`.

---

## **📌 Sample Payloads**

### ✅ **Valid Event**

```json
{
  "usr": "player_123",
  "amt": "40.25",
  "curr": "USD",
  "ts": "2025-11-14T10:20:00Z",
  "etype": "Deposit"
}
```

### ❌ **Missing required field (Amount)**

```json
{
  "usr": "player_123",
  "ts": "2025-11-14T10:20:00Z",
  "etype": "Deposit"
}
```

Response:

```json
{
  "success": false,
  "error": "Missing required internal field: Amount"
}
```

### ❌ **Invalid Amount format**

```json
{
  "usr": "player_123",
  "amt": "ABC",
  "ts": "2025-11-14T10:20:00Z",
  "etype": "Deposit"
}
```

---

## **📌 Instructions for External Providers**

### **How to send events to the system**

Send HTTP POST to:

```
POST /api/events
Content-Type: application/json
```

### **Example:**

```http
POST https://yourdomain.com/api/events
Content-Type: application/json

{
  "usr": "player_204",
  "amt": "55.90",
  "curr": "GEL",
  "ts": "2025-11-14T12:05:00Z",
  "etype": "BetPlaced"
}
```

### **Rules:**

1. JSON must be valid.
2. Required fields must be present (after mapping).
3. Timestamps must be valid ISO format.
4. Amount must be numeric.
5. You can add extra fields — the system stores them automatically.

---

## **📌 Notes**

* Every received event is stored as **RawEvent**.
* Processed events become **MappedEvent**.
* Events are then published to RabbitMQ using routing key:

  ```
  events.<eventtype>
  ```

  Examples:

  * events.betplaced
  * events.deposit
  * events.withdrawal

---

## **📌 Simulation Endpoints**

For testing without external providers:

* `POST /api/simulation/one`
  Creates and processes a single test event.

* `POST /api/simulation/batch`
  Generates and ingests 100 random events.

---

