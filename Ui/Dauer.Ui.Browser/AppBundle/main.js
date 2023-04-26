import { dotnet } from './dotnet.js'
import { registerAvaloniaModule } from './avalonia.js';
import { loadState } from './loadstate.js';

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// Activate verbose logging if the query includes e.g. "?verbose=true" or "&verbose=true"
function getIsVerbose() {
  const query = new URLSearchParams(location.search);
  return query.has("verbose") && query.get("verbose") === "true";
}

const verbose = getIsVerbose();
loadState.verbose = verbose;

function log(msg) {
  if (verbose) {
    console.log(msg);
  }
}

loadState.start();

log("main.js: Starting dotnet runtime");
const dotnetRuntime = await dotnet
    .withDiagnosticTracing(verbose)
    .withApplicationArgumentsFromQuery()
    .create();

loadState.complete();

log("main.js: Registering Avalonia module");
await registerAvaloniaModule(dotnetRuntime);

const config = dotnetRuntime.getConfig();

log(`main.js: Running dotnet with config: ${JSON.stringify(config)}`)
await dotnetRuntime.runMainAndExit(config.mainAssemblyName, [window.location.search]);
