# StackDuel CQRS templates

`dotnet new` templates that scaffold this repo's CQRS pieces (`StackDuel.Application.Commands.ICommand`,
`Mediator`, `Ardalis.Result`, `FluentValidation`) so adding a feature doesn't mean hand-writing the same
boilerplate every time. Mirrors the shape of the real `CreateUser`/`GetUserBySub` slice in
`StackDuel.Application`.

## Install / update

From the `server/` repo root:

```sh
dotnet new install ./templates --force
```

Re-run with `--force` any time you edit a template file — installs are a snapshot, not a live link.

## Templates

### `sd-command` — Command + Handler + Validator

```sh
cd StackDuel.Application/Commands/Orders   # wherever the feature's commands live
dotnet new sd-command -n CreateOrder --response Guid \
  --namespace StackDuel.Application.Commands.Orders.CreateOrder
```

Generates `CreateOrder/{CreateOrderCommand,CreateOrderHandler,CreateOrderValidator}.cs` with empty
TODO bodies. `--response` is required (the `TResult` in `ICommand<TResult>`); `--namespace` defaults
to `StackDuel.Application.Commands`.

### `sd-query` — Query + Handler

```sh
dotnet new sd-query -n GetOrderById --response OrderDto \
  --namespace StackDuel.Application.Queries.Orders.GetOrderById
```

Same idea, for the read side. `--response` is required (usually a Dto type).

### `sd-repository` — Repository interface (Domain layer)

```sh
dotnet new sd-repository -n Order --aggregate Order \
  --aggregateNs StackDuel.Domain.Order.Entities --namespace StackDuel.Domain.Order
```

Generates `IOrderRepository : IRepository<Order>`. `--aggregate` and `--aggregateNs` are both
required and always fully-qualify the aggregate type in the generated file — do the same in any
code you add by hand in a namespace that isn't `StackDuel.Domain.<Aggregate>` itself. Folders named
the same as their entity (e.g. `StackDuel.Domain/User/Entities/User.cs`) make the bare type name
`User`/`Order`/etc. unresolvable from sibling or ancestor namespaces (C# finds the *namespace*
segment first) — see the note in `StackDuel.Domain.Tests` for a worked example.

### `sd-service` (Application service, wraps `IMediator`)

```sh
dotnet new sd-service -n Order
```

Run this from the **repo root**. It generates `IOrderService`/`OrderService(IMediator mediator)` in
one file at `StackDuel.Application/OrderService.cs`, with empty method bodies you fill in by
calling `mediator.Send(new SomeCommand(...), cancellationToken)`, plus a matching test at
`StackDuel.Application.Tests/Services/Order/OrderServiceTests.cs` that constructs the service with a
mocked `IMediator` and leaves a `TODO` for the real assertions once the service has methods.

Options:

- `--namespace` (default `StackDuel.Application`): namespace for the service interface and implementation.
- `--testNamespace` (default `StackDuel.Application.Tests.Services`): namespace prefix for the test
  file (the feature name is appended automatically, e.g. `...Services.Order`).
- `--skipTests` (default `false`): pass `true` to skip the test file entirely.

### `sd-feature-slice` — all of the above at once

The composite generator: given an aggregate name and its existing entity's namespace, scaffolds a
full `Create` + `GetById` vertical slice — repository interface, command, validator, handler, query,
handler, DTO, and service — in one shot, with everything wired together and referencing the right
types. Run from the **repo root** (it writes into both `StackDuel.Domain` and
`StackDuel.Application`):

```sh
dotnet new sd-feature-slice -n Order --aggregateNs StackDuel.Domain.Order.Entities
```

Produces:

```
StackDuel.Domain/Order/IOrderRepository.cs
StackDuel.Application/Commands/Order/CreateOrder/{CreateOrderCommand,CreateOrderHandler,CreateOrderValidator}.cs
StackDuel.Application/Queries/Order/GetOrderById/{GetOrderByIdQuery,GetOrderByIdHandler}.cs
StackDuel.Application/Order/{IOrderService,OrderService,Dtos/OrderDto}.cs
```

This assumes the `Order` aggregate root **already exists** (hand-write the entity first, the way
`User` was built) — the slice scaffolds the CQRS plumbing around it, not the domain model itself.
After generating, you still need to: register `IOrderService`/`IOrderRepository` in DI, implement
the handler bodies (marked `throw new NotImplementedException()`), and fill in the DTO's fields.

### `sd-entity` (Domain entity, Domain layer)

```sh
dotnet new sd-entity -n Order --namespace StackDuel.Domain.Orders.Entities
```

By default this generates a class deriving from `AggregateRoot`, the same way `User` is built.
Pass `--base Entity` for a child entity referenced by an aggregate root, the way `UserAvatar` is
built. A child entity gets no repository or domain events of its own:

```sh
dotnet new sd-entity -n OrderLine --namespace StackDuel.Domain.Orders.Entities --base Entity
```

This scaffolds the shell only (`TODO` comments for properties/behavior). Write the constructor,
properties, and any domain events by hand, the way `User.cs` does it.

### `sd-value-object` (Domain value object, Domain layer)

```sh
dotnet new sd-value-object -n OrderNumber --namespace StackDuel.Domain.Orders.ValueObjects
```

Generates a `sealed record` wrapping a single primitive (`string` by default) with a validating
constructor and an implicit conversion back to the primitive, the same shape as `Username`/`Bio`.
Use `--valueType` for a non-string primitive:

```sh
dotnet new sd-value-object -n Money --namespace StackDuel.Domain.Orders.ValueObjects --valueType decimal
```

### `sd-controller` (Api controller, Api layer)

```sh
dotnet new sd-controller -n Order --serviceNamespace StackDuel.Application.Orders
```

Generates `OrderController : ControllerBase` constructor-injected with `IOrderService`, with the
`[ApiController]`/versioned `[Route]`/`[EnableRateLimiting]` attributes and one example
`[RequireUser]`-gated `GET` action already wired, the same shape as `LanguageController`. `--serviceNamespace`
is required (the namespace the generated `I{Name}Service` import needs, typically what
`sd-service` just created). Add `[RequirePermission(...)]` and further actions by hand, the
way `FeedbackController` does.
