const {
    joinArrayIndexPath,
    joinArrayTemplatePath,
    joinPropertyPath,
    splitObjectPath
} = require("./configPath");
const {ValidationMessageKeys} = require("./localizationKeys");

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
            properties[key] = parseSchemaNode(propertyNode, joinPropertyPath(displayPath, key));
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
        const itemNode = parseSchemaNode(value.items || {}, joinArrayTemplatePath(displayPath));
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

        for (let index = 0; index < yamlNode.items.length; index += 1) {
            validateNode(
                schemaNode.items,
                yamlNode.items[index],
                joinArrayIndexPath(displayPath, index),
                diagnostics,
                localizer);
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
        const subject = displayPath.length === 0
            ? localizer && localizer.isChinese
                ? "根对象应为对象。"
                : "Root object is expected to be an object."
            : localizer && localizer.isChinese
                ? `属性“${displayPath}”应为对象。`
                : `Property '${displayPath}' is expected to be an object.`;
        diagnostics.push({
            severity: "error",
            message: localizeValidationMessage(ValidationMessageKeys.expectedObject, localizer, {
                subject,
                displayPath
            })
        });
        return;
    }

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
            case ValidationMessageKeys.expectedObject:
                return params.subject;
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
        case ValidationMessageKeys.expectedObject:
            return params.subject;
        case ValidationMessageKeys.missingRequired:
            return `Required property '${params.displayPath}' is missing.`;
        case ValidationMessageKeys.unknownProperty:
            return `Property '${params.displayPath}' is not declared in the matching schema.`;
        default:
            return key;
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
