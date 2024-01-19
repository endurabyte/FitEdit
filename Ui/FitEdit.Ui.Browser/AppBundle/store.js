function log(msg) {
  console.log("store.js:", msg);
}

export function setLocalStorage(key, value) {
  window.localStorage.setItem(key, value);
}

export function getLocalStorage(key) {
  return window.localStorage.getItem(key) || '';
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
        log(`Got file ${file.name} (${arrayBuffer.byteLength} bytes)`);
        var bytes = new Uint8Array(arrayBuffer);
        resolve({ name: file.name, bytes: bytes });
      }

      reader.onerror = _ => {
        reject(new Error("Could not read bytes"));
      }
      reader.readAsArrayBuffer(file);
    };
    log("Opening file...");
    input.click();
    input.remove();
  })
}

export function downloadByteArray(fileName, bytes) {
  log(`Saving ${fileName} (${bytes.length} bytes)`)

  var blob = new Blob([bytes], { type: "application/octet-stream" });
  var link = document.createElement('a');
  link.href = window.URL.createObjectURL(blob);
  link.download = fileName;
  link.click();
  link.remove()
};

export async function mountAndInitializeDb() {
  log("mountAndInitializeDb()")
  const dotnetRuntime = await globalThis.getDotnetRuntime(0);
  const FS = dotnetRuntime.Module.FS;
  const IDBFS = dotnetRuntime.Module.IDBFS;

  try {
    FS.mkdir('/database');
    FS.mount(IDBFS, {}, '/database');
    return syncDb(true);
  } catch (e) {
    console.error("store.js: error:", e);
  }
}

// populate == true: load from file into memory
// populate == false: load from memory into file
// See https://emscripten.org/docs/api_reference/Filesystem-API.html#filesystem-api-idbfs
export async function syncDb(populate) {
  log(`syncDatabase(${populate})`)
  const dotnetRuntime = await globalThis.getDotnetRuntime(0);
  const FS = dotnetRuntime.Module.FS

  return new Promise((resolve, reject) => {
    FS.syncfs(populate, (err) => {
      if (err) {
        console.error('store.js: error:', err);
        reject(err);
      }
      else {
        if (populate === true) {
          log('sync from file successful.');
        }
        else {
          log('sync to file successful.');
        }
        resolve();
      }
    });
  });
}
