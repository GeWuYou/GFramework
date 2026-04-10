const {
    joinArrayIndexPath,
    joinArrayTemplatePath,
    joinPropertyPath,
    splitObjectPath
} = require("./configPath");
const {ValidationMessageKeys} = require("./localizationKeys");

const IntegerScalarPattern = /^[+-]?\d+$/u;
const NumberScalarPattern = /^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?$/u;
const BooleanScalarPattern = /^(true|false)$/iu;

/**
 * Parse the repository's minimal config-schema subset into a recursive tree.
 * The parser intentionally mirrors the same high-level contract used by the
 * runtime validator and source generator so tooling diagnostics stay aligned.
 *
 * @param {string} content Raw schema JSON text.
 * @throws {Error} Thrown when the schema declares one unsupported or invalid pattern string.
 * @returns {{
 *   type: "object",
 *   required: string[],
 *   properties: Record<string, SchemaNode>,
 *   minProperties?: number,
 *   maxProperties?: number
 * }} Parsed schema info.
 */
function parseSchemaContent(content) {
    const parsed = JSON.parse(content);
    return parseSchemaNode(parsed, "<root>");
}

/**
 * Collect top-level schema fields that the current batch editor can update
 * safely. Batch editing intentionally remains conservative even though the form
 * preview can now navigate nested object structures.
 *
 * @param {{type: "object", required: string[], properties: Record<string, SchemaNode>}} schemaInfo Parsed schema.
 * @returns {Array<{
 *   key: string,
 *   path: string,
 *   type: string,
 *   itemType?: string,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string,
 *   enumValues?: string[],
 *   itemEnumValues?: string[],
 *   refTable?: string,
 *   inputKind: "scalar" | "array",
 *   required: boolean
 * }>} Editable field descriptors.
 */
function getEditableSchemaFields(schemaInfo) {
    const editableFields = [];
    const requiredSet = new Set(Array.isArray(schemaInfo.required) ? schemaInfo.required : []);

    for (const [key, property] of Object.entries(schemaInfo.properties || {})) {
        if (isEditableScalarType(property.type)) {
            editableFields.push({
                key,
                path: key,
                type: property.type,
                title: property.title,
                description: property.description,
                defaultValue: property.defaultValue,
                enumValues: property.enumValues,
                refTable: property.refTable,
                inputKind: "scalar",
                required: requiredSet.has(key)
            });
            continue;
        }

        if (property.type === "array" && property.items && isEditableScalarType(property.items.type)) {
            editableFields.push({
                key,
                path: key,
                type: property.type,
                itemType: property.items.type,
                title: property.title,
                description: property.description,
                defaultValue: property.defaultValue,
                itemEnumValues: property.items.enumValues,
                refTable: property.refTable,
                inputKind: "array",
                required: requiredSet.has(key)
            });
        }
    }

    return editableFields.sort((left, right) => left.key.localeCompare(right.key));
}

/**
 * Parse YAML into a recursive object/array/scalar tree.
 * The parser covers the config system's intended subset: root mappings,
 * indentation-based nested objects, scalar arrays, and arrays of objects.
 *
 * @param {string} text YAML text.
 * @returns {YamlNode} Parsed YAML tree.
 */
function parseTopLevelYaml(text) {
    const tokens = tokenizeYaml(text);
    if (tokens.length === 0) {
        return createObjectNode();
    }

    const state = {index: 0};
    return parseBlock(tokens, state, tokens[0].indent);
}

/**
 * Extract comment text from a YAML document and map it to logical field paths.
 * The extractor focuses on comment lines that appear immediately above one key
 * or array item so the form preview can surface author intent near the field.
 *
 * @param {string} text YAML text.
 * @returns {Record<string, string>} Comment lookup keyed by logical path.
 */
