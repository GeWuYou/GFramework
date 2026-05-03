// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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
const EmailFormatPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/u;
const UuidFormatPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;
const DateFormatPattern = /^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})$/u;
const DateTimeFormatPattern =
    /^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})T(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})(?<fraction>\.\d+)?(?<offset>Z|[+-]\d{2}:\d{2})$/u;
const DurationFormatPattern =
    /^P(?:(?<days>\d+)D)?(?:T(?:(?<hours>\d+)H)?(?:(?<minutes>\d+)M)?(?:(?<seconds>\d+(?:\.\d+)?)S)?)?$/u;
const TimeFormatPattern =
    /^(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})(?<fraction>\.\d+)?(?<offset>Z|[+-]\d{2}:\d{2})$/u;
const SupportedStringFormats = new Set(["date", "date-time", "duration", "email", "time", "uri", "uuid"]);
const SupportedSchemaTypes = new Set(["object", "array", "string", "integer", "number", "boolean"]);

/**
 * Compare two strings using the same UTF-16 code-unit ordering as C#'s
 * string.CompareOrdinal so tooling stays aligned with the runtime.
 *
 * @param {string} left Left operand.
 * @param {string} right Right operand.
 * @returns {number} Negative when left < right, positive when left > right, zero when equal.
 */
function compareStringsOrdinal(left, right) {
    if (left === right) {
        return 0;
    }

    return left < right ? -1 : 1;
}

/**
 * Parse the repository's minimal config-schema subset into a recursive tree.
 * The parser intentionally mirrors the same high-level contract used by the
 * runtime validator and source generator so tooling diagnostics stay aligned.
 *
 * @param {string} content Raw schema JSON text.
 * @throws {Error} Thrown when the schema declares one unsupported pattern or format string.
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

    return editableFields.sort((left, right) => compareStringsOrdinal(left.key, right.key));
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
 * Normalize one schema string-format declaration into the shared supported subset.
 * The tooling intentionally rejects unknown format names so editor diagnostics do
 * not drift away from the runtime and source generator.
 *
 * @param {unknown} value Raw schema value.
 * @param {string} schemaType Current schema type.
 * @param {string} displayPath Logical property path used in diagnostics.
 * @throws {Error} Thrown when the format value is invalid or unsupported for strings.
 * @returns {string | undefined} Normalized format name.
 */
