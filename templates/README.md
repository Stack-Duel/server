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

### `stackduel-command` — Command + Handler + Validator

```sh
cd StackDuel.Application/Commands/Orders   # wherever the feature's commands live
dotnet new stackduel-command -n CreateOrder --response Guid \
  --namespace StackDuel.Application.Commands.Orders.CreateOrder
```

Generates `CreateOrder/{CreateOrderCommand,CreateOrderHandler,CreateOrderValidator}.cs` with empty
TODO bodies. `--response` is required (the `TResult` in `ICommand<TResult>`); `--namespace` defaults
to `StackDuel.Application.Commands`.

### `stackduel-query` — Query + Handler

```sh
dotnet new stackduel-query -n GetOrderById --response OrderDto \
  --namespace StackDuel.Application.Queries.Orders.GetOrderById
```

Same idea, for the read side. `--response` is required (usually a Dto type).

### `stackduel-repository` — Repository interface (Domain layer)

```sh
dotnet new stackduel-repository -n Order --aggregate Order \
  --aggregateNs StackDuel.Domain.Order.Entities --namespace StackDuel.Domain.Order
```

Generates `IOrderRepository : IRepository<Order>`. `--aggregate` and `--aggregateNs` are both
required and always fully-qualify the aggregate type in the generated file — do the same in any
code you add by hand in a namespace that isn't `StackDuel.Domain.<Aggregate>` itself. Folders named
the same as their entity (e.g. `StackDuel.Domain/User/Entities/User.cs`) make the bare type name
`User`/`Order`/etc. unresolvable from sibling or ancestor namespaces (C# finds the *namespace*
segment first) — see the note in `StackDuel.Domain.Tests` for a worked example.

### `stackduel-service` — Application service (wraps `ISender`)

```sh
dotnet new stackduel-service -n Order --namespace StackDuel.Application.Order
```

Generates `IOrderService`/`OrderService(ISender sender)` with empty method bodies you fill in by
calling `sender.Send(new SomeCommand(...), cancellationToken)`.

### `stackduel-feature-slice` — all of the above at once

The composite generator: given an aggregate name and its existing entity's namespace, scaffolds a
full `Create` + `GetById` vertical slice — repository interface, command, validator, handler, query,
handler, DTO, and service — in one shot, with everything wired together and referencing the right
types. Run from the **repo root** (it writes into both `StackDuel.Domain` and
`StackDuel.Application`):

```sh
dotnet new stackduel-feature-slice -n Order --aggregateNs StackDuel.Domain.Order.Entities
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