function extractYamlComments(text) {
    const lines = String(text).split(/\r?\n/u);
    const comments = {};
    const stack = [{indent: -1, type: "object", path: "", nextIndex: 0}];
    let pendingComments = [];

    for (let index = 0; index < lines.length; index += 1) {
        const line = lines[index];
        const trimmed = line.trim();

        if (trimmed.length === 0) {
            pendingComments = [];
            continue;
        }

        const indent = countLeadingSpaces(line);
        if (trimmed.startsWith("#")) {
            pendingComments.push(trimmed.replace(/^#\s?/u, ""));
            continue;
        }

        while (stack.length > 1 && indent < stack[stack.length - 1].indent) {
            stack.pop();
        }

        const currentContext = stack[stack.length - 1];
        if (trimmed.startsWith("-")) {
            if (currentContext.type !== "array") {
                pendingComments = [];
                continue;
            }

            const itemIndex = currentContext.nextIndex || 0;
            currentContext.nextIndex = itemIndex + 1;
            const itemPath = joinArrayIndexPath(currentContext.path, itemIndex);
            assignPendingComments(comments, itemPath, pendingComments);
            pendingComments = [];

            const rest = trimmed.slice(1).trim();
            if (rest.length === 0) {
                const nextLine = findNextMeaningfulLine(lines, index + 1);
                if (nextLine && nextLine.indent > indent) {
                    stack.push(createContextForChild(itemPath, nextLine));
                }
                continue;
            }

            const inlineObjectMapping = parseYamlMappingText(rest);
            if (!inlineObjectMapping) {
                continue;
            }

            const itemObjectContext = {indent: indent + 2, type: "object", path: itemPath, nextIndex: 0};
            stack.push(itemObjectContext);

            const key = inlineObjectMapping.key;
            const parsedValue = splitYamlValueAndInlineComment(inlineObjectMapping.rawValue.trim());
            if (parsedValue.comment) {
                comments[joinPropertyPath(itemPath, key)] = parsedValue.comment;
            }

            const nextLine = findNextMeaningfulLine(lines, index + 1);
            if (parsedValue.value.length === 0 && nextLine && nextLine.indent > indent) {
                stack.push(createContextForChild(joinPropertyPath(itemPath, key), nextLine));
            }

            continue;
        }

        const mapping = parseYamlMappingText(trimmed);
        if (!mapping) {
            pendingComments = [];
            continue;
        }

        const key = mapping.key;
        const valueInfo = splitYamlValueAndInlineComment(mapping.rawValue.trim());
        const currentPath = joinPropertyPath(currentContext.path, key);
        assignPendingComments(comments, currentPath, pendingComments);
        pendingComments = [];

        if (valueInfo.comment) {
            comments[currentPath] = comments[currentPath]
                ? `${comments[currentPath]}\n${valueInfo.comment}`
                : valueInfo.comment;
        }

        const nextLine = findNextMeaningfulLine(lines, index + 1);
        if (valueInfo.value.length === 0 && nextLine && nextLine.indent > indent) {
            stack.push(createContextForChild(currentPath, nextLine));
        }
    }

    return comments;
}

/**
 * Create one example YAML config from a parsed schema tree.
 * The sample includes schema descriptions as YAML comments so empty files can
 * be bootstrapped into a readable starting point from the form preview.
 *
 * @param {{type: "object", required: string[], properties: Record<string, SchemaNode>}} schemaInfo Parsed schema.
 * @returns {string} Example YAML text.
 */
function createSampleConfigYaml(schemaInfo) {
    const sampleRoot = createSampleNodeFromSchema(schemaInfo);
    const schemaComments = {};
    collectSchemaComments(schemaInfo, "", schemaComments);
    return renderYaml(sampleRoot, 0, "", schemaComments).join("\n");
}

/**
 * Produce extension-facing validation diagnostics from schema and parsed YAML.
 *
 * @param {{type: "object", required: string[], properties: Record<string, SchemaNode>}} schemaInfo Parsed schema.
 * @param {YamlNode} parsedYaml Parsed YAML tree.
 * @param {{isChinese?: boolean} | undefined} localizer Optional runtime localizer.
 * @returns {Array<{severity: "error" | "warning", message: string}>} Validation diagnostics.
 */
function validateParsedConfig(schemaInfo, parsedYaml, localizer) {
    const diagnostics = [];
    validateNode(schemaInfo, parsedYaml, "", diagnostics, localizer);
    return diagnostics;
}

/**
 * Determine whether the current schema type can be edited through the batch
 * editor. The richer form preview handles nested objects separately.
 *
 * @param {string} schemaType Schema type.
 * @returns {boolean} True when the type is batch-editable.
 */
function isEditableScalarType(schemaType) {
    return schemaType === "string" ||
        schemaType === "integer" ||
        schemaType === "number" ||
        schemaType === "boolean";
}

/**
 * Determine whether a scalar value matches a minimal schema type.
 *
 * @param {string} expectedType Schema type.
 * @param {string} scalarValue YAML scalar value.
 * @returns {boolean} True when compatible.
 */
function isScalarCompatible(expectedType, scalarValue) {
    const value = unquoteScalar(String(scalarValue));
    switch (expectedType) {
        case "integer":
            return IntegerScalarPattern.test(value);
        case "number":
            return NumberScalarPattern.test(value);
        case "boolean":
            return BooleanScalarPattern.test(value);
        case "string":
            return true;
        default:
            return true;
    }
}

/**
 * Apply form updates back into YAML. The implementation rewrites the YAML tree
 * from the parsed structure so nested object edits can be saved safely.
 *
 * @param {string} originalYaml Original YAML content.
 * @param {{scalars?: Record<string, string>, arrays?: Record<string, string[]>, objectArrays?: Record<string, Array<Record<string, unknown>>>, comments?: Record<string, string>}} updates Updated form values.
 * @returns {string} Updated YAML content.
 */
function applyFormUpdates(originalYaml, updates) {
    const root = normalizeRootNode(parseTopLevelYaml(originalYaml));
    const preservedComments = extractYamlComments(originalYaml);
    const scalarUpdates = updates.scalars || {};
    const arrayUpdates = updates.arrays || {};
    const objectArrayUpdates = updates.objectArrays || {};
    const commentUpdates = updates.comments || {};

    for (const [path, value] of Object.entries(scalarUpdates)) {
        setNodeAtPath(root, splitObjectPath(path), createScalarNode(String(value)));
    }

    for (const [path, values] of Object.entries(arrayUpdates)) {
        setNodeAtPath(root, splitObjectPath(path), createArrayNode(
            (values || []).map((item) => createScalarNode(String(item)))));
    }

    for (const [path, items] of Object.entries(objectArrayUpdates)) {
        setNodeAtPath(root, splitObjectPath(path), createArrayNode(
            (items || []).map((item) => createNodeFromFormValue(item))));
    }

    for (const [path, comment] of Object.entries(commentUpdates)) {
        const normalizedComment = String(comment || "").trim();
        if (normalizedComment.length === 0) {
            delete preservedComments[path];
            continue;
        }

        preservedComments[path] = normalizedComment;
    }

    return renderYaml(root, 0, "", preservedComments).join("\n");
}

/**
 * Apply only scalar updates back into YAML.
 *
 * @param {string} originalYaml Original YAML content.
 * @param {Record<string, string>} updates Updated scalar values.
 * @returns {string} Updated YAML content.
 */
function applyScalarUpdates(originalYaml, updates) {
    return applyFormUpdates(originalYaml, {scalars: updates});
}

/**
 * Parse the batch editor's comma-separated array input.
 *
 * @param {string} value Raw input value.
 * @returns {string[]} Parsed array items.
 */
function parseBatchArrayValue(value) {
    return String(value)
        .split(",")
        .map((item) => item.trim())
        .filter((item) => item.length > 0);
}

/**
 * Normalize a schema enum array into string values that can be shown in UI
 * hints and compared against parsed YAML scalar content.
 *
 * @param {unknown} value Raw schema enum value.
 * @returns {string[] | undefined} Normalized enum values.
 */
function normalizeSchemaEnumValues(value) {
    if (!Array.isArray(value)) {
        return undefined;
    }

    const normalized = value
        .filter((item) => ["string", "number", "boolean"].includes(typeof item))
        .map((item) => String(item));

    return normalized.length > 0 ? normalized : undefined;
}

/**
 * Normalize one finite schema number for tooling metadata and comparisons.
 *
 * @param {unknown} value Raw schema value.
 * @returns {number | undefined} Normalized finite number.
 */
function normalizeSchemaNumber(value) {
    return typeof value === "number" && Number.isFinite(value) ? value : undefined;
}

/**
 * Normalize one strictly positive finite schema number.
 *
 * @param {unknown} value Raw schema value.
 * @returns {number | undefined} Normalized positive number.
 */
function normalizeSchemaPositiveNumber(value) {
    return typeof value === "number" && Number.isFinite(value) && value > 0 ? value : undefined;
}

/**
 * Normalize one non-negative integer schema value for length constraints.
 *
 * @param {unknown} value Raw schema value.
 * @returns {number | undefined} Normalized non-negative integer.
 */
function normalizeSchemaNonNegativeInteger(value) {
    return Number.isInteger(value) && value >= 0 ? value : undefined;
}

/**
 * Normalize one boolean schema flag.
 *
 * @param {unknown} value Raw schema value.
 * @returns {boolean | undefined} Normalized boolean.
 */
function normalizeSchemaBoolean(value) {
    return typeof value === "boolean" ? value : undefined;
}

/**
 * Normalize one schema pattern string when the regular expression can be
 * compiled by the local tooling runtime.
 *
 * @param {unknown} value Raw schema value.
 * @param {string} displayPath Logical property path used in diagnostics.
 * @throws {Error} Thrown when the pattern string cannot be compiled.
 * @returns {{source: string, regex: RegExp} | undefined} Normalized pattern metadata.
 */
function normalizeSchemaPattern(value, displayPath) {
    if (typeof value !== "string") {
        return undefined;
    }

    try {
        return {
            source: value,
            regex: new RegExp(value, "u")
        };
    } catch (error) {
        throw new Error(`Schema property '${displayPath}' declares an invalid 'pattern' regular expression: ${error.message}`);
    }
}

/**
 * Convert a schema default value into a compact string that can be shown in UI
 * metadata hints.
 *
 * @param {unknown} value Raw schema default value.
 * @returns {string | undefined} Display string for the default value.
 */
function formatSchemaDefaultValue(value) {
    if (value === null || value === undefined) {
        return undefined;
    }

    if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
        return String(value);
    }

    if (Array.isArray(value)) {
        const normalized = value
            .filter((item) => ["string", "number", "boolean"].includes(typeof item))
            .map((item) => String(item));

        return normalized.length > 0 ? normalized.join(", ") : undefined;
    }

    if (typeof value === "object") {
        return JSON.stringify(value);
    }

    return undefined;
}

/**
 * Test one scalar value against one compiled schema pattern.
 *
 * @param {string} scalarValue Scalar value from YAML.
 * @param {RegExp | undefined} patternRegex Compiled schema pattern.
 * @returns {boolean} True when the value matches or no pattern is declared.
 */
function matchesSchemaPattern(scalarValue, patternRegex) {
    if (!(patternRegex instanceof RegExp)) {
        return true;
    }

    return patternRegex.test(scalarValue);
}

/**
 * Test whether one numeric scalar satisfies a multipleOf constraint.
 *
 * @param {string} scalarValue YAML scalar value.
 * @param {number | undefined} multipleOf Schema multipleOf value.
 * @returns {boolean} True when compatible or the constraint is absent.
 */
function matchesSchemaMultipleOf(scalarValue, multipleOf) {
    if (typeof multipleOf !== "number") {
        return true;
    }

    const exactDecimalResult = tryMatchesExactDecimalMultiple(scalarValue, String(multipleOf));
    if (exactDecimalResult !== null) {
        return exactDecimalResult;
    }

    const numericValue = Number(scalarValue);
    const quotient = numericValue / multipleOf;
    const nearestInteger = Math.round(quotient);
    const tolerance = 1e-9 * Math.max(1, Math.abs(quotient));
    return Math.abs(quotient - nearestInteger) <= tolerance;
}

/**
 * Try to evaluate one multipleOf constraint using exact decimal arithmetic.
 * This keeps common YAML / JSON decimal literals aligned with the runtime and
 * avoids large-number false positives that a pure floating-point quotient check can miss.
 *
 * @param {string} valueText YAML scalar text.
 * @param {string} divisorText Schema multipleOf text.
 * @returns {boolean | null} Exact result, or null when the inputs cannot be normalized exactly.
 */
function tryMatchesExactDecimalMultiple(valueText, divisorText) {
    const valueParts = tryParseExactDecimal(valueText);
    const divisorParts = tryParseExactDecimal(divisorText);
    if (!valueParts || !divisorParts || divisorParts.significand === 0n) {
        return null;
    }

    const commonScale = Math.max(valueParts.scale, divisorParts.scale);
    const scaledValue = scaleDecimalSignificand(valueParts.significand, valueParts.scale, commonScale);
    const scaledDivisor = scaleDecimalSignificand(divisorParts.significand, divisorParts.scale, commonScale);
    return scaledValue % scaledDivisor === 0n;
}

/**
 * Normalize a finite decimal literal into an integer significand plus decimal scale.
 * The normalized form lets multipleOf checks run as integer modulo instead of floating-point math.
 *
 * @param {string} text Numeric text to normalize.
 * @returns {{significand: bigint, scale: number} | null} Normalized parts, or null for unsupported input.
 */
function tryParseExactDecimal(text) {
    const match = /^([+-]?)(?:(\d+)(?:\.(\d*))?|\.(\d+))(?:[eE]([+-]?\d+))?$/u.exec(String(text).trim());
    if (!match) {
        return null;
    }

    const exponent = match[5] ? Number.parseInt(match[5], 10) : 0;
    if (!Number.isSafeInteger(exponent)) {
        return null;
    }

    const integerDigits = match[2] ?? "";
    const fractionDigits = match[3] !== undefined ? match[3] : (match[4] ?? "");
    let digits = `${integerDigits}${fractionDigits}`.replace(/^0+/u, "");
    if (digits.length === 0) {
        return {significand: 0n, scale: 0};
    }

    let scale = fractionDigits.length - exponent;
    if (scale < 0) {
        digits += "0".repeat(-scale);
        scale = 0;
    }

    while (scale > 0 && digits.endsWith("0")) {
        digits = digits.slice(0, -1);
        scale -= 1;
    }

    let significand = BigInt(digits);
    if (match[1] === "-") {
        significand = -significand;
    }

    return {significand, scale};
}

/**
 * Scale one normalized decimal significand to a larger decimal precision.
 *
 * @param {bigint} significand Integer significand.
 * @param {number} currentScale Current decimal scale.
 * @param {number} targetScale Target decimal scale.
 * @returns {bigint} Scaled significand.
 */
function scaleDecimalSignificand(significand, currentScale, targetScale) {
    if (currentScale === targetScale) {
        return significand;
    }

    return significand * (10n ** BigInt(targetScale - currentScale));
}

/**
 * Format a scalar value for YAML output.
 *
 * @param {string} value Scalar value.
 * @returns {string} YAML-ready scalar.
 */
function formatYamlScalar(value) {
    if (NumberScalarPattern.test(value) || BooleanScalarPattern.test(value)) {
        return value;
    }

    if (value.length === 0 || /[:#\[\]\{\},]|^\s|\s$/u.test(value)) {
        return JSON.stringify(value);
    }

    return value;
}

/**
 * Remove a simple YAML string quote wrapper.
 *
 * @param {string} value Scalar value.
 * @returns {string} Unquoted value.
 */
function unquoteScalar(value) {
    if ((value.startsWith("\"") && value.endsWith("\"")) ||
        (value.startsWith("'") && value.endsWith("'"))) {
        return value.slice(1, -1);
    }

    return value;
}

/**
 * Parse one schema node recursively.
 *
 * @param {unknown} rawNode Raw schema node.
 * @param {string} displayPath Logical property path.
 * @returns {SchemaNode} Parsed schema node.
 */
function parseSchemaNode(rawNode, displayPath) {
    const value = rawNode && typeof rawNode === "object" ? rawNode : {};
    const type = typeof value.type === "string" ? value.type : "object";
    const patternMetadata = normalizeSchemaPattern(value.pattern, displayPath);
    const metadata = {
        title: typeof value.title === "string" ? value.title : undefined,
        description: typeof value.description === "string" ? value.description : undefined,
        defaultValue: formatSchemaDefaultValue(value.default),
        minimum: normalizeSchemaNumber(value.minimum),
        exclusiveMinimum: normalizeSchemaNumber(value.exclusiveMinimum),
        maximum: normalizeSchemaNumber(value.maximum),
        exclusiveMaximum: normalizeSchemaNumber(value.exclusiveMaximum),
        multipleOf: normalizeSchemaPositiveNumber(value.multipleOf),
        minLength: normalizeSchemaNonNegativeInteger(value.minLength),
        maxLength: normalizeSchemaNonNegativeInteger(value.maxLength),
        pattern: patternMetadata ? patternMetadata.source : undefined,
        patternRegex: patternMetadata ? patternMetadata.regex : undefined,
        minItems: normalizeSchemaNonNegativeInteger(value.minItems),
        maxItems: normalizeSchemaNonNegativeInteger(value.maxItems),
        minProperties: normalizeSchemaNonNegativeInteger(value.minProperties),
        maxProperties: normalizeSchemaNonNegativeInteger(value.maxProperties),
        uniqueItems: normalizeSchemaBoolean(value.uniqueItems),
        refTable: typeof value["x-gframework-ref-table"] === "string"
            ? value["x-gframework-ref-table"]
            : undefined
    };

    if (type === "object") {
        const required = Array.isArray(value.required)
            ? value.required.filter((item) => typeof item === "string")
            : [];
        const properties = {};
        for (const [key, propertyNode] of Object.entries(value.properties || {})) {
            properties[key] = parseSchemaNode(propertyNode, joinPropertyPath(displayPath, key));
        }

        return {
            type: "object",
            displayPath,
            required,
            properties,
            minProperties: metadata.minProperties,
            maxProperties: metadata.maxProperties,
            title: metadata.title,
            description: metadata.description,
            defaultValue: metadata.defaultValue
        };
    }

    if (type === "array") {
        const itemNode = parseSchemaNode(value.items || {}, joinArrayTemplatePath(displayPath));
        return {
            type: "array",
            displayPath,
            title: metadata.title,
            description: metadata.description,
            defaultValue: metadata.defaultValue,
            minItems: metadata.minItems,
            maxItems: metadata.maxItems,
            uniqueItems: metadata.uniqueItems === true,
            refTable: metadata.refTable,
            items: itemNode
        };
    }

    return {
        type,
        displayPath,
        title: metadata.title,
        description: metadata.description,
        defaultValue: metadata.defaultValue,
        minimum: type === "integer" || type === "number"
            ? metadata.minimum
            : undefined,
        exclusiveMinimum: type === "integer" || type === "number"
            ? metadata.exclusiveMinimum
            : undefined,
        maximum: type === "integer" || type === "number"
            ? metadata.maximum
            : undefined,
        exclusiveMaximum: type === "integer" || type === "number"
            ? metadata.exclusiveMaximum
            : undefined,
        multipleOf: type === "integer" || type === "number"
            ? metadata.multipleOf
            : undefined,
        minLength: type === "string"
            ? metadata.minLength
            : undefined,
        maxLength: type === "string"
            ? metadata.maxLength
            : undefined,
        pattern: type === "string"
            ? metadata.pattern
            : undefined,
        patternRegex: type === "string"
            ? metadata.patternRegex
            : undefined,
        enumValues: normalizeSchemaEnumValues(value.enum),
        refTable: metadata.refTable
    };
}

/**
 * Validate one schema node against one YAML node.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @param {string} displayPath Current logical path.
 * @param {Array<{severity: "error" | "warning", message: string}>} diagnostics Diagnostic sink.
 * @param {{isChinese?: boolean} | undefined} localizer Optional runtime localizer.
 */
function validateNode(schemaNode, yamlNode, displayPath, diagnostics, localizer) {
    if (schemaNode.type === "object") {
        validateObjectNode(schemaNode, yamlNode, displayPath, diagnostics, localizer);
        return;
    }

    if (schemaNode.type === "array") {
        if (!yamlNode || yamlNode.kind !== "array") {
            diagnostics.push({
                severity: "error",
                message: localizeValidationMessage(ValidationMessageKeys.expectedArray, localizer, {
                    displayPath
                })
            });
            return;
        }

        if (typeof schemaNode.minItems === "number" &&
            yamlNode.items.length < schemaNode.minItems) {
            diagnostics.push({
                severity: "error",
                message: localizeValidationMessage(ValidationMessageKeys.minItemsViolation, localizer, {
                    displayPath,
                    value: String(schemaNode.minItems)
                })
            });
        }

        if (typeof schemaNode.maxItems === "number" &&
            yamlNode.items.length > schemaNode.maxItems) {
            diagnostics.push({
                severity: "error",
                message: localizeValidationMessage(ValidationMessageKeys.maxItemsViolation, localizer, {
                    displayPath,
                    value: String(schemaNode.maxItems)
                })
            });
        }

        const comparableItems = [];
        for (let index = 0; index < yamlNode.items.length; index += 1) {
            const diagnosticsBeforeValidation = diagnostics.length;
            validateNode(
                schemaNode.items,
                yamlNode.items[index],
                joinArrayIndexPath(displayPath, index),
                diagnostics,
                localizer);

            // Keep uniqueItems focused on values that are otherwise valid so a
            // shape/type error does not also surface as a misleading duplicate.
            if (diagnostics.length === diagnosticsBeforeValidation) {
                comparableItems.push({index, node: yamlNode.items[index]});
            }
        }

        if (schemaNode.uniqueItems === true) {
            const seenItems = new Map();
            for (const {index, node} of comparableItems) {
                const comparableValue = buildComparableNodeValue(schemaNode.items, node);
                if (seenItems.has(comparableValue)) {
                    diagnostics.push({
                        severity: "error",
                        message: localizeValidationMessage(ValidationMessageKeys.uniqueItemsViolation, localizer, {
                            displayPath: joinArrayIndexPath(displayPath, index),
                            duplicatePath: joinArrayIndexPath(displayPath, seenItems.get(comparableValue))
                        })
                    });
                    continue;
                }

                seenItems.set(comparableValue, index);
            }
        }

        return;
    }

    if (!yamlNode || yamlNode.kind !== "scalar") {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.expectedScalarShape, localizer, {
                displayPath,
                schemaType: schemaNode.type,
                yamlKind: yamlNode ? yamlNode.kind : "missing"
            })
        });
        return;
    }

    if (!isScalarCompatible(schemaNode.type, yamlNode.value)) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.expectedScalarValue, localizer, {
                displayPath,
                schemaType: schemaNode.type
            })
        });
        return;
    }

    if (Array.isArray(schemaNode.enumValues) &&
        schemaNode.enumValues.length > 0 &&
        !schemaNode.enumValues.includes(unquoteScalar(yamlNode.value))) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.enumMismatch, localizer, {
                displayPath,
                values: schemaNode.enumValues.join(", ")
            })
        });
    }

    const scalarValue = unquoteScalar(yamlNode.value);
    const supportsNumericConstraints = schemaNode.type === "integer" || schemaNode.type === "number";
    const supportsLengthConstraints = schemaNode.type === "string";
    const supportsPatternConstraints = schemaNode.type === "string";

    if (supportsNumericConstraints &&
        typeof schemaNode.minimum === "number" &&
        Number(scalarValue) < schemaNode.minimum) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.minimumViolation, localizer, {
                displayPath,
                value: String(schemaNode.minimum)
            })
        });
    }

    if (supportsNumericConstraints &&
        typeof schemaNode.exclusiveMinimum === "number" &&
        Number(scalarValue) <= schemaNode.exclusiveMinimum) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.exclusiveMinimumViolation, localizer, {
                displayPath,
                value: String(schemaNode.exclusiveMinimum)
            })
        });
    }

    if (supportsNumericConstraints &&
        typeof schemaNode.maximum === "number" &&
        Number(scalarValue) > schemaNode.maximum) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.maximumViolation, localizer, {
                displayPath,
                value: String(schemaNode.maximum)
            })
        });
    }

    if (supportsNumericConstraints &&
        typeof schemaNode.exclusiveMaximum === "number" &&
        Number(scalarValue) >= schemaNode.exclusiveMaximum) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.exclusiveMaximumViolation, localizer, {
                displayPath,
                value: String(schemaNode.exclusiveMaximum)
            })
        });
    }

    if (supportsNumericConstraints &&
        !matchesSchemaMultipleOf(scalarValue, schemaNode.multipleOf)) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.multipleOfViolation, localizer, {
                displayPath,
                value: String(schemaNode.multipleOf)
            })
        });
    }

    if (supportsLengthConstraints &&
        typeof schemaNode.minLength === "number" &&
        scalarValue.length < schemaNode.minLength) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.minLengthViolation, localizer, {
                displayPath,
                value: String(schemaNode.minLength)
            })
        });
    }

    if (supportsLengthConstraints &&
        typeof schemaNode.maxLength === "number" &&
        scalarValue.length > schemaNode.maxLength) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.maxLengthViolation, localizer, {
                displayPath,
                value: String(schemaNode.maxLength)
            })
        });
    }

    if (supportsPatternConstraints &&
        !matchesSchemaPattern(scalarValue, schemaNode.patternRegex)) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.patternViolation, localizer, {
                displayPath,
                value: schemaNode.pattern
            })
        });
    }
}

