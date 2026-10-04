const React = require("react");
const Reconciler = require("react-reconciler");
const { DefaultEventPriority, NoEventPriority } = require("react-reconciler/constants");

let currentUpdatePriority = NoEventPriority;

function insertBeforeIn(list, child, beforeChild) {
  const index = list.indexOf(beforeChild);
  if (index === -1) list.push(child);
  else list.splice(index, 0, child);
}

const hostConfig = {
  supportsMutation: true,
  supportsPersistence: false,
  supportsHydration: false,
  isPrimaryRenderer: true,
  noTimeout: -1,

  now: Date.now,
  getCurrentEventPriority: () => DefaultEventPriority,
  getCurrentUpdatePriority: () => currentUpdatePriority,
  setCurrentUpdatePriority: (priority) => {
    currentUpdatePriority = priority;
  },
  resolveUpdatePriority: () => currentUpdatePriority || DefaultEventPriority,
  shouldAttemptEagerTransition: () => false,
  NotPendingTransition: null,
  HostTransitionContext: React.createContext(null),
  requestPostPaintCallback: (callback) => callback(),
  maySuspendCommit: () => false,
  preloadInstance: () => true,
  startSuspendingCommit: () => {},
  suspendInstance: () => {},
  waitForCommitToBeReady: () => null,
  resetFormInstance: () => {},

  getRootHostContext: () => ({}),
  getChildHostContext: (parentHostContext) => parentHostContext,
  getPublicInstance: (instance) => instance,
  prepareForCommit: () => null,
  resetAfterCommit: () => {},
  shouldSetTextContent: () => false,

  createInstance: (type, props) => ({ type, props, children: [] }),
  createTextInstance: (text) => ({ type: "#text", text }),

  appendInitialChild: (parent, child) => parent.children.push(child),
  appendChild: (parent, child) => parent.children.push(child),
  appendChildToContainer: (container, child) => container.children.push(child),

  insertBefore: (parent, child, beforeChild) => insertBeforeIn(parent.children, child, beforeChild),
  insertInContainerBefore: (container, child, beforeChild) =>
    insertBeforeIn(container.children, child, beforeChild),

  removeChild: (parent, child) => {
    const idx = parent.children.indexOf(child);
    if (idx !== -1) parent.children.splice(idx, 1);
  },
  removeChildFromContainer: (container, child) => {
    const idx = container.children.indexOf(child);
    if (idx !== -1) container.children.splice(idx, 1);
  },
  clearContainer: (container) => {
    container.children = [];
  },

  finalizeInitialChildren: () => false,
  prepareUpdate: () => true,
  commitUpdate: (instance, _type, _oldProps, newProps) => {
    instance.props = newProps;
  },
  commitTextUpdate: (textInstance, _oldText, newText) => {
    textInstance.text = newText;
  },

  scheduleTimeout: setTimeout,
  cancelTimeout: clearTimeout,

  preparePortalMount: () => {},
  detachDeletedInstance: () => {},
};

const StackDuelReconciler = Reconciler(hostConfig);

function reportFatal(error) {
  const message = error && error.stack ? error.stack : String(error);
  process.stderr.write(message + "\n");
  process.exit(1);
}

function createRoot() {
  const container = { type: "#root", children: [] };
  const root = StackDuelReconciler.createContainer(
    container,
    0,
    null,
    false,
    null,
    "stackduel-",
    reportFatal,
    reportFatal,
    reportFatal,
    null
  );
  return { container, root };
}

function render(root, element) {
  StackDuelReconciler.updateContainerSync(element, root, null, null);
  StackDuelReconciler.flushSyncWork();
}

function runSync(fn) {
  StackDuelReconciler.flushSyncFromReconciler(fn);
  StackDuelReconciler.flushSyncWork();
}

module.exports = { createRoot, render, runSync };
