const fs = require("fs");
const path = require("path");
let vscode;
try {
    vscode = require("vscode");
} catch {
    // Tests load pure helpers from this module without the VS Code host.
    vscode = {
        env: {
            language: "en"
        },
        EventEmitter: class EventEmitter {
            constructor() {
                this.event = () => undefined;
            }

            fire() {
            }
        },
        TreeItem: class TreeItem {
            constructor(label, collapsibleState) {
                this.label = label;
                this.collapsibleState = collapsibleState;
            }
        },
        TreeItemCollapsibleState: {
            None: 0,
            Collapsed: 1,
            Expanded: 2
        },
        Uri: {
            joinPath() {
                return undefined;
            }
        },
        window: {},
        workspace: {},
        languages: {}
    };
}
const {
    applyFormUpdates,
    createSampleConfigYaml,
    extractYamlComments,
    getEditableSchemaFields,
    parseBatchArrayValue,
    parseSchemaContent,
    parseTopLevelYaml,
    unquoteScalar,
    validateParsedConfig
} = require("./configValidation");
const {
    isTemplatePath,
    joinArrayIndexPath,
    joinArrayTemplatePath,
    joinPropertyPath
} = require("./configPath");
const {buildContainsHintLines} = require("./containsSummary");
const {createLocalizer} = require("./localization");

const localizer = createLocalizer(vscode.env.language);

/**
 * Activate the GFramework config extension.
 * The current tool focuses on workspace file navigation, lightweight
 * validation, and a schema-aware form preview for common editing workflows.
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
                    localizer.t("tree.noConfigDirectory.label"),
                    "info",
                    vscode.TreeItemCollapsibleState.None,
                    undefined,
                    localizer.t("tree.noConfigDirectory.description"))
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
                ? localizer.t("tree.fileDescription.schema")
                : localizer.t("tree.fileDescription.schemaMissing");
            const item = new ConfigTreeItem(
                entry.name,
                "file",
                vscode.TreeItemCollapsibleState.None,
                fileUri,
                description);

            item.contextValue = "gframeworkConfigFile";
            item.command = {
                command: "gframeworkConfig.openRaw",
                title: localizer.t("command.openRaw.title"),
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
        void vscode.window.showWarningMessage(localizer.t("message.schemaNotFound"));
        return;
    }

    const document = await vscode.workspace.openTextDocument(schemaUri);
    await vscode.window.showTextDocument(document, {preview: false});
}

/**
 * Open the schema file for a referenced config table.
 *
 * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
 * @param {string | undefined} refTable Referenced table name.
 * @returns {Promise<void>} Async task.
 */
async function openReferenceSchemaFile(workspaceRoot, refTable) {
    if (!workspaceRoot || !refTable) {
        return;
    }

    const schemaUri = vscode.Uri.joinPath(getSchemasRoot(workspaceRoot), `${refTable}.schema.json`);
    if (!fs.existsSync(schemaUri.fsPath)) {
        void vscode.window.showWarningMessage(localizer.t("message.referenceSchemaMissing", {refTable}));
        return;
    }

    const document = await vscode.workspace.openTextDocument(schemaUri);
    await vscode.window.showTextDocument(document, {preview: false});
}

/**
 * Reveal the referenced config domain directory in the Explorer.
 *
 * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
 * @param {string | undefined} refTable Referenced table name.
 * @returns {Promise<void>} Async task.
 */
async function revealReferenceDomain(workspaceRoot, refTable) {
    if (!workspaceRoot || !refTable) {
        return;
    }

    const domainUri = vscode.Uri.joinPath(getConfigRoot(workspaceRoot), refTable);
    if (!fs.existsSync(domainUri.fsPath)) {
        void vscode.window.showWarningMessage(localizer.t("message.referenceDomainMissing", {refTable}));
        return;
    }

    await vscode.commands.executeCommand("revealInExplorer", domainUri);
}

/**
 * Open the referenced config file when the current field already has a key
 * value. If the direct file cannot be found, fall back to revealing the whole
 * referenced domain.
 *
 * @param {vscode.WorkspaceFolder} workspaceRoot Workspace root.
 * @param {string | undefined} refTable Referenced table name.
 * @param {string | undefined} refValue Referenced config id or file stem.
 * @returns {Promise<void>} Async task.
 */
async function openReferenceValueFile(workspaceRoot, refTable, refValue) {
    if (!workspaceRoot || !refTable || !refValue) {
        return;
    }

    const configRoot = getConfigRoot(workspaceRoot);
    const domainUri = vscode.Uri.joinPath(configRoot, refTable);
    const yamlCandidate = vscode.Uri.joinPath(domainUri, `${refValue}.yaml`);
    const ymlCandidate = vscode.Uri.joinPath(domainUri, `${refValue}.yml`);
    const targetUri = fs.existsSync(yamlCandidate.fsPath)
        ? yamlCandidate
        : fs.existsSync(ymlCandidate.fsPath)
            ? ymlCandidate
            : undefined;

    if (!targetUri) {
        await revealReferenceDomain(workspaceRoot, refTable);
        void vscode.window.showWarningMessage(localizer.t("message.referenceValueMissing", {
            refTable,
            refValue
        }));
        return;
    }

    const document = await vscode.workspace.openTextDocument(targetUri);
    await vscode.window.showTextDocument(document, {preview: false});
}

