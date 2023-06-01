// Notify .NET of window resizes. Call this after starting .NET
export async function listenForResize() {

  const { getAssemblyExports } = await globalThis.getDotnetRuntime(0);
  var exports = await getAssemblyExports("Dauer.Ui.dll");
  var notify = exports.Dauer.Ui.Adapters.Windowing.WebWindowAdapterImpl.NotifyWindowResized;

  window.addEventListener('resize', e => {
    notify(e.target.innerWidth, e.target.innerHeight);
  });

  // Notify of initial size
  notify(window.innerWidth, window.innerHeight);
}

