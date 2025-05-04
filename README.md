# Identity Mapping System

A centralized identity management system built with .NET 9 that manages user IDs across multiple applications. This solution allows you to migrate legacy applications to use a centralized identity service without disrupting live applications.

## Key Features

- **Centralized Identity Server**: Integrates with Microsoft Identity for uniform user management
- **Mapping System**: Links legacy user IDs from different applications to centralized identities
- **Minimized Disruption**: Transition applications to use centralized identities without affecting live systems
- **Database Integrity**: Careful handling of database updates to maintain data integrity
- **Seamless Integration**: Works across different application databases ensuring proper data consistency

## Architecture

The solution is organized into four main projects:

1. **IdentityMapping.Core**: Contains domain models, interfaces, and business logic abstractions
2. **IdentityMapping.Infrastructure**: Implements data access and persistence using Entity Framework Core
3. **IdentityMapping.UserMappingService**: Provides services for mapping between legacy and centralized user IDs
4. **IdentityMapping.IdentityServer**: ASP.NET Core Web API that exposes endpoints for identity management

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQL Server (or SQL Server LocalDB for development)
- Visual Studio 2022 or later / Visual Studio Code

### Setup

1. Clone the repository
2. Update the connection string in `src/IdentityMapping.IdentityServer/appsettings.json`
3. Run database migrations:
   ```
   cd src/IdentityMapping.IdentityServer
   dotnet ef database update
   ```
4. Start the identity server:
   ```
   cd src/IdentityMapping.IdentityServer
   dotnet run
   ```

### Migration Process

The system provides a structured approach to migrating applications to use centralized identities:

1. Register the application in the identity server
2. Create mappings between legacy user IDs and centralized identities
3. Validate the mappings
4. Run the migration process (supports dry run mode, backup creation, and batch processing)
5. Update application to use the identity server for authentication

## API Endpoints

### Applications

- `GET /api/applications`: Get all registered applications
- `GET /api/applications/{id}`: Get application by ID
- `POST /api/applications`: Register a new application
- `PUT /api/applications/{id}`: Update an application
- `DELETE /api/applications/{id}`: Delete an application
- `POST /api/applications/{id}/regenerate-api-key`: Regenerate API key for an application

### User Identities

- `GET /api/useridentities`: Get all user identities
- `GET /api/useridentities/{id}`: Get user identity by ID
- `POST /api/useridentities`: Create a new user identity
- `PUT /api/useridentities/{id}`: Update a user identity
- `DELETE /api/useridentities/{id}`: Delete a user identity
- `GET /api/useridentities/by-email/{email}`: Get user identity by email
- `GET /api/useridentities/by-legacy-id`: Get user identity by legacy ID

### User Mapping

- `GET /api/usermapping/identity/{applicationId}/{legacyUserId}`: Get centralized identity ID for a legacy user
- `GET /api/usermapping/legacy/{applicationId}/{centralizedIdentityId}`: Get legacy user ID for a centralized identity
- `POST /api/usermapping/mapping`: Create a new mapping
- `GET /api/usermapping/mapping/{id}`: Get mapping by ID
- `GET /api/usermapping/mappings/{applicationId}`: Get all mappings for an application
- `POST /api/usermapping/mapping/{id}/validate`: Validate a mapping
- `DELETE /api/usermapping/mapping/{id}`: Delete a mapping
- `POST /api/usermapping/migrate/{applicationId}`: Migrate an application to use centralized identities

## Security

The identity server uses JWT tokens for authentication and authorization. Applications can authenticate using API keys. Admin operations require the "Admin" role, while some endpoints also support "ApiClient" role for application-to-application communication.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

# Identity Mapping Service

A flexible identity mapping service that allows for dynamic mapping of identity information (claims, roles, user identifiers) between different systems.

## Communication Options

The Identity Mapping service now supports multiple communication methods to integrate with other applications:

### 1. REST API

The traditional RESTful API provides HTTP endpoints for managing mapping rules and performing identity transformations.

```
GET /api/mapping-rules?applicationId={appId}
POST /api/mapping-rules
PUT /api/mapping-rules/{id}
DELETE /api/mapping-rules/{id}

POST /api/transform/claim
POST /api/transform/claims
POST /api/transform/role
POST /api/transform/roles
```

### 2. gRPC Service

For high-performance, strongly-typed communication, the service now offers a gRPC interface with the following methods:

```
TransformClaim
TransformClaims
TransformRole
TransformRoles
GetMappingRules
CreateMappingRule
UpdateMappingRule
DeleteMappingRule
```

To use the gRPC service, connect to the endpoint `https://server-address:port` and instantiate a client using the provided proto definitions.

### 3. RabbitMQ Integration

For asynchronous communication and event-driven architectures, the service now integrates with RabbitMQ.

#### Message Queues

- **identity.mapping.rule.create** - Create new mapping rules
- **identity.mapping.rule.update** - Update existing mapping rules
- **identity.mapping.rule.delete** - Delete mapping rules
- **identity.mapping.transform.claim** - Transform claims
- **identity.mapping.transform.role** - Transform roles

#### Message Patterns

The service supports both:

1. **Fire-and-forget**: Just publish a message without expecting a response
2. **Request-Response**: Provide a `ReplyTo` queue name in your message to receive an asynchronous response

## Configuration

### RabbitMQ Settings

Configure RabbitMQ in your appsettings.json file:

```json
"RabbitMQ": {
  "HostName": "localhost",
  "UserName": "guest",
  "Password": "guest",
  "VirtualHost": "/",
  "Port": 5672,
  "ExchangeName": "identity_mapping_exchange",
  "PrefetchCount": 10,
  "Enabled": true
}
```

### gRPC Settings

Configure the gRPC endpoint in your appsettings.json file:

```json
"Grpc": {
  "Url": "https://localhost:5001"
}
```

## Client Examples

Example clients for both RabbitMQ and gRPC are provided in the codebase:

- `GrpcExampleClient` - Shows how to call the gRPC service
- `RabbitMQExampleClient` - Shows how to publish and subscribe to RabbitMQ messages

## Dependencies

- .NET 9.0
- RabbitMQ.Client 6.8.1
- Grpc.AspNetCore 2.61.0
- Grpc.Net.Client 2.61.0 