/**
 * Open a lightweight form preview for schema-bound config fields.
 * The preview walks nested object structures recursively and now supports
 * object-array editing for the repository's supported schema subset.
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

    let latestYamlText = await fs.promises.readFile(configUri.fsPath, "utf8");
    const parsedYaml = parseTopLevelYaml(latestYamlText);
    const commentLookup = extractYamlComments(latestYamlText);
    const schemaInfo = await loadSchemaInfoForConfig(configUri, workspaceRoot);
    const canInitializeFromSchema = schemaInfo.exists && latestYamlText.trim().length === 0;

    const panel = vscode.window.createWebviewPanel(
        "gframeworkConfigFormPreview",
        localizer.t("webview.panelTitle", {fileName: path.basename(configUri.fsPath)}),
        vscode.ViewColumn.Beside,
        {enableScripts: true});

    panel.webview.html = renderFormHtml(
        path.basename(configUri.fsPath),
        schemaInfo,
        parsedYaml,
        {
            commentLookup,
            canInitializeFromSchema
        });

    panel.webview.onDidReceiveMessage(async (message) => {
        if (message.type === "save") {
            latestYamlText = await fs.promises.readFile(configUri.fsPath, "utf8");
            const updatedYaml = applyFormUpdates(latestYamlText, {
                scalars: message.scalars || {},
                arrays: parseArrayFieldPayload(message.arrays || {}),
                objectArrays: message.objectArrays || {},
                comments: message.comments || {}
            });
            await fs.promises.writeFile(configUri.fsPath, updatedYaml, "utf8");
            const document = await vscode.workspace.openTextDocument(configUri);
            await document.save();
            await validateConfigFile(configUri, diagnostics);
            void vscode.window.showInformationMessage(localizer.t("message.formSaved"));
            return;
        }

        if (message.type === "openRaw") {
            await openRawFile({resourceUri: configUri});
            return;
        }

        if (message.type === "initializeFromSchema") {
            if (!schemaInfo.exists) {
                void vscode.window.showWarningMessage(localizer.t("message.schemaNotFound"));
                return;
            }

            const confirmLabel = localizer.t("button.initializeFromSchemaConfirm");
            const cancelLabel = localizer.t("button.cancel");
            const userChoice = await vscode.window.showWarningMessage(
                localizer.t("message.initializeFromSchemaConfirm"),
                {modal: true},
                confirmLabel,
                cancelLabel);

            if (userChoice !== confirmLabel) {
                return;
            }

            const sampleYaml = createSampleConfigYaml(schemaInfo);
            await fs.promises.writeFile(configUri.fsPath, sampleYaml, "utf8");
            const document = await vscode.workspace.openTextDocument(configUri);
            await document.save();
            latestYamlText = sampleYaml;
            await validateConfigFile(configUri, diagnostics);
            panel.webview.html = renderFormHtml(
                path.basename(configUri.fsPath),
                schemaInfo,
                parseTopLevelYaml(latestYamlText),
                {
                    commentLookup: extractYamlComments(latestYamlText),
                    canInitializeFromSchema: false
                });
            void vscode.window.showInformationMessage(localizer.t("message.formInitialized"));
            return;
        }

        if (message.type === "openReferenceSchema") {
            await openReferenceSchemaFile(workspaceRoot, message.refTable);
            return;
        }

        if (message.type === "openReferenceDomain") {
            await revealReferenceDomain(workspaceRoot, message.refTable);
            return;
        }

        if (message.type === "openReferenceValue") {
            await openReferenceValueFile(workspaceRoot, message.refTable, message.refValue);
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
            localizer.t("diagnostic.schemaMissing", {schemaPath: schemaInfo.schemaPath}),
            vscode.DiagnosticSeverity.Warning));
        diagnostics.set(configUri, fileDiagnostics);
        return;
    }

    for (const diagnostic of validateParsedConfig(schemaInfo, parsedYaml, localizer)) {
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
        void vscode.window.showWarningMessage(localizer.t("message.noYamlFilesInDomain"));
        return;
    }

    const selectedFiles = await vscode.window.showQuickPick(fileItems, {
        canPickMany: true,
        title: localizer.t("quickPick.batchEdit.title", {domain: path.basename(domainUri.fsPath)}),
        placeHolder: localizer.t("quickPick.batchEdit.placeholder")
    });
    if (!selectedFiles || selectedFiles.length === 0) {
        return;
    }

    const schemaInfo = await loadSchemaInfoForConfig(selectedFiles[0].fileUri, workspaceRoot);
    if (!schemaInfo.exists) {
        void vscode.window.showWarningMessage(localizer.t("message.batchEditNeedsSchema"));
        return;
    }

    const editableFields = getEditableSchemaFields(schemaInfo);
    if (editableFields.length === 0) {
        void vscode.window.showWarningMessage(localizer.t("message.batchEditNoEditableFields"));
        return;
    }

    const selectedFields = await vscode.window.showQuickPick(
        editableFields.map((field) => ({
            label: field.title || field.key,
            description: field.inputKind === "array"
                ? localizer.t("detail.arrayType", {itemType: field.itemType})
                : field.type,
            detail: [
                field.required ? localizer.t("detail.required") : "",
                field.description || "",
                field.refTable ? localizer.t("detail.refTable", {refTable: field.refTable}) : ""
            ].filter((part) => part.length > 0).join(" · ") || undefined,
            field
        })),
        {
            canPickMany: true,
            title: localizer.t("quickPick.batchEditFields.title", {domain: path.basename(domainUri.fsPath)}),
            placeHolder: localizer.t("quickPick.batchEditFields.placeholder")
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
        void vscode.window.showInformationMessage(localizer.t("message.batchEditNoChanges"));
        return;
    }

    const applied = await vscode.workspace.applyEdit(edit);
    if (!applied) {
        throw new Error(localizer.isChinese
            ? "VS Code 拒绝了这次批量编辑工作区更新。"
            : "VS Code rejected the batch edit workspace update.");
    }

    for (const document of touchedDocuments) {
        await document.save();
        await validateConfigFile(document.uri, diagnostics);
    }

    provider.refresh();
    void vscode.window.showInformationMessage(localizer.t("message.batchEditUpdated", {
        count: changedFileCount,
        domain: path.basename(domainUri.fsPath)
    }));
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
            type: parsed.type,
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
 * @param {{exists: boolean, schemaPath: string, required: string[], properties: Record<string, unknown>, type?: string}} schemaInfo Schema info.
 * @param {unknown} parsedYaml Parsed YAML data.
 * @param {{commentLookup?: Record<string, string>, canInitializeFromSchema?: boolean} | undefined} options Render options.
 * @returns {string} HTML string.
 */
