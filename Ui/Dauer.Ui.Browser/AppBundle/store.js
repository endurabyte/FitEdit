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
                console.log(`store.js: Got file ${file.name} (${arrayBuffer.byteLength} bytes)`);
                var bytes = new Uint8Array(arrayBuffer);
                resolve({ name: file.name, bytes: bytes });
            }

            reader.onerror = _ => {
                reject(new Error("Could not read bytes"));
            }
            reader.readAsArrayBuffer(file);
        };
        console.log("store.js: Opening file...");
        input.click();
        input.remove();
    })
}

export function downloadByteArray(fileName, bytes) {
    console.log(`store.js: Saving ${fileName} (${bytes.length} bytes)`)

    var blob = new Blob([bytes], {type: "application/octet-stream"});
    var link = document.createElement('a');
    link.href = window.URL.createObjectURL(blob);
    link.download = fileName;
    link.click();
    link.remove()
};
