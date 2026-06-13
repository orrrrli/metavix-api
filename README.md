# Metavix API

API REST para una plataforma médica que conecta pacientes diabéticos con sus médicos tratantes. Los pacientes registran lecturas de glucosa y resultados de laboratorio; los médicos consultan el historial de sus pacientes vinculados.

Construida con **ASP.NET Core 9**, **Clean Architecture** y **CQRS con MediatR**.

---

## Arquitectura

```
API ──► Infrastructure ──► Application ──► Domain
```

Las dependencias apuntan estrictamente hacia adentro. La capa Domain no tiene referencias a ningún framework — no sabe nada de EF Core, ASP.NET ni de ningún SDK externo. Esto significa que la lógica de negocio puede probarse en unitarios sin levantar una base de datos ni un contexto HTTP.

```
src/
├── Domain/          # Entidades, enums, value objects. Sin dependencias externas.
├── Application/     # Casos de uso: commands, queries, handlers, validators, interfaces.
├── Infrastructure/  # EF Core, repositorios, JWT, email, cliente SEP scraper.
├── API/             # Módulos Carter (Minimal API), middleware, configuración de DI.
└── Contracts/       # DTOs para el límite HTTP.
```

**¿Por qué esta estructura?** El beneficio concreto: migrar la base de datos de Neon (usada en alpha) a un VPS propio solo requiere tocar Infrastructure. Las capas Application y Domain no se ven afectadas porque dependen de `IUserRepository`, no de `AppDbContext`.

---

## Decisiones técnicas importantes

### 1. JWT entregado via cookies HTTP-Only

Los access tokens (TTL 15 minutos) y refresh tokens (TTL 7 días) se establecen como cookies `HttpOnly; Secure; SameSite=None` — nunca se devuelven en el cuerpo de la respuesta para guardarse en `localStorage`.

**Por qué:** `localStorage` es legible por cualquier script en la página, lo que hace que un ataque XSS pueda extraer el token. Una cookie `HttpOnly` es inaccesible para scripts y el navegador la envía automáticamente en cada petición. `SameSite=None` es necesario porque el frontend y la API están en orígenes distintos. (api y el sitio de la app)

La cookie del refresh token está restringida a `Path=/api/auth` para que solo se envíe al endpoint de renovación de tokens, no a cada llamada de la API.

### 2. El registro de médicos requiere verificación de cédula SEP (En progreso)

Cuando un médico se registra, la API consulta en tiempo real el registro público de cédulas profesionales de la SEP y compara el nombre enviado contra el titular registrado de esa cédula, usando normalización Unicode (Descomponemos en dos caracteres para las tildes → eliminar tildes → mayúsculas). Si los nombres no coinciden, el registro es rechazado.

**Por qué:** La plataforma maneja datos médicos sensibles de pacientes. Un médico sin verificar podría ser cualquier persona. Los servicios de verificación de identidad de terceros (ej. Didit) cobran por verificación desde el primer usuario, lo que es inviable en alpha. El registro de la SEP es público y gratuito. El scraper vive como `ICedulaVerificationService` en Infrastructure — la capa Application solo depende de la interfaz, así que puede reemplazarse por un proveedor de pago sin tocar ningún handler.

### 3. CQRS con MediatR y el patrón ErrorOr

Cada caso de uso es un `Command` o `Query` despachado a través de MediatR. Los handlers retornan `ErrorOr<T>` — una unión discriminada de un valor de éxito o uno o más errores de dominio. No se lanzan excepciones para lógica de negocio.

**Por qué:** La llamada `result.Match(onSuccess, onError)` en cada endpoint fuerza el manejo explícito de ambas ramas. No hay forma de devolver accidentalmente un 200 cuando la operación falló. Los errores de dominio llevan un código y descripción (`DoctorErrors.InvalidLicense`, `AuthErrors.EmailAlreadyExists`) para que la capa API los mapee al status HTTP correcto sin cascadas de bloques `if/else`.

### 4. Pipeline behavior de FluentValidation

Un `IPipelineBehavior<TRequest, TResponse>` de MediatR intercepta cada command antes de que llegue a su handler, ejecuta FluentValidation y corta el flujo con un `400 Bad Request` si la validación falla.

**Por qué:** La validación corre una sola vez, en un solo lugar, automáticamente — ningún handler necesita recordar llamarla. Agregar un nuevo validator para un nuevo command no requiere ningún cambio en el registro del pipeline.

### 5. Middleware de Correlation ID

El servidor genera un `Guid` único por cada request, lo empuja al `LogContext` de Serilog y lo devuelve en el header `X-Correlation-ID` de la respuesta. Esto permite buscar en los logs todo lo que ocurrió en una petición específica con una sola consulta al endpoint de admin.

El middleware también acepta el header si el cliente lo manda. Esto habilita un segundo nivel de trazabilidad: el frontend podría generar un ID al inicio de un flujo y mandarlo en todas las llamadas que lo componen (ej. login → cargar perfil → cargar registros), agrupando requests distintos bajo el mismo ID en los logs. Este uso todavía no está implementado en el frontend porque aún no se han definido los flujos multi-endpoint que requieren validación end-to-end.

**Por qué:** Sin esto, depurar un fallo en producción implica adivinar qué líneas de log pertenecen al mismo request. Con correlation IDs, basta con el ID que devuelve la respuesta para obtener el trace completo de esa petición.