function renderFormHtml(fileName, schemaInfo, parsedYaml, options) {
    const renderOptions = options || {};
    const formModel = buildFormModel(schemaInfo, parsedYaml, renderOptions.commentLookup || {});
    const saveButtonLabel = escapeHtml(localizer.t("webview.button.save"));
    const openRawButtonLabel = escapeHtml(localizer.t("webview.button.openRaw"));
    const objectArrayItemLabel = localizer.t("webview.objectArray.item");
    const initializeAction = renderOptions.canInitializeFromSchema
        ? `<button id="initializeFromSchema" class="secondary-button">${escapeHtml(localizer.t("webview.button.initialize"))}</button>`
        : "";
    const renderedFields = formModel.fields
        .map((field) => renderFormField(field))
        .join("\n");

    const unsupportedFields = formModel.unsupported
        .map((field) => `
            <div class="unsupported">
                <strong>${escapeHtml(field.path)}</strong>: ${escapeHtml(field.message)}
            </div>
        `)
        .join("\n");

    const schemaStatus = schemaInfo.exists
        ? escapeHtml(localizer.t("webview.meta.schema", {schemaPath: schemaInfo.schemaPath}))
        : escapeHtml(localizer.t("webview.meta.schemaMissing", {schemaPath: schemaInfo.schemaPath}));

    const editableContent = renderedFields;
    const unsupportedSection = unsupportedFields.length > 0
        ? `<div class="unsupported-list">${unsupportedFields}</div>`
        : "";
    const emptyState = editableContent.length > 0
        ? `${editableContent}${unsupportedSection}`
        : `<p>${escapeHtml(localizer.t("webview.emptyState"))}</p>`;

    return `<!DOCTYPE html>
<html lang="${escapeHtml(localizer.languageTag)}">
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
        .secondary-button {
            background: transparent;
            color: var(--vscode-button-foreground);
        }
        .meta {
            margin-bottom: 16px;
            color: var(--vscode-descriptionForeground);
        }
        .hint-banner {
            padding: 10px 12px;
            border: 1px solid var(--vscode-panel-border, transparent);
            border-radius: 6px;
            background: color-mix(in srgb, var(--vscode-editor-background) 92%, var(--vscode-panel-border, transparent));
        }
        .field {
            display: block;
            margin-bottom: 12px;
        }
        .section {
            margin: 18px 0 8px;
            padding-top: 12px;
            border-top: 1px solid var(--vscode-panel-border, transparent);
        }
        .section-title {
            font-weight: 700;
            margin-bottom: 4px;
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
        .field-actions {
            display: flex;
            gap: 8px;
            margin-bottom: 6px;
            flex-wrap: wrap;
        }
        .link-button {
            padding: 4px 8px;
            font-size: 12px;
        }
        .yaml-comment {
            display: block;
            margin-bottom: 6px;
            padding: 8px 10px;
            border-left: 3px solid var(--vscode-textBlockQuote-border);
            background: color-mix(in srgb, var(--vscode-editor-background) 90%, var(--vscode-textBlockQuote-border));
            color: var(--vscode-descriptionForeground);
            white-space: pre-wrap;
            font-family: var(--vscode-editor-font-family, var(--vscode-font-family));
            font-size: 12px;
        }
        .comment-editor {
            margin-top: 8px;
        }
        .object-array {
            margin-bottom: 18px;
            padding: 12px;
            border: 1px solid var(--vscode-panel-border, transparent);
            border-radius: 6px;
        }
        .object-array-items {
            display: flex;
            flex-direction: column;
            gap: 12px;
            margin-bottom: 12px;
        }
        .object-array-item {
            padding: 12px;
            border: 1px solid var(--vscode-input-border, transparent);
            border-radius: 6px;
            background: color-mix(in srgb, var(--vscode-editor-background) 88%, var(--vscode-panel-border, transparent));
        }
        .object-array-item-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 12px;
            margin-bottom: 8px;
        }
        .object-array-item-title {
            font-weight: 700;
        }
        .depth-1 {
            margin-left: 12px;
        }
        .depth-2 {
            margin-left: 24px;
        }
        .depth-3 {
            margin-left: 36px;
        }
        .depth-4 {
            margin-left: 48px;
        }
        .depth-5 {
            margin-left: 60px;
        }
    </style>
</head>
<body>
    <div class="toolbar">
        <button id="save">${saveButtonLabel}</button>
        <button id="openRaw">${openRawButtonLabel}</button>
        ${initializeAction}
    </div>
    <div class="meta hint-banner">${escapeHtml(localizer.t("webview.help.summary"))}</div>
    <div class="meta">
        <div>${escapeHtml(localizer.t("webview.meta.file", {fileName}))}</div>
        <div>${schemaStatus}</div>
    </div>
    <div id="fields">${emptyState}</div>
    <script>
        const vscode = acquireVsCodeApi();
        const objectArrayItemLabel = ${JSON.stringify(objectArrayItemLabel)};
        function parseArrayEditorValue(value) {
            return String(value)
                .split(/\\r?\\n/u)
                .map((item) => item.trim())
                .filter((item) => item.length > 0);
        }
        function setNestedObjectValue(target, path, value) {
            const segments = path.split(".").filter((segment) => segment.length > 0);
            if (segments.length === 0) {
                return;
            }

            let current = target;
            for (let index = 0; index < segments.length; index += 1) {
                const segment = segments[index];
                if (index === segments.length - 1) {
                    current[segment] = value;
                    return;
                }

                if (!current[segment] || typeof current[segment] !== "object" || Array.isArray(current[segment])) {
                    current[segment] = {};
                }

                current = current[segment];
            }
        }
        function getDirectObjectArrayItems(editor) {
            const itemsHost = editor.querySelector(":scope > [data-object-array-items]");
            return itemsHost
                ? Array.from(itemsHost.querySelectorAll(":scope > [data-object-array-item]"))
                : [];
        }
        function renumberObjectArrayItems(editor) {
            const items = getDirectObjectArrayItems(editor);
            items.forEach((item, index) => {
                const title = item.querySelector(".object-array-item-title");
                if (title) {
                    title.textContent = objectArrayItemLabel + " " + (index + 1);
                }
            });
        }
        function shouldIncludeNestedControl(control, ownerItem) {
            return control.closest("[data-object-array-item]") === ownerItem;
        }
        function collectObjectArrayEditorItems(editor) {
            const items = [];
            for (const item of getDirectObjectArrayItems(editor)) {
                items.push(collectObjectArrayItemValue(item));
            }

            return items;
        }
        function collectObjectArrayItemValue(item) {
            const itemValue = {};

            for (const control of item.querySelectorAll("[data-item-local-path]")) {
                if (!shouldIncludeNestedControl(control, item)) {
                    continue;
                }

                setNestedObjectValue(itemValue, control.dataset.itemLocalPath, control.value);
            }

            for (const textarea of item.querySelectorAll("textarea[data-item-array-path]")) {
                if (!shouldIncludeNestedControl(textarea, item)) {
                    continue;
                }

                setNestedObjectValue(
                    itemValue,
                    textarea.dataset.itemArrayPath,
                    parseArrayEditorValue(textarea.value));
            }

            for (const nestedEditor of item.querySelectorAll("[data-item-object-array-path]")) {
                if (!shouldIncludeNestedControl(nestedEditor, item)) {
                    continue;
                }

                setNestedObjectValue(
                    itemValue,
                    nestedEditor.dataset.itemObjectArrayPath,
                    collectObjectArrayEditorItems(nestedEditor));
            }

            return itemValue;
        }
        document.addEventListener("click", (event) => {
            const schemaButton = event.target.closest("[data-open-ref-schema]");
            if (schemaButton) {
                vscode.postMessage({
                    type: "openReferenceSchema",
                    refTable: schemaButton.dataset.openRefSchema
                });
                return;
            }

            const domainButton = event.target.closest("[data-open-ref-domain]");
            if (domainButton) {
                vscode.postMessage({
                    type: "openReferenceDomain",
                    refTable: domainButton.dataset.openRefDomain
                });
                return;
            }

            const valueButton = event.target.closest("[data-open-ref-value]");
            if (valueButton) {
                vscode.postMessage({
                    type: "openReferenceValue",
                    refTable: valueButton.dataset.refTable,
                    refValue: valueButton.dataset.refValue
                });
                return;
            }

            const addButton = event.target.closest("[data-add-object-array-item]");
            if (addButton) {
                const editor = addButton.closest("[data-object-array-editor]");
                const itemsHost = editor.querySelector("[data-object-array-items]");
                const template = editor.querySelector("template[data-object-array-template]");
                if (itemsHost && template) {
                    itemsHost.appendChild(template.content.cloneNode(true));
                    renumberObjectArrayItems(editor);
                }
                return;
            }

            const removeButton = event.target.closest("[data-remove-object-array-item]");
            if (removeButton) {
                const item = removeButton.closest("[data-object-array-item]");
                const editor = removeButton.closest("[data-object-array-editor]");
                if (item) {
                    item.remove();
                }
                if (editor) {
                    renumberObjectArrayItems(editor);
                }
            }
        });
        document.getElementById("save").addEventListener("click", () => {
            const scalars = {};
            const arrays = {};
            const objectArrays = {};
            const comments = {};
            for (const control of document.querySelectorAll("[data-path]")) {
                scalars[control.dataset.path] = control.value;
            }
            for (const textarea of document.querySelectorAll("textarea[data-array-path]")) {
                arrays[textarea.dataset.arrayPath] = textarea.value;
            }
            for (const textarea of document.querySelectorAll("textarea[data-comment-path]")) {
                comments[textarea.dataset.commentPath] = textarea.value;
            }
            for (const editor of document.querySelectorAll("[data-object-array-editor][data-object-array-path]")) {
                const path = editor.dataset.objectArrayPath;
                objectArrays[path] = collectObjectArrayEditorItems(editor);
            }
            vscode.postMessage({ type: "save", scalars, arrays, objectArrays, comments });
        });
        document.getElementById("openRaw").addEventListener("click", () => {
            vscode.postMessage({ type: "openRaw" });
        });
        const initializeButton = document.getElementById("initializeFromSchema");
        if (initializeButton) {
            initializeButton.addEventListener("click", () => {
                vscode.postMessage({ type: "initializeFromSchema" });
            });
        }
    </script>
</body>
</html>`;
}

