using GtaSaModManager.Modsyn.Ast;
using GtaSaModManager.Modsyn.Lexer;

namespace GtaSaModManager.Modsyn.Validation;

public static class ModsynValidator
{
    public static ModsynValidationResult Validate(ModsynDocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var errors = new List<ModsynValidationError>();
        ValidatePropertySet(document.Body, ModsynLanguageDefinition.RootProperties, "root-level", errors);

        var (normalizedType, typeIsValid) = ResolveType(FindProperty(document.Body, "type"), errors);
        if (typeIsValid)
        {
            ValidateBackupApplicability(document.Body, normalizedType, errors);
        }

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

        var backupEnabled = FindProperty(document.Body, "backup")?.Value is ModsynBooleanNode backup
            ? backup.Value
            : true;

        return new ModsynValidationResult(normalizedType, backupEnabled, requirements.AsReadOnly(), errors.AsReadOnly());
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
        foreach (var property in value.Properties)
        {
            if (!definitions.TryGetValue(property.Name, out var definition))
            {
                errors.Add(new ModsynValidationError($"Unknown {scope} property '{property.Name}'.", property.Location));
                continue;
            }

            ValidateValue(property.Name, property.Value, definition, errors);
        }
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