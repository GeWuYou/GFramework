const fs = require("fs");
const path = require("path");
const vscode = require("vscode");
const {
    applyFormUpdates,
    getEditableSchemaFields,
    parseBatchArrayValue,
    parseSchemaContent,
    parseTopLevelYaml,
    unquoteScalar,
    validateParsedConfig
} = require("./configValidation");

/**
 * Activate the GFramework config extension.
 * The initial MVP focuses on workspace file navigation, lightweight validation,
 * and a small form-preview entry for top-level scalar values.
 *
 * @param {vscode.ExtensionContext} context Extension context.
 */
function activate(context) {
    const diagnostics = vscode.languages.createDiagnosticCollection("gframeworkConfig");
    const provider = new ConfigTreeDataProvider();

    context.subscriptions.push(diagnostics);
    context.subscriptions.push(
        vscode.window.registerTreeDataProvider("gframeworkConfigExplorer", provider),
        vscode.commands.registerCommand("gframeworkConfig.refresh", async () => {
            provider.refresh();
            await validateAllConfigs(diagnostics);
        }),
        vscode.commands.registerCommand("gframeworkConfig.openRaw", async (item) => {
            await openRawFile(item);
        }),
        vscode.commands.registerCommand("gframeworkConfig.openSchema", async (item) => {
            await openSchemaFile(item);
        }),
        vscode.commands.registerCommand("gframeworkConfig.openFormPreview", async (item) => {
            await openFormPreview(item, diagnostics);
        }),
        vscode.commands.registerCommand("gframeworkConfig.batchEditDomain", async (item) => {
            await openBatchEdit(item, diagnostics, provider);
        }),
        vscode.commands.registerCommand("gframeworkConfig.validateAll", async () => {
            await validateAllConfigs(diagnostics);
        }),
        vscode.workspace.onDidSaveTextDocument(async (document) => {
            const workspaceRoot = getWorkspaceRoot();
            if (!workspaceRoot) {
                return;
            }

            if (!isConfigFile(document.uri, workspaceRoot)) {
                return;
            }

            await validateConfigFile(document.uri, diagnostics);
            provider.refresh();
        }),
        vscode.workspace.onDidChangeWorkspaceFolders(async () => {
            provider.refresh();
            await validateAllConfigs(diagnostics);
        })
    );

    void validateAllConfigs(diagnostics);
}

/**
 * Deactivate the extension.
 */
function deactivate() {
}

/**
 * Tree provider for the GFramework config explorer view.
 */
class ConfigTreeDataProvider {
    constructor() {
        this._emitter = new vscode.EventEmitter();
        this.onDidChangeTreeData = this._emitter.event;
    }

    /**
     * Refresh the tree view.
     */
    refresh() {
        this._emitter.fire(undefined);
    }

    /**
     * Resolve a tree item.
     *
     * @param {ConfigTreeItem} element Tree element.
     * @returns {vscode.TreeItem} Tree item.
     */
    getTreeItem(element) {
        return element;
    }

    /**
     * Resolve child elements.
     *
     * @param {ConfigTreeItem | undefined} element Parent element.
     * @returns {Thenable<ConfigTreeItem[]>} Child items.
     */
    async getChildren(element) {
        const workspaceRoot = getWorkspaceRoot();
        if (!workspaceRoot) {
            return [];
        }

        if (!element) {
            return this.getRootItems(workspaceRoot);
        }

        if (element.kind !== "domain" || !element.resourceUri) {
            return [];
        }

        return this.getFileItems(workspaceRoot, element.resourceUri);
    }

    /**
     * Build root domain items from the config directory.
     *
     * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
     * @returns {Promise<ConfigTreeItem[]>} Root items.
     */
    async getRootItems(workspaceRoot) {
        const configRoot = getConfigRoot(workspaceRoot);
        if (!configRoot || !fs.existsSync(configRoot.fsPath)) {
            return [
                new ConfigTreeItem(
                    "No config directory",
                    "info",
                    vscode.TreeItemCollapsibleState.None,
                    undefined,
                    "Set gframeworkConfig.configPath or create the directory.")
            ];
        }

        const entries = fs.readdirSync(configRoot.fsPath, {withFileTypes: true})
            .filter((entry) => entry.isDirectory())
            .sort((left, right) => left.name.localeCompare(right.name));

        return entries.map((entry) => {
            const domainUri = vscode.Uri.joinPath(configRoot, entry.name);
            return new ConfigTreeItem(
                entry.name,
                "domain",
                vscode.TreeItemCollapsibleState.Collapsed,
                domainUri,
                undefined);
        });
    }

