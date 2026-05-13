## Install All Dependencies (Complete Setup)

### Option 1: Using dotnet restore (Recommended)
From the solution root directory:
```bash
cd "Online Consultation Backend Service"
dotnet restore
```
This automatically downloads all packages defined in each `.csproj` file.

### Option 2: Manual installation by layer
```bash
# 1. Presentation Layer
cd ConsultationApi
dotnet add package AutoMapper --version 12.0.1
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.0.1
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package FluentValidation.AspNetCore --version 11.3.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package Microsoft.AspNetCore.SignalR --version 1.1.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.0
dotnet add package Serilog.Sinks.File --version 5.0.0
dotnet add package Swashbuckle.AspNetCore --version 6.6.2
cd ..

# 2. Core Layer
cd ConsultationApi.Core
# No external packages
cd ..

# 3. Infrastructure Layer
cd ConsultationApi.Infrastructure
dotnet add package AutoMapper --version 12.0.1
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.0.1
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package Microsoft.AspNetCore.Http.Abstractions --version 2.2.0
dotnet add package Microsoft.AspNetCore.SignalR.Core --version 1.1.0
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.Extensions.Hosting.Abstractions --version 8.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.0.2
cd ..

# 4. Testing Layer
cd ConsultationApi.Tests
dotnet add package coverlet.collector --version 6.0.0
dotnet add package FluentAssertions --version 6.12.0
dotnet add package FluentValidation --version 11.5.1
dotnet add package FluentValidation.AspNetCore --version 11.3.0
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0
dotnet add package Moq --version 4.20.69
dotnet add package Testcontainers.PostgreSql --version 3.7.0
dotnet add package xunit --version 2.5.3
dotnet add package xunit.runner.visualstudio --version 2.5.3
cd ..
```

## Dependency Tree

```
ConsultationApi (Presentation)
├─ AutoMapper 12.0.1
├─ AutoMapper.Extensions 12.0.1
├─ BCrypt.Net-Next 4.0.3
├─ FluentValidation.AspNetCore 11.3.0
├─ Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
├─ Microsoft.AspNetCore.SignalR 1.1.0
├─ Microsoft.EntityFrameworkCore.Design 8.0.0
├─ Serilog.AspNetCore 8.0.0
├─ Serilog.Sinks.Console 5.0.0
├─ Serilog.Sinks.File 5.0.0
├─ Swashbuckle.AspNetCore 6.6.2
├─ ProjectRef: ConsultationApi.Core
└─ ProjectRef: ConsultationApi.Infrastructure

ConsultationApi.Core (Domain)
└─ NO external NuGet packages

ConsultationApi.Infrastructure (Data Access)
├─ AutoMapper 12.0.1
├─ AutoMapper.Extensions 12.0.1
├─ BCrypt.Net-Next 4.0.3
├─ Microsoft.AspNetCore.Http.Abstractions 2.2.0
├─ Microsoft.AspNetCore.SignalR.Core 1.1.0
├─ Microsoft.EntityFrameworkCore 8.0.0
├─ Microsoft.EntityFrameworkCore.Design 8.0.0
├─ Microsoft.Extensions.Hosting.Abstractions 8.0.0
├─ Npgsql.EntityFrameworkCore.PostgreSQL 8.0.0
├─ System.IdentityModel.Tokens.Jwt 8.0.2
└─ ProjectRef: ConsultationApi.Core

ConsultationApi.Tests (Testing)
├─ coverlet.collector 6.0.0
├─ FluentAssertions 6.12.0
├─ FluentValidation 11.5.1
├─ FluentValidation.AspNetCore 11.3.0
├─ Microsoft.AspNetCore.Mvc.Testing 8.0.0
├─ Microsoft.NET.Test.Sdk 17.8.0
├─ Moq 4.20.69
├─ Testcontainers.PostgreSql 3.7.0
├─ xunit 2.5.3
├─ xunit.runner.visualstudio 2.5.3
├─ ProjectRef: ConsultationApi.Core
├─ ProjectRef: ConsultationApi.Infrastructure
└─ ProjectRef: ConsultationApi
```

---

## Updating Packages

To check for outdated packages:
```bash
dotnet outdated
```

To update a specific package:
```bash
dotnet add ConsultationApi package AutoMapper --version 13.0.0
```

To update all packages in the solution:
```bash
# Requires dotnet-interactive installed
dotnet package search
```

---

## Version Compatibility

- **.NET Version**: 8.0 (LTS)
- **EF Core**: 8.0.0 (matches .NET version)
- **AutoMapper**: 12.0.1
- **FluentValidation**: 11.x series
- **xUnit**: 2.5.x series
- **SignalR**: 1.1.0 (stable)

All packages are compatible with .NET 8 and tested together.
