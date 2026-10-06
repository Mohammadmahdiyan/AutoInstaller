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

test("addToUserFile objects complete only their source and destination keys", () => {
  const items = atMarker('mod { type: DSL addToUserFile: [{ from: "JLNSJ", | }] }');
  assert.deepEqual(
    items.map((item) => item.label),
    ["fromBase", "to"],
  );

  const sourceValue = atMarker('mod { type: DSL addToUserFile: [{ from: "JL|" }] }');
  assert.deepEqual(sourceValue, []);
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

test("backup offers only all, none, and some", () => {
  const items = atMarker("mod { type: Replacing backup: | }");
  assert.deepEqual(
    items.map((item) => item.label),
    ["all", "none", "some"],
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

test("addToUserFile diagnostics require one non-empty relative source", () => {
  const diagnostics = core.getDiagnostics(
    'mod { type: DSL addToUserFile: [{ from: "" fromBase: "../bad" to: "../outside" }] }',
    metadata,
  );
  assert.equal(
    diagnostics.some((issue) => issue.message.includes("exactly one of 'from' or 'fromBase'")),
    true,
  );
  assert.equal(diagnostics.some((issue) => issue.message.includes("non-empty relative path")), true);
  assert.equal(diagnostics.some((issue) => issue.message.includes("Property 'to' requires a relative path")), true);
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
  assert.equal(incompleteDuplicate.length, 2);
  assert.equal(incompleteDuplicate[0].code, "duplicate-property");
  assert.equal(incompleteDuplicate[1].code, "missing-value");
  assert.match(
    incompleteDuplicate[0].message,
    /Duplicate root property 'type'/,
  );
});

test("diagnostics reject unsupported type and backup modes", () => {
  const diagnostics = core.getDiagnostics(
    "mod { type: UnknownType backup: maybe }",
    metadata,
  );

  assert.deepEqual(
    diagnostics.map((issue) => issue.message.split(" (line")[0]),
    [
      "Unsupported package type 'UnknownType'.",
      "Unsupported backup value 'maybe'. Expected all, none, or some.",
    ],
  );
  assert.deepEqual(
    diagnostics.map((issue) => [issue.line, issue.column]),
    [
      [1, 13],
      [1, 33],
    ],
  );
});

test("diagnostics enforce This/These shapes and string-only array items", () => {
  const diagnostics = core.getDiagnostics(
    'mod { type: Replacing backup: some installThis: true installThese: "one" dontBackupThese: ["a" false] }',
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

test("backup some is accepted with a populated selector", () => {
  assert.deepEqual(
    core.getDiagnostics(
      'mod { type: Replacing backup: some backupThis: "data/file.dat" }',
      metadata,
    ),
    [],
  );
});

test("all and none stand alone; selectors are exclusive to some", () => {
  assert.deepEqual(
    core.getDiagnostics("mod { type: Replacing backup: all }", metadata),
    [],
  );
  assert.deepEqual(
    core.getDiagnostics("mod { type: Replacing backup: none }", metadata),
    [],
  );

  for (const source of [
    'mod { type: Replacing backup: all backupThis: "one" }',
    'mod { type: Replacing backup: none dontBackupThis: "cache" }',
    'mod { type: Replacing backupThis: "one" }',
  ]) {
    assert.ok(
      core
        .getDiagnostics(source, metadata)
        .some((issue) => issue.code === "backup-selector-requires-some"),
      source,
    );
  }
});

test("missing type defaults but explicit empty and unsupported types fail", () => {
  assert.deepEqual(core.getDiagnostics("mod {}", metadata), []);
  assert.match(
    core.getDiagnostics('mod { type: "" }', metadata)[0].message,
    /Unsupported package type ''/,
  );
  assert.match(
    core.getDiagnostics("mod { type: NotASupportedType }", metadata)[0].message,
    /Unsupported package type 'NotASupportedType'/,
  );
});

test("backup some requires a selector with a non-empty value", () => {
  const cases = [
    "mod { type: Replacing backup: some backupThis: }",
    'mod { type: Replacing backup: some backupThis: "" }',
    "mod { type: Replacing backup: some backupThese: [] }",
  ];
  for (const source of cases) {
    assert.ok(
      core
        .getDiagnostics(source, metadata)
        .some((issue) => issue.code === "backup-some-without-selection"),
      source,
    );
  }

  for (const source of [
    'mod { type: Replacing backup: some backupThis: "data/file.dat" }',
    'mod { type: Replacing backup: some backupThese: ["data/file.dat" "another"] }',
  ]) {
    assert.equal(
      core
        .getDiagnostics(source, metadata)
        .some((issue) => issue.code === "backup-some-without-selection"),
      false,
      source,
    );
  }
});

test("backup This requires a non-empty string and These requires a non-empty string array", () => {
  const invalidSources = [
    'mod { type: Replacing backup: some backupThis: "" }',
    "mod { type: Replacing backup: some dontBackupThis: }",
    "mod { type: Replacing backup: some backupThese: [] }",
    'mod { type: Replacing backup: some dontBackupThese: [""] }',
  ];
  for (const source of invalidSources) {
    assert.ok(core.getDiagnostics(source, metadata).length > 0, source);
  }

  assert.deepEqual(
    core.getDiagnostics(
      'mod { type: Replacing backup: some backupThese: ["one" "two"] }',
      metadata,
    ),
    [],
  );
});

test("These with one path is a warning recommending This", () => {
  const diagnostics = core.getDiagnostics(
    'mod { type: Replacing backup: some backupThese: ["data/file.dat"] dontBackupThese: ["cache"] }',
    metadata,
  );

  assert.equal(diagnostics.length, 2);
  assert.ok(diagnostics.every((issue) => issue.severity === "warning"));
  assert.match(diagnostics[0].message, /use 'backupThis' instead/);
  assert.match(diagnostics[1].message, /use 'dontBackupThis' instead/);
});
