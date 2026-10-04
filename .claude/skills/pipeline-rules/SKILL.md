---
name: pipeline-rules
description: Backend (.NET) format, build, and test checks that mirror what CI enforces on this repo — dotnet format, a clean dotnet build, and dotnet test. Use whenever C# files under server/ are added, edited, or reviewed, before considering the work done.
metadata:
  type: workflow
when_to_use: "finishing a backend change, editing a .cs file, before saying a server-side task is done, reviewing a C# diff, formatting check, dotnet build, dotnet test, dotnet format"
---

# Backend pipeline rules

Before treating any task that touches files under `server/` (not `server/.claude/worktrees/`) as finished, run the same checks CI would run — in this order, from the `server/` directory:

1. **Format** — `dotnet format`
   Auto-fixes indentation, line length (120 cols), and other style rules from `.editorconfig`. Scope it to what you touched (e.g. `dotnet format --include <paths>`) rather than running it unscoped over the whole solution — an unscoped run reformats every file with pending changes anywhere in the repo, including other in-progress work that has nothing to do with your change. Use `dotnet format --verify-no-changes` instead if you only want to check without rewriting files.

   Do not use `dotnet csharpier format`/`dotnet csharpier check` — this repo's CI enforces `dotnet format`/`.editorconfig`, not CSharpier.

2. **Build** — `dotnet build Server.slnx`
   Must succeed with no new errors. Don't introduce new warnings either, unless they're pre-existing and unrelated to the change.

3. **Test** — run whatever covers the change:
   - Targeted: `dotnet test <Project> --filter "FullyQualifiedName~<Area>"` for a fast pass scoped to what changed.
   - Full: `dotnet test Server.slnx` when the change is broad, touches shared/domain code, or the blast radius is unclear.

Format first, then build, then test — a format pass can shift line numbers, so verify build/test against the formatted code, not before it.

Run this for every backend change, not only ones that look formatting-related. Code that compiles and passes tests but doesn't match `dotnet format`/`.editorconfig` conventions still fails CI.
