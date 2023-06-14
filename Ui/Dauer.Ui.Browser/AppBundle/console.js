export async function setMessage() {
  const { getAssemblyExports } = await globalThis.getDotnetRuntime(0);
  var exports = await getAssemblyExports("FitEdit.dll");

  var message = exports.Dauer.Ui.Browser.Adapters.WebConsoleAdapter.GetMessage();
  console.log(message);
}