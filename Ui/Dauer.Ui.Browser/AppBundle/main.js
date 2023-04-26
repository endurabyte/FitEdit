import { dotnet } from './dotnet.js'
import { registerAvaloniaModule } from './avalonia.js';
import { loadState } from './loadstate.js';

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

loadState.start();

console.log("main.js: Starting dotnet runtime");
const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

loadState.complete();

console.log("main.js: Registering Avalonia module");
await registerAvaloniaModule(dotnetRuntime);

const config = dotnetRuntime.getConfig();

console.log(`main.js: Running dotnet with config: ${JSON.stringify(config)}`)
await dotnetRuntime.runMainAndExit(config.mainAssemblyName, [window.location.search]);
