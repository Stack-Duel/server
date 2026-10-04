const { Solution } = require("./solution");

function createElement(type) {
  const node = {
    type,
    props: {},
    children: [],
    listeners: {},
    appendChild(child) {
      node.children.push(child);
      return child;
    },
    setAttribute(name, value) {
      node.props[name] = value;
    },
    removeAttribute(name) {
      delete node.props[name];
    },
    addEventListener(eventType, handler) {
      node.listeners[eventType] = handler;
    },
    set textContent(value) {
      node.children = [{ type: "#text", text: String(value) }];
    },
    get textContent() {
      return node.children.map((c) => (c.type === "#text" ? c.text : "")).join("");
    },
    set className(value) {
      node.props.class = value;
    },
    get className() {
      return node.props.class || "";
    },
  };
  return node;
}

global.document = {
  createElement,
  createTextNode: (text) => ({ type: "#text", text: String(text) }),
};

function serialize(node) {
  if (node.type === "#text") return node.text;

  const props = {};
  for (const [key, value] of Object.entries(node.props || {})) {
    if (typeof value === "function") continue;
    props[key] = value;
  }

  return { type: node.type, props, children: node.children.map(serialize) };
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
  click: (node) => node.listeners.click && node.listeners.click(),
  change: (node, action) =>
    node.listeners.change && node.listeners.change({ target: { value: action.value } }),
};

function main() {
  const raw = require("fs").readFileSync(0, "utf8");
  const { props = {}, actions = [] } = JSON.parse(raw || "{}");

  const container = createElement("div");
  Solution(container, props);

  for (const action of actions) {
    const target = findByTestId(container, action.testId);
    if (!target) throw new Error(`No element with data-testid="${action.testId}" found`);
    const handler = ACTION_HANDLERS[action.type];
    if (!handler) throw new Error(`Unsupported action type "${action.type}"`);
    handler(target, action);
  }

  const output = container.children.map(serialize);
  process.stdout.write(JSON.stringify(output.length === 1 ? output[0] : output));
}

main();
