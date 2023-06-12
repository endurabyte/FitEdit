export async function listenForMessages() {
  const { getAssemblyExports } = await globalThis.getDotnetRuntime(0);
  var exports = await getAssemblyExports("Dauer.Ui.dll");
  var notify = exports.Dauer.Ui.Adapters.Windowing.WebWindowAdapterImpl.NotifyMessageReceived;

  window.addEventListener('message', function (event) {
    // For security reasons, check the origin of the message
    if (event.origin !== 'https://localhost:5001') { // TODO replace with real domain
      return;
    }

    console.log('windowing.js: Received message from ' + event.origin + ': ' + event.data);
    notify(event.data);
  });

  console.log("windowing.js: Subscribed to window messages");
}

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

const openedWindows = {};

export async function openWindow(url, windowName) {
  console.log("windowing.js: Opening window named " + windowName + " to " + url)
  let handle = window.open(url, windowName);
  console.log("windowing.js: Got window:")
  console.log(handle);
  openedWindows[windowName] = handle;
}

export async function closeWindow(windowName) {
  console.log("windowing.js: Open windows:")
  for (const [key, value] of Object.entries(openedWindows)) {
    console.log(key, value);
  }

  if (windowName in openedWindows) {
    console.log("winowing.js: Closing window " + windowName)
    openedWindows[windowName].close();
    delete openedWindows[windowName];
  }
}
