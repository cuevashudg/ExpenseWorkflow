# Workflow & Approval System

## Overview

Enterprise-grade expense approval system built with **ASP.NET Core 10**, **Entity Framework Core**, **PostgreSQL**, and **JWT authentication**. The system implements a complete expense workflow with role-based authorization, domain-driven business rules, and comprehensive audit logging.

**Key Technologies:**
- ASP.NET Core 10.0
- Entity Framework Core 10.0
- PostgreSQL 18
- ASP.NET Core Identity (Guid-based)
- JWT Bearer Authentication (HS256)
- Clean Architecture

## Architecture

### Clean Architecture Layers

```
Workflow.Api (Presentation)
    ↓
Workflow.Application (Use Cases)
    ↓
Workflow.Infrastructure (Data Access)
    ↓
Workflow.Domain (Business Logic)
```

**Workflow.Domain:**
- Core business entities: `ExpenseRequest`, `AuditLog`, `Attachment`, `ApplicationUser`
- Domain enums: `ExpenseStatus`, `UserRole`
- Business rules enforced through entity methods
- No external dependencies

**Workflow.Application:**
- `ExpenseService`: Orchestrates expense operations
- Scoped service lifetime (matches DbContext)
- Coordinates between API controllers and domain entities

**Workflow.Infrastructure:**
- `WorkflowDbContext`: EF Core + ASP.NET Core Identity integration
- PostgreSQL with snake_case naming convention
- Migrations for schema management
- Connection string management

**Workflow.Api:**
- REST API controllers: `AuthController`, `ExpensesController`
- JWT token generation and validation
- Policy-based and role-based authorization
- RoleSeeder for startup initialization

## Core Features

### Expense Workflow
- **Draft**: Employee creates expense (editable)
- **Submitted**: Employee submits for approval (immutable)
- **Approved**: Manager approves expense
- **Rejected**: Manager rejects with reason

### Authentication & Authorization
- **JWT Bearer Tokens**: 60-minute expiration, symmetric key signing
- **Claims-based Identity**: NameIdentifier, Email, Name, Role
- **Role-Based Access Control**: Employee, Manager, Admin roles
- **Policy-Based Authorization**: "CanApproveExpense" policy for approval operations

### Security Features
- Password hashing via ASP.NET Core Identity
- Token validation: issuer, audience, lifetime, signing key
- Authorization attributes on sensitive endpoints
- User context extraction from JWT claims

### Audit Logging
- `AuditLog` entity tracks all workflow state changes
- Records: user, timestamp, old/new status
- Immutable audit trail for compliance

### Data Management
- Expense CRUD operations with validation
- Attachment support (multiple per expense)
- Query operations: by creator, by status, pending approval
- Soft validation through domain rules

## Why This Design

### Why Domain is Isolated
**Reason**: Domain represents pure business logic independent of infrastructure concerns.

**Benefits:**
- **Testability**: Domain rules tested in isolation without database/framework dependencies
- **Maintainability**: Business logic changes don't ripple through infrastructure
- **Portability**: Domain can be reused across different presentation layers (API, CLI, desktop)
- **Clarity**: Business rules are explicit in entity methods, not hidden in services

**Example**: `ExpenseRequest.Approve(managerId, userRole)` enforces "only managers can approve" as a domain rule, not a controller concern.

### Why PostgreSQL
**Reason**: Enterprise-grade relational database with excellent .NET support.

**Benefits:**
- **ACID Compliance**: Transactions ensure data integrity for financial operations
- **Rich Data Types**: Native support for JSON, arrays, UUIDs (Guid primary keys)
- **Performance**: Robust indexing, query optimization, connection pooling
- **Cost-Effective**: Open-source with strong community support
- **EF Core Integration**: Npgsql provider with full LINQ support

**Use Case**: Expense approval workflow requires transactional consistency—an expense cannot be in "Draft" and "Approved" simultaneously.

