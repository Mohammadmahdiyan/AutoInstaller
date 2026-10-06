function tokenize(source) {
  const tokens = [];
  let index = 0;
  while (index < source.length) {
    const character = source[index];
    if (/\s/.test(character)) {
      index++;
      continue;
    }

    const start = index;
    if (character === '"') {
      index++;
      const valueStart = index;
      while (
        index < source.length &&
        source[index] !== '"' &&
        source[index] !== "\r" &&
        source[index] !== "\n"
      ) {
        index++;
      }
      const closed = source[index] === '"';
      const value = source.slice(valueStart, index);
      if (closed) {
        index++;
      }
      tokens.push({ kind: "string", value, start, end: index, closed });
      continue;
    }

    if (/[A-Za-z_]/.test(character)) {
      index++;
      while (index < source.length && /[A-Za-z0-9_]/.test(source[index])) {
        index++;
      }
      const value = source.slice(start, index);
      tokens.push({
        kind:
          value === "true" || value === "false" || value === "null"
            ? value
            : "identifier",
        value,
        start,
        end: index,
      });
      continue;
    }

    if ("{}[]:,".includes(character)) {
      tokens.push({ kind: character, value: character, start, end: ++index });
      continue;
    }

    index++;
  }
  return tokens;
}

function propertyMap(metadata, scope) {
  if (scope === "root") return metadata.rootProperties;
  if (scope === "requirement") return metadata.requirementProperties;
  if (scope === "replacement") return metadata.replacementProperties;
  if (scope === "userFile") return metadata.addToUserFileProperties;
  return [];
}

function propertyDefinition(metadata, scope, name) {
  return propertyMap(metadata, scope).find(
    (property) => property.name === name,
  );
}

function activePrefix(source, cursorOffset) {
  const prefixSource = source.slice(0, cursorOffset);
  const tokens = tokenize(prefixSource);
  const last = tokens[tokens.length - 1];
  if (last && last.kind === "string" && !last.closed) {
    return { source: prefixSource.slice(0, last.start), prefix: last.value };
  }
  if (last && last.kind === "identifier" && last.end === cursorOffset) {
    return { source: prefixSource.slice(0, last.start), prefix: last.value };
  }
  return { source: prefixSource, prefix: "" };
}

function detectContext(source, cursorOffset, metadata) {
  const active = activePrefix(source, cursorOffset);
  const tokens = tokenize(active.source);
  const stack = [];
  let sawMod = false;

  for (let index = 0; index < tokens.length; index++) {
    const token = tokens[index];
    const previous = tokens[index - 1];
    if (stack.length === 0) {
      if (token.kind === "identifier" && token.value === "mod") sawMod = true;
      else if (token.kind === "{" && sawMod && previous?.value === "mod")
        stack.push({ kind: "object", scope: "root", state: "key" });
      continue;
    }

    let frame = stack[stack.length - 1];
    if (token.kind === "{") {
      const scope =
        frame.kind === "array" && frame.itemScope
          ? frame.itemScope
          : frame.kind === "object" &&
              frame.state === "value" &&
              frame.property === "require"
            ? "requirement"
            : "other";
      stack.push({ kind: "object", scope, state: "key" });
      completeParentValue(stack, frame);
      continue;
    }
    if (token.kind === "[") {
      const definition =
        frame.kind === "object"
          ? propertyDefinition(metadata, frame.scope, frame.property)
          : undefined;
      const itemScope =
        frame.scope === "root" && frame.property === "requires"
          ? "requirement"
          : frame.scope === "root" && frame.property === "replacements"
            ? "replacement"
            : frame.scope === "root" && frame.property === "addToUserFile"
              ? "userFile"
            : null;
      stack.push({
        kind: "array",
        itemScope,
        itemKinds: definition?.arrayItemKinds ?? [],
      });
      completeParentValue(stack, frame);
      continue;
    }
    if (token.kind === "}" || token.kind === "]") {
      if (stack.length > 1) {
        stack.pop();
        completeParentValue(stack, stack[stack.length - 1]);
      } else if (token.kind === "}") {
        stack.length = 0;
      }
      continue;
    }

    frame = stack[stack.length - 1];
    if (frame.kind === "array") continue;
    if (token.kind === ",") {
      frame.state = "key";
      frame.property = undefined;
    } else if (frame.state === "key" && token.kind === "identifier") {
      frame.property = token.value;
      frame.properties ??= new Set();
      frame.properties.add(token.value);
      frame.state = "colon";
    } else if (frame.state === "colon" && token.kind === ":") {
      frame.state = "value";
    } else if (frame.state === "value") {
      frame.state = "key";
      frame.property = undefined;
    }
  }

  if (!stack.length) return { kind: "none", prefix: active.prefix };
  const frame = stack[stack.length - 1];
  if (frame.kind === "array") {
    return {
      kind: frame.itemKinds.includes("String") ? "path" : "none",
      property: undefined,
      prefix: active.prefix,
    };
  }
  const usedProperties = frame.properties ?? new Set();
  if (frame.state === "value" && frame.property) {
    if (frame.scope === "root" && frame.property === "type")
      return { kind: "type", property: frame.property, prefix: active.prefix };
    const definition = propertyDefinition(
      metadata,
      frame.scope,
      frame.property,
    );
    if (definition?.allowedValueKinds.includes("String"))
      return { kind: "path", property: frame.property, prefix: active.prefix };
    return { kind: "value", property: frame.property, prefix: active.prefix };
  }
  return {
    kind:
      frame.scope === "root"
        ? "root"
        : frame.scope === "requirement"
          ? "requirement"
            : frame.scope === "userFile"
              ? "userFile"
          : "none",
    usedProperties,
    prefix: active.prefix,
  };
}