    /**
     * Build file items for a config domain directory.
     *
     * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
     * @param {vscode.Uri} domainUri Domain directory URI.
     * @returns {Promise<ConfigTreeItem[]>} File items.
     */
    async getFileItems(workspaceRoot, domainUri) {
        const entries = fs.readdirSync(domainUri.fsPath, {withFileTypes: true})
            .filter((entry) => entry.isFile() && isYamlPath(entry.name))
            .sort((left, right) => left.name.localeCompare(right.name));

        return entries.map((entry) => {
            const fileUri = vscode.Uri.joinPath(domainUri, entry.name);
            const schemaUri = getSchemaUriForConfigFile(fileUri, workspaceRoot);
            const description = schemaUri && fs.existsSync(schemaUri.fsPath)
                ? "schema"
                : "schema missing";
            const item = new ConfigTreeItem(
                entry.name,
                "file",
                vscode.TreeItemCollapsibleState.None,
                fileUri,
                description);

            item.contextValue = "gframeworkConfigFile";
            item.command = {
                command: "gframeworkConfig.openRaw",
                title: "Open Raw",
                arguments: [item]
            };

            return item;
        });
    }
}

/**
 * Tree item used by the config explorer.
 */
class ConfigTreeItem extends vscode.TreeItem {
    /**
     * @param {string} label Display label.
     * @param {"domain" | "file" | "info"} kind Item kind.
     * @param {vscode.TreeItemCollapsibleState} collapsibleState Collapsible state.
     * @param {vscode.Uri | undefined} resourceUri Resource URI.
     * @param {string | undefined} description Description.
     */
    constructor(label, kind, collapsibleState, resourceUri, description) {
        super(label, collapsibleState);
        this.kind = kind;
        this.resourceUri = resourceUri;
        this.description = description;
        this.contextValue = kind === "file" ? "gframeworkConfigFile" : kind;
    }
}

/**
 * Open the selected raw config file.
 *
 * @param {ConfigTreeItem | { resourceUri?: vscode.Uri }} item Tree item.
 * @returns {Promise<void>} Async task.
 */
async function openRawFile(item) {
    const uri = item && item.resourceUri;
    if (!uri) {
        return;
    }

    const document = await vscode.workspace.openTextDocument(uri);
    await vscode.window.showTextDocument(document, {preview: false});
}

/**
 * Open the matching schema file for a selected config item.
 *
 * @param {ConfigTreeItem | { resourceUri?: vscode.Uri }} item Tree item.
 * @returns {Promise<void>} Async task.
 */
async function openSchemaFile(item) {
    const workspaceRoot = getWorkspaceRoot();
    const configUri = item && item.resourceUri;
    if (!workspaceRoot || !configUri) {
        return;
    }

    const schemaUri = getSchemaUriForConfigFile(configUri, workspaceRoot);
    if (!schemaUri || !fs.existsSync(schemaUri.fsPath)) {
        void vscode.window.showWarningMessage("Matching schema file was not found.");
        return;
    }

    const document = await vscode.workspace.openTextDocument(schemaUri);
    await vscode.window.showTextDocument(document, {preview: false});
}

/**
 * Open a lightweight form preview for top-level scalar fields and scalar
 * arrays. Nested objects and more complex array shapes still use raw YAML as
 * the escape hatch.
 *
 * @param {ConfigTreeItem | { resourceUri?: vscode.Uri }} item Tree item.
 * @param {vscode.DiagnosticCollection} diagnostics Diagnostic collection.
 * @returns {Promise<void>} Async task.
 */