/**
 * Validate an object node recursively.
 *
 * @param {Extract<SchemaNode, {type: "object"}>} schemaNode Object schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @param {string} displayPath Current logical path.
 * @param {Array<{severity: "error" | "warning", message: string}>} diagnostics Diagnostic sink.
 * @param {{isChinese?: boolean} | undefined} localizer Optional runtime localizer.
 */
function validateObjectNode(schemaNode, yamlNode, displayPath, diagnostics, localizer) {
    if (!yamlNode || yamlNode.kind !== "object") {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.expectedObject, localizer, {
                displayPath
            })
        });
        return;
    }

    const propertyCount = yamlNode.map instanceof Map
        ? yamlNode.map.size
        : Array.isArray(yamlNode.entries)
            ? new Set(yamlNode.entries.map((entry) => entry.key)).size
            : 0;

    for (const requiredProperty of schemaNode.required) {
        if (!yamlNode.map.has(requiredProperty)) {
            diagnostics.push({
                severity: "error",
                message: localizeValidationMessage(ValidationMessageKeys.missingRequired, localizer, {
                    displayPath: joinPropertyPath(displayPath, requiredProperty)
                })
            });
        }
    }

    for (const entry of yamlNode.entries) {
        if (!Object.prototype.hasOwnProperty.call(schemaNode.properties, entry.key)) {
            diagnostics.push({
                severity: "error",
                message: localizeValidationMessage(ValidationMessageKeys.unknownProperty, localizer, {
                    displayPath: joinPropertyPath(displayPath, entry.key)
                })
            });
            continue;
        }

        validateNode(
            schemaNode.properties[entry.key],
            entry.node,
            joinPropertyPath(displayPath, entry.key),
            diagnostics,
            localizer);
    }

    if (typeof schemaNode.minProperties === "number" &&
        propertyCount < schemaNode.minProperties) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.minPropertiesViolation, localizer, {
                displayPath,
                value: String(schemaNode.minProperties)
            })
        });
    }

    if (typeof schemaNode.maxProperties === "number" &&
        propertyCount > schemaNode.maxProperties) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.maxPropertiesViolation, localizer, {
                displayPath,
                value: String(schemaNode.maxProperties)
            })
        });
    }
}

