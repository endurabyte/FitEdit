import { dotnet } from './dotnet.js'
import { registerAvaloniaModule } from './avalonia.js';
import * as ProgressBar from './progressbar.min.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

var progressBar = new window.ProgressBar.Line('#progress-bar', {
  duration: 0,
  step: (state, bar) => {
    bar.setText(Math.round(bar.value() * 100) + '%')
  }
});

// The first fetch dotnet makes is to get mono-config.json which
// contains a list of everything else dotnet will fetch,
// such as dotnet.wasm and assemblies including theirs
// e.g.mscorlib.dll and ours e.g.Dauer.Ui.Browser.dll.
// We can keep a dictionary of these with the file name as the key
// mapping to a boolean indicating whether the file is downloaded.
// We can use this information to infer startup progress.
var mono_config = {};

// Example response excerpt:
//{
//  "mainAssemblyName": "Dauer.Ui.Browser.dll",
//  "assemblyRootFolder": "managed",
//  "debugLevel": -1,
//  "assets": [
//    {
//      "behavior": "assembly",
//      "name": "Avalonia.dll"
//    },
//    {
//      "behavior": "assembly",
//      "name": "Avalonia.Browser.dll"
//    },
//    {
//      "behavior": "assembly",
//      "name": "System.dll"
//    },
//    {
//      "behavior": "assembly",
//      "name": "mscorlib.dll"
//    },
//    ...
//    {
//      "behavior": "assembly",
//      "name": "Dauer.Ui.Browser.dll"
//    },
//    {
//      "behavior": "assembly",
//      "name": "Dauer.Ui.Browser.pdb"
//    },
//    ...
//    {
//      "behavior": "dotnetwasm",
//      "name": "dotnet.wasm"
//    }
//  ],
//  "remoteSources": [],
//  "pthreadPoolSize": 0
//}
async function getMonoConfig(response) {

  var text = await response.blob().then(blob => blob.text());
  console.log(`main.js: Got ${response.url}:\n ${text}`);

  const jsonObject = JSON.parse(text);
  const dict = {};

  jsonObject.assets.forEach((asset) => {
    dict[asset.name.split('/').pop()] = false;
  });

  return dict;
}

// Extract e.g. "mono-config.json" from a Response object, e.g.
//const responseObject = {
//  url: "https://localhost:5001/mono-config.json",
//  ...
//};
function extractFileName(response) {
  if (!response || !(response instanceof Response) || !response.url) {
    return null;
  }

  const url = new URL(response.url);
  const pathname = url.pathname;
  const filename = pathname.split('/').pop();

  // console.log(`Got file ${filename}`)
  return filename;
}

function collectFalseKeys(dict) {
  const falseKeys = Object.entries(dict)
    .filter(([key, value]) => !value)
    .map(([key, value]) => key);

  return falseKeys;
}

var originalTitle = document.title;

function notifyAssemblyLoadProgress(percent, numLoaded, total) {
  let title = originalTitle;
  if (percent < 100){
    title = `${originalTitle} (${percent.toFixed(0)}% loaded)`
  }

  if (title !== document.title) {
    document.title = title;
  }

  progressBar.animate(percent / 100);

  console.log(`Assembly load progress: ${percent.toFixed(1)}% (${numLoaded}/${total})`);
}

// Log a message showing assembly load progress
const handleFileLoadProgress = (fileName) => {
  if (mono_config === null || !(fileName in mono_config)) {
    return;
  }

  mono_config[fileName] = true;

  const numLoaded = Object.values(mono_config).reduce((count, value) => {
    return count + (value === true ? 1 : 0);
  }, 0);
  const total = Object.values(mono_config).length;
  const percent = numLoaded / total * 100;

  notifyAssemblyLoadProgress(percent, numLoaded, total);
  return { percent, numLoaded, total};
};

// Intercept fetch so we can show loading progress
const { fetch: origFetch } = window;

window.fetch = async (...args) => {
  // console.log("main.js: fetch called with args:", args);
  const response = await origFetch(...args);

  // console.log(`main.js: Got response`, response);
  const fileName = extractFileName(response);

  if (fileName === null) {
    return;
  }

  if (fileName === "mono-config.json") {
    mono_config = await getMonoConfig(response.clone());
    console.log(`Got mono_config: ${JSON.stringify(mono_config)}`);
  }

  handleFileLoadProgress(fileName);

  return response;
};

console.log("main.js: Starting dotnet runtime");
const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

// Stop intercepting fetch now that dotnet is loaded
window.fetch = origFetch;

var missing_assemblies = collectFalseKeys(mono_config);

if (missing_assemblies.length > 0) {
  console.log(`main.js: Warning: Didn't load the following assemblies:`, missing_assemblies);
}

console.log("main.js: Registering Avalonia module");
await registerAvaloniaModule(dotnetRuntime);

const config = dotnetRuntime.getConfig();

console.log(`main.js: Running dotnet with config: ${JSON.stringify(config)}`)
await dotnetRuntime.runMainAndExit(config.mainAssemblyName, [window.location.search]);
