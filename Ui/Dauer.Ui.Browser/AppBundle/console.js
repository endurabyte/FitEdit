export async function setMessage() {
  const { getAssemblyExports } = await globalThis.getDotnetRuntime(0);
  var exports = await getAssemblyExports("Dauer.Ui.dll");

  var message = exports.Dauer.Ui.Adapters.WebConsoleAdapter.GetMessage();
  console.log(message);
}