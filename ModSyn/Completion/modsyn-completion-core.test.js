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
      "Unknown root property 'mystery'. (line 1, column 7).",
      "Unknown requirement property 'checkFile'. (line 1, column 32).",
    ],
  );
});

test("unknown keys suggest a unique close key in root and nested scopes", () => {
  const diagnostics = core.getDiagnostics(
    'mod { instalThese: [] require: { checkThes: ["x"] } }',
    metadata,
  );

  assert.deepEqual(
    diagnostics.map((issue) => issue.message.split(" (line")[0]),
    [
      "Unknown root property 'instalThese'. Did you mean 'installThese'?",
      "Unknown requirement property 'checkThes'. Did you mean 'checkThese'?",
    ],
  );
});

test("duplicate keys are errors, including an incomplete repeated key", () => {
  const diagnostics = core.getDiagnostics(
    'mod { type: PIM type: DSL require: { reqPath: "one" reqPath: "two" } }',
    metadata,
  );

  assert.deepEqual(
    diagnostics
      .filter((issue) => issue.code === "duplicate-property")
      .map((issue) => issue.message.split(" (line")[0]),
    [
      "Duplicate root property 'type'.",
      "Duplicate requirement property 'reqPath'.",
    ],
  );

  const incompleteDuplicate = core.getDiagnostics(
    "mod { type: PIM, type }",
    metadata,
  );
  assert.equal(incompleteDuplicate.length, 1);
  assert.equal(incompleteDuplicate[0].code, "duplicate-property");
  assert.match(
    incompleteDuplicate[0].message,
    /Duplicate root property 'type'/,
  );
});

test("diagnostics reject unsupported type, backup literals, and backup null without selectors", () => {
  const diagnostics = core.getDiagnostics(
    'mod { type: UnknownType backup: "yes" }',
    metadata,
  );

  assert.deepEqual(
    diagnostics.map((issue) => issue.message.split(" (line")[0]),
    [
      "Unsupported package type 'UnknownType'.",
      "Property 'backup' expects boolean or null, but found string.",
    ],
  );
  assert.deepEqual(
    diagnostics.map((issue) => [issue.line, issue.column]),
    [
      [1, 13],
      [1, 33],
    ],
  );

  const nullBackupDiagnostics = core.getDiagnostics(
    "mod { type: Replacing backup: null }",
    metadata,
  );
  assert.deepEqual(
    nullBackupDiagnostics.map((issue) => issue.message.split(" (line")[0]),
    [
      "Property 'backup' cannot be null unless at least one of backupThis, backupThese, dontBackupThis, or dontBackupThese is provided.",
    ],
  );
});

test("diagnostics enforce This/These shapes and string-only array items", () => {
  const diagnostics = core.getDiagnostics(
    'mod { type: Replacing installThis: true installThese: "one" dontBackupThese: ["a" false] }',
    metadata,
  );

  assert.deepEqual(
    diagnostics.map((issue) => issue.message.split(" (line")[0]),
    [
      "Property 'installThis' expects string, but found boolean.",
      "Property 'installThese' expects array, but found string.",
      "Array item for 'dontBackupThese' expects string, but found boolean.",
    ],
  );
});

test("backup null is accepted with a backup selection key for replacement types", () => {
  assert.deepEqual(
    core.getDiagnostics(
      "mod { type: Replacing backup: null backupThese: [] }",
      metadata,
    ),
    [],
  );
});

test("empty type uses the default without unrelated semantic errors", () => {
  assert.deepEqual(core.getDiagnostics('mod { type: "" }', metadata), []);
});