/**
 * Render one form field.
 *
 * @param {Record<string, unknown>} field Form field descriptor.
 * @returns {string} HTML fragment.
 */
function renderFormField(field) {
    if (field.kind === "section") {
        return `
            <div class="section depth-${field.depth}">
                <div class="section-title">${escapeHtml(field.label)} ${field.required ? `<span class="badge">${escapeHtml(localizer.t("webview.badge.required"))}</span>` : ""}</div>
                <div class="meta-key">${escapeHtml(field.displayPath || field.path)}</div>
                ${renderYamlCommentBlock(field)}
                ${field.description ? `<span class="hint">${escapeHtml(field.description)}</span>` : ""}
                ${field.schema ? renderFieldHint(field.schema, false, false) : ""}
                ${renderCommentEditor(field)}
            </div>
        `;
    }

    if (field.kind === "objectArray") {
        const renderedItems = field.items
            .map((item) => renderObjectArrayItem(item))
            .join("\n");
        const renderedTemplate = renderObjectArrayItem({
            title: localizer.t("webview.objectArray.item"),
            fields: field.templateFields
        });
        const pathAttribute = field.itemMode
            ? `data-item-object-array-path="${escapeHtml(field.path)}"`
            : `data-object-array-path="${escapeHtml(field.path)}"`;
        return `
            <div class="object-array depth-${field.depth}" data-object-array-editor ${pathAttribute}>
                <div class="label">${escapeHtml(field.label)} ${field.required ? `<span class="badge">${escapeHtml(localizer.t("webview.badge.required"))}</span>` : ""}</div>
                <div class="meta-key">${escapeHtml(field.displayPath || field.path)}</div>
                ${renderYamlCommentBlock(field)}
                <span class="hint">${escapeHtml(localizer.t("webview.objectArray.hint"))}</span>
                ${renderFieldHint(field.schema, true)}
                ${renderReferenceActions(field)}
                ${renderCommentEditor(field)}
                <div class="object-array-items" data-object-array-items>${renderedItems}</div>
                <template data-object-array-template>${renderedTemplate}</template>
                <button type="button" class="secondary-button" data-add-object-array-item>${escapeHtml(localizer.t("webview.objectArray.add"))}</button>
            </div>
        `;
    }

    if (field.kind === "array") {
        const itemType = field.itemType
            ? `array<${field.itemType}>`
            : "array";
        const dataAttribute = field.itemMode
            ? `data-item-array-path="${escapeHtml(field.path)}"`
            : `data-array-path="${escapeHtml(field.path)}"`;
        return `
            <label class="field depth-${field.depth}">
                <span class="label">${escapeHtml(field.label)} ${field.required ? `<span class="badge">${escapeHtml(localizer.t("webview.badge.required"))}</span>` : ""}</span>
                <span class="meta-key">${escapeHtml(field.displayPath || field.path)}</span>
                ${renderYamlCommentBlock(field)}
                <span class="hint">${escapeHtml(localizer.t("webview.array.hint", {itemType}))}</span>
                ${renderFieldHint(field.schema, true)}
                ${renderReferenceActions(field)}
                <textarea ${dataAttribute} rows="5">${escapeHtml(field.value.join("\n"))}</textarea>
                ${renderCommentEditor(field)}
            </label>
        `;
    }

    const enumValues = Array.isArray(field.schema.enumValues) ? field.schema.enumValues : [];
    const dataAttribute = field.itemMode
        ? `data-item-local-path="${escapeHtml(field.path)}"`
        : `data-path="${escapeHtml(field.path)}"`;
    const inputControl = enumValues.length > 0
        ? `
            <select ${dataAttribute}>
                ${enumValues.map((value) => {
            const escapedOption = escapeHtml(value);
            const selected = value === field.value ? " selected" : "";
            return `<option value="${escapedOption}"${selected}>${escapedOption}</option>`;
        }).join("\n")}
            </select>
        `
        : `<input ${dataAttribute} value="${escapeHtml(field.value)}" />`;

    return `
        <label class="field depth-${field.depth}">
            <span class="label">${escapeHtml(field.label)} ${field.required ? `<span class="badge">${escapeHtml(localizer.t("webview.badge.required"))}</span>` : ""}</span>
            <span class="meta-key">${escapeHtml(field.displayPath || field.path)}</span>
            ${renderYamlCommentBlock(field)}
            ${renderFieldHint(field.schema, false)}
            ${renderReferenceActions(field)}
            ${inputControl}
            ${renderCommentEditor(field)}
        </label>
    `;
}

/**
 * Render one existing YAML comment block for a field.
 *
 * @param {{comment?: string}} field Form field descriptor.
 * @returns {string} HTML fragment.
 */
function renderYamlCommentBlock(field) {
    if (!field.comment) {
        return "";
    }

    return `<span class="yaml-comment">${escapeHtml(field.comment)}</span>`;
}

/**
 * Render one comment editor so users can add or update YAML comments directly
 * from the structured form without dropping down to raw YAML first.
 *
 * @param {{displayPath?: string, path: string, comment?: string}} field Form field descriptor.
 * @returns {string} HTML fragment.
 */