function normalizeSchemaStringFormat(value, schemaType, displayPath) {
    if (value === undefined) {
        return undefined;
    }

    if (schemaType !== "string") {
        throw new Error(`Schema property '${displayPath}' can only declare 'format' on type 'string'.`);
    }

    if (typeof value !== "string") {
        throw new Error(`Schema property '${displayPath}' must declare 'format' as a string.`);
    }

    if (SupportedStringFormats.has(value)) {
        return value;
    }

    throw new Error(
        `Schema property '${displayPath}' declares unsupported string format '${value}'. ` +
        "Supported formats are 'date', 'date-time', 'duration', 'email', 'time', 'uri', and 'uuid'.");
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
 * Convert a schema const value into the raw scalar text used by sample YAML
 * generation and scalar editors.
 *
 * @param {SchemaNode} schemaNode Parsed schema node.
 * @param {unknown} value Raw schema const value.
 * @returns {string | undefined} Raw scalar text, or a JSON literal fallback.
 */
function formatSchemaConstEditableValue(schemaNode, value) {
    if (value === undefined) {
        return undefined;
    }

    if (schemaNode.type === "string" && typeof value === "string") {
        return value;
    }

    if ((schemaNode.type === "integer" || schemaNode.type === "number") &&
        typeof value === "number" &&
        Number.isFinite(value)) {
        return String(value);
    }

    if (schemaNode.type === "boolean" && typeof value === "boolean") {
        return String(value);
    }

    return formatSchemaConstDisplayValue(value);
}

/**
 * Convert a schema const value into an exact JSON-style literal for diagnostics
 * and metadata hints.
 *
 * @param {unknown} value Raw schema const value.
 * @returns {string | undefined} Display string for the const value.
 */
function formatSchemaConstDisplayValue(value) {
    if (value === undefined) {
        return undefined;
    }

    if (typeof value === "string") {
        return JSON.stringify(value);
    }

    if (typeof value === "number" || typeof value === "boolean") {
        return String(value);
    }

    if (value === null || Array.isArray(value) || typeof value === "object") {
        return JSON.stringify(value);
    }

    return undefined;
}

/**
 * Attach parsed const metadata to one schema node.
 *
 * @param {SchemaNode} schemaNode Parsed schema node.
 * @param {unknown} rawConst Raw schema const value.
 * @param {string} displayPath Logical property path.
 * @returns {SchemaNode} Schema node with optional const metadata.
 */
function applyConstMetadata(schemaNode, rawConst, displayPath) {
    if (rawConst === undefined) {
        return schemaNode;
    }

    return {
        ...schemaNode,
        constValue: formatSchemaConstEditableValue(schemaNode, rawConst),
        constDisplayValue: formatSchemaConstDisplayValue(rawConst),
        constComparableValue: buildSchemaConstComparableValue(schemaNode, rawConst, displayPath)
    };
}

/**
 * Attach parsed enum metadata to one schema node.
 *
 * @param {SchemaNode} schemaNode Parsed schema node.
 * @param {unknown} rawEnum Raw schema enum value.
 * @param {string} displayPath Logical property path.
 * @returns {SchemaNode} Schema node with optional enum metadata.
 */
function applyEnumMetadata(schemaNode, rawEnum, displayPath) {
    if (!Array.isArray(rawEnum)) {
        return schemaNode;
    }

    if (rawEnum.length === 0) {
        throw new Error(`Schema property '${displayPath}' must declare 'enum' with at least one value.`);
    }

    const enumComparableValues = [];
    const enumDisplayValues = [];
    const enumValues = [];

    for (const item of rawEnum) {
        enumComparableValues.push(buildSchemaConstComparableValue(schemaNode, item, displayPath));

        const displayValue = formatSchemaConstDisplayValue(item);
        if (displayValue !== undefined) {
            enumDisplayValues.push(displayValue);
        }

        if (schemaNode.type !== "object" && schemaNode.type !== "array") {
            const editableValue = formatSchemaConstEditableValue(schemaNode, item);
            if (editableValue !== undefined) {
                enumValues.push(editableValue);
            }
        }
    }

    return {
        ...schemaNode,
        enumSampleValue: rawEnum[0],
        enumValues: enumValues.length > 0 ? enumValues : undefined,
        enumDisplayValues: enumDisplayValues.length > 0 ? enumDisplayValues : undefined,
        enumComparableValues: enumComparableValues.length > 0 ? enumComparableValues : undefined
    };
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
 * Test one scalar value against one shared string-format constraint.
 *
 * @param {string} scalarValue Scalar value from YAML.
 * @param {string | undefined} formatName Normalized schema format name.
 * @returns {boolean} True when compatible or no format is declared.
 */
function matchesSchemaStringFormat(scalarValue, formatName) {
    if (typeof formatName !== "string") {
        return true;
    }

    switch (formatName) {
        case "date":
            return matchesSchemaDateFormat(scalarValue);
        case "date-time":
            return matchesSchemaDateTimeFormat(scalarValue);
        case "duration":
            return matchesSchemaDurationFormat(scalarValue);
        case "email":
            return EmailFormatPattern.test(scalarValue);
        case "time":
            return matchesSchemaTimeFormat(scalarValue);
        case "uri":
            return matchesSchemaUriFormat(scalarValue);
        case "uuid":
            return UuidFormatPattern.test(scalarValue);
        default:
            return false;
    }
}

/**
 * Validate one RFC 3339 full-date string.
 *
 * @param {string} scalarValue Scalar value from YAML.
 * @returns {boolean} True when the value is a valid calendar date.
 */
function matchesSchemaDateFormat(scalarValue) {
    const match = DateFormatPattern.exec(scalarValue);
    if (!match || !match.groups) {
        return false;
    }

    const year = Number.parseInt(match.groups.year, 10);
    const month = Number.parseInt(match.groups.month, 10);
    const day = Number.parseInt(match.groups.day, 10);
    return isValidCalendarDate(year, month, day);
}

/**
 * Validate one RFC 3339 date-time string with explicit timezone offset.
 *
 * @param {string} scalarValue Scalar value from YAML.
 * @returns {boolean} True when the value is structurally and calendrically valid.
 */
function matchesSchemaDateTimeFormat(scalarValue) {
    const match = DateTimeFormatPattern.exec(scalarValue);
    if (!match || !match.groups) {
        return false;
    }

    const year = Number.parseInt(match.groups.year, 10);
    const month = Number.parseInt(match.groups.month, 10);
    const day = Number.parseInt(match.groups.day, 10);
    if (!isValidCalendarDate(year, month, day)) {
        return false;
    }

    const hour = Number.parseInt(match.groups.hour, 10);
    const minute = Number.parseInt(match.groups.minute, 10);
    const second = Number.parseInt(match.groups.second, 10);
    if (hour > 23 || minute > 59 || second > 59) {
        return false;
    }

    const offset = match.groups.offset;
    if (offset === "Z") {
        return true;
    }

    const offsetHour = Number.parseInt(offset.slice(1, 3), 10);
    const offsetMinute = Number.parseInt(offset.slice(4, 6), 10);
    return offsetHour <= 23 && offsetMinute <= 59;
}

/**
 * Validate one shared day-time duration string.
 *
 * @param {string} scalarValue Scalar value from YAML.
 * @returns {boolean} True when the value stays within the shared day-time subset.
 */
function matchesSchemaDurationFormat(scalarValue) {
    const match = DurationFormatPattern.exec(scalarValue);
    if (!match || !match.groups) {
        return false;
    }

    const hasDayComponent = match.groups.days !== undefined;
    const hasHourComponent = match.groups.hours !== undefined;
    const hasMinuteComponent = match.groups.minutes !== undefined;
    const hasSecondComponent = match.groups.seconds !== undefined;
    const hasAnyComponent = hasDayComponent || hasHourComponent || hasMinuteComponent || hasSecondComponent;
    if (!hasAnyComponent) {
        return false;
    }

    const hasTimeSection = scalarValue.includes("T");
    if (hasTimeSection && !hasHourComponent && !hasMinuteComponent && !hasSecondComponent) {
        return false;
    }

    return true;
}

/**
 * Validate one RFC 3339 full-time string with explicit timezone offset.
 *
 * @param {string} scalarValue Scalar value from YAML.
 * @returns {boolean} True when the value is structurally valid.
 */
function matchesSchemaTimeFormat(scalarValue) {
    const match = TimeFormatPattern.exec(scalarValue);
    if (!match || !match.groups) {
        return false;
    }

    const hour = Number.parseInt(match.groups.hour, 10);
    const minute = Number.parseInt(match.groups.minute, 10);
    const second = Number.parseInt(match.groups.second, 10);
    if (hour > 23 || minute > 59 || second > 59) {
        return false;
    }

    const offset = match.groups.offset;
    if (offset === "Z") {
        return true;
    }

    const offsetHour = Number.parseInt(offset.slice(1, 3), 10);
    const offsetMinute = Number.parseInt(offset.slice(4, 6), 10);
    return offsetHour <= 23 && offsetMinute <= 59;
}

/**
 * Validate one absolute URI string using the platform URL parser.
 *
 * @param {string} scalarValue Scalar value from YAML.
 * @returns {boolean} True when the value parses as an absolute URI.
 */
function matchesSchemaUriFormat(scalarValue) {
    try {
        const parsed = new URL(scalarValue);
        return typeof parsed.protocol === "string" && parsed.protocol.length > 1;
    } catch {
        return false;
    }
}

/**
 * Check whether one year-month-day triple forms a valid calendar date.
 *
 * @param {number} year Year component.
 * @param {number} month Month component.
 * @param {number} day Day component.
 * @returns {boolean} True when the date exists in the Gregorian calendar.
 */
function isValidCalendarDate(year, month, day) {
    if (!Number.isInteger(year) || !Number.isInteger(month) || !Number.isInteger(day)) {
        return false;
    }

    if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1) {
        return false;
    }

    const isLeapYear = (year % 4 === 0 && year % 100 !== 0) || (year % 400 === 0);
    const monthDays = [31, isLeapYear ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    const lastDay = monthDays[month - 1];
    return day <= lastDay;
}

/**
 * Build one schema-normalized comparable key for a const value declared in
 * JSON Schema so tooling comparisons align with runtime comparisons.
 *
 * @param {SchemaNode} schemaNode Parsed schema node.
 * @param {unknown} rawConst Raw schema const value.
 * @param {string} displayPath Logical property path.
 * @returns {string} Comparable key.
 */
function buildSchemaConstComparableValue(schemaNode, rawConst, displayPath) {
    if (schemaNode.type === "object") {
        return buildSchemaConstObjectComparableValue(schemaNode, rawConst, displayPath);
    }

    if (schemaNode.type === "array") {
        return buildSchemaConstArrayComparableValue(schemaNode, rawConst, displayPath);
    }

    return buildSchemaConstScalarComparableValue(schemaNode, rawConst, displayPath);
}

/**
 * Build one comparable key for an object-shaped const value.
 *
 * @param {Extract<SchemaNode, {type: "object"}>} schemaNode Parsed object schema node.
 * @param {unknown} rawConst Raw schema const value.
 * @param {string} displayPath Logical property path.
 * @returns {string} Comparable key.
 */
function buildSchemaConstObjectComparableValue(schemaNode, rawConst, displayPath) {
    if (!rawConst || typeof rawConst !== "object" || Array.isArray(rawConst)) {
        throw new Error(`Schema property '${displayPath}' declares 'const', but the value is not compatible with schema type 'object'.`);
    }

    const objectEntries = [];
    for (const [key, value] of Object.entries(rawConst)) {
        if (!Object.prototype.hasOwnProperty.call(schemaNode.properties, key)) {
            const childPath = joinPropertyPath(displayPath, key);
            throw new Error(`Schema property '${displayPath}' declares 'const', but nested property '${childPath}' is not declared in the object schema.`);
        }

        const childComparableValue = buildSchemaConstComparableValue(
            schemaNode.properties[key],
            value,
            joinPropertyPath(displayPath, key));
        objectEntries.push([key, childComparableValue]);
    }

    objectEntries.sort((left, right) => compareStringsOrdinal(left[0], right[0]));
    return objectEntries.map(([key, value]) => `${key.length}:${key}=${value.length}:${value}`).join("|");
}

/**
 * Build one comparable key for an array-shaped const value.
 *
 * @param {Extract<SchemaNode, {type: "array"}>} schemaNode Parsed array schema node.
 * @param {unknown} rawConst Raw schema const value.
 * @param {string} displayPath Logical property path.
 * @returns {string} Comparable key.
 */
function buildSchemaConstArrayComparableValue(schemaNode, rawConst, displayPath) {
    if (!Array.isArray(rawConst)) {
        throw new Error(`Schema property '${displayPath}' declares 'const', but the value is not compatible with schema type 'array'.`);
    }

    return `[${rawConst.map((item, index) => {
        const comparableValue = buildSchemaConstComparableValue(
            schemaNode.items,
            item,
            joinArrayIndexPath(displayPath, index));
        return `${comparableValue.length}:${comparableValue}`;
    }).join(",")}]`;
}

/**
 * Build one comparable key for a scalar const value.
 *
 * @param {Extract<SchemaNode, {type: "string" | "integer" | "number" | "boolean"}>} schemaNode Parsed scalar schema node.
 * @param {unknown} rawConst Raw schema const value.
 * @param {string} displayPath Logical property path.
 * @returns {string} Comparable key.
 */
function buildSchemaConstScalarComparableValue(schemaNode, rawConst, displayPath) {
    const normalizedValue = normalizeSchemaConstScalarValue(schemaNode.type, rawConst, displayPath);
    return `${schemaNode.type}:${normalizedValue.length}:${normalizedValue}`;
}

/**
 * Normalize one scalar const value into the same comparison format used by
 * parsed YAML scalar nodes.
 *
 * @param {"string" | "integer" | "number" | "boolean"} schemaType Scalar schema type.
 * @param {unknown} rawConst Raw schema const value.
 * @param {string} displayPath Logical property path.
 * @returns {string} Normalized scalar value.
 */
function normalizeSchemaConstScalarValue(schemaType, rawConst, displayPath) {
    switch (schemaType) {
        case "integer":
            if (typeof rawConst === "number" && Number.isInteger(rawConst)) {
                return String(rawConst);
            }
            break;
        case "number":
            if (typeof rawConst === "number" && Number.isFinite(rawConst)) {
                return String(rawConst);
            }
            break;
        case "boolean":
            if (typeof rawConst === "boolean") {
                return String(rawConst);
            }
            break;
        case "string":
            if (typeof rawConst === "string") {
                return rawConst;
            }
            break;
        default:
            break;
    }

    throw new Error(`Schema property '${displayPath}' declares 'const', but the value is not compatible with schema type '${schemaType}'.`);
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
    const unsupportedCombinatorKeyword = getUnsupportedCombinatorKeywordName(value);
    if (unsupportedCombinatorKeyword) {
        throw new Error(
            `Schema property '${displayPath}' declares unsupported combinator keyword '${unsupportedCombinatorKeyword}'. ` +
            "The current config schema subset does not support combinators that can change generated type shape.");
    }

    validateUnsupportedOpenObjectKeyword(value, displayPath);

    const type = resolveSupportedSchemaType(value.type, displayPath);
    const patternMetadata = normalizeSchemaPattern(value.pattern, displayPath);
    const stringFormat = normalizeSchemaStringFormat(value.format, type, displayPath);
    const negatedSchemaNode = parseNegatedSchemaNode(value.not, displayPath);
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
        format: stringFormat,
        minItems: normalizeSchemaNonNegativeInteger(value.minItems),
        maxItems: normalizeSchemaNonNegativeInteger(value.maxItems),
        minContains: normalizeSchemaNonNegativeInteger(value.minContains),
        maxContains: normalizeSchemaNonNegativeInteger(value.maxContains),
        minProperties: normalizeSchemaNonNegativeInteger(value.minProperties),
        maxProperties: normalizeSchemaNonNegativeInteger(value.maxProperties),
        uniqueItems: normalizeSchemaBoolean(value.uniqueItems),
        refTable: typeof value["x-gframework-ref-table"] === "string"
            ? value["x-gframework-ref-table"]
            : undefined
    };

    if (value.allOf !== undefined && type !== "object") {
        throw new Error(`Only object schemas can declare 'allOf' at '${displayPath}'.`);
    }

    if ((value.if !== undefined || value.then !== undefined || value.else !== undefined) &&
        type !== "object") {
        throw new Error(`Only object schemas can declare 'if', 'then', or 'else' at '${displayPath}'.`);
    }

    if (type === "object") {
        const required = Array.isArray(value.required)
            ? value.required.filter((item) => typeof item === "string")
            : [];
        const properties = {};
        for (const [key, propertyNode] of Object.entries(value.properties || {})) {
            properties[key] = parseSchemaNode(propertyNode, joinPropertyPath(displayPath, key));
        }
        const dependentRequired = parseDependentRequiredMetadata(value.dependentRequired, displayPath, properties);
        const dependentSchemas = parseDependentSchemasMetadata(value.dependentSchemas, displayPath, properties);
        const allOf = parseAllOfSchemaNodes(value.allOf, displayPath, properties);
        const conditionalSchemas = parseConditionalSchemaMetadata(value.if, value.then, value.else, displayPath, properties);

        return applyEnumMetadata(applyConstMetadata({
            type: "object",
            displayPath,
            required,
            properties,
            minProperties: metadata.minProperties,
            maxProperties: metadata.maxProperties,
            dependentRequired,
            dependentSchemas,
            allOf,
            ifSchema: conditionalSchemas ? conditionalSchemas.ifSchema : undefined,
            thenSchema: conditionalSchemas ? conditionalSchemas.thenSchema : undefined,
            elseSchema: conditionalSchemas ? conditionalSchemas.elseSchema : undefined,
            title: metadata.title,
            description: metadata.description,
            defaultValue: metadata.defaultValue,
            not: negatedSchemaNode
        }, value.const, displayPath), value.enum, displayPath);
    }

    if (type === "array") {
        const itemNode = parseRequiredArrayChildSchema(value.items, displayPath, "items");
        const containsNode = value.contains === undefined
            ? undefined
            : parseOptionalArrayChildSchema(value.contains, displayPath, "contains");
        if (!containsNode &&
            (typeof metadata.minContains === "number" || typeof metadata.maxContains === "number")) {
            throw new Error(`Schema property '${displayPath}' declares 'minContains' or 'maxContains' without 'contains'.`);
        }

        if (itemNode.type === "array") {
            throw new Error(`Schema property '${displayPath}' uses unsupported nested array items.`);
        }

        if (containsNode && containsNode.type === "array") {
            throw new Error(`Schema property '${displayPath}' uses unsupported nested array 'contains' schemas.`);
        }

        const effectiveMinContains = containsNode
            ? (typeof metadata.minContains === "number" ? metadata.minContains : 1)
            : undefined;
        if (containsNode &&
            typeof metadata.maxContains === "number" &&
            effectiveMinContains > metadata.maxContains) {
            throw new Error(`Schema property '${displayPath}' declares 'minContains' greater than 'maxContains'.`);
        }

        return applyEnumMetadata(applyConstMetadata({
            type: "array",
            displayPath,
            title: metadata.title,
            description: metadata.description,
            defaultValue: metadata.defaultValue,
            minItems: metadata.minItems,
            maxItems: metadata.maxItems,
            minContains: containsNode
                ? metadata.minContains
                : undefined,
            maxContains: containsNode
                ? metadata.maxContains
                : undefined,
            uniqueItems: metadata.uniqueItems === true,
            refTable: metadata.refTable,
            contains: containsNode,
            items: itemNode,
            not: negatedSchemaNode
        }, value.const, displayPath), value.enum, displayPath);
    }

    return applyEnumMetadata(applyConstMetadata({
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
        format: type === "string"
            ? metadata.format
            : undefined,
        refTable: metadata.refTable,
        not: negatedSchemaNode
    }, value.const, displayPath), value.enum, displayPath);
}

/**
 * Reject open-object keyword forms that would drift away from the Runtime and
 * Source Generator contracts. The current shared subset keeps object fields
 * closed and only accepts an explicit `additionalProperties: false` reminder.
 *
 * @param {Record<string, unknown>} schemaNode Raw schema object.
 * @param {string} displayPath Logical property path.
 */
function validateUnsupportedOpenObjectKeyword(schemaNode, displayPath) {
    if (!Object.prototype.hasOwnProperty.call(schemaNode, "additionalProperties")) {
        return;
    }

    if (schemaNode.additionalProperties === false) {
        return;
    }

    throw new Error(
        `Schema property '${displayPath}' uses unsupported 'additionalProperties' metadata. ` +
        "The current config schema subset only accepts 'additionalProperties: false' so object fields remain closed and strongly typed.");
}

/**
 * Parse one required array child schema while keeping tooling errors aligned
 * with the Runtime and Source Generator contracts.
 *
 * @param {unknown} rawChild Raw child schema node.
 * @param {string} displayPath Logical parent array path.
 * @param {"items" | "contains"} keywordName Child schema keyword.
 * @returns {SchemaNode} Parsed child schema node.
 */
function parseRequiredArrayChildSchema(rawChild, displayPath, keywordName) {
    return parseArrayChildSchema(rawChild, displayPath, keywordName);
}

/**
 * Parse one optional array child schema when it is present.
 *
 * @param {unknown} rawChild Raw child schema node.
 * @param {string} displayPath Logical parent array path.
 * @param {"items" | "contains"} keywordName Child schema keyword.
 * @returns {SchemaNode | undefined} Parsed child schema node.
 */
function parseOptionalArrayChildSchema(rawChild, displayPath, keywordName) {
    return parseArrayChildSchema(rawChild, displayPath, keywordName);
}

/**
 * Parse one array child schema only when it is object-shaped and explicitly
 * typed. This avoids silently treating tuple arrays or malformed child
 * schemas as empty object nodes.
 *
 * @param {unknown} rawChild Raw child schema node.
 * @param {string} displayPath Logical parent array path.
 * @param {"items" | "contains"} keywordName Child schema keyword.
 * @returns {SchemaNode} Parsed child schema node.
 */
function parseArrayChildSchema(rawChild, displayPath, keywordName) {
    if (!rawChild || typeof rawChild !== "object" || Array.isArray(rawChild)) {
        throw new Error(
            `Schema property '${displayPath}' must declare '${keywordName}' as an object-valued schema with an explicit 'type'.`);
    }

    if (typeof rawChild.type !== "string") {
        throw new Error(
            `Schema property '${displayPath}' must declare '${keywordName}' as an object-valued schema with an explicit 'type'.`);
    }

    return parseSchemaNode(rawChild, joinArrayTemplatePath(displayPath));
}

/**
 * Resolve one schema type while rejecting explicit strings that the shared
 * subset does not support.
 *
 * @param {unknown} rawType Raw schema type value.
 * @param {string} displayPath Logical property path.
 * @returns {"object" | "array" | "string" | "integer" | "number" | "boolean"} Supported schema type.
 */
function resolveSupportedSchemaType(rawType, displayPath) {
    if (typeof rawType !== "string") {
        return "object";
    }

    if (!SupportedSchemaTypes.has(rawType)) {
        throw new Error(`Schema property '${displayPath}' declares unsupported type '${rawType}'.`);
    }

    return rawType;
}

/**
 * Return the first combinator keyword that the current shared schema subset
 * intentionally rejects to keep Runtime / Generator / Tooling behavior aligned.
 *
 * @param {Record<string, unknown>} schemaNode Raw schema object.
 * @returns {string | undefined} Unsupported keyword name when present.
 */
function getUnsupportedCombinatorKeywordName(schemaNode) {
    if (Object.prototype.hasOwnProperty.call(schemaNode, "oneOf")) {
        return "oneOf";
    }

    if (Object.prototype.hasOwnProperty.call(schemaNode, "anyOf")) {
        return "anyOf";
    }

    return undefined;
}

/**
 * Parse one optional `not` sub-schema and keep path formatting aligned with
 * the runtime/generator diagnostics.
 *
 * @param {unknown} rawNot Raw `not` node.
 * @param {string} displayPath Parent schema path.
 * @returns {SchemaNode | undefined} Parsed negated schema node.
 */
function parseNegatedSchemaNode(rawNot, displayPath) {
    if (rawNot === undefined) {
        return undefined;
    }

    if (!rawNot || typeof rawNot !== "object" || Array.isArray(rawNot)) {
        throw new Error(`Schema property '${displayPath}' must declare 'not' as an object-valued schema.`);
    }

    return parseSchemaNode(rawNot, `${displayPath}[not]`);
}

/**
 * Parse one object-level `dependentRequired` map and keep it aligned with the
 * runtime's "declared siblings only" contract.
 *
 * @param {unknown} rawDependentRequired Raw dependentRequired node.
 * @param {string} displayPath Parent schema path.
 * @param {Record<string, SchemaNode>} properties Declared object properties.
 * @returns {Record<string, string[]> | undefined} Normalized dependency map.
 */
function parseDependentRequiredMetadata(rawDependentRequired, displayPath, properties) {
    if (rawDependentRequired === undefined) {
        return undefined;
    }

    if (!rawDependentRequired ||
        typeof rawDependentRequired !== "object" ||
        Array.isArray(rawDependentRequired)) {
        throw new Error(`Schema property '${displayPath}' must declare 'dependentRequired' as an object.`);
    }

    const normalized = {};
    for (const [triggerProperty, rawDependencies] of Object.entries(rawDependentRequired)) {
        if (!Object.prototype.hasOwnProperty.call(properties, triggerProperty)) {
            throw new Error(
                `Schema property '${displayPath}' declares 'dependentRequired' for undeclared property '${triggerProperty}'.`);
        }

        if (!Array.isArray(rawDependencies)) {
            throw new Error(
                `Schema property '${displayPath}' must declare 'dependentRequired' for '${triggerProperty}' as an array of sibling property names.`);
        }

        const dependencies = [];
        const seenDependencies = new Set();
        for (const dependency of rawDependencies) {
            if (typeof dependency !== "string") {
                throw new Error(
                    `Schema property '${displayPath}' must declare 'dependentRequired' entries for '${triggerProperty}' as strings.`);
            }

            if (dependency.trim().length === 0) {
                throw new Error(
                    `Schema property '${displayPath}' cannot declare blank 'dependentRequired' entries for '${triggerProperty}'.`);
            }

            if (!Object.prototype.hasOwnProperty.call(properties, dependency)) {
                throw new Error(
                    `Schema property '${displayPath}' declares 'dependentRequired' target '${dependency}' that is not declared in the same object schema.`);
            }

            if (!seenDependencies.has(dependency)) {
                seenDependencies.add(dependency);
                dependencies.push(dependency);
            }
        }

        if (dependencies.length > 0) {
            normalized[triggerProperty] = dependencies;
        }
    }

    return Object.keys(normalized).length > 0
        ? normalized
        : undefined;
}

/**
 * Parse one object-level `dependentSchemas` map and keep it aligned with the
 * runtime's "declared siblings trigger object-typed inline schemas" contract.
 *
 * @param {unknown} rawDependentSchemas Raw dependentSchemas node.
 * @param {string} displayPath Parent schema path.
 * @param {Record<string, SchemaNode>} properties Declared object properties.
 * @returns {Record<string, SchemaNode> | undefined} Normalized dependency schema map.
 */
function parseDependentSchemasMetadata(rawDependentSchemas, displayPath, properties) {
    if (rawDependentSchemas === undefined) {
        return undefined;
    }

    if (!rawDependentSchemas ||
        typeof rawDependentSchemas !== "object" ||
        Array.isArray(rawDependentSchemas)) {
        throw new Error(`Schema property '${displayPath}' must declare 'dependentSchemas' as an object.`);
    }

    const normalized = {};
    for (const [triggerProperty, rawDependencySchema] of Object.entries(rawDependentSchemas)) {
        if (!Object.prototype.hasOwnProperty.call(properties, triggerProperty)) {
            throw new Error(
                `Schema property '${displayPath}' declares 'dependentSchemas' for undeclared property '${triggerProperty}'.`);
        }

        if (!rawDependencySchema ||
            typeof rawDependencySchema !== "object" ||
            Array.isArray(rawDependencySchema)) {
            throw new Error(
                `Schema property '${displayPath}' must declare 'dependentSchemas' for '${triggerProperty}' as an object-valued schema.`);
        }

        const dependencySchema = parseSchemaNode(
            rawDependencySchema,
            `${displayPath}[dependentSchemas:${triggerProperty}]`);
        if (dependencySchema.type !== "object") {
            throw new Error(
                `Schema property '${displayPath}' must declare an object-typed 'dependentSchemas' schema for '${triggerProperty}'.`);
        }

        normalized[triggerProperty] = dependencySchema;
    }

    return Object.keys(normalized).length > 0
        ? normalized
        : undefined;
}

/**
 * Parse one object-level `allOf` list and keep it aligned with the runtime's
 * focused-constraint-block contract.
 *
 * @param {unknown} rawAllOf Raw `allOf` node.
 * @param {string} displayPath Parent schema path.
 * @param {Record<string, SchemaNode>} properties Declared object properties.
 * @returns {SchemaNode[] | undefined} Normalized allOf schema list.
 */
function parseAllOfSchemaNodes(rawAllOf, displayPath, properties) {
    if (rawAllOf === undefined) {
        return undefined;
    }

    if (!Array.isArray(rawAllOf)) {
        throw new Error(`Schema property '${displayPath}' must declare 'allOf' as an array.`);
    }

    const normalized = [];
    for (let index = 0; index < rawAllOf.length; index += 1) {
        const rawAllOfSchema = rawAllOf[index];
        if (!rawAllOfSchema || typeof rawAllOfSchema !== "object" || Array.isArray(rawAllOfSchema)) {
            throw new Error(
                `Schema property '${displayPath}' must declare 'allOf' entry #${index + 1} as an object-valued schema.`);
        }

        if (rawAllOfSchema.type !== "object") {
            throw new Error(
                `Schema property '${displayPath}' must declare object-typed schemas in 'allOf' entry #${index + 1}.`);
        }

        validateAllOfEntryTargets(rawAllOfSchema, displayPath, index, properties);
        const allOfSchema = parseSchemaNode(rawAllOfSchema, `${displayPath}[allOf[${index}]]`);
        normalized.push(allOfSchema);
    }

    return normalized.length > 0
        ? normalized
        : undefined;
}

/**
 * Parse one object-level `if/then/else` group and keep it aligned with the
 * runtime's object-focused conditional constraint contract.
 *
 * @param {unknown} rawIf Raw `if` node.
 * @param {unknown} rawThen Raw `then` node.
 * @param {unknown} rawElse Raw `else` node.
 * @param {string} displayPath Parent schema path.
 * @param {Record<string, SchemaNode>} properties Declared parent properties.
 * @returns {{ifSchema: SchemaNode, thenSchema?: SchemaNode, elseSchema?: SchemaNode} | undefined} Normalized conditional schema group.
 */
function parseConditionalSchemaMetadata(rawIf, rawThen, rawElse, displayPath, properties) {
    const hasIf = rawIf !== undefined;
    const hasThen = rawThen !== undefined;
    const hasElse = rawElse !== undefined;
    if (!hasIf && !hasThen && !hasElse) {
        return undefined;
    }

    if (!hasIf) {
        throw new Error(`Schema property '${displayPath}' must declare 'if' when using 'then' or 'else'.`);
    }

    if (!hasThen && !hasElse) {
        throw new Error(`Schema property '${displayPath}' must declare at least one of 'then' or 'else' when using 'if'.`);
    }

    const ifSchema = parseConditionalObjectSchema(rawIf, displayPath, "if", properties);
    const conditionalSchemas = {ifSchema};

    if (hasThen) {
        conditionalSchemas.thenSchema = parseConditionalObjectSchema(rawThen, displayPath, "then", properties);
    }

    if (hasElse) {
        conditionalSchemas.elseSchema = parseConditionalObjectSchema(rawElse, displayPath, "else", properties);
    }

    return conditionalSchemas;
}

/**
 * Parse one object-focused conditional branch schema.
 *
 * @param {unknown} rawSchema Raw branch schema.
 * @param {string} displayPath Parent schema path.
 * @param {"if" | "then" | "else"} keywordName Branch keyword.
 * @param {Record<string, SchemaNode>} properties Declared parent properties.
 * @returns {SchemaNode} Parsed object-typed branch schema.
 */
function parseConditionalObjectSchema(rawSchema, displayPath, keywordName, properties) {
    if (!rawSchema || typeof rawSchema !== "object" || Array.isArray(rawSchema)) {
        throw new Error(`Schema property '${displayPath}' must declare '${keywordName}' as an object-valued schema.`);
    }

    if (rawSchema.type !== "object") {
        throw new Error(`Schema property '${displayPath}' must declare an object-typed '${keywordName}' schema.`);
    }

    validateConditionalSchemaTargets(rawSchema, displayPath, keywordName, properties);
    const conditionalSchema = parseSchemaNode(rawSchema, `${displayPath}[${keywordName}]`);
    if (conditionalSchema.type !== "object") {
        throw new Error(`Schema property '${displayPath}' must declare an object-typed '${keywordName}' schema.`);
    }

    return conditionalSchema;
}

/**
 * Ensure one object-focused conditional branch only constrains properties that
 * the parent object schema already declared.
 *
 * @param {unknown} rawSchema Raw branch schema.
 * @param {string} displayPath Parent schema path.
 * @param {"if" | "then" | "else"} keywordName Branch keyword.
 * @param {Record<string, SchemaNode>} properties Declared parent properties.
 */
function validateConditionalSchemaTargets(rawSchema, displayPath, keywordName, properties) {
    validateDeclaredTargetReferences(rawSchema, displayPath, `'${keywordName}'`, properties);
}

/**
 * Ensure one object-focused `allOf` entry only constrains properties that the
 * parent object schema already declared.
 *
 * @param {unknown} rawAllOfSchema Raw allOf entry.
 * @param {string} displayPath Parent schema path.
 * @param {number} index Zero-based allOf entry index.
 * @param {Record<string, SchemaNode>} properties Declared parent properties.
 */
function validateAllOfEntryTargets(rawAllOfSchema, displayPath, index, properties) {
    validateDeclaredTargetReferences(rawAllOfSchema, displayPath, `'allOf' entry #${index + 1}`, properties);
}

/**
 * Ensure one focused object schema only references properties that the parent
 * object schema already declared.
 *
 * @param {unknown} rawSchema Raw object-focused schema.
 * @param {string} displayPath Parent schema path.
 * @param {string} contextLabel Human-readable constraint origin label.
 * @param {Record<string, SchemaNode>} properties Declared parent properties.
 */
function validateDeclaredTargetReferences(rawSchema, displayPath, contextLabel, properties) {
    if (!rawSchema || typeof rawSchema !== "object" || Array.isArray(rawSchema)) {
        return;
    }

    if (rawSchema.properties !== undefined) {
        if (!rawSchema.properties ||
            typeof rawSchema.properties !== "object" ||
            Array.isArray(rawSchema.properties)) {
            throw new Error(
                `Schema property '${displayPath}' must declare 'properties' in ${contextLabel} as an object-valued map.`);
        }

        for (const propertyName of Object.keys(rawSchema.properties)) {
            if (Object.prototype.hasOwnProperty.call(properties, propertyName)) {
                continue;
            }

            throw new Error(
                `Schema property '${displayPath}' declares property '${propertyName}' in ${contextLabel}, ` +
                "but that property is not declared in the parent object schema.");
        }
    }

    if (rawSchema.required === undefined) {
        return;
    }

    if (!Array.isArray(rawSchema.required)) {
        throw new Error(
            `Schema property '${displayPath}' must declare 'required' in ${contextLabel} as an array of property names.`);
    }

    for (const requiredProperty of rawSchema.required) {
        if (typeof requiredProperty !== "string") {
            throw new Error(
                `Schema property '${displayPath}' must declare 'required' entries in ${contextLabel} as property-name strings.`);
        }

        if (requiredProperty.trim().length === 0) {
            throw new Error(
                `Schema property '${displayPath}' cannot declare blank property names in 'required' for ${contextLabel}.`);
        }

        if (Object.prototype.hasOwnProperty.call(properties, requiredProperty)) {
            continue;
        }

        throw new Error(
            `Schema property '${displayPath}' requires property '${requiredProperty}' in ${contextLabel}, ` +
            "but that property is not declared in the parent object schema.");
    }
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
        const diagnosticsBeforeNode = diagnostics.length;
        validateObjectNode(schemaNode, yamlNode, displayPath, diagnostics, localizer, diagnosticsBeforeNode);
        return;
    }

    if (schemaNode.type === "array") {
        const diagnosticsBeforeNode = diagnostics.length;
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
        const containsCandidateItems = [];
        let hasStructurallyInvalidArrayItems = false;
        for (let index = 0; index < yamlNode.items.length; index += 1) {
            const diagnosticsBeforeValidation = diagnostics.length;
            validateNode(
                schemaNode.items,
                yamlNode.items[index],
                joinArrayIndexPath(displayPath, index),
                diagnostics,
                localizer);

            if (isStructurallyCompatibleWithSchemaNode(schemaNode.items, yamlNode.items[index])) {
                containsCandidateItems.push({index, node: yamlNode.items[index]});
            } else {
                hasStructurallyInvalidArrayItems = true;
            }

            // Keep uniqueItems focused on values that are otherwise valid so a
            // shape/type or constraint error does not also surface as a misleading duplicate.
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

        if (!hasStructurallyInvalidArrayItems && schemaNode.contains) {
            let matchingContainsCount = 0;
            for (const {node} of containsCandidateItems) {
                if (matchesSchemaNode(schemaNode.contains, node, true)) {
                    matchingContainsCount += 1;
                }
            }

            const requiredMinContains = typeof schemaNode.minContains === "number"
                ? schemaNode.minContains
                : 1;
            if (matchingContainsCount < requiredMinContains) {
                diagnostics.push({
                    severity: "error",
                    message: localizeValidationMessage(ValidationMessageKeys.minContainsViolation, localizer, {
                        displayPath,
                        value: String(requiredMinContains)
                    })
                });
            }

            if (typeof schemaNode.maxContains === "number" &&
                matchingContainsCount > schemaNode.maxContains) {
                diagnostics.push({
                    severity: "error",
                    message: localizeValidationMessage(ValidationMessageKeys.maxContainsViolation, localizer, {
                        displayPath,
                        value: String(schemaNode.maxContains)
                    })
                });
            }
        }

        validateEnumComparableValue(schemaNode, yamlNode, displayPath, diagnostics, localizer, diagnosticsBeforeNode);
        validateConstComparableValue(schemaNode, yamlNode, displayPath, diagnostics, localizer);
        validateNotSchemaMatch(schemaNode, yamlNode, displayPath, diagnostics, localizer);

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

    validateEnumComparableValue(schemaNode, yamlNode, displayPath, diagnostics, localizer);

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

    if (supportsPatternConstraints &&
        !matchesSchemaStringFormat(scalarValue, schemaNode.format)) {
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.formatViolation, localizer, {
                displayPath,
                value: schemaNode.format
            })
        });
    }

    validateConstComparableValue(schemaNode, yamlNode, displayPath, diagnostics, localizer);
    validateNotSchemaMatch(schemaNode, yamlNode, displayPath, diagnostics, localizer);
}

