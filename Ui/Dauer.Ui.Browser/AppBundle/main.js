import { dotnet } from './dotnet.js'
import { registerAvaloniaModule } from './avalonia.js';

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

await registerAvaloniaModule(dotnetRuntime);

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMainAndExit(config.mainAssemblyName, [window.location.search]);

export function setLocalStorage(key, value) {
    window.localStorage.setItem(key, value);
}

export function getLocalStorage(key) {
    return window.localStorage.getItem(key) || '';
}

export async function setMessage() {
    const { getAssemblyExports } = await globalThis.getDotnetRuntime(0);
    var exports = await getAssemblyExports("Dauer.Ui.dll");

    var message = exports.Dauer.Ui.Services.WebStorage.GetMessage();
    console.log(message);
}

export async function openFile() {

    return new Promise((resolve, reject) => {
        var input = document.createElement('input');
        input.type = 'file';

        input.onchange = e => {

            var file = e.target.files[0];
            var reader = new FileReader();

            reader.onload = _ => {
                var arrayBuffer = reader.result;
                console.log(`js: Got file ${file.name} (${arrayBuffer.byteLength} bytes)`);
                var bytes = new Uint8Array(arrayBuffer);
                resolve({ name: file.name, bytes: bytes });
            }

            reader.onerror = _ => {
                reject(new Error("Could not read bytes"));
            }
            reader.readAsArrayBuffer(file);
        };
        console.log("js: Opening file...");
        input.click();
        input.remove();
    })
}

export function downloadByteArray(fileName, bytes) {
    console.log(`js: Saving ${fileName} (${bytes.length} bytes)`)

    var blob = new Blob([bytes], {type: "application/octet-stream"});
    var link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = fileName;
    link.click();
    link.remove()
};