async function openFormPreview(item, diagnostics) {
    const workspaceRoot = getWorkspaceRoot();
    const configUri = item && item.resourceUri;
    if (!workspaceRoot || !configUri) {
        return;
    }

    const yamlText = await fs.promises.readFile(configUri.fsPath, "utf8");
    const parsedYaml = parseTopLevelYaml(yamlText);
    const schemaInfo = await loadSchemaInfoForConfig(configUri, workspaceRoot);

    const panel = vscode.window.createWebviewPanel(
        "gframeworkConfigFormPreview",
        `Config Form: ${path.basename(configUri.fsPath)}`,
        vscode.ViewColumn.Beside,
        {enableScripts: true});

    panel.webview.html = renderFormHtml(
        path.basename(configUri.fsPath),
        schemaInfo,
        parsedYaml);

    panel.webview.onDidReceiveMessage(async (message) => {
        if (message.type === "save") {
            const updatedYaml = applyFormUpdates(yamlText, {
                scalars: message.scalars || {},
                arrays: parseArrayFieldPayload(message.arrays || {})
            });
            await fs.promises.writeFile(configUri.fsPath, updatedYaml, "utf8");
            const document = await vscode.workspace.openTextDocument(configUri);
            await document.save();
            await validateConfigFile(configUri, diagnostics);
            void vscode.window.showInformationMessage("Config file saved from form preview.");
        }

        if (message.type === "openRaw") {
            await openRawFile({resourceUri: configUri});
        }
    });
}

/**
 * Validate all config files in the configured config directory.
 *
 * @param {vscode.DiagnosticCollection} diagnostics Diagnostic collection.
 * @returns {Promise<void>} Async task.
 */
async function validateAllConfigs(diagnostics) {
    diagnostics.clear();

    const workspaceRoot = getWorkspaceRoot();
    if (!workspaceRoot) {
        return;
    }

    const configRoot = getConfigRoot(workspaceRoot);
    if (!configRoot || !fs.existsSync(configRoot.fsPath)) {
        return;
    }

    const files = enumerateYamlFiles(configRoot.fsPath);
    for (const filePath of files) {
        await validateConfigFile(vscode.Uri.file(filePath), diagnostics);
    }
}

/**
 * Validate a single config file against its matching schema.
 *
 * @param {vscode.Uri} configUri Config file URI.
 * @param {vscode.DiagnosticCollection} diagnostics Diagnostic collection.
 * @returns {Promise<void>} Async task.
 */
async function validateConfigFile(configUri, diagnostics) {
    const workspaceRoot = getWorkspaceRoot();
    if (!workspaceRoot) {
        return;
    }

    if (!isConfigFile(configUri, workspaceRoot)) {
        return;
    }

    const yamlText = await fs.promises.readFile(configUri.fsPath, "utf8");
    const parsedYaml = parseTopLevelYaml(yamlText);
    const schemaInfo = await loadSchemaInfoForConfig(configUri, workspaceRoot);
    const fileDiagnostics = [];

    if (!schemaInfo.exists) {
        fileDiagnostics.push(new vscode.Diagnostic(
            new vscode.Range(0, 0, 0, 1),
            `Matching schema file not found: ${schemaInfo.schemaPath}`,
            vscode.DiagnosticSeverity.Warning));
        diagnostics.set(configUri, fileDiagnostics);
        return;
    }

    for (const diagnostic of validateParsedConfig(schemaInfo, parsedYaml)) {
        fileDiagnostics.push(new vscode.Diagnostic(
            new vscode.Range(0, 0, 0, 1),
            diagnostic.message,
            diagnostic.severity === "error"
                ? vscode.DiagnosticSeverity.Error
                : vscode.DiagnosticSeverity.Warning));
    }

    diagnostics.set(configUri, fileDiagnostics);
}

/**
 * Open a minimal batch editor for one config domain.
 * The workflow intentionally focuses on one schema-bound directory at a time
 * so designers can apply the same top-level scalar or scalar-array values
 * across multiple files without dropping down to repetitive raw-YAML edits.
 *
 * @param {ConfigTreeItem | { kind?: string, resourceUri?: vscode.Uri }} item Tree item.
 * @param {vscode.DiagnosticCollection} diagnostics Diagnostic collection.
 * @param {ConfigTreeDataProvider} provider Tree provider.
 * @returns {Promise<void>} Async task.
 */
