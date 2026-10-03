const vscode = require("vscode");
const metadata = require("./modsyn-language.json");
const core = require("./modsyn-completion-core");

function activate(context) {
  const diagnostics = vscode.languages.createDiagnosticCollection("modsyn");
  const selector = { language: "modsyn", scheme: "file" };

  const completionProvider = {
    provideCompletionItems(document, position) {
      const offset = document.offsetAt(position);
      return core
        .getCompletions(document.getText(), offset, metadata)
        .map((item) => {
          const completion = new vscode.CompletionItem(
            item.label,
            item.kind === "type"
              ? vscode.CompletionItemKind.EnumMember
              : item.kind === "property"
                ? vscode.CompletionItemKind.Property
                : vscode.CompletionItemKind.Value,
          );
          completion.insertText = item.insertText;
          completion.detail = item.description;
          return completion;
        });
    },
  };

  const validate = (document) => {
    const isModsynFile = document.fileName.toLowerCase().endsWith(".modsyn");
    if (document.languageId !== "modsyn" && !isModsynFile) {
      return;
    }

    const entries = core
      .getDiagnostics(document.getText(), metadata)
      .map((issue) => {
        const start = document.positionAt(issue.start);
        const end = document.positionAt(issue.end);
        const diagnostic = new vscode.Diagnostic(
          new vscode.Range(start, end),
          issue.message,
          vscode.DiagnosticSeverity.Error,
        );
        diagnostic.source = "Modsyn";
        diagnostic.code = issue.code ?? "unknown-property";
        return diagnostic;
      });
    diagnostics.set(document.uri, entries);
  };

  context.subscriptions.push(
    diagnostics,
    vscode.languages.registerCompletionItemProvider(
      selector,
      completionProvider,
      ":",
      " ",
    ),
    vscode.workspace.onDidOpenTextDocument(validate),
    vscode.workspace.onDidChangeTextDocument((event) =>
      validate(event.document),
    ),
    vscode.workspace.onDidCloseTextDocument((document) =>
      diagnostics.delete(document.uri),
    ),
  );

  for (const document of vscode.workspace.textDocuments) {
    validate(document);
  }
}

function deactivate() {}

module.exports = { activate, deactivate };
