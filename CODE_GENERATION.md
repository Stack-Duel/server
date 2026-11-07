# Code generation

`templates/` holds `dotnet new` templates that scaffold this repo's CQRS/DDD boilerplate. Full
docs and flags for each one are in [`templates/README.md`](templates/README.md); this file is just
a quick index.

## Install

From the `server/` repo root:

```sh
dotnet new install ./templates --force
```

Run this again with `--force` whenever a template file changes. Installs are a snapshot, not a
live link to the source files.

## Templates

| Command | Layer | Produces |
| --- | --- | --- |
| `dotnet new sd-entity` | Domain | An `AggregateRoot` (default) or child `Entity` |
| `dotnet new sd-value-object` | Domain | A value `record` wrapping one primitive, with validation |
| `dotnet new sd-repository` | Domain | An `IRepository<T>` interface for an aggregate |
| `dotnet new sd-command` | Application | Command, handler, and validator |
| `dotnet new sd-query` | Application | Query and handler |
| `dotnet new sd-service` | Application | `I{Name}Service` and an implementation wrapping `IMediator`, in one file, plus a test (run from the repo root) |
| `dotnet new sd-feature-slice` | Domain + Application | Repository, `Create` command, `GetById` query, and service in one shot |
| `dotnet new sd-controller` | Api | `{Name}Controller` wired to an `I{Name}Service` |

Each one gives you the shell, not the finished feature: types, constructors, and attributes are
in place, and the actual logic is left as `TODO` for you to fill in. See `templates/README.md`
for the exact flags each template takes.
