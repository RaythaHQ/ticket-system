# App Architecture & Coding Standards (Examples Cookbook)

This document provides **concrete examples and patterns** for implementing
features in the App boilerplate. It is **illustrative**, not normative.

The **authoritative rules and gates** live in the
[`App` Constitution](../.specify/memory/constitution.md). When in doubt:

1. Check the Constitution for non‑negotiable rules.
2. Use the examples here to align new code with existing patterns.
3. Prefer copying and adapting these patterns over inventing new ones.

---

## 1. Razor PageModels – Good vs Bad

PageModels are **thin orchestrators** that:

- Accept input and bind form models
- Call `Mediator.Send(...)` for all application work
- Map DTOs to view models
- Set success/error messages

They MUST NOT access `AppDbContext` directly or contain core business logic.

### ✅ Good: Thin PageModel using Mediator

```csharp
[Authorize]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; } = new();

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(/* ... */);

        var rolesResponse = await Mediator.Send(new GetRoles.Query());
        Form = new FormModel
        {
            AvailableRoles = rolesResponse.Result.Items,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        var command = new CreateUser.Command
        {
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress,
            IsAdmin = Form.IsAdmin,
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Created successfully.");
            return RedirectToPage("Edit", new { id = response.Result });
        }

        SetErrorMessage("Error creating.", response.GetErrors());
        await RepopulateFormOnError();
        return Page();
    }

    public record FormModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public List<RoleDto> AvailableRoles { get; set; } = new();
    }
}
```

### ❌ Bad: Business logic and DbContext in PageModel

```csharp
// Anti‑pattern – DO NOT COPY
public class Create : BaseAdminPageModel
{
    private readonly AppDbContext _db; // ❌ Concrete DbContext in PageModel

    public Create(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public FormModel Form { get; set; } = new();

    public async Task<IActionResult> OnPost()
    {
        // ❌ Business logic in PageModel
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(p => p.EmailAddress == Form.EmailAddress);
        if (existingUser != null)
            return BadRequest();

        // ❌ Direct entity manipulation
        var entity = new User
        {
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress,
        };

        _db.Users.Add(entity);
        await _db.SaveChangesAsync(); // ❌ Transaction handled here instead of handler

        return RedirectToPage("Edit", new { id = entity.Id });
    }
}
```

---

## 2. Command / Query / Handler Pattern

Every use case is a **vertical slice** with:

- `Command` or `Query` record (immutable input)
- `Validator` (FluentValidation)
- `Handler` (implements business logic)

### Example: Command

```csharp
public class CreateUser
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string EmailAddress { get; init; } = string.Empty;
        public bool IsAdmin { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.EmailAddress)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x).Custom((request, context) =>
            {
                var existingUser = db.Users
                    .FirstOrDefault(p => p.EmailAddress == request.EmailAddress);

                if (existingUser != null)
                {
                    context.AddFailure(
                        Constants.VALIDATION_SUMMARY,
                        "A user with this email address already exists."
                    );
                }
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.EmailAddress,
                IsAdmin = request.IsAdmin,
            };

            _db.Users.Add(entity);
            entity.AddDomainEvent(new UserCreatedEvent(entity));
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
```

### Example: Query

```csharp
public class GetUsers
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<UserDto>>>
    {
        public string? Search { get; init; }
        public bool? IsAdmin { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<UserDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<UserDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.Users.AsNoTracking().AsQueryable();

            if (request.IsAdmin.HasValue)
            {
                query = query.Where(p => p.IsAdmin == request.IsAdmin.Value);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(p =>
                    p.FirstName.ToLower().Contains(search)
                    || p.LastName.ToLower().Contains(search)
                    || p.EmailAddress.ToLower().Contains(search)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(UserDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<UserDto>>(
                new ListResultDto<UserDto>(items, total)
            );
        }
    }
}
```

---

## 3. Domain Entities & Value Objects

### Entity Pattern

```csharp
public class User : BaseAuditableEntity
{
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;

    public Guid? AuthenticationSchemeId { get; set; }
    public virtual AuthenticationScheme? AuthenticationScheme { get; set; }

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    public override string ToString() => FullName;
}
```

**Key points**:

- Entities inherit from `BaseEntity` or `BaseAuditableEntity`.
- Navigation properties are `virtual` collections/refs.
- Computed properties that are not persisted use `[NotMapped]`.
- Domain behavior is generally small; orchestration lives in handlers.

### Value Object Pattern