function renderCommentEditor(field) {
    const commentPath = field.displayPath || field.path;
    if (isTemplatePath(commentPath)) {
        return "";
    }

    return `
        <div class="comment-editor">
            <span class="hint">${escapeHtml(localizer.t("webview.comment.label"))}</span>
            <textarea data-comment-path="${escapeHtml(commentPath)}" rows="2">${escapeHtml(field.comment || "")}</textarea>
        </div>
    `;
}

/**
 * Render lightweight reference-navigation actions for fields that point to
 * another config table.
 *
 * @param {{schema?: {refTable?: string}, value?: string, kind?: string, displayPath?: string}} field Form field descriptor.
 * @returns {string} HTML fragment.
 */
function renderReferenceActions(field) {
    if (!field.schema || !field.schema.refTable) {
        return "";
    }

    const refTable = escapeHtml(field.schema.refTable);
    const actions = [
        `<button type="button" class="secondary-button link-button" data-open-ref-schema="${refTable}">${escapeHtml(localizer.t("webview.ref.openSchema"))}</button>`,
        `<button type="button" class="secondary-button link-button" data-open-ref-domain="${refTable}">${escapeHtml(localizer.t("webview.ref.openDomain"))}</button>`
    ];

    if (field.kind === "scalar" && field.value) {
        actions.push(
            `<button type="button" class="secondary-button link-button" data-open-ref-value="true" data-ref-table="${refTable}" data-ref-value="${escapeHtml(field.value)}">${escapeHtml(localizer.t("webview.ref.openValue"))}</button>`);
    }

    return `<div class="field-actions">${actions.join("")}</div>`;
}

/**
 * Render one object-array item editor block.
 *
 * @param {{title: string, fields: Array<Record<string, unknown>>}} item Item model.
 * @returns {string} HTML fragment.
 */
function renderObjectArrayItem(item) {
    return `
        <div class="object-array-item" data-object-array-item>
            <div class="object-array-item-header">
                <span class="object-array-item-title">${escapeHtml(item.title)}</span>
                <button type="button" class="secondary-button" data-remove-object-array-item>${escapeHtml(localizer.t("webview.objectArray.remove"))}</button>
            </div>
            ${item.fields.map((field) => renderFormField(field)).join("\n")}
        </div>
    `;
}

/**
 * Build a recursive form model from schema and parsed YAML.
 *
 * @param {{exists: boolean, schemaPath: string, required: string[], properties: Record<string, unknown>, type?: string}} schemaInfo Schema info.
 * @param {unknown} parsedYaml Parsed YAML data.
 * @param {Record<string, string>} commentLookup YAML comment lookup.
 * @returns {{fields: Array<Record<string, unknown>>, unsupported: Array<{path: string, message: string}>}} Form model.
 */
function buildFormModel(schemaInfo, parsedYaml, commentLookup) {
    if (!schemaInfo || schemaInfo.type !== "object") {
        return {fields: [], unsupported: []};
    }

    const fields = [];
    const unsupported = [];
    collectFormFields(schemaInfo, parsedYaml, "", 0, fields, unsupported, commentLookup || {});
    return {fields, unsupported};
}

/**
 * Recursively collect top-level form-editable fields.
 *
 * @param {{type: string, required?: string[], properties?: Record<string, unknown>, title?: string, description?: string}} schemaNode Schema node.
 * @param {unknown} yamlNode YAML node.
 * @param {string} currentPath Current logical path.
 * @param {number} depth Current depth.
 * @param {Array<Record<string, unknown>>} fields Field sink.
 * @param {Array<{path: string, message: string}>} unsupported Unsupported sink.
 * @param {Record<string, string>} commentLookup YAML comment lookup.
 */
function collectFormFields(schemaNode, yamlNode, currentPath, depth, fields, unsupported, commentLookup) {
    if (!schemaNode || schemaNode.type !== "object") {
        return;
    }

    const yamlMap = getYamlObjectMap(yamlNode);
    const requiredSet = new Set(Array.isArray(schemaNode.required) ? schemaNode.required : []);

    for (const [key, propertySchema] of Object.entries(schemaNode.properties || {})) {
        const propertyPath = joinPropertyPath(currentPath, key);
        const label = propertySchema.title || key;
        const propertyValue = yamlMap.get(key);

        if (propertySchema.type === "object") {
            fields.push({
                kind: "section",
                path: propertyPath,
                label,
                description: propertySchema.description,
                schema: propertySchema,
                comment: commentLookup[propertyPath] || "",
                required: requiredSet.has(key),
                depth
            });
            collectFormFields(propertySchema, propertyValue, propertyPath, depth + 1, fields, unsupported, commentLookup);
            continue;
        }

        if (propertySchema.type === "array" &&
            propertySchema.items &&
            ["string", "integer", "number", "boolean"].includes(propertySchema.items.type)) {
            fields.push({
                kind: "array",
                path: propertyPath,
                displayPath: propertyPath,
                label,
                required: requiredSet.has(key),
                depth,
                itemType: propertySchema.items.type,
                value: getScalarArrayValue(propertyValue),
                schema: propertySchema,
                comment: commentLookup[propertyPath] || ""
            });
            continue;
        }

        if (propertySchema.type === "array" &&
            propertySchema.items &&
            propertySchema.items.type === "object") {
            const itemFieldsTemplate = [];
            collectObjectArrayItemFields(
                propertySchema.items,
                undefined,
                "",
                joinArrayTemplatePath(propertyPath),
                depth + 1,
                itemFieldsTemplate,
                unsupported,
                commentLookup);
            fields.push({
                kind: "objectArray",
                path: propertyPath,
                displayPath: propertyPath,
                label,
                required: requiredSet.has(key),
                depth,
                schema: propertySchema,
                comment: commentLookup[propertyPath] || "",
                items: buildObjectArrayItemModels(
                    propertySchema.items,
                    propertyValue,
                    propertyPath,
                    depth + 1,
                    unsupported,
                    commentLookup),
                templateFields: itemFieldsTemplate
            });
            continue;
        }

        if (["string", "integer", "number", "boolean"].includes(propertySchema.type)) {
            fields.push({
                kind: "scalar",
                path: propertyPath,
                displayPath: propertyPath,
                label,
                required: requiredSet.has(key),
                depth,
                value: getScalarFieldValue(propertyValue, propertySchema.constValue ?? propertySchema.defaultValue),
                schema: propertySchema,
                comment: commentLookup[propertyPath] || ""
            });
            continue;
        }

        unsupported.push({
            path: propertyPath,
            message: propertySchema.type === "array"
                ? localizer.t("webview.unsupported.array")
                : localizer.t("webview.unsupported.type", {type: propertySchema.type})
        });
    }
}

/**
 * Build object-array item models from the current YAML array value.
 *
 * @param {{type: string, required?: string[], properties?: Record<string, unknown>}} itemSchema Array item schema.
 * @param {unknown} yamlNode YAML node.
 * @param {string} propertyPath Top-level object-array path.
 * @param {number} depth Current depth.
 * @param {Array<{path: string, message: string}>} unsupported Unsupported sink.
 * @param {Record<string, string>} commentLookup YAML comment lookup.
 * @returns {Array<{title: string, fields: Array<Record<string, unknown>>}>} Item models.
 */
