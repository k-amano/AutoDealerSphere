# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AutoDealerSphere is a Blazor WebAssembly application built with .NET 8.0, designed for auto dealer customer relationship management (CRM). The solution consists of three projects following a standard Blazor architecture:

- **Client** - Blazor WebAssembly frontend with Syncfusion UI components
- **Server** - ASP.NET Core backend API with Entity Framework Core
- **Shared** - Common models and interfaces shared between Client and Server

## Build and Run Commands

```bash
# Build the entire solution
dotnet build

# Run the application (from Server directory)
cd Server
dotnet run

# The application will be available at:
# HTTP: http://localhost:5259
# HTTPS: https://localhost:7187
```

## Architecture

### Technology Stack
- **.NET 8.0** - Target framework for all projects
- **Blazor WebAssembly** - Client-side SPA framework
- **Entity Framework Core 8.0.4** - ORM with SQLite provider
- **Syncfusion Blazor Components** - UI component library (Grid, DataForm, Themes)
- **SQLite** - Database (located at Server/Data/crm01.db)

### Key Architectural Patterns

1. **Three-Layer Architecture**:
   - Client handles UI and user interactions using Blazor components
   - Server provides RESTful API endpoints and database access
   - Shared contains domain models used by both layers

2. **Database Initialization**:
   - On startup, the application recreates the database with sample data (see Server/Program.cs:22-27)
   - Database context is configured with SQLite connection string from appsettings.json
   - Sample data initialization is handled by DbInitializer service

3. **Service Pattern**:
   - Services are registered in DI container (e.g., IClientService)
   - Controllers use injected services to handle business logic
   - DbContextFactory pattern is used for database context management

4. **Blazor Component Structure**:
   - Pages are split into .razor (markup) and .razor.cs (code-behind) files
   - Shared components (UserForm, ClientForm) are reused across pages
   - Menu navigation is managed through MenuItems.cs

## Important Notes

- The application uses Syncfusion components which require a license key (configured in Client/Program.cs)
- Database is recreated on every startup - this behavior may need modification for production
- The application supports both User and Client management with full CRUD operations
- Japanese language is used throughout the UI (labels, error messages, etc.)