const test = require("node:test");
const assert = require("node:assert/strict");
const metadata = require("./modsyn-language.json");
const core = require("./modsyn-completion-core");

function atMarker(markedSource) {
  const cursor = markedSource.indexOf("|");
  assert.notEqual(cursor, -1);
  return core.getCompletions(markedSource.replace("|", ""), cursor, metadata);
}

test("root property completion filters strictly to defined metadata", () => {
  const items = atMarker(
    'mod {\n    installThis: "models/example.dff"\n    target: "data/handling.cfg"\n    t|\n}',
  );
  assert.deepEqual(
    items.map((item) => item.label),
    ["type"],
  );
});

test("single-character i prefix returns only install and ignore keys", () => {
  const items = atMarker("mod { i| }");
  assert.deepEqual(
    new Set(items.map((item) => item.label)),
    new Set(["installThis", "installThese", "ignoreThis", "ignoreThese"]),
  );
});

test("used root keys are not suggested after comma or newline", () => {
  const commaSource = "mod { type: PutAndReplace, | }";
  const newlineSource = "mod {\n  type: PutAndReplace\n  |\n}";
  const commaCursor = commaSource.indexOf("|");
  const newlineCursor = newlineSource.indexOf("|");
  const commaItems = core.getCompletions(
    commaSource.replace("|", ""),
    commaCursor,
    metadata,
  );
  const newlineItems = core.getCompletions(
    newlineSource.replace("|", ""),
    newlineCursor,
    metadata,
  );
  const expected = metadata.rootProperties
    .map((property) => property.name)
    .filter((name) => name !== "type");

  assert.deepEqual(
    commaItems.map((item) => item.label),
    expected,
  );
  assert.deepEqual(
    newlineItems.map((item) => item.label),
    expected,
  );
});

test("every existing key is omitted in root and requirement objects", () => {
  const rootSource = 'mod { type: PIM, installThis: "cleo/main.cs", | }';
  const requirementSource = 'mod { require: { checkThis: "cleo.asi", | } }';
  const rootCursor = rootSource.indexOf("|");
  const requirementCursor = requirementSource.indexOf("|");
  const rootItems = core.getCompletions(
    rootSource.replace("|", ""),
    rootCursor,
    metadata,
  );
  const requirementItems = core.getCompletions(
    requirementSource.replace("|", ""),
    requirementCursor,
    metadata,
  );

  assert.equal(
    rootItems.some((item) => ["type", "installThis"].includes(item.label)),
    false,
  );
  assert.equal(
    requirementItems.some((item) => item.label === "checkThis"),
    false,
  );
});

test("empty root object contains keys only, never words from values", () => {
  const source =
    'mod { installThis: "models/example.dff" replacements: [{ source: "data/handling.cfg" target: "data/handling.cfg" }] | }';
  const cursor = source.indexOf("|");
  const items = core.getCompletions(source.replace("|", ""), cursor, metadata);
  assert.deepEqual(
    new Set(items.map((item) => item.label)),
    new Set(
      metadata.rootProperties
        .map((property) => property.name)
        .filter((name) => !["installThis", "replacements"].includes(name)),
    ),
  );
  assert.equal(
    items.some((item) =>
      [
        "models",
        "example",
        "dff",
        "data",
        "handling",
        "cfg",
        "source",
        "target",
      ].includes(item.label),
    ),
    false,
  );
});

test("type completion is metadata-backed and prefix-filtered", () => {
  const source = 'mod { type: "Pi|" }';
  const cursor = source.indexOf("|");
  const items = core.getCompletions(source.replace("|", ""), cursor, metadata);
  assert.deepEqual(
    items.map((item) => item.label),
    ["PIM", "PIC"],
  );
});

test("backup offers only its predictable literal values", () => {
  const items = atMarker("mod { type: Replacing backup: | }");
  assert.deepEqual(
    items.map((item) => item.label),
    ["true", "false", "null"],
  );
});

test("non-predictable path values offer no document-derived words", () => {
  const items = atMarker(
    'mod { installThis: "models/example.dff" ignoreThis: | }',
  );
  assert.deepEqual(items, []);
});

test("unknown root and nested keys produce diagnostics", () => {
  const diagnostics = core.getDiagnostics(
    'mod { mystery: true require: { checkFile: "x" } }',
    metadata,
  );
  assert.deepEqual(
    diagnostics.map((issue) => issue.message),
    [
      "Unknown root property 'mystery'.",
      "Unknown requirement property 'checkFile'.",
    ],
  );
});