/**
 * Build one schema-aware comparable key for uniqueItems checks.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode | undefined} yamlNode YAML node.
 * @returns {string} Comparable key.
 */
function buildComparableNodeValue(schemaNode, yamlNode) {
    if (!yamlNode) {
        return "missing";
    }

    if (schemaNode.type === "object") {
        if (yamlNode.kind !== "object") {
            return yamlNode.kind;
        }

        return Object.keys(schemaNode.properties)
            .filter((key) => yamlNode.map.has(key))
            .sort((left, right) => left.localeCompare(right))
            .map((key) => {
                const valueKey = buildComparableNodeValue(schemaNode.properties[key], yamlNode.map.get(key));
                return `${key.length}:${key}=${valueKey.length}:${valueKey}`;
            })
            .join("|");
    }

    if (schemaNode.type === "array") {
        if (yamlNode.kind !== "array") {
            return yamlNode.kind;
        }

        return `[${yamlNode.items.map((item) => {
            const valueKey = buildComparableNodeValue(schemaNode.items, item);
            return `${valueKey.length}:${valueKey}`;
        }).join(",")}]`;
    }

    if (yamlNode.kind !== "scalar") {
        return yamlNode.kind;
    }

    const scalarValue = unquoteScalar(yamlNode.value);
    const normalizedScalar = schemaNode.type === "integer" || schemaNode.type === "number"
        ? String(Number(scalarValue))
        : schemaNode.type === "boolean"
            ? String(/^true$/iu.test(scalarValue))
            : scalarValue;
    return `${schemaNode.type}:${normalizedScalar.length}:${normalizedScalar}`;
}