---

## Endpoints


| Módulo  | Método | Ruta                               | Auth      |
| ------- | ------ | ---------------------------------- | --------- |
| Auth    | POST   | `/api/auth/login`                  | Público   |
| Auth    | POST   | `/api/auth/register/patient`       | Público   |
| Auth    | POST   | `/api/auth/register/doctor`        | Público   |
| Auth    | POST   | `/api/auth/refresh`                | Público   |
| Auth    | POST   | `/api/auth/logout`                 | Público   |
| Auth    | POST   | `/api/auth/forgot-password`        | Público   |
| Auth    | POST   | `/api/auth/reset-password`         | Público   |
| Auth    | GET    | `/api/auth/google`                 | Público   |
| Auth    | GET    | `/api/auth/google/callback`        | Público   |
| Auth    | GET    | `/api/auth/me`                     | Requerida |
| Doctor  | GET    | `/api/doctor/profile`              | Doctor    |
| Doctor  | GET    | `/api/doctor/profile/:id`          | Doctor    |
| Doctor  | GET    | `/api/doctor/patients/:id/profile` | Doctor    |
| Doctor  | GET    | `/api/doctor/patients/:id/records` | Doctor    |
| Doctor  | GET    | `/api/doctor/patients/:id/labs`    | Doctor    |
| Patient | GET    | `/api/patient/me`                  | Patient   |
| Patient | POST   | `/api/patient/records`             | Patient   |
| Patient | GET    | `/api/patient/records`             | Patient   |
| Patient | GET    | `/api/patient/records/:id`         | Patient   |
| Admin   | GET    | `/api/admin/logs`                  | Admin     |
| Admin   | GET    | `/api/admin/logs/:correlationId`   | Admin     |


Los endpoints de login y registro tienen rate limiting. Los endpoints con rol Doctor verifican que el usuario autenticado tenga ese rol via claims del JWT.

---

## Stack tecnológico


| Área              | Elección                                 |
| ----------------- | ---------------------------------------- |
| Framework         | ASP.NET Core 9 Minimal APIs (Carter)     |
| ORM               | Entity Framework Core 9                  |
| Base de datos     | PostgreSQL (Neon en alpha)               |
| CQRS              | MediatR                                  |
| Validación        | FluentValidation                         |
| Mapping           | Mapster                                  |
| Manejo de errores | ErrorOr                                  |
| Logging           | Serilog (estructurado, hacia Postgres)   |
| Autenticación     | JWT + Refresh Token (cookie HTTP-Only)   |
| OAuth             | Google OAuth 2.0                         |
| Email             | `IEmailService` (agnóstico al proveedor) |


---

## Correr localmente

```bash
# Restaurar y compilar
dotnet build

# Configurar secrets (correr desde src/API/)
dotnet user-secrets set "Jwt:Secret" "<tu-secret>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<tu-postgres-url>"

# Aplicar migraciones
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj

# Correr
dotnet run --project src/API/API.csproj
```

Documentación OpenAPI disponible en `/swagger` en desarrollo.

---

## Tests

```bash
# Correr todos los tests
dotnet test

# Correr solo el proyecto de unit tests
dotnet test tests/Application.Tests/Application.Tests.csproj
```

Los tests unitarios cubren los handlers de Application usando **xUnit**, **NSubstitute** para mocks y **FluentAssertions** para assertions. No requieren base de datos ni levantar el servidor — todas las dependencias se mockean a través de las interfaces definidas en Application.

---

## Migraciones

Cuando se modifica una propiedad de una entidad (renombrar columna, cambiar tipo, agregar campo), el flujo es:

**1. Modificar la entidad en Domain**

```csharp
// src/Domain/Models/Patient.cs
public string PhoneNumber { get; set; } = string.Empty; // campo nuevo
```

**2. Actualizar la configuración de EF Core en Infrastructure** (si aplica)

```csharp
// src/Infrastructure/Persistence/Configurations/PatientConfiguration.cs
builder.Property(p => p.PhoneNumber).HasMaxLength(20);
```

**3. Generar la migración**

```bash
dotnet ef migrations add <NombreDescriptivo> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj \
  --output-dir Migrations
```

El nombre debe describir el cambio, por ejemplo: `AddPhoneNumberToPatient`, `RenameLastNameToPaternalLastName`.

**4. Revisar el archivo generado antes de aplicar**

EF Core genera el archivo en `src/Infrastructure/Migrations/`. Verificar que el `Up()` y `Down()` reflejan exactamente el cambio esperado — especialmente en renombrados, donde EF a veces genera un `DropColumn` + `AddColumn` en lugar de un `RenameColumn`, lo que causaría pérdida de datos.

**5. Aplicar la migración**

```bash
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj
```

> **Por qué `--output-dir Migrations` es obligatorio:** `AppDbContext` vive en el namespace `Infrastructure.Common.Persistence`. Sin este flag, EF deriva la carpeta de destino desde ese namespace y coloca las migraciones en `src/Infrastructure/Common/Persistence/Migrations/`, rompiendo la cadena de migraciones.

---

## Estado del proyecto

En desarrollo activo — actualmente en alpha. La API y Postgres están desplegados en un VPS con Ubuntu Linux, en el mismo servidor para reducir latencia y simplificar operaciones.