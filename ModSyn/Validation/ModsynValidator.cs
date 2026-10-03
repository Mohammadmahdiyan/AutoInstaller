using GtaSaModManager.Modsyn.Ast;
using GtaSaModManager.Modsyn.Lexer;

namespace GtaSaModManager.Modsyn.Validation;

public static class ModsynValidator
{
    public static ModsynValidationResult Validate(ModsynDocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var errors = new List<ModsynValidationError>();
        var warnings = new List<ModsynValidationWarning>();
        ValidatePropertySet(document.Body, ModsynLanguageDefinition.RootProperties, "root-level", errors);

        var (normalizedType, typeIsValid) = ResolveType(FindProperty(document.Body, "type"), errors);
        if (typeIsValid)
        {
            ValidateBackupApplicability(document.Body, normalizedType, errors);
        }
        var (backupMode, backupModeIsValid) = ResolveBackupMode(document.Body, errors);
        ValidateBackupSelectors(document.Body, backupMode, backupModeIsValid, errors, warnings);

        var requirements = new List<ModsynResolvedRequirement>();
        foreach (var property in document.Body.Properties)
        {
            if (property.Name == "require" && property.Value is ModsynObjectNode requirementObject)
            {
                requirements.Add(ValidateRequirement(requirementObject, errors));
            }
            else if (property.Name == "requires" && property.Value is ModsynArrayNode requirementArray)
            {
                foreach (var item in requirementArray.Items.OfType<ModsynObjectNode>())
                {
                    requirements.Add(ValidateRequirement(item, errors));
                }
            }
            else if (property.Name == "replacements" && property.Value is ModsynArrayNode replacements)
            {
                foreach (var replacement in replacements.Items.OfType<ModsynObjectNode>())
                {
                    ValidateReplacement(replacement, errors);
                }
            }
        }

        return new ModsynValidationResult(
            normalizedType,
            backupMode,
            requirements.AsReadOnly(),
            errors.AsReadOnly(),
            warnings.AsReadOnly());
    }

    private static (string TypeName, bool IsValid) ResolveType(
        ModsynPropertyNode? typeProperty,
        List<ModsynValidationError> errors)
    {
        if (typeProperty is null)
        {
            return (ModsynLanguageDefinition.DefaultTypeName, true);
        }

        var rawType = typeProperty.Value switch
        {
            ModsynStringNode stringNode => stringNode.Value,
            ModsynIdentifierNode identifierNode => identifierNode.Name,
            _ => null
        };

        if (rawType is null)
        {
            return (ModsynLanguageDefinition.DefaultTypeName, false);
        }

        if (string.IsNullOrWhiteSpace(rawType))
        {
            errors.Add(new ModsynValidationError("Unsupported package type ''.", typeProperty.Value.Location));
            return (ModsynLanguageDefinition.DefaultTypeName, false);
        }

        if (ModsynLanguageDefinition.TryResolveType(rawType, out var type) && type is not null)
        {
            return (type.Name, true);
        }

        errors.Add(new ModsynValidationError($"Unsupported package type '{rawType}'.", typeProperty.Value.Location));
        return (ModsynLanguageDefinition.DefaultTypeName, false);
    }

    private static void ValidateBackupApplicability(
        ModsynObjectNode root,
        string normalizedType,
        List<ModsynValidationError> errors)
    {
        foreach (var property in root.Properties)
        {
            if (!ModsynLanguageDefinition.RootProperties.TryGetValue(property.Name, out var definition)
                || definition.ApplicableTypes.Count == 0
                || definition.ApplicableTypes.Contains(normalizedType, StringComparer.Ordinal))
            {
                continue;
            }

            errors.Add(new ModsynValidationError(
                $"Property '{property.Name}' is only valid for types: {string.Join(", ", definition.ApplicableTypes)}.",
                property.Location));
        }
    }