async function openBatchEdit(item, diagnostics, provider) {
    const workspaceRoot = getWorkspaceRoot();
    const domainUri = item && item.resourceUri;
    if (!workspaceRoot || !domainUri || item.kind !== "domain") {
        return;
    }

    const fileItems = fs.readdirSync(domainUri.fsPath, {withFileTypes: true})
        .filter((entry) => entry.isFile() && isYamlPath(entry.name))
        .sort((left, right) => left.name.localeCompare(right.name))
        .map((entry) => {
            const fileUri = vscode.Uri.joinPath(domainUri, entry.name);
            return {
                label: entry.name,
                description: path.relative(workspaceRoot.uri.fsPath, fileUri.fsPath),
                fileUri,
                picked: true
            };
        });

    if (fileItems.length === 0) {
        void vscode.window.showWarningMessage("No YAML config files were found in the selected domain.");
        return;
    }

    const selectedFiles = await vscode.window.showQuickPick(fileItems, {
        canPickMany: true,
        title: `Batch Edit: ${path.basename(domainUri.fsPath)}`,
        placeHolder: "Select the config files to update."
    });
    if (!selectedFiles || selectedFiles.length === 0) {
        return;
    }

    const schemaInfo = await loadSchemaInfoForConfig(selectedFiles[0].fileUri, workspaceRoot);
    if (!schemaInfo.exists) {
        void vscode.window.showWarningMessage("Batch edit requires a matching schema file for the selected domain.");
        return;
    }

    const editableFields = getEditableSchemaFields(schemaInfo);
    if (editableFields.length === 0) {
        void vscode.window.showWarningMessage(
            "No top-level scalar or scalar-array fields were found in the matching schema.");
        return;
    }

    const selectedFields = await vscode.window.showQuickPick(
        editableFields.map((field) => ({
            label: field.title || field.key,
            description: field.inputKind === "array"
                ? `array<${field.itemType}>`
                : field.type,
            detail: [
                field.required ? "required" : "",
                field.description || "",
                field.refTable ? `ref: ${field.refTable}` : ""
            ].filter((part) => part.length > 0).join(" · ") || undefined,
            field
        })),
        {
            canPickMany: true,
            title: `Batch Edit Fields: ${path.basename(domainUri.fsPath)}`,
            placeHolder: "Select the fields to apply across the chosen files."
        });
    if (!selectedFields || selectedFields.length === 0) {
        return;
    }

    const updates = {
        scalars: {},
        arrays: {}
    };

    for (const selectedField of selectedFields) {
        const field = selectedField.field;
        const rawValue = await promptBatchFieldValue(field);
        if (rawValue === undefined) {
            return;
        }

        if (field.inputKind === "array") {
            updates.arrays[field.key] = parseBatchArrayValue(rawValue);
            continue;
        }

        updates.scalars[field.key] = rawValue;
    }

    const edit = new vscode.WorkspaceEdit();
    const touchedDocuments = [];
    let changedFileCount = 0;

    for (const fileItem of selectedFiles) {
        const document = await vscode.workspace.openTextDocument(fileItem.fileUri);
        const originalYaml = document.getText();
        const updatedYaml = applyFormUpdates(originalYaml, updates);
        if (updatedYaml === originalYaml) {
            continue;
        }

        const fullRange = new vscode.Range(
            document.positionAt(0),
            document.positionAt(originalYaml.length));
        edit.replace(fileItem.fileUri, fullRange, updatedYaml);
        touchedDocuments.push(document);
        changedFileCount += 1;
    }

    if (changedFileCount === 0) {
        void vscode.window.showInformationMessage("Batch edit did not change any selected config files.");
        return;
    }

    const applied = await vscode.workspace.applyEdit(edit);
    if (!applied) {
        throw new Error("VS Code rejected the batch edit workspace update.");
    }

    for (const document of touchedDocuments) {
        await document.save();
        await validateConfigFile(document.uri, diagnostics);
    }

    provider.refresh();
    void vscode.window.showInformationMessage(
        `Batch updated ${changedFileCount} config file(s) in '${path.basename(domainUri.fsPath)}'.`);
}