/**
 * Format one validation message in either English or Simplified Chinese.
 *
 * @param {string} key Message key.
 * @param {{isChinese?: boolean} | undefined} localizer Optional runtime localizer.
 * @param {Record<string, string>} params Message parameters.
 * @returns {string} Localized validation message.
 */
function localizeValidationMessage(key, localizer, params) {
    if (key === ValidationMessageKeys.expectedObject) {
        return formatExpectedObjectMessage(params.displayPath, Boolean(localizer && localizer.isChinese));
    }

    if (key === ValidationMessageKeys.minPropertiesViolation) {
        if (localizer && typeof localizer.t === "function" && params.displayPath) {
            return localizer.t(key, params);
        }

        return formatObjectPropertyCountMessage(
            params.displayPath,
            params.value,
            "min",
            Boolean(localizer && localizer.isChinese));
    }

    if (key === ValidationMessageKeys.maxPropertiesViolation) {
        if (localizer && typeof localizer.t === "function" && params.displayPath) {
            return localizer.t(key, params);
        }

        return formatObjectPropertyCountMessage(
            params.displayPath,
            params.value,
            "max",
            Boolean(localizer && localizer.isChinese));
    }

    if (localizer && typeof localizer.t === "function") {
        return localizer.t(key, params);
    }

    if (localizer && localizer.isChinese) {
        switch (key) {
            case ValidationMessageKeys.expectedArray:
                return `属性“${params.displayPath}”应为数组。`;
            case ValidationMessageKeys.expectedScalarShape:
                return `属性“${params.displayPath}”应为“${params.schemaType}”，但当前 YAML 结构是“${params.yamlKind}”。`;
            case ValidationMessageKeys.expectedScalarValue:
                return `属性“${params.displayPath}”应为“${params.schemaType}”，但当前标量值不兼容。`;
            case ValidationMessageKeys.enumMismatch:
                return `属性“${params.displayPath}”必须是以下值之一：${params.values}。`;
            case ValidationMessageKeys.exclusiveMaximumViolation:
                return `属性“${params.displayPath}”必须小于 ${params.value}。`;
            case ValidationMessageKeys.exclusiveMinimumViolation:
                return `属性“${params.displayPath}”必须大于 ${params.value}。`;
            case ValidationMessageKeys.maximumViolation:
                return `属性“${params.displayPath}”必须小于或等于 ${params.value}。`;
            case ValidationMessageKeys.maxItemsViolation:
                return `属性“${params.displayPath}”最多只能包含 ${params.value} 个元素。`;
            case ValidationMessageKeys.maxLengthViolation:
                return `属性“${params.displayPath}”长度必须不超过 ${params.value} 个字符。`;
            case ValidationMessageKeys.minimumViolation:
                return `属性“${params.displayPath}”必须大于或等于 ${params.value}。`;
            case ValidationMessageKeys.multipleOfViolation:
                return `属性“${params.displayPath}”必须是 ${params.value} 的整数倍。`;
            case ValidationMessageKeys.minItemsViolation:
                return `属性“${params.displayPath}”至少需要包含 ${params.value} 个元素。`;
            case ValidationMessageKeys.minLengthViolation:
                return `属性“${params.displayPath}”长度必须至少为 ${params.value} 个字符。`;
            case ValidationMessageKeys.patternViolation:
                return `属性“${params.displayPath}”必须匹配正则模式“${params.value}”。`;
            case ValidationMessageKeys.uniqueItemsViolation:
                return `属性“${params.displayPath}”与更早的数组元素 ${params.duplicatePath} 重复；该数组要求元素唯一。`;
            case ValidationMessageKeys.missingRequired:
                return `缺少必填属性“${params.displayPath}”。`;
            case ValidationMessageKeys.unknownProperty:
                return `属性“${params.displayPath}”未在匹配的 schema 中声明。`;
            default:
                return key;
        }
    }

    switch (key) {
        case ValidationMessageKeys.expectedArray:
            return `Property '${params.displayPath}' is expected to be an array.`;
        case ValidationMessageKeys.expectedScalarShape:
            return `Property '${params.displayPath}' is expected to be '${params.schemaType}', but the current YAML shape is '${params.yamlKind}'.`;
        case ValidationMessageKeys.expectedScalarValue:
            return `Property '${params.displayPath}' is expected to be '${params.schemaType}', but the current scalar value is incompatible.`;
        case ValidationMessageKeys.enumMismatch:
            return `Property '${params.displayPath}' must be one of: ${params.values}.`;
        case ValidationMessageKeys.exclusiveMaximumViolation:
            return `Property '${params.displayPath}' must be less than ${params.value}.`;
        case ValidationMessageKeys.exclusiveMinimumViolation:
            return `Property '${params.displayPath}' must be greater than ${params.value}.`;
        case ValidationMessageKeys.maximumViolation:
            return `Property '${params.displayPath}' must be less than or equal to ${params.value}.`;
        case ValidationMessageKeys.maxItemsViolation:
            return `Property '${params.displayPath}' must contain at most ${params.value} items.`;
        case ValidationMessageKeys.maxLengthViolation:
            return `Property '${params.displayPath}' must be at most ${params.value} characters long.`;
        case ValidationMessageKeys.minimumViolation:
            return `Property '${params.displayPath}' must be greater than or equal to ${params.value}.`;
        case ValidationMessageKeys.multipleOfViolation:
            return `Property '${params.displayPath}' must be a multiple of ${params.value}.`;
        case ValidationMessageKeys.minItemsViolation:
            return `Property '${params.displayPath}' must contain at least ${params.value} items.`;
        case ValidationMessageKeys.minLengthViolation:
            return `Property '${params.displayPath}' must be at least ${params.value} characters long.`;
        case ValidationMessageKeys.patternViolation:
            return `Property '${params.displayPath}' must match pattern '${params.value}'.`;
        case ValidationMessageKeys.uniqueItemsViolation:
            return `Property '${params.displayPath}' duplicates earlier array item '${params.duplicatePath}', but uniqueItems is required.`;
        case ValidationMessageKeys.missingRequired:
            return `Required property '${params.displayPath}' is missing.`;
        case ValidationMessageKeys.unknownProperty:
            return `Property '${params.displayPath}' is not declared in the matching schema.`;
        default:
            return key;
    }
}