    private static (ModsynBackupMode Mode, bool IsValid) ResolveBackupMode(
        ModsynObjectNode root,
        List<ModsynValidationError> errors)
    {
        var backup = FindProperty(root, "backup");
        if (backup is null)
        {
            return (ModsynBackupMode.All, true);
        }

        if (backup.Value is not ModsynIdentifierNode identifier)
        {
            return (ModsynBackupMode.All, false);
        }

        switch (identifier.Name.ToLowerInvariant())
        {
            case "all":
                return (ModsynBackupMode.All, true);
            case "none":
                return (ModsynBackupMode.None, true);
            case "some":
                return (ModsynBackupMode.Some, true);
            default:
                errors.Add(new ModsynValidationError(
                    $"Unsupported backup value '{identifier.Name}'. Expected all, none, or some.",
                    identifier.Location));
                return (ModsynBackupMode.All, false);
        }
    }

            private static void ValidateBackupSelectors(
                ModsynObjectNode root,
                ModsynBackupMode backupMode,
                bool backupModeIsValid,
                List<ModsynValidationError> errors,
                List<ModsynValidationWarning> warnings)
            {
                var selectorProperties = root.Properties
                    .Where(property => property.Name is "backupThis" or "backupThese" or "dontBackupThis" or "dontBackupThese")
                    .ToList();
                if (backupModeIsValid && backupMode != ModsynBackupMode.Some)
                {
                    foreach (var property in selectorProperties)
                    {
                        errors.Add(new ModsynValidationError(
                            $"Property '{property.Name}' requires backup: some.",
                            property.Location));
                    }
                }

                foreach (var property in root.Properties)
                {
                    if (property.Name is "backupThis" or "dontBackupThis"
                        && property.Value is ModsynStringNode singlePath
                        && string.IsNullOrWhiteSpace(singlePath.Value))
                    {
                        errors.Add(new ModsynValidationError(
                            $"Property '{property.Name}' requires a non-empty string path.",
                            singlePath.Location));
                    }

                    if (property.Name is not ("backupThese" or "dontBackupThese")
                        || property.Value is not ModsynArrayNode paths)
                    {
                        continue;
                    }

                    if (paths.Items.Count == 0)
                    {
                        errors.Add(new ModsynValidationError(
                            $"Property '{property.Name}' requires at least one string path.",
                            paths.Location));
                        continue;
                    }

                    foreach (var item in paths.Items.OfType<ModsynStringNode>().Where(item => string.IsNullOrWhiteSpace(item.Value)))
                    {
                        errors.Add(new ModsynValidationError(
                            $"Property '{property.Name}' cannot contain an empty string path.",
                            item.Location));
                    }

                    if (paths.Items.Count == 1 && paths.Items[0] is ModsynStringNode onlyPath
                        && !string.IsNullOrWhiteSpace(onlyPath.Value))
                    {
                        var singularName = property.Name == "backupThese" ? "backupThis" : "dontBackupThis";
                        warnings.Add(new ModsynValidationWarning(
                            $"Property '{property.Name}' contains one path; use '{singularName}' instead.",
                            paths.Location));
                    }
                }

                var hasSelectorValue = selectorProperties.Any(property => property.Name switch
                {
                    "backupThis" or "dontBackupThis" => property.Value is ModsynStringNode value
                        && !string.IsNullOrWhiteSpace(value.Value),
                    "backupThese" or "dontBackupThese" => property.Value is ModsynArrayNode array
                        && array.Items.OfType<ModsynStringNode>().Any(item => !string.IsNullOrWhiteSpace(item.Value)),
                    _ => false
                });
                if (backupModeIsValid && backupMode == ModsynBackupMode.Some && !hasSelectorValue)
                {
                    var location = FindProperty(root, "backup")?.Value.Location ?? root.Location;
                    errors.Add(new ModsynValidationError(
                        "backup: some requires at least one of backupThis, backupThese, dontBackupThis, or dontBackupThese with a value.",
                        location));
                }
            }