### Why Policy-Based Authorization
**Reason**: Decouples authorization logic from business logic and provides flexibility.

**Benefits:**
- **Declarative Security**: `[Authorize(Policy = "CanApproveExpense")]` vs. manual role checks
- **Centralized Rules**: Authorization policies defined once in `Program.cs`
- **Composability**: Policies can combine multiple requirements (role + claim + custom logic)
- **Testability**: Policies tested independently from controllers
- **Maintainability**: Changing authorization rules doesn't require controller modifications

**Example**: "CanApproveExpense" policy currently requires Manager role, but could be extended to include "Department Head" or "Spending Limit" checks without touching controller code.

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL 18
- Visual Studio Code or Visual Studio 2022

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd "TaskAuditLog Backend"
   ```

2. **Update connection string** (if needed)
   ```json
   // Workflow.Api/appsettings.Development.json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=workflowdb;Username=postgres;Password=postgres"
   }
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update --project Workflow.Infrastructure --startup-project Workflow.Api
   ```

4. **Run the application**
   ```bash
   dotnet run --project Workflow.Api
   ```

### Default Users
Roles are automatically seeded on startup. Register users with:
- **Employee**: Any user registered with default settings
- **Manager**: User with manager privileges (set via admin)
- **Admin**: System administrator

### API Endpoints

**Authentication:**
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and receive JWT token
- `GET /api/auth/me` - Get current user info (requires auth)

**Expenses:**
- `POST /api/expenses` - Create expense (Employee)
- `GET /api/expenses` - List my expenses
- `GET /api/expenses/{id}` - Get expense details
- `PUT /api/expenses/{id}` - Update draft expense
- `POST /api/expenses/{id}/submit` - Submit for approval
- `GET /api/expenses/pending` - List pending expenses (Manager)
- `POST /api/expenses/{id}/approve` - Approve expense (Manager)
- `POST /api/expenses/{id}/reject` - Reject expense (Manager)

## Project Structure

```
TaskAuditLog Backend/
├── Workflow.Domain/           # Business entities and rules
│   ├── Entities/
│   ├── Enums/
│   └── Exceptions/
├── Workflow.Application/      # Use case orchestration
│   └── Services/
├── Workflow.Infrastructure/   # Data access and persistence
│   ├── Data/
│   └── Migrations/
├── Workflow.Api/              # REST API and authentication
│   ├── Controllers/
│   ├── Data/                  # RoleSeeder
│   └── Program.cs
└── Workflow.Domain.Tests/     # Unit tests
```

## Testing

### Unit Tests
```bash
dotnet test Workflow.Domain.Tests
```

### Integration Tests
```powershell
# Start the API first
dotnet run --project Workflow.Api

### Manual Testing with psql
```bash
# Connect to database
psql -U postgres -d workflowdb

# View users
SELECT "Email", "FullName", "Role" FROM users;

# View expenses
SELECT "Id", "Title", "Amount", "Status", "CreatedBy" FROM expense_requests;

# View audit log
SELECT * FROM audit_logs ORDER BY "Timestamp" DESC;
```

## Development Roadmap

**Phase 1 - Core Foundation** ✅
- Domain entities and business rules
- PostgreSQL integration
- Unit tests
- Identity and JWT authentication

**Phase 2 - API & Authorization** ✅
- REST API controllers
- Role-based and policy-based authorization
- Expense workflow endpoints

**Phase 3 - Enhancements** (Planned)
- Expense categories and cost centers
- Comments and discussion threads
- Resubmission capability
- File attachment storage

**Phase 4 - Frontend** (Planned)
- React SPA
- Redux state management
- Material-UI components

**Phase 5 - Deployment** (Planned)
- Azure App Service deployment
- Azure Database for PostgreSQL
- CI/CD pipeline
- Monitoring and logging

## Contributing

Follow Clean Architecture principles and domain-driven design patterns. Ensure all domain rules are tested before submitting pull requests.

## License

[Your License Here]