/**
 * Format one object-shape expectation diagnostic.
 *
 * @param {string} displayPath Logical object path, or empty for the root object.
 * @param {boolean} isChinese Whether Chinese text should be produced.
 * @returns {string} Formatted message.
 */
function formatExpectedObjectMessage(displayPath, isChinese) {
    const isRoot = !displayPath;
    if (isChinese) {
        return isRoot
            ? "根对象应为对象。"
            : `属性“${displayPath}”应为对象。`;
    }

    return isRoot
        ? "Root object is expected to be an object."
        : `Property '${displayPath}' is expected to be an object.`;
}

/**
 * Format one object-property-count validation message.
 *
 * @param {string} displayPath Logical object path, or empty for the root object.
 * @param {string} value Constraint value.
 * @param {"min" | "max"} mode Whether the message describes a minimum or maximum.
 * @param {boolean} isChinese Whether Chinese text should be produced.
 * @returns {string} Formatted message.
 */
function formatObjectPropertyCountMessage(displayPath, value, mode, isChinese) {
    const isRoot = !displayPath;
    if (isChinese) {
        if (mode === "min") {
            return isRoot
                ? `根对象至少需要包含 ${value} 个属性。`
                : `对象属性“${displayPath}”至少需要包含 ${value} 个子属性。`;
        }

        return isRoot
            ? `根对象最多只能包含 ${value} 个属性。`
            : `对象属性“${displayPath}”最多只能包含 ${value} 个子属性。`;
    }

    if (mode === "min") {
        return isRoot
            ? `Root object must contain at least ${value} properties.`
            : `Property '${displayPath}' must contain at least ${value} properties.`;
    }

    return isRoot
        ? `Root object must contain at most ${value} properties.`
        : `Property '${displayPath}' must contain at most ${value} properties.`;
}

/**
 * Tokenize YAML lines into indentation-aware units.
 *
 * @param {string} text YAML text.
 * @returns {Array<{indent: number, text: string}>} Tokens.
 */
function tokenizeYaml(text) {
    const tokens = [];
    const lines = String(text).split(/\r?\n/u);

    for (const line of lines) {
        if (!line || line.trim().length === 0 || line.trimStart().startsWith("#")) {
            continue;
        }

        const indentMatch = /^(\s*)/u.exec(line);
        const indent = indentMatch ? indentMatch[1].length : 0;
        const trimmed = line.slice(indent);
        tokens.push({indent, text: trimmed});
    }

    return tokens;
}

/**
 * Parse the next YAML block from the token stream.
 *
 * @param {Array<{indent: number, text: string}>} tokens Token array.
 * @param {{index: number}} state Mutable parser state.
 * @param {number} indent Expected indentation.
 * @returns {YamlNode} Parsed node.
 */
function parseBlock(tokens, state, indent) {
    if (state.index >= tokens.length) {
        return createObjectNode();
    }

    const token = tokens[state.index];
    if (token.text.startsWith("-")) {
        return parseSequence(tokens, state, indent);
    }

    return parseMapping(tokens, state, indent);
}

/**
 * Parse a mapping block.
 *
 * @param {Array<{indent: number, text: string}>} tokens Token array.
 * @param {{index: number}} state Mutable parser state.
 * @param {number} indent Expected indentation.
 * @returns {YamlNode} Parsed object node.
 */
function parseMapping(tokens, state, indent) {
    const entries = [];
    const map = new Map();

    while (state.index < tokens.length) {
        const token = tokens[state.index];
        if (token.indent < indent || token.text.startsWith("-")) {
            break;
        }

        if (token.indent > indent) {
            state.index += 1;
            continue;
        }

        const mapping = parseYamlMappingText(token.text);
        if (!mapping) {
            state.index += 1;
            continue;
        }

        const key = mapping.key;
        const rawValue = mapping.rawValue.trim();
        state.index += 1;

        let node;
        if (rawValue.length > 0 && !rawValue.startsWith("|") && !rawValue.startsWith(">")) {
            node = createScalarNode(rawValue);
        } else if (state.index < tokens.length && tokens[state.index].indent > indent) {
            node = parseBlock(tokens, state, tokens[state.index].indent);
        } else {
            node = createScalarNode("");
        }

        entries.push({key, node});
        map.set(key, node);
    }

    return {kind: "object", entries, map};
}

/**
 * Parse a sequence block.
 *
 * @param {Array<{indent: number, text: string}>} tokens Token array.
 * @param {{index: number}} state Mutable parser state.
 * @param {number} indent Expected indentation.
 * @returns {YamlNode} Parsed array node.
 */
function parseSequence(tokens, state, indent) {
    const items = [];

    while (state.index < tokens.length) {
        const token = tokens[state.index];
        if (token.indent !== indent || !token.text.startsWith("-")) {
            break;
        }

        const rest = token.text.slice(1).trim();
        state.index += 1;

        if (rest.length === 0) {
            if (state.index < tokens.length && tokens[state.index].indent > indent) {
                items.push(parseBlock(tokens, state, tokens[state.index].indent));
            } else {
                items.push(createScalarNode(""));
            }
            continue;
        }

        if (parseYamlMappingText(rest)) {
            items.push(parseInlineObjectItem(tokens, state, indent, rest));
            continue;
        }

        items.push(createScalarNode(rest));
    }

    return createArrayNode(items);
}

/**
 * Parse an array item written as an inline mapping head followed by nested
 * child lines, for example `- wave: 1`.
 *
 * @param {Array<{indent: number, text: string}>} tokens Token array.
 * @param {{index: number}} state Mutable parser state.
 * @param {number} parentIndent Array indentation.
 * @param {string} firstEntry Inline first entry text.
 * @returns {YamlNode} Parsed object node.
 */
