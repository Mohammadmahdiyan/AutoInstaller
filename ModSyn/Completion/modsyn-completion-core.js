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
  let completedType = false;

  for (let index = 0; index < tokens.length; index++) {
    const token = tokens[index];
    const previous = tokens[index - 1];
    completedType = false;
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
      frame.state = "colon";
    } else if (frame.state === "colon" && token.kind === ":") {
      frame.state = "value";
    } else if (frame.state === "value") {
      completedType =
        frame.scope === "root" &&
        frame.property === "type" &&
        (token.kind === "identifier" || token.kind === "string");
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
  if (frame.scope === "root" && completedType)
    return { kind: "type", property: "type", prefix: active.prefix };
  return {
    kind:
      frame.scope === "root"
        ? "root"
        : frame.scope === "requirement"
          ? "requirement"
          : "none",
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
    items = propertyMap(metadata, "root").map((property) =>
      item(property.name, property.name, property.description, "property"),
    );
  } else if (context.kind === "requirement") {
    items = propertyMap(metadata, "requirement").map((property) =>
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
      item("true", "true", "Enable backups.", "value"),
      item("false", "false", "Disable backups.", "value"),
      item("null", "null", "Use the default backup behavior.", "value"),
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

  function parseObject(scope, closingKind) {
    while (index < tokens.length && tokens[index].kind !== closingKind) {
      const key = tokens[index];
      if (key.kind !== "identifier" || tokens[index + 1]?.kind !== ":") {
        index++;
        continue;
      }
      index += 2;
      const allowed = propertyMap(metadata, scope);
      if (!allowed.some((property) => property.name === key.value)) {
        diagnostics.push({
          start: key.start,
          end: key.end,
          message: `Unknown ${scope} property '${key.value}'.`,
        });
      }
      parseValue(scope, key.value);
    }
    if (tokens[index]?.kind === closingKind) index++;
  }

  function parseValue(parentScope, property) {
    if (tokens[index]?.kind === "{") {
      index++;
      const nestedScope =
        property === "require" &&
        (parentScope === "root" || parentScope === "requirement")
          ? "requirement"
          : parentScope === "array-requirements"
            ? "requirement"
            : "other";
      parseObject(nestedScope, "}");
      return;
    }
    if (tokens[index]?.kind === "[") {
      index++;
      const itemScope =
        parentScope === "root" && property === "requires"
          ? "requirement"
          : parentScope === "root" && property === "replacements"
            ? "replacement"
            : null;
      while (index < tokens.length && tokens[index].kind !== "]") {
        if (tokens[index].kind === "{" && itemScope) {
          index++;
          parseObject(itemScope, "}");
        } else if (tokens[index].kind === "[") {
          parseValue("array", "");
        } else {
          index++;
        }
      }
      if (tokens[index]?.kind === "]") index++;
      return;
    }
    if (tokens[index]) index++;
  }

  if (
    tokens[0]?.kind === "identifier" &&
    tokens[0].value === "mod" &&
    tokens[1]?.kind === "{"
  ) {
    index = 2;
    parseObject("root", "}");
  }
  return diagnostics;
}

function item(label, insertText, description, kind) {
  return { label, insertText, description, kind };
}

module.exports = { detectContext, getCompletions, getDiagnostics, tokenize };