function completeParentValue(stack, parent) {
  if (stack.length > 1 && parent.kind === "object") {
    parent.state = "key";
    parent.property = undefined;
  }
}

function getCompletions(source, cursorOffset, metadata) {
  const context = detectContext(source, cursorOffset, metadata);
  let items = [];
  if (context.kind === "root") {
    items = propertyMap(metadata, "root")
      .filter((property) => !context.usedProperties?.has(property.name))
      .map((property) =>
        item(property.name, property.name, property.description, "property"),
      );
  } else if (context.kind === "requirement") {
    items = propertyMap(metadata, "requirement")
      .filter((property) => !context.usedProperties?.has(property.name))
      .map((property) =>
        item(property.name, property.name, property.description, "property"),
      );
  } else if (context.kind === "userFile") {
    items = propertyMap(metadata, "userFile")
      .filter((property) => !context.usedProperties?.has(property.name))
      .map((property) =>
        item(property.name, property.name, property.description, "property"),
      );
  } else if (context.kind === "type") {
    items = metadata.types.flatMap((type) => [
      item(type.name, type.name, type.description, "type"),
      ...type.aliases.map((alias) =>
        item(alias, alias, type.description, "type"),
      ),
    ]);
  } else if (context.kind === "value" && context.property === "backup") {
    items = [
      item("all", "all", "Back up all files.", "value"),
      item("none", "none", "Do not back up files.", "value"),
      item("some", "some", "Back up only selected paths.", "value"),
    ];
  }
  return items.filter((candidate) =>
    candidate.label.toLowerCase().startsWith(context.prefix.toLowerCase()),
  );
}