    private static ModsynResolvedRequirement ValidateRequirement(
        ModsynObjectNode requirement,
        List<ModsynValidationError> errors)
    {
        ValidatePropertySet(requirement, ModsynLanguageDefinition.RequirementProperties, "requirement", errors);

        var checkThisProperty = FindProperty(requirement, "checkThis");
        var checkTheseProperty = FindProperty(requirement, "checkThese");
        var reqAddressProperty = FindProperty(requirement, "reqAddress");
        var reqPathProperty = FindProperty(requirement, "reqPath");
        var nestedProperty = FindProperty(requirement, "require");

        var localCheckThis = checkThisProperty?.Value as ModsynStringNode;
        var localCheckThese = checkTheseProperty?.Value as ModsynArrayNode;
        var localAddressProperty = reqAddressProperty ?? reqPathProperty;
        var localAddress = localAddressProperty?.Value as ModsynStringNode;
        var nested = nestedProperty?.Value is ModsynObjectNode nestedObject
            ? ValidateRequirement(nestedObject, errors)
            : null;

        var localCheckValues = localCheckThese is not null ? ReadStringArray(localCheckThese) : Array.Empty<string>();
        var checkThese = localCheckValues.Count > 0
            ? localCheckValues
            : nested?.CheckThese ?? Array.Empty<string>();

        return new ModsynResolvedRequirement(
            !string.IsNullOrWhiteSpace(localCheckThis?.Value) ? localCheckThis.Value : nested?.CheckThis,
            checkThese,
            localAddressProperty is not null ? localAddress?.Value : nested?.RequestAddress);
    }

    private static void ValidateReplacement(
        ModsynObjectNode replacement,
        List<ModsynValidationError> errors)
    {
        ValidatePropertySet(replacement, ModsynLanguageDefinition.ReplacementProperties, "replacement", errors);
        foreach (var requiredName in new[] { "source", "target" })
        {
            if (FindProperty(replacement, requiredName) is null)
            {
                errors.Add(new ModsynValidationError(
                    $"Replacement object requires a '{requiredName}' string property.",
                    replacement.Location));
            }
        }
    }