function parseInlineObjectItem(tokens, state, parentIndent, firstEntry) {
    const syntheticTokens = [{indent: parentIndent + 2, text: firstEntry}];
    while (state.index < tokens.length && tokens[state.index].indent > parentIndent) {
        syntheticTokens.push(tokens[state.index]);
        state.index += 1;
    }

    return parseBlock(syntheticTokens, {index: 0}, parentIndent + 2);
}

/**
 * Ensure the root node is an object, creating one if the YAML was empty or not
 * object-shaped enough for structured edits.
 *
 * @param {YamlNode} node Parsed node.
 * @returns {YamlObjectNode} Root object node.
 */
function normalizeRootNode(node) {
    return node && node.kind === "object" ? node : createObjectNode();
}

/**
 * Replace or create a node at a dot-separated object path.
 *
 * @param {YamlObjectNode} root Root object node.
 * @param {string[]} segments Path segments.
 * @param {YamlNode} valueNode Value node.
 */
function setNodeAtPath(root, segments, valueNode) {
    let current = root;

    for (let index = 0; index < segments.length; index += 1) {
        const segment = segments[index];
        if (!segment) {
            continue;
        }

        if (index === segments.length - 1) {
            setObjectEntry(current, segment, valueNode);
            return;
        }

        let nextNode = current.map.get(segment);
        if (!nextNode || nextNode.kind !== "object") {
            nextNode = createObjectNode();
            setObjectEntry(current, segment, nextNode);
        }

        current = nextNode;
    }
}

/**
 * Insert or replace one mapping entry while preserving insertion order.
 *
 * @param {YamlObjectNode} objectNode Target object node.
 * @param {string} key Mapping key.
 * @param {YamlNode} valueNode Value node.
 */
function setObjectEntry(objectNode, key, valueNode) {
    const existingIndex = objectNode.entries.findIndex((entry) => entry.key === key);
    if (existingIndex >= 0) {
        objectNode.entries[existingIndex] = {key, node: valueNode};
    } else {
        objectNode.entries.push({key, node: valueNode});
    }

    objectNode.map.set(key, valueNode);
}

/**
 * Render a YAML node back to text lines.
 *
 * @param {YamlNode} node YAML node.
 * @param {number} indent Current indentation.
 * @returns {string[]} YAML lines.
 */
function renderYaml(node, indent = 0, currentPath = "", commentMap = {}) {
    if (node.kind === "object") {
        return renderObjectNode(node, indent, currentPath, commentMap);
    }

    if (node.kind === "array") {
        return renderArrayNode(node, indent, currentPath, commentMap);
    }

    return [`${" ".repeat(indent)}${formatYamlScalar(node.value)}`];
}

/**
 * Render an object node.
 *
 * @param {YamlObjectNode} node Object node.
 * @param {number} indent Current indentation.
 * @returns {string[]} YAML lines.
 */
function renderObjectNode(node, indent, currentPath, commentMap) {
    const lines = [];
    for (const entry of node.entries) {
        const entryPath = joinPropertyPath(currentPath, entry.key);
        if (commentMap[entryPath]) {
            lines.push(...renderYamlComments(commentMap[entryPath], indent));
        }

        if (entry.node.kind === "scalar") {
            lines.push(`${" ".repeat(indent)}${entry.key}: ${formatYamlScalar(entry.node.value)}`);
            continue;
        }

        if (entry.node.kind === "array" && entry.node.items.length === 0) {
            lines.push(`${" ".repeat(indent)}${entry.key}: []`);
            continue;
        }

        lines.push(`${" ".repeat(indent)}${entry.key}:`);
        lines.push(...renderYaml(entry.node, indent + 2, entryPath, commentMap));
    }

    return lines;
}

/**
 * Render an array node.
 *
 * @param {YamlArrayNode} node Array node.
 * @param {number} indent Current indentation.
 * @returns {string[]} YAML lines.
 */
function renderArrayNode(node, indent, currentPath, commentMap) {
    const lines = [];
    for (let index = 0; index < node.items.length; index += 1) {
        const item = node.items[index];
        const itemPath = joinArrayIndexPath(currentPath, index);
        if (commentMap[itemPath]) {
            lines.push(...renderYamlComments(commentMap[itemPath], indent));
        }

        if (item.kind === "scalar") {
            lines.push(`${" ".repeat(indent)}- ${formatYamlScalar(item.value)}`);
            continue;
        }

        lines.push(`${" ".repeat(indent)}-`);
        lines.push(...renderYaml(item, indent + 2, itemPath, commentMap));
    }

    return lines;
}

/**
 * Create a scalar node.
 *
 * @param {string} value Scalar value.
 * @returns {YamlScalarNode} Scalar node.
 */
function createScalarNode(value) {
    return {kind: "scalar", value};
}

/**
 * Create an array node.
 *
 * @param {YamlNode[]} items Array items.
 * @returns {YamlArrayNode} Array node.
 */
function createArrayNode(items) {
    return {kind: "array", items};
}

/**
 * Convert one structured form value back into a YAML node tree.
 * Object-array editors submit plain JavaScript objects so the writer can
 * rebuild the full array deterministically instead of patching item paths
 * one by one.
 *
 * @param {unknown} value Structured form value.
 * @returns {YamlNode} YAML node.
 */
function createNodeFromFormValue(value) {
    if (Array.isArray(value)) {
        return createArrayNode(value.map((item) => createNodeFromFormValue(item)));
    }

    if (value && typeof value === "object") {
        const objectNode = createObjectNode();
        for (const [key, childValue] of Object.entries(value)) {
            setObjectEntry(objectNode, key, createNodeFromFormValue(childValue));
        }

        return objectNode;
    }

    return createScalarNode(String(value ?? ""));
}

/**
 * Create an object node.
 *
 * @returns {YamlObjectNode} Object node.
 */
function createObjectNode() {
    return {kind: "object", entries: [], map: new Map()};
}

/**
 * Build one example node recursively from schema metadata.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @returns {YamlNode} Example YAML node.
 */
function createSampleNodeFromSchema(schemaNode) {
    if (!schemaNode || schemaNode.type === "object") {
        const objectNode = createObjectNode();
        for (const [key, propertySchema] of Object.entries(schemaNode && schemaNode.properties ? schemaNode.properties : {})) {
            const childNode = createSampleNodeFromSchema(propertySchema);
            setObjectEntry(objectNode, key, childNode);
        }

        return objectNode;
    }

    if (schemaNode.type === "array") {
        if (schemaNode.items.type === "object") {
            return createArrayNode([createSampleNodeFromSchema(schemaNode.items)]);
        }

        return createArrayNode([createScalarNode(getSampleScalarValue(schemaNode.items))]);
    }

    return createScalarNode(getSampleScalarValue(schemaNode));
}

/**
 * Collect schema descriptions into a YAML comment lookup so sample configs can
 * start with human-readable guidance right above generated fields.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {string} currentPath Current logical path.
 * @param {Record<string, string>} commentMap Comment lookup.
 */
function collectSchemaComments(schemaNode, currentPath, commentMap) {
    if (!schemaNode || schemaNode.type !== "object") {
        return;
    }

    for (const [key, propertySchema] of Object.entries(schemaNode.properties || {})) {
        const propertyPath = joinPropertyPath(currentPath, key);
        if (propertySchema.description) {
            commentMap[propertyPath] = propertySchema.description;
        }

        if (propertySchema.type === "object") {
            collectSchemaComments(propertySchema, propertyPath, commentMap);
            continue;
        }

        if (propertySchema.type === "array" && propertySchema.items.type === "object") {
            collectSchemaComments(propertySchema.items, joinArrayIndexPath(propertyPath, 0), commentMap);
        }
    }
}

