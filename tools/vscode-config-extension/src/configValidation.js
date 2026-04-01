/**
 * Parse a minimal JSON schema document used by the config extension.
 * The parser intentionally supports the same schema subset that the current
 * runtime validator and source generator depend on.
 *
 * @param {string} content Raw schema JSON text.
 * @returns {{required: string[], properties: Record<string, {
 *     type: string,
 *     itemType?: string,
 *     title?: string,
 *     description?: string,
 *     defaultValue?: string,
 *     enumValues?: string[],
 *     itemEnumValues?: string[],
 *     refTable?: string
 * }>}} Parsed schema info.
 */
function parseSchemaContent(content) {
    const parsed = JSON.parse(content);
    const required = Array.isArray(parsed.required)
        ? parsed.required.filter((value) => typeof value === "string")
        : [];
    const properties = {};
    const propertyBag = parsed.properties || {};

    for (const [key, value] of Object.entries(propertyBag)) {
        if (!value || typeof value !== "object" || typeof value.type !== "string") {
            continue;
        }

        const metadata = {
            title: typeof value.title === "string" ? value.title : undefined,
            description: typeof value.description === "string" ? value.description : undefined,
            defaultValue: formatSchemaDefaultValue(value.default),
            enumValues: normalizeSchemaEnumValues(value.enum),
            refTable: typeof value["x-gframework-ref-table"] === "string"
                ? value["x-gframework-ref-table"]
                : undefined
        };

        if (value.type === "array" &&
            value.items &&
            typeof value.items === "object" &&
            typeof value.items.type === "string") {
            properties[key] = {
                type: "array",
                itemType: value.items.type,
                title: metadata.title,
                description: metadata.description,
                defaultValue: metadata.defaultValue,
                refTable: metadata.refTable,
                itemEnumValues: normalizeSchemaEnumValues(value.items.enum)
            };
            continue;
        }

        properties[key] = {
            type: value.type,
            title: metadata.title,
            description: metadata.description,
            defaultValue: metadata.defaultValue,
            enumValues: metadata.enumValues,
            refTable: metadata.refTable
        };
    }

    return {
        required,
        properties
    };
}

/**
 * Collect top-level schema fields that the current tooling can edit in bulk.
 * The bulk editor intentionally stays aligned with the lightweight form editor:
 * top-level scalars and scalar arrays are supported, while nested objects and
 * complex array items remain raw-YAML-only.
 *
 * @param {{required: string[], properties: Record<string, {
 *     type: string,
 *     itemType?: string,
 *     title?: string,
 *     description?: string,
 *     defaultValue?: string,
 *     enumValues?: string[],
 *     itemEnumValues?: string[],
 *     refTable?: string
 * }>}} schemaInfo Parsed schema info.
 * @returns {Array<{
 *     key: string,
 *     type: string,
 *     itemType?: string,
 *     title?: string,
 *     description?: string,
 *     defaultValue?: string,
 *     enumValues?: string[],
 *     itemEnumValues?: string[],
 *     refTable?: string,
 *     inputKind: "scalar" | "array",
 *     required: boolean
 * }>} Editable field descriptors.
 */
