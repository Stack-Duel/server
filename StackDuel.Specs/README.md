# StackDuel.Specs

Acceptance specs for business rules, written as Gherkin `.feature` files and executed with [Reqnroll](https://reqnroll.net/) on xUnit.

Each scenario exercises a real `Application` command/query handler (constructed directly, against a fake repository in `Support/`) instead of going through HTTP — keeps specs fast and focused on business rules rather than wiring. For full-stack HTTP scenarios, use `StackDuel.IntegrationTests` instead.

## Workflow

1. Create the GitHub issue/PBI for the feature on the board first.
2. Write the `.feature` file under `Features/<Aggregate>/<Feature>.feature` describing the rules in Given/When/Then, with the issue number in a header comment (`# Issue: #123`). Open this as a PR before writing any implementation code, so the rules can be reviewed on their own.
3. Paste the `.feature` file's repo path into the issue/PBI description, so the board links straight to the rules.
4. Implement the domain/application code and step definitions until the scenarios pass.

The `.feature` files are the single source of truth for "what are the rules for this feature" — no separate rules doc to keep in sync.
