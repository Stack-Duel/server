const React = require("react");
const { createRoot, render, runSync } = require("./host-config");
const { Solution } = require("./solution");

const PRESENTATIONAL_PROPS = new Set(["className", "style"]);

function serialize(node) {
  if (node.type === "#text") return node.text;

  const props = {};
  for (const [key, value] of Object.entries(node.props || {})) {
    if (key === "children") continue;
    if (typeof value === "function") continue;
    if (PRESENTATIONAL_PROPS.has(key)) continue;
    props[key] = value;
  }

  return {
    type: node.type,
    props,
    children: node.children.map(serialize),
  };
}

function findByTestId(node, testId) {
  if (node.props && node.props["data-testid"] === testId) return node;
  for (const child of node.children) {
    if (child.type === "#text") continue;
    const found = findByTestId(child, testId);
    if (found) return found;
  }
  return null;
}

const ACTION_HANDLERS = {
  click: (node) => node.props.onClick && node.props.onClick(),
  change: (node, action) =>
    node.props.onChange && node.props.onChange({ target: { value: action.value } }),
};

function main() {
  const raw = require("fs").readFileSync(0, "utf8");
  const { props = {}, actions = [] } = JSON.parse(raw || "{}");

  const { container, root } = createRoot();

  render(root, React.createElement(Solution, props));

  for (const action of actions) {
    const target = findByTestId(container, action.testId);
    if (!target) throw new Error(`No element with data-testid="${action.testId}" found`);
    const handler = ACTION_HANDLERS[action.type];
    if (!handler) throw new Error(`Unsupported action type "${action.type}"`);

    runSync(() => {
      handler(target, action);
    });
  }

  const output = container.children.map(serialize);
  process.stdout.write(JSON.stringify(output.length === 1 ? output[0] : output));
}

main();