function buildObjectArrayItemModels(itemSchema, yamlNode, propertyPath, depth, unsupported, commentLookup) {
    if (!yamlNode || yamlNode.kind !== "array") {
        return [];
    }

    const items = [];
    for (let index = 0; index < yamlNode.items.length; index += 1) {
        const itemNode = yamlNode.items[index];
        const itemPath = joinArrayIndexPath(propertyPath, index);
        if (!itemNode || itemNode.kind !== "object") {
            unsupported.push({
                path: itemPath,
                message: localizer.t("webview.unsupported.objectArrayMixed")
            });
            continue;
        }

        const fields = [];
        collectObjectArrayItemFields(
            itemSchema,
            itemNode,
            "",
            itemPath,
            depth,
            fields,
            unsupported,
            commentLookup);
        items.push({
            title: localizer.t("webview.objectArray.itemNumber", {index: index + 1}),
            fields
        });
    }

    return items;
}

/**
 * Recursively collect editable fields inside one object-array item.
 * Nested objects remain editable, while nested object arrays still fall back
 * to raw YAML until a deeper editor model is added.
 *
 * @param {{type: string, required?: string[], properties?: Record<string, unknown>, title?: string, description?: string}} schemaNode Schema node.
 * @param {unknown} yamlNode YAML node.
 * @param {string} localPath Path inside the current array item.
 * @param {string} displayPath Full logical path for UI display.
 * @param {number} depth Current depth.
 * @param {Array<Record<string, unknown>>} fields Field sink.
 * @param {Array<{path: string, message: string}>} unsupported Unsupported sink.
 * @param {Record<string, string>} commentLookup YAML comment lookup.
 */
function collectObjectArrayItemFields(schemaNode, yamlNode, localPath, displayPath, depth, fields, unsupported, commentLookup) {
    if (!schemaNode || schemaNode.type !== "object") {
        return;
    }

    const yamlMap = getYamlObjectMap(yamlNode);
    const requiredSet = new Set(Array.isArray(schemaNode.required) ? schemaNode.required : []);

    for (const [key, propertySchema] of Object.entries(schemaNode.properties || {})) {
        const itemLocalPath = joinPropertyPath(localPath, key);
        const itemDisplayPath = joinPropertyPath(displayPath, key);
        const label = propertySchema.title || key;
        const propertyValue = yamlMap.get(key);

        if (propertySchema.type === "object") {
            fields.push({
                kind: "section",
                path: itemLocalPath,
                displayPath: itemDisplayPath,
                label,
                description: propertySchema.description,
                schema: propertySchema,
                comment: commentLookup[itemDisplayPath] || "",
                required: requiredSet.has(key),
                depth
            });
            collectObjectArrayItemFields(
                propertySchema,
                propertyValue,
                itemLocalPath,
                itemDisplayPath,
                depth + 1,
                fields,
                unsupported,
                commentLookup);
            continue;
        }

        if (propertySchema.type === "array" &&
            propertySchema.items &&
            ["string", "integer", "number", "boolean"].includes(propertySchema.items.type)) {
            fields.push({
                kind: "array",
                path: itemLocalPath,
                displayPath: itemDisplayPath,
                label,
                required: requiredSet.has(key),
                depth,
                itemType: propertySchema.items.type,
                value: getScalarArrayValue(propertyValue),
                schema: propertySchema,
                itemMode: true,
                comment: commentLookup[itemDisplayPath] || ""
            });
            continue;
        }

        if (propertySchema.type === "array" &&
            propertySchema.items &&
            propertySchema.items.type === "object") {
            const templateFields = [];
            collectObjectArrayItemFields(
                propertySchema.items,
                undefined,
                "",
                joinArrayTemplatePath(itemDisplayPath),
                depth + 1,
                templateFields,
                unsupported,
                commentLookup);
            fields.push({
                kind: "objectArray",
                path: itemLocalPath,
                displayPath: itemDisplayPath,
                label,
                required: requiredSet.has(key),
                depth,
                schema: propertySchema,
                itemMode: true,
                comment: commentLookup[itemDisplayPath] || "",
                items: buildObjectArrayItemModels(
                    propertySchema.items,
                    propertyValue,
                    itemDisplayPath,
                    depth + 1,
                    unsupported,
                    commentLookup),
                templateFields
            });
            continue;
        }

        if (["string", "integer", "number", "boolean"].includes(propertySchema.type)) {
            fields.push({
                kind: "scalar",
                path: itemLocalPath,
                displayPath: itemDisplayPath,
                label,
                required: requiredSet.has(key),
                depth,
                value: getScalarFieldValue(propertyValue, propertySchema.constValue ?? propertySchema.defaultValue),
                schema: propertySchema,
                itemMode: true,
                comment: commentLookup[itemDisplayPath] || ""
            });
            continue;
        }

        unsupported.push({
            path: itemDisplayPath,
            message: propertySchema.type === "array"
                ? localizer.t("webview.unsupported.nestedObjectArray")
                : localizer.t("webview.unsupported.type", {type: propertySchema.type})
        });
    }
}

/**
 * Get the mapping lookup for one parsed YAML object node.
 *
 * @param {unknown} yamlNode YAML node.
 * @returns {Map<string, unknown>} Mapping lookup.
 */
function getYamlObjectMap(yamlNode) {
    return yamlNode && yamlNode.kind === "object" && yamlNode.map instanceof Map
        ? yamlNode.map
        : new Map();
}

/**
 * Extract a scalar field value from a parsed YAML node.
 *
 * @param {unknown} yamlNode YAML node.
 * @param {string | undefined} fallbackValue Schema-provided fallback value.
 * @returns {string} Scalar display value.
 */
function getScalarFieldValue(yamlNode, fallbackValue) {
    if (yamlNode && yamlNode.kind === "scalar") {
        return unquoteScalar(yamlNode.value || "");
    }

    return fallbackValue ?? "";
}

/**
 * Extract a scalar-array value list from a parsed YAML node.
 *
 * @param {unknown} yamlNode YAML node.
 * @returns {string[]} Scalar array value list.
 */
function getScalarArrayValue(yamlNode) {
    if (!yamlNode || yamlNode.kind !== "array") {
        return [];
    }

    return yamlNode.items
        .filter((item) => item && item.kind === "scalar")
        .map((item) => unquoteScalar(item.value || ""));
}

