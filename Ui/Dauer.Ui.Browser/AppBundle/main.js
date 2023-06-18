import { dotnet } from './dotnet.js'
import { loadState } from './loadstate.js';

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

// Activate verbose logging if the query includes e.g. "?verbose=true" or "&verbose=true"
function getIsVerbose() {
  const query = new URLSearchParams(location.search);
  return query.has("verbose") && query.get("verbose") === "true";
}

// Activate low resolution progressbar if the query includes e.g. "?lores=true" or "&lores=true"
function getProgressIsLowResolution() {
  const query = new URLSearchParams(location.search);
  return query.has("lores") && query.get("lores") === "true";
}

const verbose = getIsVerbose();
const lores = getProgressIsLowResolution();
loadState.verbose = verbose;
loadState.lores = lores;

function log(msg) {
  if (verbose) {
    console.log("main.js:", msg);
  }
}

loadState.start();

log("Starting dotnet runtime");

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(verbose)
    .withApplicationArgumentsFromQuery()
    .create();

loadState.complete();

const config = dotnetRuntime.getConfig();

log("Running dotnet with config ", config);
await dotnetRuntime.runMainAndExit(config.mainAssemblyName, [window.location.search]);