function getEditableSchemaFields(schemaInfo) {
    const editableFields = [];
    const requiredSet = new Set(Array.isArray(schemaInfo.required) ? schemaInfo.required : []);

    for (const [key, property] of Object.entries(schemaInfo.properties || {})) {
        if (isEditableScalarType(property.type)) {
            editableFields.push({
                key,
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

        if (property.type === "array" && isEditableScalarType(property.itemType || "")) {
            editableFields.push({
                key,
                type: property.type,
                itemType: property.itemType,
                title: property.title,
                description: property.description,
                defaultValue: property.defaultValue,
                itemEnumValues: property.itemEnumValues,
                refTable: property.refTable,
                inputKind: "array",
                required: requiredSet.has(key)
            });
        }
    }

    return editableFields.sort((left, right) => left.key.localeCompare(right.key));
}

/**
 * Parse a minimal top-level YAML structure for config validation and form
 * preview. This parser intentionally focuses on the repository's current
 * config conventions: one root mapping object per file, top-level scalar
 * fields, and top-level scalar arrays.
 *
 * @param {string} text YAML text.
 * @returns {{entries: Map<string, {kind: string, value?: string, items?: Array<{raw: string, isComplex: boolean}>}>, keys: Set<string>}} Parsed YAML.
 */
function parseTopLevelYaml(text) {
    const entries = new Map();
    const keys = new Set();
    const lines = text.split(/\r?\n/u);

    for (let index = 0; index < lines.length; index += 1) {
        const line = lines[index];
        if (!line || line.trim().length === 0 || line.trim().startsWith("#")) {
            continue;
        }

        if (/^\s/u.test(line)) {
            continue;
        }

        const match = /^([A-Za-z0-9_]+):(?:\s*(.*))?$/u.exec(line);
        if (!match) {
            continue;
        }

        const key = match[1];
        const rawValue = match[2] || "";
        keys.add(key);

        if (rawValue.length > 0 && !rawValue.startsWith("|") && !rawValue.startsWith(">")) {
            entries.set(key, {
                kind: "scalar",
                value: rawValue.trim()
            });
            continue;
        }

        const childLines = [];
        let cursor = index + 1;
        while (cursor < lines.length) {
            const childLine = lines[cursor];
            if (childLine.trim().length === 0 || childLine.trim().startsWith("#")) {
                cursor += 1;
                continue;
            }

            if (!/^\s/u.test(childLine)) {
                break;
            }

            childLines.push(childLine);
            cursor += 1;
        }

        if (childLines.length === 0) {
            entries.set(key, {
                kind: "empty"
            });
            continue;
        }

        const arrayItems = parseTopLevelArray(childLines);
        if (arrayItems) {
            entries.set(key, {
                kind: "array",
                items: arrayItems
            });
            index = cursor - 1;
            continue;
        }

        entries.set(key, {
            kind: "object"
        });
        index = cursor - 1;
    }

    return {
        entries,
        keys
    };
}

/**
 * Produce extension-facing validation diagnostics from schema and parsed YAML.
 *
 * @param {{required: string[], properties: Record<string, {
 *     type: string,
 *     itemType?: string,
 *     title?: string,
 *     description?: string,
 *     defaultValue?: string,
 *     enumValues?: string[],
 *     itemEnumValues?: string[],
 *     refTable?: string
 * }>}} schemaInfo Parsed schema info.
 * @param {{entries: Map<string, {kind: string, value?: string, items?: Array<{raw: string, isComplex: boolean}>}>, keys: Set<string>}} parsedYaml Parsed YAML.
 * @returns {Array<{severity: "error" | "warning", message: string}>} Validation diagnostics.
 */
function validateParsedConfig(schemaInfo, parsedYaml) {
    const diagnostics = [];

    for (const requiredProperty of schemaInfo.required) {
        if (!parsedYaml.keys.has(requiredProperty)) {
            diagnostics.push({
                severity: "error",
                message: `Required property '${requiredProperty}' is missing.`
            });
        }
    }

    for (const key of parsedYaml.keys) {
        if (!Object.prototype.hasOwnProperty.call(schemaInfo.properties, key)) {
            diagnostics.push({
                severity: "error",
                message: `Property '${key}' is not declared in the matching schema.`
            });
        }
    }

    for (const [propertyName, propertySchema] of Object.entries(schemaInfo.properties)) {
        if (!parsedYaml.entries.has(propertyName)) {
            continue;
        }

        const entry = parsedYaml.entries.get(propertyName);
        if (propertySchema.type === "array") {
            if (entry.kind !== "array") {
                diagnostics.push({
                    severity: "error",
                    message: `Property '${propertyName}' is expected to be an array.`
                });
                continue;
            }

            for (const item of entry.items || []) {
                if (item.isComplex || !isScalarCompatible(propertySchema.itemType || "", item.raw)) {
                    diagnostics.push({
                        severity: "error",
                        message: `Array item in property '${propertyName}' is expected to be '${propertySchema.itemType}', but the current value is incompatible.`
                    });
                    break;
                }

                if (Array.isArray(propertySchema.itemEnumValues) &&
                    propertySchema.itemEnumValues.length > 0 &&
                    !propertySchema.itemEnumValues.includes(unquoteScalar(item.raw))) {
                    diagnostics.push({
                        severity: "error",
                        message: `Array item in property '${propertyName}' must be one of: ${propertySchema.itemEnumValues.join(", ")}.`
                    });
                    break;
                }
            }

            continue;
        }

        if (entry.kind !== "scalar") {
            diagnostics.push({
                severity: "error",
                message: `Property '${propertyName}' is expected to be '${propertySchema.type}', but the current YAML shape is '${entry.kind}'.`
            });
            continue;
        }

        if (!isScalarCompatible(propertySchema.type, entry.value || "")) {
            diagnostics.push({
                severity: "error",
                message: `Property '${propertyName}' is expected to be '${propertySchema.type}', but the current scalar value is incompatible.`
            });
            continue;
        }

        if (Array.isArray(propertySchema.enumValues) &&
            propertySchema.enumValues.length > 0 &&
            !propertySchema.enumValues.includes(unquoteScalar(entry.value || ""))) {
            diagnostics.push({
                severity: "error",
                message: `Property '${propertyName}' must be one of: ${propertySchema.enumValues.join(", ")}.`
            });
        }
    }

    return diagnostics;
}

/**
 * Determine whether the current schema type can be edited through the
 * lightweight form or batch-edit tooling.
 *
 * @param {string} schemaType Schema type.
 * @returns {boolean} True when the type is supported by the lightweight editors.
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
    const value = unquoteScalar(scalarValue);
    switch (expectedType) {
        case "integer":
            return /^-?\d+$/u.test(value);
        case "number":
            return /^-?\d+(?:\.\d+)?$/u.test(value);
        case "boolean":
            return /^(true|false)$/iu.test(value);
        case "string":
            return true;
        default:
            return true;
    }
}

/**
 * Apply form field updates back into the original YAML text.
 * The current form editor supports top-level scalar fields and top-level scalar
 * arrays, while nested objects and complex arrays remain raw-YAML-only.
 *
 * @param {string} originalYaml Original YAML content.
 * @param {{scalars?: Record<string, string>, arrays?: Record<string, string[]>}} updates Updated form values.
 * @returns {string} Updated YAML content.
 */
function applyFormUpdates(originalYaml, updates) {
    const lines = originalYaml.split(/\r?\n/u);
    const scalarUpdates = updates.scalars || {};
    const arrayUpdates = updates.arrays || {};
    const touchedScalarKeys = new Set();
    const touchedArrayKeys = new Set();
    const blocks = findTopLevelBlocks(lines);
    const updatedLines = [];
    let cursor = 0;

    for (const block of blocks) {
        while (cursor < block.start) {
            updatedLines.push(lines[cursor]);
            cursor += 1;
        }

        if (Object.prototype.hasOwnProperty.call(scalarUpdates, block.key)) {
            touchedScalarKeys.add(block.key);
            updatedLines.push(renderScalarLine(block.key, scalarUpdates[block.key]));
            cursor = block.end + 1;
            continue;
        }

        if (Object.prototype.hasOwnProperty.call(arrayUpdates, block.key)) {
            touchedArrayKeys.add(block.key);
            updatedLines.push(...renderArrayBlock(block.key, arrayUpdates[block.key]));
            cursor = block.end + 1;
            continue;
        }

        while (cursor <= block.end) {
            updatedLines.push(lines[cursor]);
            cursor += 1;
        }
    }

    while (cursor < lines.length) {
        updatedLines.push(lines[cursor]);
        cursor += 1;
    }

    for (const [key, value] of Object.entries(scalarUpdates)) {
        if (touchedScalarKeys.has(key)) {
            continue;
        }

        updatedLines.push(renderScalarLine(key, value));
    }

    for (const [key, value] of Object.entries(arrayUpdates)) {
        if (touchedArrayKeys.has(key)) {
            continue;
        }

        updatedLines.push(...renderArrayBlock(key, value));
    }

    return updatedLines.join("\n");
}

/**
 * Apply only scalar updates back into the original YAML text.
 * This helper is preserved for compatibility with existing tests and callers
 * that only edit top-level scalar fields.
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

    return undefined;
}

/**
 * Format a scalar value for YAML output.
 *
 * @param {string} value Scalar value.
 * @returns {string} YAML-ready scalar.
 */
function formatYamlScalar(value) {
    if (/^-?\d+(?:\.\d+)?$/u.test(value) || /^(true|false)$/iu.test(value)) {
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
 * Parse a sequence of child lines as a top-level scalar array.
 *
 * @param {string[]} childLines Indented child lines.
 * @returns {Array<{raw: string, isComplex: boolean}> | null} Parsed array items or null when the block is not an array.
 */
function parseTopLevelArray(childLines) {
    const items = [];

    for (const line of childLines) {
        if (line.trim().length === 0 || line.trim().startsWith("#")) {
            continue;
        }

        const trimmed = line.trimStart();
        if (!trimmed.startsWith("-")) {
            return null;
        }

        const raw = trimmed.slice(1).trim();
        items.push({
            raw,
            isComplex: raw.length === 0 || raw.startsWith("{") || raw.startsWith("[") || /^[A-Za-z0-9_]+:\s*/u.test(raw)
        });
    }

    return items;
}

/**
 * Find top-level YAML blocks so form updates can replace whole entries without
 * touching unrelated domains in the file.
 *
 * @param {string[]} lines YAML lines.
 * @returns {Array<{key: string, start: number, end: number}>} Top-level blocks.
 */
function findTopLevelBlocks(lines) {
    const blocks = [];

    for (let index = 0; index < lines.length; index += 1) {
        const line = lines[index];
        if (!line || line.trim().length === 0 || line.trim().startsWith("#") || /^\s/u.test(line)) {
            continue;
        }

        const match = /^([A-Za-z0-9_]+):(?:\s*(.*))?$/u.exec(line);
        if (!match) {
            continue;
        }

        let cursor = index + 1;
        while (cursor < lines.length) {
            const nextLine = lines[cursor];
            if (nextLine.trim().length === 0 || nextLine.trim().startsWith("#")) {
                cursor += 1;
                continue;
            }

            if (!/^\s/u.test(nextLine)) {
                break;
            }

            cursor += 1;
        }

        blocks.push({
            key: match[1],
            start: index,
            end: cursor - 1
        });
        index = cursor - 1;
    }

    return blocks;
}

/**
 * Render a top-level scalar line.
 *
 * @param {string} key Property name.
 * @param {string} value Scalar value.
 * @returns {string} Rendered YAML line.
 */
function renderScalarLine(key, value) {
    return `${key}: ${formatYamlScalar(value)}`;
}

/**
 * Render a top-level scalar array block.
 *
 * @param {string} key Property name.
 * @param {string[]} items Array items.
 * @returns {string[]} Rendered YAML lines.
 */
function renderArrayBlock(key, items) {
    const normalizedItems = Array.isArray(items)
        ? items
            .map((item) => String(item).trim())
            .filter((item) => item.length > 0)
        : [];

    const lines = [`${key}:`];
    for (const item of normalizedItems) {
        lines.push(`  - ${formatYamlScalar(item)}`);
    }

    return lines;
}

module.exports = {
    applyFormUpdates,
    applyScalarUpdates,
    findTopLevelBlocks,
    formatYamlScalar,
    getEditableSchemaFields,
    isScalarCompatible,
    normalizeSchemaEnumValues,
    parseBatchArrayValue,
    parseSchemaContent,
    parseTopLevelYaml,
    unquoteScalar,
    validateParsedConfig,
    formatSchemaDefaultValue
};