```csharp
public class SortOrder : ValueObject
{
    protected SortOrder(string developerName)
    {
        DeveloperName = developerName;
    }

    public string DeveloperName { get; private set; } = string.Empty;

    public static SortOrder From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p =>
            p.DeveloperName == developerName.ToLower()
        );
        if (type == null)
            throw new SortOrderNotFoundException(developerName);
        return type;
    }

    public static implicit operator string(SortOrder order) => order.DeveloperName;
    public static explicit operator SortOrder(string type) => From(type);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
```

**Key points**:

- Inherit from `ValueObject` and implement `GetEqualityComponents`.
- Immutable: set state in constructor, private setters only.
- Provide `From(...)` factory for validation and lookup.
- Optional implicit/explicit operators for ergonomics.

---

## 4. Data Access & EF Core Usage

All data access goes through `IAppDbContext` in the Application layer.

### Good Handler with EF Core

```csharp
public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
{
    private readonly IAppDbContext _db;

    public Handler(IAppDbContext db)
    {
        _db = db;
    }

    public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
        Command request,
        CancellationToken cancellationToken
    )
    {
        var user = await _db.Users
            .Include(p => p.Roles)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", request.Id);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        await _db.SaveChangesAsync(cancellationToken);

        return new CommandResponseDto<ShortGuid>(user.Id);
    }
}
```

### Read‑Only Query with `AsNoTracking`

```csharp
var users = await _db.Users
    .AsNoTracking()
    .Where(p => p.IsAdmin)
    .Select(UserDto.GetProjection())
    .ToListAsync(cancellationToken);
```

**Guidelines**:

- Inject **`IAppDbContext`**, not `AppDbContext`.
- Use async EF methods and always pass `CancellationToken`.
- Use `AsNoTracking()` for read‑only queries.
- Use projections to DTOs instead of returning entities directly.
- Save changes once at the end of the handler.

---

## 5. Testing Patterns

### NUnit Example – Value Object

```csharp
[Test]
public void SortOrder_From_ValidDeveloperName_ReturnsCorrectType()
{
    // Arrange
    string developerName = "asc";

    // Act
    var sortOrder = SortOrder.From(developerName);

    // Assert
    Assert.That(sortOrder, Is.EqualTo(SortOrder.Ascending));
    Assert.That(sortOrder.DeveloperName, Is.EqualTo("asc"));
}

[Test]
public void SortOrder_From_InvalidDeveloperName_ThrowsException()
{
    // Arrange
    string developerName = "invalid";

    // Act & Assert
    Assert.Throws<SortOrderNotFoundException>(() => SortOrder.From(developerName));
}
```

### NUnit Example – Handler Integration

```csharp
[Test]
public async Task CreateUser_ValidInput_ReturnsSuccess()
{
    // Arrange
    using var context = CreateDbContext();
    var handler = new CreateUser.Handler(context);
    var command = new CreateUser.Command
    {
        FirstName = "John",
        LastName = "Doe",
        EmailAddress = "john.doe@example.com",
        IsAdmin = false,
    };

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Result, Is.Not.EqualTo(ShortGuid.Empty));
}
```

**Guidelines**:

- Use Arrange–Act–Assert.
- Prioritize tests for value objects, validators, and critical commands.
- Keep unit tests free of real databases; use integration tests where
  persistence is important.

---

## 6. JavaScript Usage – Do / Dont

The app is **server‑rendered** and intentionally avoids SPAs.

### Allowed

- Progressive enhancement: date pickers, WYSIWYG editors, file uploads.
- Small interactive widgets: modals, dropdowns, confirmations.
- Vanilla JS / ES6 modules — no jQuery, no heavy frameworks.

```javascript
import { delegate } from '/js/core/events.js';

export function initConfirmDialogs() {
    delegate(document, 'click', '[data-confirm]', (event) => {
        const button = event.target.closest('[data-confirm]');
        const message = button.dataset.confirm || 'Are you sure?';

        if (!confirm(message)) {
            event.preventDefault();
            event.stopPropagation();
        }
    });
}

initConfirmDialogs();
```

### Not Allowed

- Building full SPAs or rendering major sections of the UI with JS.
- Implementing core business logic solely in JS.
- Relying on jQuery or other legacy libraries.

---

## 7. How to Use This Cookbook

When implementing a new feature:

1. **Check the Constitution** for high‑level rules
   (`.specify/memory/constitution.md`).
2. **Find a similar example** here or in existing code and copy the pattern.
3. Keep PageModels thin, push behavior into Command/Query handlers.
4. Use EF Core through `IAppDbContext` with async APIs and sensible
   performance practices.
5. Add tests at the appropriate level (value objects, validators, handlers).

If you discover a better pattern that you apply across the codebase, update
this document alongside the refactor so future contributors can follow it.