/**
 * Validate an object node recursively.
 *
 * @param {Extract<SchemaNode, {type: "object"}>} schemaNode Object schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @param {string} displayPath Current logical path.
 * @param {Array<{severity: "error" | "warning", message: string}>} diagnostics Diagnostic sink.
 * @param {{isChinese?: boolean} | undefined} localizer Optional runtime localizer.
 * @param {number} diagnosticsBeforeNode Diagnostic count recorded before validating this object node.
 */
function validateObjectNode(schemaNode, yamlNode, displayPath, diagnostics, localizer, diagnosticsBeforeNode) {
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

    const reportedMessages = new Set(
        diagnostics
            .slice(diagnosticsBeforeNode)
            .map((diagnostic) => diagnostic.message));

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

    if (schemaNode.dependentRequired && typeof schemaNode.dependentRequired === "object") {
        for (const [triggerProperty, dependencies] of Object.entries(schemaNode.dependentRequired)) {
            if (!yamlNode.map.has(triggerProperty)) {
                continue;
            }

            for (const dependency of dependencies) {
                if (yamlNode.map.has(dependency)) {
                    continue;
                }

                const localizedMessage = localizeValidationMessage(
                    ValidationMessageKeys.dependentRequiredViolation,
                    localizer,
                    {
                        displayPath: joinPropertyPath(displayPath, dependency),
                        triggerProperty: joinPropertyPath(displayPath, triggerProperty)
                    });

                if (reportedMessages.has(localizedMessage)) {
                    continue;
                }

                diagnostics.push({
                    severity: "error",
                    message: localizedMessage
                });
                reportedMessages.add(localizedMessage);
            }
        }
    }

    if (schemaNode.dependentSchemas && typeof schemaNode.dependentSchemas === "object") {
        for (const [triggerProperty, dependentSchema] of getTriggeredDependentSchemas(schemaNode, yamlNode)) {
            if (matchesSchemaNode(dependentSchema, yamlNode, true)) {
                continue;
            }

            const localizedMessage = localizeValidationMessage(
                ValidationMessageKeys.dependentSchemasViolation,
                localizer,
                {
                    displayPath: displayPath || "<root>",
                    triggerProperty: joinPropertyPath(displayPath, triggerProperty)
                });

            if (reportedMessages.has(localizedMessage)) {
                continue;
            }

            diagnostics.push({
                severity: "error",
                message: localizedMessage
            });
            reportedMessages.add(localizedMessage);
        }
    }

    if (Array.isArray(schemaNode.allOf)) {
        for (let index = 0; index < schemaNode.allOf.length; index += 1) {
            if (matchesSchemaNode(schemaNode.allOf[index], yamlNode, true)) {
                continue;
            }

            const localizedMessage = localizeValidationMessage(
                ValidationMessageKeys.allOfViolation,
                localizer,
                {
                    displayPath: displayPath || "<root>",
                    index: String(index + 1)
                });

            if (reportedMessages.has(localizedMessage)) {
                continue;
            }

            diagnostics.push({
                severity: "error",
                message: localizedMessage
            });
            reportedMessages.add(localizedMessage);
        }
    }

    const ifMatched = schemaNode.ifSchema
        ? matchesSchemaNode(schemaNode.ifSchema, yamlNode, true)
        : false;
    if (ifMatched &&
        schemaNode.thenSchema &&
        !matchesSchemaNode(schemaNode.thenSchema, yamlNode, true)) {
        const localizedMessage = localizeValidationMessage(
            ValidationMessageKeys.thenViolation,
            localizer,
            {
                displayPath: displayPath || "<root>"
            });

        if (!reportedMessages.has(localizedMessage)) {
            diagnostics.push({
                severity: "error",
                message: localizedMessage
            });
            reportedMessages.add(localizedMessage);
        }
    }

    if (!ifMatched &&
        schemaNode.ifSchema &&
        schemaNode.elseSchema &&
        !matchesSchemaNode(schemaNode.elseSchema, yamlNode, true)) {
        const localizedMessage = localizeValidationMessage(
            ValidationMessageKeys.elseViolation,
            localizer,
            {
                displayPath: displayPath || "<root>"
            });

        if (!reportedMessages.has(localizedMessage)) {
            diagnostics.push({
                severity: "error",
                message: localizedMessage
            });
            reportedMessages.add(localizedMessage);
        }
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

    validateEnumComparableValue(schemaNode, yamlNode, displayPath, diagnostics, localizer, diagnosticsBeforeNode);
    validateConstComparableValue(schemaNode, yamlNode, displayPath, diagnostics, localizer);
    validateNotSchemaMatch(schemaNode, yamlNode, displayPath, diagnostics, localizer);
}

/**
 * Enumerate object-level `dependentSchemas` entries whose trigger property is
 * present on the current YAML object.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @returns {Array<[string, SchemaNode]>} Triggered dependent schema entries.
 */
function getTriggeredDependentSchemas(schemaNode, yamlNode) {
    if (!schemaNode.dependentSchemas ||
        typeof schemaNode.dependentSchemas !== "object" ||
        !yamlNode ||
        yamlNode.kind !== "object") {
        return [];
    }

    const triggeredSchemas = [];
    for (const [triggerProperty, dependentSchema] of Object.entries(schemaNode.dependentSchemas)) {
        if (yamlNode.map.has(triggerProperty)) {
            triggeredSchemas.push([triggerProperty, dependentSchema]);
        }
    }

    return triggeredSchemas;
}

/**
 * Test whether one YAML node satisfies one schema node without emitting user-facing diagnostics.
 * This is used by array `contains`, where object sub-schemas must behave like
 * partial matchers: declared properties, required members, and constraints must
 * match, but additional object members outside the sub-schema must not block a hit.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @param {boolean} allowUnknownObjectProperties Whether object matching should
 * tolerate extra undeclared properties.
 * @returns {boolean} True when the YAML node matches the schema node.
 */
function matchesSchemaNode(schemaNode, yamlNode, allowUnknownObjectProperties = false) {
    return matchesSchemaNodeInternal(schemaNode, yamlNode, allowUnknownObjectProperties);
}

/**
 * Match one YAML node against one schema node using JSON-Schema-style subset semantics.
 * The helper mirrors validation rules closely, but it intentionally skips unknown-property
 * rejection for objects so `contains` can test whether one item satisfies a sub-schema.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @param {boolean} allowUnknownObjectProperties Whether object matching should
 * tolerate extra undeclared properties.
 * @returns {boolean} True when the YAML node satisfies the schema node.
 */
function matchesSchemaNodeInternal(schemaNode, yamlNode, allowUnknownObjectProperties) {
    if (schemaNode.type === "object") {
        if (!yamlNode || yamlNode.kind !== "object") {
            return false;
        }

        const propertyCount = yamlNode.map instanceof Map
            ? yamlNode.map.size
            : Array.isArray(yamlNode.entries)
                ? new Set(yamlNode.entries.map((entry) => entry.key)).size
                : 0;

        for (const requiredProperty of schemaNode.required) {
            if (!yamlNode.map.has(requiredProperty)) {
                return false;
            }
        }

        if (!allowUnknownObjectProperties) {
            for (const entry of yamlNode.entries) {
                if (!Object.prototype.hasOwnProperty.call(schemaNode.properties, entry.key)) {
                    return false;
                }
            }
        }

        for (const [key, childSchema] of Object.entries(schemaNode.properties)) {
            if (yamlNode.map.has(key) &&
                !matchesSchemaNodeInternal(childSchema, yamlNode.map.get(key), allowUnknownObjectProperties)) {
                return false;
            }
        }

        if (schemaNode.dependentRequired && typeof schemaNode.dependentRequired === "object") {
            for (const [triggerProperty, dependencies] of Object.entries(schemaNode.dependentRequired)) {
                if (!yamlNode.map.has(triggerProperty)) {
                    continue;
                }

                for (const dependency of dependencies) {
                    if (!yamlNode.map.has(dependency)) {
                        return false;
                    }
                }
            }
        }

        for (const [, dependentSchema] of getTriggeredDependentSchemas(schemaNode, yamlNode)) {
            if (!matchesSchemaNodeInternal(dependentSchema, yamlNode, true)) {
                return false;
            }
        }

        if (Array.isArray(schemaNode.allOf)) {
            for (const allOfSchema of schemaNode.allOf) {
                if (!matchesSchemaNodeInternal(allOfSchema, yamlNode, true)) {
                    return false;
                }
            }
        }

        const ifMatched = schemaNode.ifSchema
            ? matchesSchemaNodeInternal(schemaNode.ifSchema, yamlNode, true)
            : false;
        if (ifMatched &&
            schemaNode.thenSchema &&
            !matchesSchemaNodeInternal(schemaNode.thenSchema, yamlNode, true)) {
            return false;
        }

        if (!ifMatched &&
            schemaNode.ifSchema &&
            schemaNode.elseSchema &&
            !matchesSchemaNodeInternal(schemaNode.elseSchema, yamlNode, true)) {
            return false;
        }

        if (typeof schemaNode.minProperties === "number" &&
            propertyCount < schemaNode.minProperties) {
            return false;
        }

        if (typeof schemaNode.maxProperties === "number" &&
            propertyCount > schemaNode.maxProperties) {
            return false;
        }

        if (Array.isArray(schemaNode.enumComparableValues) &&
            schemaNode.enumComparableValues.length > 0 &&
            !schemaNode.enumComparableValues.includes(buildComparableNodeValue(schemaNode, yamlNode))) {
            return false;
        }

        if (typeof schemaNode.constComparableValue === "string" &&
            buildComparableNodeValue(schemaNode, yamlNode) !== schemaNode.constComparableValue) {
            return false;
        }

        return !schemaNode.not || !matchesSchemaNodeInternal(schemaNode.not, yamlNode, false);
    }

    if (schemaNode.type === "array") {
        if (!yamlNode || yamlNode.kind !== "array") {
            return false;
        }

        if (typeof schemaNode.minItems === "number" &&
            yamlNode.items.length < schemaNode.minItems) {
            return false;
        }

        if (typeof schemaNode.maxItems === "number" &&
            yamlNode.items.length > schemaNode.maxItems) {
            return false;
        }

        for (const item of yamlNode.items) {
            if (!matchesSchemaNodeInternal(schemaNode.items, item, allowUnknownObjectProperties)) {
                return false;
            }
        }

        if (schemaNode.uniqueItems === true) {
            const seenItems = new Set();
            for (const item of yamlNode.items) {
                const comparableValue = buildComparableNodeValue(schemaNode.items, item);
                if (seenItems.has(comparableValue)) {
                    return false;
                }

                seenItems.add(comparableValue);
            }
        }

        if (schemaNode.contains) {
            let matchingContainsCount = 0;
            for (const item of yamlNode.items) {
                if (matchesSchemaNodeInternal(schemaNode.contains, item, true)) {
                    matchingContainsCount += 1;
                }
            }

            const requiredMinContains = typeof schemaNode.minContains === "number"
                ? schemaNode.minContains
                : 1;
            if (matchingContainsCount < requiredMinContains) {
                return false;
            }

            if (typeof schemaNode.maxContains === "number" &&
                matchingContainsCount > schemaNode.maxContains) {
                return false;
            }
        }

        if (Array.isArray(schemaNode.enumComparableValues) &&
            schemaNode.enumComparableValues.length > 0 &&
            !schemaNode.enumComparableValues.includes(buildComparableNodeValue(schemaNode, yamlNode))) {
            return false;
        }

        if (typeof schemaNode.constComparableValue === "string" &&
            buildComparableNodeValue(schemaNode, yamlNode) !== schemaNode.constComparableValue) {
            return false;
        }

        return !schemaNode.not || !matchesSchemaNodeInternal(schemaNode.not, yamlNode, false);
    }

    if (!yamlNode || yamlNode.kind !== "scalar") {
        return false;
    }

    if (!isScalarCompatible(schemaNode.type, yamlNode.value)) {
        return false;
    }

    if (Array.isArray(schemaNode.enumComparableValues) &&
        schemaNode.enumComparableValues.length > 0 &&
        !schemaNode.enumComparableValues.includes(buildComparableNodeValue(schemaNode, yamlNode))) {
        return false;
    }

    const scalarValue = unquoteScalar(yamlNode.value);
    const supportsNumericConstraints = schemaNode.type === "integer" || schemaNode.type === "number";
    const supportsLengthConstraints = schemaNode.type === "string";
    const supportsPatternConstraints = schemaNode.type === "string";

    if (supportsNumericConstraints &&
        typeof schemaNode.minimum === "number" &&
        Number(scalarValue) < schemaNode.minimum) {
        return false;
    }

    if (supportsNumericConstraints &&
        typeof schemaNode.exclusiveMinimum === "number" &&
        Number(scalarValue) <= schemaNode.exclusiveMinimum) {
        return false;
    }

    if (supportsNumericConstraints &&
        typeof schemaNode.maximum === "number" &&
        Number(scalarValue) > schemaNode.maximum) {
        return false;
    }

    if (supportsNumericConstraints &&
        typeof schemaNode.exclusiveMaximum === "number" &&
        Number(scalarValue) >= schemaNode.exclusiveMaximum) {
        return false;
    }

    if (supportsNumericConstraints &&
        !matchesSchemaMultipleOf(scalarValue, schemaNode.multipleOf)) {
        return false;
    }

    if (supportsLengthConstraints &&
        typeof schemaNode.minLength === "number" &&
        scalarValue.length < schemaNode.minLength) {
        return false;
    }

    if (supportsLengthConstraints &&
        typeof schemaNode.maxLength === "number" &&
        scalarValue.length > schemaNode.maxLength) {
        return false;
    }

    if (supportsPatternConstraints &&
        !matchesSchemaPattern(scalarValue, schemaNode.patternRegex)) {
        return false;
    }

    if (supportsPatternConstraints &&
        !matchesSchemaStringFormat(scalarValue, schemaNode.format)) {
        return false;
    }

    if (typeof schemaNode.constComparableValue === "string" &&
        buildComparableNodeValue(schemaNode, yamlNode) !== schemaNode.constComparableValue) {
        return false;
    }

    return !schemaNode.not || !matchesSchemaNodeInternal(schemaNode.not, yamlNode, false);
}

/**
 * Emit one validation error when the current YAML node matches a forbidden `not`
 * sub-schema. Unlike `contains`, this path keeps object matching strict so
 * undeclared members still block the negated branch from matching.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @param {string} displayPath Current logical path.
 * @param {Array<{severity: "error" | "warning", message: string}>} diagnostics Diagnostic sink.
 * @param {{isChinese?: boolean} | undefined} localizer Optional runtime localizer.
 */
function validateNotSchemaMatch(schemaNode, yamlNode, displayPath, diagnostics, localizer) {
    if (!schemaNode.not || !matchesSchemaNode(schemaNode.not, yamlNode, false)) {
        return;
    }

    diagnostics.push({
        severity: "error",
        message: localizeValidationMessage(ValidationMessageKeys.notViolation, localizer, {
            displayPath
        })
    });
}

/**
 * Test whether one YAML node is structurally compatible with one schema node.
 * This keeps array-level `contains` validation from producing noisy follow-on
 * diagnostics when an item already has a shape or scalar-type mismatch, while
 * still allowing value-level constraint failures to participate in contains counting.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @returns {boolean} True when the YAML node has the expected recursive shape.
 */
function isStructurallyCompatibleWithSchemaNode(schemaNode, yamlNode) {
    if (schemaNode.type === "object") {
        if (!yamlNode || yamlNode.kind !== "object") {
            return false;
        }

        for (const requiredProperty of schemaNode.required) {
            if (!yamlNode.map.has(requiredProperty)) {
                return false;
            }
        }

        for (const entry of yamlNode.entries) {
            if (!Object.prototype.hasOwnProperty.call(schemaNode.properties, entry.key)) {
                return false;
            }

            if (!isStructurallyCompatibleWithSchemaNode(schemaNode.properties[entry.key], entry.node)) {
                return false;
            }
        }

        return true;
    }

    if (schemaNode.type === "array") {
        if (!yamlNode || yamlNode.kind !== "array") {
            return false;
        }

        for (const item of yamlNode.items) {
            if (!isStructurallyCompatibleWithSchemaNode(schemaNode.items, item)) {
                return false;
            }
        }

        return true;
    }

    return Boolean(yamlNode) &&
        yamlNode.kind === "scalar" &&
        isScalarCompatible(schemaNode.type, yamlNode.value);
}

/**
 * Validate one parsed YAML node against one normalized enum comparable set.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @param {string} displayPath Current logical path.
 * @param {Array<{severity: "error" | "warning", message: string}>} diagnostics Diagnostic sink.
 * @param {{isChinese?: boolean} | undefined} localizer Optional runtime localizer.
 * @param {number} [diagnosticsBeforeNode] Diagnostic count recorded before validating this node.
 */
function validateEnumComparableValue(schemaNode, yamlNode, displayPath, diagnostics, localizer, diagnosticsBeforeNode) {
    if (!Array.isArray(schemaNode.enumComparableValues) || schemaNode.enumComparableValues.length === 0) {
        return;
    }

    if (typeof diagnosticsBeforeNode === "number" && diagnostics.length !== diagnosticsBeforeNode) {
        return;
    }

    const comparableValue = buildComparableNodeValue(schemaNode, yamlNode);
    if (schemaNode.enumComparableValues.includes(comparableValue)) {
        return;
    }

    const displayValues = Array.isArray(schemaNode.enumDisplayValues) && schemaNode.enumDisplayValues.length > 0
        ? schemaNode.enumDisplayValues
        : Array.isArray(schemaNode.enumValues)
            ? schemaNode.enumValues
            : [];
    diagnostics.push({
        severity: "error",
        message: localizeValidationMessage(ValidationMessageKeys.enumMismatch, localizer, {
            displayPath,
            values: displayValues.join(", ")
        })
    });
}

/**
 * Validate one parsed YAML node against one normalized const comparable value.
 * The helper reuses the same comparable-key logic as uniqueItems so array order
 * and scalar normalization stay aligned with runtime behavior.
 *
 * @param {SchemaNode} schemaNode Schema node.
 * @param {YamlNode} yamlNode YAML node.
 * @param {string} displayPath Current logical path.
 * @param {Array<{severity: "error" | "warning", message: string}>} diagnostics Diagnostic sink.
 * @param {{isChinese?: boolean} | undefined} localizer Optional runtime localizer.
 */
function validateConstComparableValue(schemaNode, yamlNode, displayPath, diagnostics, localizer) {
    if (typeof schemaNode.constComparableValue !== "string") {
        return;
    }

    if (buildComparableNodeValue(schemaNode, yamlNode) === schemaNode.constComparableValue) {
        return;
    }

    diagnostics.push({
        severity: "error",
        message: localizeValidationMessage(ValidationMessageKeys.constMismatch, localizer, {
            displayPath,
            value: schemaNode.constDisplayValue ?? schemaNode.constValue
        })
    });
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
            .sort(compareStringsOrdinal)
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
            case ValidationMessageKeys.allOfViolation:
                return `对象“${params.displayPath}”必须满足全部 \`allOf\` schema，第 ${params.index} 项未匹配。`;
            case ValidationMessageKeys.constMismatch:
                return `属性“${params.displayPath}”必须匹配固定值 ${params.value}。`;
            case ValidationMessageKeys.dependentRequiredViolation:
                return `属性“${params.triggerProperty}”存在时，必须同时声明属性“${params.displayPath}”。`;
            case ValidationMessageKeys.dependentSchemasViolation:
                return `对象“${params.displayPath}”在属性“${params.triggerProperty}”存在时，必须满足对应的 dependent schema。`;
            case ValidationMessageKeys.elseViolation:
                return `对象“${params.displayPath}”在内联 \`if\` 条件未命中时，必须满足对应的 \`else\` schema。`;
            case ValidationMessageKeys.expectedArray:
                return `属性“${params.displayPath}”应为数组。`;
            case ValidationMessageKeys.expectedScalarShape:
                return `属性“${params.displayPath}”应为“${params.schemaType}”，但当前 YAML 结构是“${params.yamlKind}”。`;
            case ValidationMessageKeys.expectedScalarValue:
                return `属性“${params.displayPath}”应为“${params.schemaType}”，但当前标量值不兼容。`;
            case ValidationMessageKeys.formatViolation:
                return `属性“${params.displayPath}”必须满足字符串格式“${params.value}”。`;
            case ValidationMessageKeys.enumMismatch:
                return `属性“${params.displayPath}”必须是以下值之一：${params.values}。`;
            case ValidationMessageKeys.exclusiveMaximumViolation:
                return `属性“${params.displayPath}”必须小于 ${params.value}。`;
            case ValidationMessageKeys.exclusiveMinimumViolation:
                return `属性“${params.displayPath}”必须大于 ${params.value}。`;
            case ValidationMessageKeys.maximumViolation:
                return `属性“${params.displayPath}”必须小于或等于 ${params.value}。`;
            case ValidationMessageKeys.maxContainsViolation:
                return `属性“${params.displayPath}”最多只能包含 ${params.value} 个匹配 contains 条件的元素。`;
            case ValidationMessageKeys.maxItemsViolation:
                return `属性“${params.displayPath}”最多只能包含 ${params.value} 个元素。`;
            case ValidationMessageKeys.maxLengthViolation:
                return `属性“${params.displayPath}”长度必须不超过 ${params.value} 个字符。`;
            case ValidationMessageKeys.minimumViolation:
                return `属性“${params.displayPath}”必须大于或等于 ${params.value}。`;
            case ValidationMessageKeys.multipleOfViolation:
                return `属性“${params.displayPath}”必须是 ${params.value} 的整数倍。`;
            case ValidationMessageKeys.notViolation:
                return `属性“${params.displayPath}”不能匹配被 \`not\` 禁止的 schema。`;
            case ValidationMessageKeys.thenViolation:
                return `对象“${params.displayPath}”在内联 \`if\` 条件命中时，必须满足对应的 \`then\` schema。`;
            case ValidationMessageKeys.minContainsViolation:
                return `属性“${params.displayPath}”至少需要包含 ${params.value} 个匹配 contains 条件的元素。`;
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
        case ValidationMessageKeys.allOfViolation:
            return `Object '${params.displayPath}' must satisfy all 'allOf' schemas; entry #${params.index} did not match.`;
        case ValidationMessageKeys.constMismatch:
            return `Property '${params.displayPath}' must match constant value ${params.value}.`;
        case ValidationMessageKeys.dependentRequiredViolation:
            return `Property '${params.displayPath}' is required when sibling property '${params.triggerProperty}' is present.`;
        case ValidationMessageKeys.dependentSchemasViolation:
            return `Object '${params.displayPath}' must satisfy the dependent schema triggered by sibling property '${params.triggerProperty}'.`;
        case ValidationMessageKeys.elseViolation:
            return `Object '${params.displayPath}' must satisfy the 'else' schema because the inline 'if' condition did not match.`;
        case ValidationMessageKeys.expectedArray:
            return `Property '${params.displayPath}' is expected to be an array.`;
        case ValidationMessageKeys.expectedScalarShape:
            return `Property '${params.displayPath}' is expected to be '${params.schemaType}', but the current YAML shape is '${params.yamlKind}'.`;
        case ValidationMessageKeys.expectedScalarValue:
            return `Property '${params.displayPath}' is expected to be '${params.schemaType}', but the current scalar value is incompatible.`;
        case ValidationMessageKeys.formatViolation:
            return `Property '${params.displayPath}' must satisfy string format '${params.value}'.`;
        case ValidationMessageKeys.enumMismatch:
            return `Property '${params.displayPath}' must be one of: ${params.values}.`;
        case ValidationMessageKeys.exclusiveMaximumViolation:
            return `Property '${params.displayPath}' must be less than ${params.value}.`;
        case ValidationMessageKeys.exclusiveMinimumViolation:
            return `Property '${params.displayPath}' must be greater than ${params.value}.`;
        case ValidationMessageKeys.maximumViolation:
            return `Property '${params.displayPath}' must be less than or equal to ${params.value}.`;
        case ValidationMessageKeys.maxContainsViolation:
            return `Property '${params.displayPath}' must contain at most ${params.value} items matching the 'contains' schema.`;
        case ValidationMessageKeys.maxItemsViolation:
            return `Property '${params.displayPath}' must contain at most ${params.value} items.`;
        case ValidationMessageKeys.maxLengthViolation:
            return `Property '${params.displayPath}' must be at most ${params.value} characters long.`;
        case ValidationMessageKeys.minimumViolation:
            return `Property '${params.displayPath}' must be greater than or equal to ${params.value}.`;
        case ValidationMessageKeys.multipleOfViolation:
            return `Property '${params.displayPath}' must be a multiple of ${params.value}.`;
        case ValidationMessageKeys.notViolation:
            return `Property '${params.displayPath}' must not match the forbidden 'not' schema.`;
        case ValidationMessageKeys.thenViolation:
            return `Object '${params.displayPath}' must satisfy the 'then' schema because the inline 'if' condition matched.`;
        case ValidationMessageKeys.minContainsViolation:
            return `Property '${params.displayPath}' must contain at least ${params.value} items matching the 'contains' schema.`;
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
        if (schemaNode && schemaNode.enumSampleValue !== undefined) {
            return createNodeFromFormValue(schemaNode.enumSampleValue);
        }

        const objectNode = createObjectNode();
        for (const [key, propertySchema] of Object.entries(schemaNode && schemaNode.properties ? schemaNode.properties : {})) {
            const childNode = createSampleNodeFromSchema(propertySchema);
            setObjectEntry(objectNode, key, childNode);
        }

        return objectNode;
    }

    if (schemaNode.type === "array") {
        if (schemaNode.enumSampleValue !== undefined) {
            return createNodeFromFormValue(schemaNode.enumSampleValue);
        }

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
    if (schemaNode.constValue !== undefined) {
        return schemaNode.constValue;
    }

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
 *   dependentRequired?: Record<string, string[]>,
 *   dependentSchemas?: Record<string, SchemaNode>,
 *   allOf?: SchemaNode[],
 *   ifSchema?: SchemaNode,
 *   thenSchema?: SchemaNode,
 *   elseSchema?: SchemaNode,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string,
 *   enumSampleValue?: unknown,
 *   enumDisplayValues?: string[],
 *   enumComparableValues?: string[],
 *   constValue?: string,
 *   constDisplayValue?: string,
 *   constComparableValue?: string,
 *   not?: SchemaNode
 * } | {
 *   type: "array",
 *   displayPath: string,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string,
 *   enumSampleValue?: unknown,
 *   enumDisplayValues?: string[],
 *   enumComparableValues?: string[],
 *   constValue?: string,
 *   constDisplayValue?: string,
 *   constComparableValue?: string,
 *   minItems?: number,
 *   maxItems?: number,
 *   minContains?: number,
 *   maxContains?: number,
 *   uniqueItems?: boolean,
 *   refTable?: string,
 *   contains?: SchemaNode,
 *   not?: SchemaNode,
 *   items: SchemaNode
 * } | {
 *   type: "string" | "integer" | "number" | "boolean",
 *   displayPath: string,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string,
 *   enumSampleValue?: unknown,
 *   constValue?: string,
 *   constDisplayValue?: string,
 *   constComparableValue?: string,
 *   enumDisplayValues?: string[],
 *   enumComparableValues?: string[],
 *   minimum?: number,
 *   exclusiveMinimum?: number,
 *   maximum?: number,
 *   exclusiveMaximum?: number,
 *   multipleOf?: number,
 *   minLength?: number,
 *   maxLength?: number,
 *   pattern?: string,
 *   patternRegex?: RegExp,
 *   format?: string,
 *   enumValues?: string[],
 *   refTable?: string,
 *   not?: SchemaNode
 * }} SchemaNode
 */

/**
 * @typedef {{kind: "scalar", value: string}} YamlScalarNode
 * @typedef {{kind: "array", items: YamlNode[]}} YamlArrayNode
 * @typedef {{kind: "object", entries: Array<{key: string, node: YamlNode}>, map: Map<string, YamlNode>}} YamlObjectNode
 * @typedef {YamlScalarNode | YamlArrayNode | YamlObjectNode} YamlNode
 */