/**
 * @typedef {object} InlineObjectSchemaHint
 * @property {string=} type Inline schema type.
 * @property {string[]=} required Required properties.
 * @property {string[]=} enumValues Allowed enum values.
 * @property {string=} constValue Raw const value.
 * @property {string=} constDisplayValue Human-readable const value.
 * @property {string=} pattern String pattern metadata.
 * @property {string=} refTable Referenced table name.
 *
 * @typedef {object} ContainsSchemaHint
 * @property {string=} type Inline schema type.
 * @property {string[]=} enumValues Allowed enum values.
 * @property {string=} constValue Raw const value.
 * @property {string=} constDisplayValue Human-readable const value.
 * @property {string=} pattern String pattern metadata.
 * @property {string=} format String format metadata.
 * @property {string=} refTable Referenced table name.
 *
 * @typedef {object} ScalarArrayItemHint
 * @property {string[]=} enumValues Allowed enum values.
 * @property {string=} constValue Raw const value.
 * @property {string=} constDisplayValue Human-readable const value.
 * @property {number=} minimum Inclusive minimum.
 * @property {number=} exclusiveMinimum Exclusive minimum.
 * @property {number=} maximum Inclusive maximum.
 * @property {number=} exclusiveMaximum Exclusive maximum.
 * @property {number=} multipleOf Numeric multiple constraint.
 * @property {number=} minLength Minimum length.
 * @property {number=} maxLength Maximum length.
 * @property {string=} pattern String pattern metadata.
 * @property {string=} format String format metadata.
 *
 * @typedef {object} PropertySchemaHint
 * @property {string=} type Schema type.
 * @property {string=} description Human-facing description.
 * @property {string=} defaultValue Default value text.
 * @property {string=} constValue Raw const value.
 * @property {string=} constDisplayValue Human-readable const value.
 * @property {number=} minimum Inclusive minimum.
 * @property {number=} exclusiveMinimum Exclusive minimum.
 * @property {number=} maximum Inclusive maximum.
 * @property {number=} exclusiveMaximum Exclusive maximum.
 * @property {number=} multipleOf Numeric multiple constraint.
 * @property {number=} minLength Minimum length.
 * @property {number=} maxLength Maximum length.
 * @property {string=} pattern String pattern metadata.
 * @property {string=} format String format metadata.
 * @property {number=} minItems Minimum array item count.
 * @property {number=} maxItems Maximum array item count.
 * @property {number=} minContains Minimum contains matches.
 * @property {number=} maxContains Maximum contains matches.
 * @property {number=} minProperties Minimum property count.
 * @property {number=} maxProperties Maximum property count.
 * @property {string[]=} required Required properties.
 * @property {Record<string, string[]>=} dependentRequired dependentRequired metadata.
 * @property {Record<string, InlineObjectSchemaHint>=} dependentSchemas dependentSchemas metadata.
 * @property {Array<InlineObjectSchemaHint>=} allOf allOf metadata.
 * @property {InlineObjectSchemaHint=} ifSchema if metadata.
 * @property {InlineObjectSchemaHint=} thenSchema then metadata.
 * @property {InlineObjectSchemaHint=} elseSchema else metadata.
 * @property {boolean=} uniqueItems uniqueItems metadata.
 * @property {string[]=} enumValues Allowed enum values.
 * @property {ContainsSchemaHint=} contains contains metadata.
 * @property {ScalarArrayItemHint=} items Array item metadata.
 * @property {string=} refTable Referenced table name.
 *
 * Render one compact inline-schema summary for form hints.
 *
 * @param {InlineObjectSchemaHint} schema Parsed inline schema metadata.
 * @param {boolean} includeRequiredProperties Whether object `required` members should be surfaced.
 * @returns {string} Localized summary.
 */
function describeInlineSchemaForHint(schema, includeRequiredProperties = false) {
    const parts = [];
    if (schema.type) {
        parts.push(schema.type);
    }

    if (includeRequiredProperties &&
        Array.isArray(schema.required) &&
        schema.required.length > 0) {
        parts.push(localizer.t("webview.hint.required", {
            properties: schema.required.join(", ")
        }));
    }

    if (schema.constValue !== undefined) {
        parts.push(localizer.t("webview.hint.const", {
            value: schema.constDisplayValue ?? schema.constValue
        }));
    } else if (Array.isArray(schema.enumValues) && schema.enumValues.length > 0) {
        parts.push(localizer.t("webview.hint.allowed", {
            values: schema.enumValues.join(", ")
        }));
    } else if (schema.pattern) {
        parts.push(localizer.t("webview.hint.pattern", {
            value: schema.pattern
        }));
    }

    if (schema.refTable) {
        parts.push(localizer.t("webview.hint.refTable", {
            refTable: schema.refTable
        }));
    }

    return parts.join(", ") || localizer.t("webview.objectArray.item");
}

/**
 * Render human-facing metadata hints for one schema field.
 *
 * @param {PropertySchemaHint} propertySchema Property schema metadata.
 * @param {boolean} isArrayField Whether the field is an array.
 * @param {boolean} includeDescription Whether description text should be included in the hint output.
 * @returns {string} HTML fragment.
 */