/**
 * Resolve one sample scalar value from schema metadata.
 *
 * @param {Extract<SchemaNode, {type: "string" | "integer" | "number" | "boolean"}>} schemaNode Scalar schema node.
 * @returns {string} Sample scalar value.
 */
function getSampleScalarValue(schemaNode) {
    if (schemaNode.defaultValue !== undefined) {
        return schemaNode.defaultValue;
    }

    if (Array.isArray(schemaNode.enumValues) && schemaNode.enumValues.length > 0) {
        return schemaNode.enumValues[0];
    }

    switch (schemaNode.type) {
        case "integer":
            return "0";
        case "number":
            return "0";
        case "boolean":
            return "false";
        case "string":
        default:
            return schemaNode.refTable
                ? "example_id"
                : "example";
    }
}

/**
 * Render one comment block to YAML lines.
 *
 * @param {string} commentText Comment text.
 * @param {number} indent Current indentation.
 * @returns {string[]} YAML comment lines.
 */
function renderYamlComments(commentText, indent) {
    return String(commentText)
        .split(/\r?\n/u)
        .filter((line) => line.length > 0)
        .map((line) => `${" ".repeat(indent)}# ${line}`);
}

/**
 * Assign pending comment lines to one logical path.
 *
 * @param {Record<string, string>} commentMap Comment lookup.
 * @param {string} path Logical path.
 * @param {string[]} pendingComments Pending comment lines.
 */
function assignPendingComments(commentMap, path, pendingComments) {
    if (!path || !Array.isArray(pendingComments) || pendingComments.length === 0) {
        return;
    }

    commentMap[path] = pendingComments.join("\n");
}

/**
 * Count leading spaces in one source line.
 *
 * @param {string} line Source line.
 * @returns {number} Leading-space count.
 */
function countLeadingSpaces(line) {
    const indentMatch = /^(\s*)/u.exec(line);
    return indentMatch ? indentMatch[1].length : 0;
}

/**
 * Find the next non-empty, non-comment source line.
 *
 * @param {string[]} lines Source lines.
 * @param {number} startIndex Starting index.
 * @returns {{indent: number, trimmed: string} | undefined} Next significant line.
 */
function findNextMeaningfulLine(lines, startIndex) {
    for (let index = startIndex; index < lines.length; index += 1) {
        const line = lines[index];
        const trimmed = line.trim();
        if (trimmed.length === 0 || trimmed.startsWith("#")) {
            continue;
        }

        return {
            indent: countLeadingSpaces(line),
            trimmed
        };
    }

    return undefined;
}

/**
 * Create one container context from the next meaningful line.
 *
 * @param {string} path Logical parent path.
 * @param {{indent: number, trimmed: string}} nextLine Next meaningful line.
 * @returns {{indent: number, type: "object" | "array", path: string, nextIndex: number}} Context model.
 */
function createContextForChild(path, nextLine) {
    return {
        indent: nextLine.indent,
        type: nextLine.trimmed.startsWith("-") ? "array" : "object",
        path,
        nextIndex: 0
    };
}

/**
 * Split a YAML value from one inline trailing comment.
 *
 * @param {string} rawValue Raw value segment after `key:`.
 * @returns {{value: string, comment?: string}} Parsed value and optional comment.
 */
function splitYamlValueAndInlineComment(rawValue) {
    let inSingleQuote = false;
    let inDoubleQuote = false;

    for (let index = 0; index < rawValue.length; index += 1) {
        const character = rawValue[index];
        if (character === "'" && !inDoubleQuote) {
            inSingleQuote = !inSingleQuote;
            continue;
        }

        if (character === "\"" && !inSingleQuote) {
            inDoubleQuote = !inDoubleQuote;
            continue;
        }

        if (character === "#" && !inSingleQuote && !inDoubleQuote && (index === 0 || /\s/u.test(rawValue[index - 1]))) {
            return {
                value: rawValue.slice(0, index).trimEnd(),
                comment: rawValue.slice(index + 1).trim()
            };
        }
    }

    return {value: rawValue};
}

/**
 * Parse one YAML mapping entry such as `key: value` or `"complex key": value`.
 *
 * @param {string} text Raw YAML line text without leading indentation.
 * @returns {{key: string, rawValue: string} | undefined} Parsed mapping entry.
 */
function parseYamlMappingText(text) {
    const separatorIndex = findYamlKeyValueSeparator(text);
    if (separatorIndex < 0) {
        return undefined;
    }

    const rawKey = text.slice(0, separatorIndex).trim();
    if (rawKey.length === 0) {
        return undefined;
    }

    return {
        key: normalizeYamlKey(rawKey),
        rawValue: text.slice(separatorIndex + 1)
    };
}

/**
 * Find the first `:` that acts as a YAML key/value separator.
 *
 * @param {string} text Raw YAML line text without leading indentation.
 * @returns {number} Separator index, or -1 when not found.
 */
function findYamlKeyValueSeparator(text) {
    let inSingleQuote = false;
    let inDoubleQuote = false;

    for (let index = 0; index < text.length; index += 1) {
        const character = text[index];
        if (character === "'" && !inDoubleQuote) {
            inSingleQuote = !inSingleQuote;
            continue;
        }

        if (character === "\"" && !inSingleQuote) {
            inDoubleQuote = !inDoubleQuote;
            continue;
        }

        if (character === ":" && !inSingleQuote && !inDoubleQuote) {
            return index;
        }
    }

    return -1;
}

/**
 * Normalize a YAML key token into the logical key name used in the form model.
 *
 * @param {string} rawKey Raw YAML key token.
 * @returns {string} Normalized key name.
 */
function normalizeYamlKey(rawKey) {
    return unquoteScalar(rawKey.trim());
}

module.exports = {
    applyFormUpdates,
    applyScalarUpdates,
    createSampleConfigYaml,
    extractYamlComments,
    getEditableSchemaFields,
    isEditableScalarType,
    isScalarCompatible,
    parseBatchArrayValue,
    parseSchemaContent,
    parseTopLevelYaml,
    unquoteScalar,
    validateParsedConfig
};

/**
 * @typedef {{
 *   type: "object",
 *   displayPath: string,
 *   required: string[],
 *   properties: Record<string, SchemaNode>,
 *   minProperties?: number,
 *   maxProperties?: number,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string
 * } | {
 *   type: "array",
 *   displayPath: string,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string,
 *   minItems?: number,
 *   maxItems?: number,
 *   uniqueItems?: boolean,
 *   refTable?: string,
 *   items: SchemaNode
 * } | {
 *   type: "string" | "integer" | "number" | "boolean",
 *   displayPath: string,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string,
 *   minimum?: number,
 *   exclusiveMinimum?: number,
 *   maximum?: number,
 *   exclusiveMaximum?: number,
 *   multipleOf?: number,
 *   minLength?: number,
 *   maxLength?: number,
 *   pattern?: string,
 *   patternRegex?: RegExp,
 *   enumValues?: string[],
 *   refTable?: string
 * }} SchemaNode
 */

/**
 * @typedef {{kind: "scalar", value: string}} YamlScalarNode
 * @typedef {{kind: "array", items: YamlNode[]}} YamlArrayNode
 * @typedef {{kind: "object", entries: Array<{key: string, node: YamlNode}>, map: Map<string, YamlNode>}} YamlObjectNode
 * @typedef {YamlScalarNode | YamlArrayNode | YamlObjectNode} YamlNode
 */
