/**
 * Parse the repository's minimal config-schema subset into a recursive tree.
 * The parser intentionally mirrors the same high-level contract used by the
 * runtime validator and source generator so tooling diagnostics stay aligned.
 *
 * @param {string} content Raw schema JSON text.
 * @returns {{
 *   type: "object",
 *   required: string[],
 *   properties: Record<string, SchemaNode>
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
 * Produce extension-facing validation diagnostics from schema and parsed YAML.
 *
 * @param {{type: "object", required: string[], properties: Record<string, SchemaNode>}} schemaInfo Parsed schema.
 * @param {YamlNode} parsedYaml Parsed YAML tree.
 * @returns {Array<{severity: "error" | "warning", message: string}>} Validation diagnostics.
 */
function validateParsedConfig(schemaInfo, parsedYaml) {
    const diagnostics = [];
    validateNode(schemaInfo, parsedYaml, "", diagnostics);
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
 * Apply form updates back into YAML. The implementation rewrites the YAML tree
 * from the parsed structure so nested object edits can be saved safely.
 *
 * @param {string} originalYaml Original YAML content.
 * @param {{scalars?: Record<string, string>, arrays?: Record<string, string[]>, objectArrays?: Record<string, Array<Record<string, unknown>>>}} updates Updated form values.
 * @returns {string} Updated YAML content.
 */
function applyFormUpdates(originalYaml, updates) {
    const root = normalizeRootNode(parseTopLevelYaml(originalYaml));
    const scalarUpdates = updates.scalars || {};
    const arrayUpdates = updates.arrays || {};
    const objectArrayUpdates = updates.objectArrays || {};

    for (const [path, value] of Object.entries(scalarUpdates)) {
        setNodeAtPath(root, path.split("."), createScalarNode(String(value)));
    }

    for (const [path, values] of Object.entries(arrayUpdates)) {
        setNodeAtPath(root, path.split("."), createArrayNode(
            (values || []).map((item) => createScalarNode(String(item)))));
    }

    for (const [path, items] of Object.entries(objectArrayUpdates)) {
        setNodeAtPath(root, path.split("."), createArrayNode(
            (items || []).map((item) => createNodeFromFormValue(item))));
    }

    return renderYaml(root).join("\n");
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
 * Parse one schema node recursively.
 *
 * @param {unknown} rawNode Raw schema node.
 * @param {string} displayPath Logical property path.
 * @returns {SchemaNode} Parsed schema node.
 */
function parseSchemaNode(rawNode, displayPath) {
    const value = rawNode && typeof rawNode === "object" ? rawNode : {};
    const type = typeof value.type === "string" ? value.type : "object";
    const metadata = {
        title: typeof value.title === "string" ? value.title : undefined,
        description: typeof value.description === "string" ? value.description : undefined,
        defaultValue: formatSchemaDefaultValue(value.default),
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
            properties[key] = parseSchemaNode(propertyNode, combinePath(displayPath, key));
        }

        return {
            type: "object",
            displayPath,
            required,
            properties,
            title: metadata.title,
            description: metadata.description,
            defaultValue: metadata.defaultValue
        };
    }

    if (type === "array") {
        const itemNode = parseSchemaNode(value.items || {}, `${displayPath}[]`);
        return {
            type: "array",
            displayPath,
            title: metadata.title,
            description: metadata.description,
            defaultValue: metadata.defaultValue,
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
 */
function validateNode(schemaNode, yamlNode, displayPath, diagnostics) {
    if (schemaNode.type === "object") {
        validateObjectNode(schemaNode, yamlNode, displayPath, diagnostics);
        return;
    }

    if (schemaNode.type === "array") {
        if (!yamlNode || yamlNode.kind !== "array") {
            diagnostics.push({
                severity: "error",
                message: `Property '${displayPath}' is expected to be an array.`
            });
            return;
        }

        for (let index = 0; index < yamlNode.items.length; index += 1) {
            validateNode(schemaNode.items, yamlNode.items[index], `${displayPath}[${index}]`, diagnostics);
        }
        return;
    }

    if (!yamlNode || yamlNode.kind !== "scalar") {
        diagnostics.push({
            severity: "error",
            message: `Property '${displayPath}' is expected to be '${schemaNode.type}', but the current YAML shape is '${yamlNode ? yamlNode.kind : "missing"}'.`
        });
        return;
    }

    if (!isScalarCompatible(schemaNode.type, yamlNode.value)) {
        diagnostics.push({
            severity: "error",
            message: `Property '${displayPath}' is expected to be '${schemaNode.type}', but the current scalar value is incompatible.`
        });
        return;
    }

    if (Array.isArray(schemaNode.enumValues) &&
        schemaNode.enumValues.length > 0 &&
        !schemaNode.enumValues.includes(unquoteScalar(yamlNode.value))) {
        diagnostics.push({
            severity: "error",
            message: `Property '${displayPath}' must be one of: ${schemaNode.enumValues.join(", ")}.`
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
 */
function validateObjectNode(schemaNode, yamlNode, displayPath, diagnostics) {
    if (!yamlNode || yamlNode.kind !== "object") {
        const subject = displayPath.length === 0 ? "Root object" : `Property '${displayPath}'`;
        diagnostics.push({
            severity: "error",
            message: `${subject} is expected to be an object.`
        });
        return;
    }

    for (const requiredProperty of schemaNode.required) {
        if (!yamlNode.map.has(requiredProperty)) {
            diagnostics.push({
                severity: "error",
                message: `Required property '${combinePath(displayPath, requiredProperty)}' is missing.`
            });
        }
    }

    for (const entry of yamlNode.entries) {
        if (!Object.prototype.hasOwnProperty.call(schemaNode.properties, entry.key)) {
            diagnostics.push({
                severity: "error",
                message: `Property '${combinePath(displayPath, entry.key)}' is not declared in the matching schema.`
            });
            continue;
        }

        validateNode(
            schemaNode.properties[entry.key],
            entry.node,
            combinePath(displayPath, entry.key),
            diagnostics);
    }
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

        const match = /^([A-Za-z0-9_]+):(.*)$/u.exec(token.text);
        if (!match) {
            state.index += 1;
            continue;
        }

        const key = match[1];
        const rawValue = match[2].trim();
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

        if (/^[A-Za-z0-9_]+:/u.test(rest)) {
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
function renderYaml(node, indent = 0) {
    if (node.kind === "object") {
        return renderObjectNode(node, indent);
    }

    if (node.kind === "array") {
        return renderArrayNode(node, indent);
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
function renderObjectNode(node, indent) {
    const lines = [];
    for (const entry of node.entries) {
        if (entry.node.kind === "scalar") {
            lines.push(`${" ".repeat(indent)}${entry.key}: ${formatYamlScalar(entry.node.value)}`);
            continue;
        }

        if (entry.node.kind === "array" && entry.node.items.length === 0) {
            lines.push(`${" ".repeat(indent)}${entry.key}: []`);
            continue;
        }

        lines.push(`${" ".repeat(indent)}${entry.key}:`);
        lines.push(...renderYaml(entry.node, indent + 2));
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
function renderArrayNode(node, indent) {
    const lines = [];
    for (const item of node.items) {
        if (item.kind === "scalar") {
            lines.push(`${" ".repeat(indent)}- ${formatYamlScalar(item.value)}`);
            continue;
        }

        lines.push(`${" ".repeat(indent)}-`);
        lines.push(...renderYaml(item, indent + 2));
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
 * Combine a parent path with one child segment.
 *
 * @param {string} parentPath Parent path.
 * @param {string} key Child key.
 * @returns {string} Combined path.
 */
function combinePath(parentPath, key) {
    return parentPath && parentPath !== "<root>" ? `${parentPath}.${key}` : key;
}

module.exports = {
    applyFormUpdates,
    applyScalarUpdates,
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
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string
 * } | {
 *   type: "array",
 *   displayPath: string,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string,
 *   refTable?: string,
 *   items: SchemaNode
 * } | {
 *   type: "string" | "integer" | "number" | "boolean",
 *   displayPath: string,
 *   title?: string,
 *   description?: string,
 *   defaultValue?: string,
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