/**
 * Load schema info for a config file.
 *
 * @param {vscode.Uri} configUri Config file URI.
 * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
 * @returns {Promise<{exists: boolean, schemaPath: string, required: string[], properties: Record<string, {
 *     type: string,
 *     itemType?: string,
 *     title?: string,
 *     description?: string,
 *     defaultValue?: string,
 *     enumValues?: string[],
 *     itemEnumValues?: string[],
 *     refTable?: string
 * }>}>} Schema info.
 */
async function loadSchemaInfoForConfig(configUri, workspaceRoot) {
    const schemaUri = getSchemaUriForConfigFile(configUri, workspaceRoot);
    const schemaPath = schemaUri ? schemaUri.fsPath : "";
    if (!schemaUri || !fs.existsSync(schemaUri.fsPath)) {
        return {
            exists: false,
            schemaPath,
            required: [],
            properties: {}
        };
    }

    const content = await fs.promises.readFile(schemaUri.fsPath, "utf8");
    try {
        const parsed = parseSchemaContent(content);

        return {
            exists: true,
            schemaPath,
            required: parsed.required,
            properties: parsed.properties
        };
    } catch (error) {
        return {
            exists: false,
            schemaPath,
            required: [],
            properties: {}
        };
    }
}

/**
 * Render the form-preview webview HTML.
 *
 * @param {string} fileName File name.
 * @param {{exists: boolean, schemaPath: string, required: string[], properties: Record<string, {
 *     type: string,
 *     itemType?: string,
 *     title?: string,
 *     description?: string,
 *     defaultValue?: string,
 *     enumValues?: string[],
 *     itemEnumValues?: string[],
 *     refTable?: string
 * }>}} schemaInfo Schema info.
 * @param {{entries: Map<string, {kind: string, value?: string, items?: Array<{raw: string, isComplex: boolean}>}>, keys: Set<string>}} parsedYaml Parsed YAML data.
 * @returns {string} HTML string.
 */