    private static void ValidatePropertySet(
        ModsynObjectNode value,
        IReadOnlyDictionary<string, ModsynPropertyDefinition> definitions,
        string scope,
        List<ModsynValidationError> errors)
    {
        var seenProperties = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.Properties)
        {
            if (!seenProperties.Add(property.Name))
            {
                errors.Add(new ModsynValidationError(
                    $"Duplicate {scope} property '{property.Name}'.",
                    property.Location));
            }

            if (!definitions.TryGetValue(property.Name, out var definition))
            {
                var suggestion = FindClosestPropertyName(property.Name, definitions.Keys);
                var message = suggestion is null
                    ? $"Unknown {scope} property '{property.Name}'."
                    : $"Unknown {scope} property '{property.Name}'. Did you mean '{suggestion}'?";
                errors.Add(new ModsynValidationError(message, property.Location));
                continue;
            }

            ValidateValue(property.Name, property.Value, definition, errors);
        }
    }

    private static string? FindClosestPropertyName(string propertyName, IEnumerable<string> candidates)
    {
        var normalizedName = propertyName.ToLowerInvariant();
        var maximumDistance = Math.Max(1, normalizedName.Length / 4);
        var closest = candidates
            .Select(candidate =>
            {
                var normalizedCandidate = candidate.ToLowerInvariant();
                return (
                    Name: candidate,
                    Distance: GetEditDistance(normalizedName, normalizedCandidate),
                    CommonPrefixLength: GetCommonPrefixLength(normalizedName, normalizedCandidate));
            })
            .Where(candidate => candidate.Distance <= maximumDistance)
            .OrderBy(candidate => candidate.Distance)
            .ThenByDescending(candidate => candidate.CommonPrefixLength)
            .ToList();

        if (closest.Count == 0 || closest.Count > 1
            && closest[0].Distance == closest[1].Distance
            && closest[0].CommonPrefixLength == closest[1].CommonPrefixLength)
        {
            return null;
        }

        return closest[0].Name;
    }

    private static int GetEditDistance(string left, string right)
    {
        var distances = new int[left.Length + 1, right.Length + 1];
        for (var row = 0; row <= left.Length; row++)
        {
            distances[row, 0] = row;
        }

        for (var column = 0; column <= right.Length; column++)
        {
            distances[0, column] = column;
        }

        for (var row = 1; row <= left.Length; row++)
        {
            for (var column = 1; column <= right.Length; column++)
            {
                var substitutionCost = left[row - 1] == right[column - 1] ? 0 : 1;
                distances[row, column] = Math.Min(
                    Math.Min(distances[row - 1, column] + 1, distances[row, column - 1] + 1),
                    distances[row - 1, column - 1] + substitutionCost);
            }
        }

        return distances[left.Length, right.Length];
    }

    private static int GetCommonPrefixLength(string left, string right)
    {
        var index = 0;
        while (index < left.Length && index < right.Length && left[index] == right[index])
        {
            index++;
        }

        return index;
    }

    private static void ValidateValue(
        string propertyName,
        ModsynValueNode value,
        ModsynPropertyDefinition definition,
        List<ModsynValidationError> errors)
    {
        if (!TryGetValueKind(value, out var actualKind)
            || !definition.AllowedValueKinds.Contains(actualKind))
        {
            var actualName = TryGetValueKind(value, out actualKind)
                ? ModsynLanguageDefinition.ValueKinds[actualKind].Name
                : "unknown value";
            errors.Add(new ModsynValidationError(
                $"Property '{propertyName}' expects {DescribeKinds(definition.AllowedValueKinds)}, but found {actualName}.",
                value.Location));
            return;
        }

        if (value is not ModsynArrayNode array || definition.ArrayItemKinds.Count == 0)
        {
            return;
        }

        foreach (var item in array.Items)
        {
            if (!TryGetValueKind(item, out actualKind) || !definition.ArrayItemKinds.Contains(actualKind))
            {
                var actualName = TryGetValueKind(item, out actualKind)
                    ? ModsynLanguageDefinition.ValueKinds[actualKind].Name
                    : "unknown value";
                errors.Add(new ModsynValidationError(
                    $"Array item for '{propertyName}' expects {DescribeKinds(definition.ArrayItemKinds)}, but found {actualName}.",
                    item.Location));
            }
        }
    }

    private static string DescribeKinds(IReadOnlyList<ModsynValueKind> kinds)
    {
        return string.Join(" or ", kinds.Select(kind => ModsynLanguageDefinition.ValueKinds[kind].Name));
    }

    private static bool TryGetValueKind(ModsynValueNode value, out ModsynValueKind kind)
    {
        kind = value switch
        {
            ModsynStringNode => ModsynValueKind.String,
            ModsynBooleanNode => ModsynValueKind.Boolean,
            ModsynNullNode => ModsynValueKind.Null,
            ModsynIdentifierNode => ModsynValueKind.Identifier,
            ModsynArrayNode => ModsynValueKind.Array,
            ModsynObjectNode => ModsynValueKind.Object,
            _ => default
        };
        return value is ModsynStringNode or ModsynBooleanNode or ModsynNullNode
            or ModsynIdentifierNode or ModsynArrayNode or ModsynObjectNode;
    }

    private static IReadOnlyList<string> ReadStringArray(ModsynArrayNode array)
    {
        return array.Items.OfType<ModsynStringNode>()
            .Select(item => item.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static ModsynPropertyNode? FindProperty(ModsynObjectNode value, string name)
    {
        return value.Properties.FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.Ordinal));
    }
}