function getDiagnostics(source, metadata) {
  const tokens = tokenize(source);
  const diagnostics = [];
  let index = 0;

  function addDiagnostic(
    token,
    message,
    code = "unknown-property",
    severity = "error",
  ) {
    const before = source.slice(0, token.start);
    const lines = before.split(/\r\n|\r|\n/);
    diagnostics.push({
      start: token.start,
      end: token.end,
      line: lines.length,
      column: lines[lines.length - 1].length + 1,
      message: `${message} (line ${lines.length}, column ${lines[lines.length - 1].length + 1}).`,
      code,
      severity,
    });
  }

  function findClosestPropertyName(propertyName, definitions) {
    const normalizedName = propertyName.toLowerCase();
    const maximumDistance = Math.max(1, Math.floor(normalizedName.length / 4));
    const closest = definitions
      .map((definition) => ({
        name: definition.name,
        distance: getEditDistance(
          normalizedName,
          definition.name.toLowerCase(),
        ),
        commonPrefixLength: getCommonPrefixLength(
          normalizedName,
          definition.name.toLowerCase(),
        ),
      }))
      .filter((candidate) => candidate.distance <= maximumDistance)
      .sort(
        (left, right) =>
          left.distance - right.distance ||
          right.commonPrefixLength - left.commonPrefixLength,
      );

    if (
      closest.length === 0 ||
      (closest.length > 1 &&
        closest[0].distance === closest[1].distance &&
        closest[0].commonPrefixLength === closest[1].commonPrefixLength)
    ) {
      return undefined;
    }
    return closest[0].name;
  }

  function getEditDistance(left, right) {
    const distances = Array.from({ length: left.length + 1 }, () => []);
    for (let row = 0; row <= left.length; row++) distances[row][0] = row;
    for (let column = 0; column <= right.length; column++)
      distances[0][column] = column;

    for (let row = 1; row <= left.length; row++) {
      for (let column = 1; column <= right.length; column++) {
        const substitutionCost = left[row - 1] === right[column - 1] ? 0 : 1;
        distances[row][column] = Math.min(
          distances[row - 1][column] + 1,
          distances[row][column - 1] + 1,
          distances[row - 1][column - 1] + substitutionCost,
        );
      }
    }
    return distances[left.length][right.length];
  }

  function getCommonPrefixLength(left, right) {
    let index = 0;
    while (
      index < left.length &&
      index < right.length &&
      left[index] === right[index]
    ) {
      index++;
    }
    return index;
  }

  function valueKind(node) {
    if (node.kind === "string") return "String";
    if (node.kind === "true" || node.kind === "false") return "Boolean";
    if (node.kind === "null") return "Null";
    if (node.kind === "identifier") return "Identifier";
    if (node.kind === "Array") return "Array";
    if (node.kind === "Object") return "Object";
    return "Unknown";
  }

  function normalizeType(value) {
    return value
      .trim()
      .replace(/[\s_-]/g, "")
      .toLowerCase();
  }

  function isSupportedType(value) {
    const key = normalizeType(value);
    return metadata.types.some((type) =>
      [type.name, ...type.aliases].some(
        (candidate) => normalizeType(candidate) === key,
      ),
    );
  }

  function parseValue() {
    const token = tokens[index];
    if (!token)
      return {
        kind: "Unknown",
        token: tokens[tokens.length - 1],
        value: undefined,
      };
    if (token.kind === "{") {
      index++;
      const object = parseObject("}");
      object.token = token;
      return object;
    }
    if (token.kind === "[") {
      index++;
      const items = [];
      while (index < tokens.length && tokens[index].kind !== "]") {
        if (tokens[index].kind === ",") {
          index++;
          continue;
        }
        items.push(parseValue());
      }
      if (tokens[index]?.kind === "]") index++;
      return { kind: "Array", token, items };
    }
    index++;
    return { kind: token.kind, token, value: token.value };
  }

  function validateProperty(property, definition, scope) {
    const kind = valueKind(property.value);
    if (!definition.allowedValueKinds.includes(kind)) {
      addDiagnostic(
        property.value.token,
        `Property '${property.name}' expects ${definition.allowedValueKinds.map((value) => metadata.valueKinds.find((entry) => entry.kind === value)?.name ?? value).join(" or ")}, but found ${metadata.valueKinds.find((entry) => entry.kind === kind)?.name ?? kind}.`,
      );
      return;
    }

    if (kind === "Array" && definition.arrayItemKinds.length) {
      for (const itemNode of property.value.items) {
        const itemKind = valueKind(itemNode);
        if (!definition.arrayItemKinds.includes(itemKind)) {
          addDiagnostic(
            itemNode.token,
            `Array item for '${property.name}' expects ${definition.arrayItemKinds.map((value) => metadata.valueKinds.find((entry) => entry.kind === value)?.name ?? value).join(" or ")}, but found ${metadata.valueKinds.find((entry) => entry.kind === itemKind)?.name ?? itemKind}.`,
          );
        }
      }
    }

    if (scope === "root" && property.name === "type") {
      const typeValue = property.value.value;
      if (
        (property.value.kind === "string" ||
          property.value.kind === "identifier") &&
        !isSupportedType(typeValue)
      ) {
        addDiagnostic(
          property.value.token,
          `Unsupported package type '${typeValue ?? ""}'.`,
        );
      }
    }

    if (
      scope === "root" &&
      property.name === "backup" &&
      !["all", "none", "some"].includes(
        String(property.value.value).toLowerCase(),
      )
    ) {
      addDiagnostic(
        property.value.token,
        `Unsupported backup value '${property.value.value}'. Expected all, none, or some.`,
        "unsupported-backup-mode",
      );
    }

    if (property.value.kind === "Object") {
      const nestedScope =
        property.name === "require" &&
        (scope === "root" || scope === "requirement")
          ? "requirement"
          : "other";
      validateObject(property.value, nestedScope);
    } else if (property.value.kind === "Array") {
      for (const itemNode of property.value.items) {
        if (itemNode.kind !== "Object") continue;
        const nestedScope =
          scope === "root" && property.name === "requires"
            ? "requirement"
            : scope === "root" && property.name === "replacements"
              ? "replacement"
              : scope === "root" && property.name === "addToUserFile"
                ? "userFile"
              : "other";
        validateObject(itemNode, nestedScope);
      }
    }
  }

  function validateObject(objectNode, scope) {
    const definitions = propertyMap(metadata, scope);
    const seenProperties = new Set();
    for (const property of objectNode.properties) {
      if (seenProperties.has(property.name)) {
        addDiagnostic(
          property.token,
          `Duplicate ${scope} property '${property.name}'.`,
          "duplicate-property",
        );
      }
      seenProperties.add(property.name);

      const definition = definitions.find(
        (candidate) => candidate.name === property.name,
      );
      if (!definition) {
        const suggestion = findClosestPropertyName(property.name, definitions);
        addDiagnostic(
          property.token,
          suggestion
            ? `Unknown ${scope} property '${property.name}'. Did you mean '${suggestion}'?`
            : `Unknown ${scope} property '${property.name}'.`,
        );
        continue;
      }
      if (property.incomplete || property.incompleteValue) {
        const message = property.incomplete
          ? `Property '${property.name}' is missing ':' and a value.`
          : `Missing value for property '${property.name}'.`;
        addDiagnostic(property.token, message, "missing-value");
        continue;
      }
      validateProperty(property, definition, scope);
    }

    if (scope === "replacement") {
      for (const requiredName of ["source", "target"]) {
        if (
          !objectNode.properties.some(
            (property) =>
              property.name === requiredName &&
              property.value.kind === "string",
          )
        ) {
          addDiagnostic(
            objectNode.token,
            `Replacement object requires a '${requiredName}' string property.`,
          );
        }
      }
    }

    if (scope === "userFile") {
      const sources = objectNode.properties.filter((property) =>
        ["from", "fromBase"].includes(property.name),
      );
      if (sources.length !== 1) {
        addDiagnostic(
          objectNode.token,
          "Each addToUserFile entry requires exactly one of 'from' or 'fromBase'.",
        );
      }
      for (const property of [...sources, ...objectNode.properties.filter((item) => item.name === "to")]) {
        if (property.value.kind !== "string") continue;
        const value = property.value.value;
        const normalized = value.replace(/\\/g, "/");
        const rooted = /^(?:[a-z]:|\/|\\\\)/i.test(value);
        if (property.name !== "to" && value.trim() === "") {
          addDiagnostic(
            property.value.token,
            `Property '${property.name}' requires a non-empty relative path without '..'.`,
          );
        } else if (rooted || normalized.split("/").includes("..")) {
          addDiagnostic(
            property.value.token,
            property.name === "to"
              ? "Property 'to' requires a relative path without '..'."
              : `Property '${property.name}' requires a non-empty relative path without '..'.`,
          );
        }
      }
    }

    if (scope !== "root") return;
    const typeProperty = objectNode.properties.find(
      (property) => property.name === "type",
    );
    const rawType = typeProperty?.value.value;
    const matchedType =
      typeof rawType === "string" && rawType.trim() !== ""
        ? metadata.types.find((type) =>
            [type.name, ...type.aliases].some(
              (candidate) =>
                normalizeType(candidate) === normalizeType(rawType),
            ),
          )
        : null;
    const typeIsValid = !typeProperty || matchedType !== undefined;
    const resolvedType = !typeProperty
      ? metadata.defaultTypeName
      : matchedType?.name;
    const backupSelectorNames = [
      "backupThis",
      "backupThese",
      "dontBackupThis",
      "dontBackupThese",
    ];
    for (const property of objectNode.properties) {
      if (["backupThis", "dontBackupThis"].includes(property.name)) {
        if (
          property.value.kind === "string" &&
          property.value.value.trim() === ""
        ) {
          addDiagnostic(
            property.value.token,
            `Property '${property.name}' requires a non-empty string path.`,
            "empty-backup-path",
          );
        }
        continue;
      }

      if (
        !["backupThese", "dontBackupThese"].includes(property.name) ||
        property.value.kind !== "Array"
      ) {
        continue;
      }

      if (property.value.items.length === 0) {
        addDiagnostic(
          property.value.token,
          `Property '${property.name}' requires at least one string path.`,
          "empty-backup-list",
        );
        continue;
      }

      for (const itemNode of property.value.items) {
        if (itemNode.kind === "string" && itemNode.value.trim() === "") {
          addDiagnostic(
            itemNode.token,
            `Property '${property.name}' cannot contain an empty string path.`,
            "empty-backup-path",
          );
        }
      }

      if (
        property.value.items.length === 1 &&
        property.value.items[0].kind === "string" &&
        property.value.items[0].value.trim() !== ""
      ) {
        const singularName =
          property.name === "backupThese" ? "backupThis" : "dontBackupThis";
        addDiagnostic(
          property.value.token,
          `Property '${property.name}' contains one path; use '${singularName}' instead.`,
          "single-value-array",
          "warning",
        );
      }
    }
    for (const property of objectNode.properties) {
      const definition = definitions.find(
        (candidate) => candidate.name === property.name,
      );
      if (
        typeIsValid &&
        definition?.applicableTypes.length &&
        !definition.applicableTypes.includes(resolvedType)
      ) {
        addDiagnostic(
          property.token,
          `Property '${property.name}' is only valid for types: ${definition.applicableTypes.join(", ")}.`,
        );
      }
    }
    const backupProperty = objectNode.properties.find(
      (property) => property.name === "backup",
    );
    const backupMode = backupProperty
      ? String(backupProperty.value.value).toLowerCase()
      : "all";
    const backupModeIsValid = ["all", "none", "some"].includes(backupMode);
    const selectors = objectNode.properties.filter((property) =>
      backupSelectorNames.includes(property.name),
    );
    if (backupModeIsValid && backupMode !== "some") {
      for (const property of selectors) {
        addDiagnostic(
          property.token,
          `Property '${property.name}' requires backup: some.`,
          "backup-selector-requires-some",
        );
      }
    }

    const hasSelectorValue = selectors.some((property) =>
      property.name.endsWith("This")
        ? property.value.kind === "string" && property.value.value.trim() !== ""
        : property.value.kind === "Array" &&
          property.value.items.some(
            (itemNode) =>
              itemNode.kind === "string" && itemNode.value.trim() !== "",
          ),
    );
    if (backupModeIsValid && backupMode === "some" && !hasSelectorValue) {
      addDiagnostic(
        backupProperty?.value.token ?? objectNode.token,
        "backup: some requires at least one of backupThis, backupThese, dontBackupThis, or dontBackupThese with a value.",
        "backup-some-without-selection",
      );
    }
  }

  function parseObject(closingKind) {
    const properties = [];
    while (index < tokens.length && tokens[index].kind !== closingKind) {
      if (tokens[index].kind === ",") {
        index++;
        continue;
      }
      const key = tokens[index];
      if (key.kind !== "identifier") {
        index++;
        continue;
      }
      if (tokens[index + 1]?.kind !== ":") {
        properties.push({
          name: key.value,
          token: key,
          value: { kind: "Unknown", token: key },
          incomplete: true,
        });
        index++;
        continue;
      }
      index += 2;
      const nextToken = tokens[index];
      const missingValue =
        !nextToken ||
        nextToken.kind === closingKind ||
        nextToken.kind === "," ||
        (nextToken.kind === "identifier" && tokens[index + 1]?.kind === ":");
      if (missingValue) {
        properties.push({
          name: key.value,
          token: key,
          value: { kind: "Unknown", token: key },
          incompleteValue: true,
        });
        continue;
      }
      properties.push({ name: key.value, token: key, value: parseValue() });
    }
    if (tokens[index]?.kind === closingKind) index++;
    return {
      kind: "Object",
      token: tokens[index - 1] ?? tokens[0],
      properties,
    };
  }

  if (
    tokens[0]?.kind === "identifier" &&
    tokens[0].value === "mod" &&
    tokens[1]?.kind === "{"
  ) {
    index = 2;
    validateObject(parseObject("}"), "root");
  }
  return diagnostics;
}

function item(label, insertText, description, kind) {
  return { label, insertText, description, kind };
}

module.exports = { detectContext, getCompletions, getDiagnostics, tokenize };