function renderFormHtml(fileName, schemaInfo, parsedYaml) {
    const scalarFields = Array.from(parsedYaml.entries.entries())
        .filter(([, entry]) => entry.kind === "scalar")
        .map(([key, entry]) => {
            const propertySchema = schemaInfo.properties[key] || {};
            const displayName = propertySchema.title || key;
            const escapedKey = escapeHtml(key);
            const escapedDisplayName = escapeHtml(displayName);
            const escapedValue = escapeHtml(unquoteScalar(entry.value || ""));
            const required = schemaInfo.required.includes(key) ? "<span class=\"badge\">required</span>" : "";
            const metadataHint = renderFieldHint(propertySchema, false);
            const enumValues = Array.isArray(propertySchema.enumValues) ? propertySchema.enumValues : [];
            const inputControl = enumValues.length > 0
                ? `
                    <select data-key="${escapedKey}">
                        ${enumValues.map((value) => {
                    const escapedOption = escapeHtml(value);
                    const selected = value === unquoteScalar(entry.value || "") ? " selected" : "";
                    return `<option value="${escapedOption}"${selected}>${escapedOption}</option>`;
                }).join("\n")}
                    </select>
                `
                : `<input data-key="${escapedKey}" value="${escapedValue}" />`;
            return `
                <label class="field">
                    <span class="label">${escapedDisplayName} ${required}</span>
                    <span class="meta-key">${escapedKey}</span>
                    ${metadataHint}
                    ${inputControl}
                </label>
            `;
        })
        .join("\n");

    const arrayFields = Array.from(parsedYaml.entries.entries())
        .filter(([, entry]) => entry.kind === "array")
        .map(([key, entry]) => {
            const propertySchema = schemaInfo.properties[key] || {};
            const displayName = propertySchema.title || key;
            const escapedKey = escapeHtml(key);
            const escapedDisplayName = escapeHtml(displayName);
            const escapedValue = escapeHtml((entry.items || [])
                .map((item) => unquoteScalar(item.raw))
                .join("\n"));
            const required = schemaInfo.required.includes(key) ? "<span class=\"badge\">required</span>" : "";
            const itemType = propertySchema.itemType
                ? `array<${escapeHtml(propertySchema.itemType)}>`
                : "array";
            const metadataHint = renderFieldHint(propertySchema, true);

            return `
                <label class="field">
                    <span class="label">${escapedDisplayName} ${required}</span>
                    <span class="meta-key">${escapedKey}</span>
                    <span class="hint">One item per line. Expected type: ${itemType}</span>
                    ${metadataHint}
                    <textarea data-array-key="${escapedKey}" rows="5">${escapedValue}</textarea>
                </label>
            `;
        })
        .join("\n");

    const unsupportedFields = Array.from(parsedYaml.entries.entries())
        .filter(([, entry]) => entry.kind !== "scalar" && entry.kind !== "array")
        .map(([key, entry]) => `
            <div class="unsupported">
                <strong>${escapeHtml(key)}</strong>: ${escapeHtml(entry.kind)} fields are currently raw-YAML-only.
            </div>
        `)
        .join("\n");

    const schemaStatus = schemaInfo.exists
        ? `Schema: ${escapeHtml(schemaInfo.schemaPath)}`
        : `Schema missing: ${escapeHtml(schemaInfo.schemaPath)}`;

    const editableContent = [scalarFields, arrayFields].filter((content) => content.length > 0).join("\n");
    const unsupportedSection = unsupportedFields.length > 0
        ? `<div class="unsupported-list">${unsupportedFields}</div>`
        : "";
    const emptyState = editableContent.length > 0
        ? `${editableContent}${unsupportedSection}`
        : "<p>No editable top-level scalar or scalar-array fields were detected. Use raw YAML for nested objects or complex arrays.</p>";

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <style>
        body {
            font-family: var(--vscode-font-family);
            color: var(--vscode-foreground);
            background: var(--vscode-editor-background);
            padding: 16px;
        }
        .toolbar {
            display: flex;
            gap: 12px;
            margin-bottom: 16px;
        }
        button {
            border: 1px solid var(--vscode-button-border, transparent);
            background: var(--vscode-button-background);
            color: var(--vscode-button-foreground);
            padding: 8px 12px;
            cursor: pointer;
        }
        .meta {
            margin-bottom: 16px;
            color: var(--vscode-descriptionForeground);
        }
        .field {
            display: block;
            margin-bottom: 12px;
        }
        .meta-key {
            display: inline-block;
            margin-bottom: 6px;
            color: var(--vscode-descriptionForeground);
            font-size: 12px;
        }
        .label {
            display: block;
            margin-bottom: 4px;
            font-weight: 600;
        }
        input, select {
            width: 100%;
            padding: 8px;
            box-sizing: border-box;
            border: 1px solid var(--vscode-input-border, transparent);
            background: var(--vscode-input-background);
            color: var(--vscode-input-foreground);
        }
        textarea {
            width: 100%;
            padding: 8px;
            box-sizing: border-box;
            border: 1px solid var(--vscode-input-border, transparent);
            background: var(--vscode-input-background);
            color: var(--vscode-input-foreground);
            font-family: var(--vscode-editor-font-family, var(--vscode-font-family));
            resize: vertical;
        }
        .hint {
            display: block;
            margin-bottom: 6px;
            color: var(--vscode-descriptionForeground);
            font-size: 12px;
        }
        .badge {
            display: inline-block;
            margin-left: 6px;
            padding: 1px 6px;
            border-radius: 999px;
            background: var(--vscode-badge-background);
            color: var(--vscode-badge-foreground);
            font-size: 11px;
        }
        .unsupported-list {
            margin-top: 20px;
            border-top: 1px solid var(--vscode-panel-border, transparent);
            padding-top: 16px;
        }
        .unsupported {
            margin-bottom: 10px;
            color: var(--vscode-descriptionForeground);
        }
    </style>
</head>
<body>
    <div class="toolbar">
        <button id="save">Save Form</button>
        <button id="openRaw">Open Raw YAML</button>
    </div>
    <div class="meta">
        <div>File: ${escapeHtml(fileName)}</div>
        <div>${schemaStatus}</div>
    </div>
    <div id="fields">${emptyState}</div>
    <script>
        const vscode = acquireVsCodeApi();
        document.getElementById("save").addEventListener("click", () => {
            const scalars = {};
            const arrays = {};
            for (const control of document.querySelectorAll("[data-key]")) {
                scalars[control.dataset.key] = control.value;
            }
            for (const textarea of document.querySelectorAll("textarea[data-array-key]")) {
                arrays[textarea.dataset.arrayKey] = textarea.value;
            }
            vscode.postMessage({ type: "save", scalars, arrays });
        });
        document.getElementById("openRaw").addEventListener("click", () => {
            vscode.postMessage({ type: "openRaw" });
        });
    </script>
</body>
</html>`;
}

/**
 * Render human-facing metadata hints for one schema field.
 *
 * @param {{description?: string, defaultValue?: string, enumValues?: string[], itemEnumValues?: string[], refTable?: string}} propertySchema Property schema metadata.
 * @param {boolean} isArrayField Whether the field is an array.
 * @returns {string} HTML fragment.
 */
function renderFieldHint(propertySchema, isArrayField) {
    const hints = [];

    if (propertySchema.description) {
        hints.push(escapeHtml(propertySchema.description));
    }

    if (propertySchema.defaultValue) {
        hints.push(`Default: ${escapeHtml(propertySchema.defaultValue)}`);
    }

    const enumValues = isArrayField ? propertySchema.itemEnumValues : propertySchema.enumValues;
    if (Array.isArray(enumValues) && enumValues.length > 0) {
        hints.push(`Allowed: ${escapeHtml(enumValues.join(", "))}`);
    }

    if (propertySchema.refTable) {
        hints.push(`Ref table: ${escapeHtml(propertySchema.refTable)}`);
    }

    if (hints.length === 0) {
        return "";
    }

    return `<span class="hint">${hints.join(" · ")}</span>`;
}

/**
 * Prompt for one batch-edit field value.
 *
 * @param {{key: string, type: string, itemType?: string, title?: string, description?: string, defaultValue?: string, enumValues?: string[], itemEnumValues?: string[], refTable?: string, inputKind: "scalar" | "array", required: boolean}} field Editable field descriptor.
 * @returns {Promise<string | undefined>} User input, or undefined when cancelled.
 */
async function promptBatchFieldValue(field) {
    if (field.inputKind === "array") {
        const hintParts = [];
        if (field.itemEnumValues && field.itemEnumValues.length > 0) {
            hintParts.push(`Allowed items: ${field.itemEnumValues.join(", ")}`);
        }

        if (field.defaultValue) {
            hintParts.push(`Default: ${field.defaultValue}`);
        }

        return vscode.window.showInputBox({
            title: `Batch Edit Array: ${field.title || field.key}`,
            prompt: `Enter comma-separated items for '${field.key}' (expected array<${field.itemType}>). Leave empty to clear the array.`,
            placeHolder: hintParts.join(" | "),
            ignoreFocusOut: true
        });
    }

    if (field.enumValues && field.enumValues.length > 0) {
        const picked = await vscode.window.showQuickPick(
            field.enumValues.map((value) => ({
                label: value,
                description: value === field.defaultValue ? "default" : undefined
            })),
            {
                title: `Batch Edit Field: ${field.title || field.key}`,
                placeHolder: `Select a value for '${field.key}'.`
            });
        return picked ? picked.label : undefined;
    }

    return vscode.window.showInputBox({
        title: `Batch Edit Field: ${field.title || field.key}`,
        prompt: `Enter the new value for '${field.key}' (expected ${field.type}).`,
        placeHolder: [
            field.description || "",
            field.defaultValue ? `Default: ${field.defaultValue}` : "",
            field.refTable ? `Ref table: ${field.refTable}` : ""
        ].filter((part) => part.length > 0).join(" | ") || undefined,
        ignoreFocusOut: true
    });
}

/**
 * Enumerate all YAML files recursively.
 *
 * @param {string} rootPath Root path.
 * @returns {string[]} YAML file paths.
 */
function enumerateYamlFiles(rootPath) {
    const results = [];

    for (const entry of fs.readdirSync(rootPath, {withFileTypes: true})) {
        const fullPath = path.join(rootPath, entry.name);
        if (entry.isDirectory()) {
            results.push(...enumerateYamlFiles(fullPath));
            continue;
        }

        if (entry.isFile() && isYamlPath(entry.name)) {
            results.push(fullPath);
        }
    }

    return results;
}

/**
 * Check whether a path is a YAML file.
 *
 * @param {string} filePath File path.
 * @returns {boolean} True for YAML files.
 */
function isYamlPath(filePath) {
    return filePath.endsWith(".yaml") || filePath.endsWith(".yml");
}

/**
 * Resolve the first workspace root.
 *
 * @returns {vscode.WorkspaceFolder | undefined} Workspace root.
 */
function getWorkspaceRoot() {
    const folders = vscode.workspace.workspaceFolders;
    return folders && folders.length > 0 ? folders[0] : undefined;
}

/**
 * Resolve the configured config root.
 *
 * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
 * @returns {vscode.Uri | undefined} Config root URI.
 */
function getConfigRoot(workspaceRoot) {
    const relativePath = vscode.workspace.getConfiguration("gframeworkConfig")
        .get("configPath", "config");
    return vscode.Uri.joinPath(workspaceRoot.uri, relativePath);
}

/**
 * Resolve the configured schemas root.
 *
 * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
 * @returns {vscode.Uri | undefined} Schema root URI.
 */
function getSchemasRoot(workspaceRoot) {
    const relativePath = vscode.workspace.getConfiguration("gframeworkConfig")
        .get("schemasPath", "schemas");
    return vscode.Uri.joinPath(workspaceRoot.uri, relativePath);
}

/**
 * Resolve the matching schema URI for a config file.
 *
 * @param {vscode.Uri} configUri Config file URI.
 * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
 * @returns {vscode.Uri | undefined} Schema URI.
 */
function getSchemaUriForConfigFile(configUri, workspaceRoot) {
    const configRoot = getConfigRoot(workspaceRoot);
    const schemaRoot = getSchemasRoot(workspaceRoot);
    if (!configRoot || !schemaRoot) {
        return undefined;
    }

    const relativePath = path.relative(configRoot.fsPath, configUri.fsPath);
    const segments = relativePath.split(path.sep);
    if (segments.length === 0 || !segments[0]) {
        return undefined;
    }

    return vscode.Uri.joinPath(schemaRoot, `${segments[0]}.schema.json`);
}

/**
 * Check whether a URI is inside the configured config root.
 *
 * @param {vscode.Uri} uri File URI.
 * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
 * @returns {boolean} True when the file belongs to the config tree.
 */
function isConfigFile(uri, workspaceRoot) {
    const configRoot = getConfigRoot(workspaceRoot);
    if (!configRoot) {
        return false;
    }

    const relativePath = path.relative(configRoot.fsPath, uri.fsPath);
    return !relativePath.startsWith("..") && !path.isAbsolute(relativePath) && isYamlPath(uri.fsPath);
}

/**
 * Escape HTML text.
 *
 * @param {string} value Raw string.
 * @returns {string} Escaped string.
 */
function escapeHtml(value) {
    return String(value)
        .replace(/&/gu, "&amp;")
        .replace(/</gu, "&lt;")
        .replace(/>/gu, "&gt;")
        .replace(/"/gu, "&quot;")
        .replace(/'/gu, "&#39;");
}

/**
 * Convert raw textarea payloads into scalar-array items.
 *
 * @param {Record<string, string>} arrays Raw array editor payload.
 * @returns {Record<string, string[]>} Parsed array updates.
 */
function parseArrayFieldPayload(arrays) {
    const parsed = {};

    for (const [key, value] of Object.entries(arrays)) {
        parsed[key] = String(value)
            .split(/\r?\n/u)
            .map((item) => item.trim())
            .filter((item) => item.length > 0);
    }

    return parsed;
}

module.exports = {
    activate,
    deactivate
};