function renderFieldHint(propertySchema, isArrayField, includeDescription = true) {
    const hints = [];

    if (includeDescription && propertySchema.description) {
        hints.push(escapeHtml(propertySchema.description));
    }

    if (propertySchema.defaultValue) {
        hints.push(escapeHtml(localizer.t("webview.hint.default", {value: propertySchema.defaultValue})));
    }

    if (propertySchema.constValue !== undefined) {
        hints.push(escapeHtml(localizer.t("webview.hint.const", {
            value: propertySchema.constDisplayValue ?? propertySchema.constValue
        })));
    }

    const enumValues = isArrayField
        ? propertySchema.items && Array.isArray(propertySchema.items.enumValues)
            ? propertySchema.items.enumValues
            : []
        : propertySchema.enumValues;
    if (Array.isArray(enumValues) && enumValues.length > 0) {
        hints.push(escapeHtml(localizer.t("webview.hint.allowed", {values: enumValues.join(", ")})));
    }

    if (!isArrayField && typeof propertySchema.minimum === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.minimum", {value: propertySchema.minimum})));
    }

    if (!isArrayField && typeof propertySchema.exclusiveMinimum === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.exclusiveMinimum", {value: propertySchema.exclusiveMinimum})));
    }

    if (!isArrayField && typeof propertySchema.maximum === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.maximum", {value: propertySchema.maximum})));
    }

    if (!isArrayField && typeof propertySchema.exclusiveMaximum === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.exclusiveMaximum", {value: propertySchema.exclusiveMaximum})));
    }

    if (!isArrayField && typeof propertySchema.multipleOf === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.multipleOf", {value: propertySchema.multipleOf})));
    }

    if (!isArrayField && typeof propertySchema.minLength === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.minLength", {value: propertySchema.minLength})));
    }

    if (!isArrayField && typeof propertySchema.maxLength === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.maxLength", {value: propertySchema.maxLength})));
    }

    if (!isArrayField && propertySchema.pattern) {
        hints.push(escapeHtml(localizer.t("webview.hint.pattern", {value: propertySchema.pattern})));
    }

    if (!isArrayField && propertySchema.format) {
        hints.push(escapeHtml(localizer.t("webview.hint.format", {value: propertySchema.format})));
    }

    if (propertySchema.type === "object" && typeof propertySchema.minProperties === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.minProperties", {value: propertySchema.minProperties})));
    }

    if (propertySchema.type === "object" && typeof propertySchema.maxProperties === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.maxProperties", {value: propertySchema.maxProperties})));
    }

    if (propertySchema.type === "object" &&
        propertySchema.dependentRequired &&
        typeof propertySchema.dependentRequired === "object") {
        for (const [trigger, dependencies] of Object.entries(propertySchema.dependentRequired)) {
            if (!Array.isArray(dependencies) || dependencies.length === 0) {
                continue;
            }

            hints.push(escapeHtml(localizer.t("webview.hint.dependentRequired", {
                trigger,
                dependencies: dependencies.join(", ")
            })));
        }
    }

    if (propertySchema.type === "object" &&
        propertySchema.dependentSchemas &&
        typeof propertySchema.dependentSchemas === "object") {
        for (const [trigger, dependentSchema] of Object.entries(propertySchema.dependentSchemas)) {
            hints.push(escapeHtml(localizer.t("webview.hint.dependentSchemas", {
                trigger,
                schema: describeInlineSchemaForHint(dependentSchema, true)
            })));
        }
    }

    if (propertySchema.type === "object" &&
        Array.isArray(propertySchema.allOf)) {
        for (const allOfSchema of propertySchema.allOf) {
            hints.push(escapeHtml(localizer.t("webview.hint.allOf", {
                schema: describeInlineSchemaForHint(allOfSchema, true)
            })));
        }
    }

    if (propertySchema.type === "object" &&
        propertySchema.ifSchema &&
        propertySchema.thenSchema) {
        hints.push(escapeHtml(localizer.t("webview.hint.ifThen", {
            condition: describeInlineSchemaForHint(propertySchema.ifSchema, true),
            schema: describeInlineSchemaForHint(propertySchema.thenSchema, true)
        })));
    }

    if (propertySchema.type === "object" &&
        propertySchema.ifSchema &&
        propertySchema.elseSchema) {
        hints.push(escapeHtml(localizer.t("webview.hint.ifElse", {
            condition: describeInlineSchemaForHint(propertySchema.ifSchema, true),
            schema: describeInlineSchemaForHint(propertySchema.elseSchema, true)
        })));
    }

    if (isArrayField && typeof propertySchema.minItems === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.minItems", {value: propertySchema.minItems})));
    }

    if (isArrayField && typeof propertySchema.maxItems === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.maxItems", {value: propertySchema.maxItems})));
    }

    if (isArrayField && propertySchema.contains) {
        const containsHints = buildContainsHintLines(propertySchema, localizer);
        for (const containsHint of containsHints) {
            hints.push(escapeHtml(containsHint));
        }
    }

    if (isArrayField && typeof propertySchema.maxContains === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.maxContains", {value: propertySchema.maxContains})));
    }

    if (isArrayField && propertySchema.uniqueItems === true) {
        hints.push(escapeHtml(localizer.t("webview.hint.uniqueItems")));
    }

    if (isArrayField && propertySchema.items && typeof propertySchema.items.minimum === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.itemMinimum", {value: propertySchema.items.minimum})));
    }

    if (isArrayField && propertySchema.items && propertySchema.items.constValue !== undefined) {
        hints.push(escapeHtml(localizer.t("webview.hint.itemConst", {
            value: propertySchema.items.constDisplayValue ?? propertySchema.items.constValue
        })));
    }

    if (isArrayField && propertySchema.items && typeof propertySchema.items.exclusiveMinimum === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.itemExclusiveMinimum", {value: propertySchema.items.exclusiveMinimum})));
    }

    if (isArrayField && propertySchema.items && typeof propertySchema.items.maximum === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.itemMaximum", {value: propertySchema.items.maximum})));
    }

    if (isArrayField && propertySchema.items && typeof propertySchema.items.exclusiveMaximum === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.itemExclusiveMaximum", {value: propertySchema.items.exclusiveMaximum})));
    }

    if (isArrayField && propertySchema.items && typeof propertySchema.items.multipleOf === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.itemMultipleOf", {value: propertySchema.items.multipleOf})));
    }

    if (isArrayField && propertySchema.items && typeof propertySchema.items.minLength === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.itemMinLength", {value: propertySchema.items.minLength})));
    }

    if (isArrayField && propertySchema.items && typeof propertySchema.items.maxLength === "number") {
        hints.push(escapeHtml(localizer.t("webview.hint.itemMaxLength", {value: propertySchema.items.maxLength})));
    }

    if (isArrayField && propertySchema.items && propertySchema.items.pattern) {
        hints.push(escapeHtml(localizer.t("webview.hint.itemPattern", {value: propertySchema.items.pattern})));
    }

    if (isArrayField && propertySchema.items && propertySchema.items.format) {
        hints.push(escapeHtml(localizer.t("webview.hint.itemFormat", {value: propertySchema.items.format})));
    }

    if (propertySchema.refTable) {
        hints.push(escapeHtml(localizer.t("webview.hint.refTable", {refTable: propertySchema.refTable})));
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
            hintParts.push(localizer.t("input.batchArray.placeholder.allowedItems", {
                values: field.itemEnumValues.join(", ")
            }));
        }

        if (field.defaultValue) {
            hintParts.push(localizer.t("input.batchArray.placeholder.default", {value: field.defaultValue}));
        }

        return vscode.window.showInputBox({
            title: localizer.t("input.batchArray.title", {field: field.title || field.key}),
            prompt: localizer.t("input.batchArray.prompt", {
                fieldKey: field.key,
                itemType: field.itemType
            }),
            placeHolder: hintParts.join(" | "),
            ignoreFocusOut: true
        });
    }

    if (field.enumValues && field.enumValues.length > 0) {
        const picked = await vscode.window.showQuickPick(
            field.enumValues.map((value) => ({
                label: value,
                description: value === field.defaultValue
                    ? localizer.t("detail.default")
                    : undefined
            })),
            {
                title: localizer.t("quickPick.batchField.title", {field: field.title || field.key}),
                placeHolder: localizer.t("quickPick.batchField.placeholder", {fieldKey: field.key})
            });
        return picked ? picked.label : undefined;
    }

    return vscode.window.showInputBox({
        title: localizer.t("input.batchField.title", {field: field.title || field.key}),
        prompt: localizer.t("input.batchField.prompt", {
            fieldKey: field.key,
            type: field.type
        }),
        placeHolder: [
            field.description || "",
            field.defaultValue ? localizer.t("input.batchArray.placeholder.default", {value: field.defaultValue}) : "",
            field.refTable ? localizer.t("input.batchField.placeholder.refTable", {refTable: field.refTable}) : ""
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
    deactivate,
    __test: {
        buildFormModel
    }
};
