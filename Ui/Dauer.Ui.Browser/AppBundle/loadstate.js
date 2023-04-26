import * as ProgressBar from './progressbar.min.js'

class LoadState {

  verbose;

  constructor() {
    this.progressBar = new window.ProgressBar.Line('#progress-bar', {
      duration: 0,
      step: (state, bar) => {
        bar.setText(Math.round(bar.value() * 100) + '%')
      }
    });

    // The first fetch dotnet makes is to get mono-config.json which
    // contains a list of everything else dotnet will fetch,
    // such as dotnet.wasm and assemblies including theirs
    // e.g.mscorlib.dll and ours e.g.Dauer.Ui.Browser.dll.
    // We can keep a dictionary of these to infer startup progress.
    // key: filename e.g. "mono-config.json"
    // value: int (progress percent 0-100)
    this.mono_config = {};

    // For setting the progress in the window titlebar
    this.originalTitle = document.title;
    this.verbose = false;
  }

  log(...args) {
    if (this.verbose) {
      console.log(`loadstate.js: `, ...args);
    }
  }

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
  async getMonoConfig(response) {

    let text = await response.blob().then(blob => blob.text());

    if (this.verbose) {
      this.log(`Got ${response.url}:\n ${text}`);
    }

    const jsonObject = JSON.parse(text);
    const dict = {};

    jsonObject.assets.forEach((asset) => {
      dict[asset.name.split('/').pop()] = 0;
    });

    return dict;
  }

  // Extract e.g. "mono-config.json" from a Response object, e.g.
  //const responseObject = {
  //  url: "https://localhost:5001/mono-config.json",
  //  ...
  //};
  extractFileName(response) {
    if (!response || !(response instanceof Response) || !response.url) {
      return null;
    }

    const url = new URL(response.url);
    const pathname = url.pathname;
    const filename = pathname.split('/').pop();

    if (this.verbose) {
      this.log(`Got file ${filename}`)
    }
    return filename;
  }

  // Notify assembly load progress
  notifyAssemblyLoadProgress(percent, numLoaded, total) {
    let title = this.originalTitle;

    if (percent < 100){
      title = `${this.originalTitle} (${percent.toFixed(0)}% loaded)`
    }

    if (title !== document.title) {
      document.title = title;
    }

    this.progressBar.animate(percent / 100);

    if (this.verbose) {
      this.log(`Assembly load progress: ${percent.toFixed(1)}% (${numLoaded}/${total})`);
    }
  }

  // perc: percent 0-100 of fetch of filename
  // returns overall percent 0-100 of all fetches
  handleFileLoadProgress(filename, perc) {
    if (this.mono_config === null || !(filename in this.mono_config)) {
      return;
    }

    this.mono_config[filename] = perc;

    let values = Object.values(this.mono_config);
    const numLoaded = values.reduce((count, value) => count + (value < 100 ? 0 : 1), 0);
    const totalPercent = values.reduce((accumulator, currentValue) => accumulator + currentValue, 0);
    const total = values.length;
    const percent = totalPercent / total;

    this.notifyAssemblyLoadProgress(percent, numLoaded, total);
    return { percent, numLoaded, total};
  }

  start() {

    // Intercept fetch so we can show loading progress
    const { fetch: origFetch } = window;
    this.origFetch = origFetch;

    window.fetch = async (...args) => {
      if (this.verbose) {
        this.log("fetch called with args:", args);
      }

      const origResponse = await origFetch(...args);
      // const response = origResponse;
      
      // Intercept stream read for fetch progress
      let filename = "";
      if (args instanceof Array && args.length > 0 && typeof args[0] === 'string') {
        filename = args[0].split('/').pop();
      }
      const response = await this.readWithProgress(filename, origResponse);

      if (this.verbose) {
        this.log(`Got response`, response);
      }
      const respFilename = this.extractFileName(response);

      if (respFilename === null) {
        this.log(`No response. Expected ${filename}, got null`)
        return;
      }

      if (respFilename !== filename) {
        this.log(`Mismatched response. Expected ${filename}, got ${respFilename}`)
      }

      if (filename === "mono-config.json") {
        this.mono_config = await this.getMonoConfig(response.clone());

        if (this.verbose) {
          this.log(`Got mono_config: ${JSON.stringify(this.mono_config)}`);
        }
      }

      this.handleFileLoadProgress(filename, 100);

      return response;
    };
  }

  async readWithProgress(filename, response) {

    const length = response.headers.get('Content-Length');
    if (!length) {
      return;
    }

    const data = new Uint8Array(length);
    let at = 0;

    const reader = response.body.getReader();

    for (;;) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      data.set(value, at);
      at += value.length;

      const perc = (at / length * 100);
      
      this.log(`${filename} read progress: ${perc.toFixed(1)}% (${at}/${length} bytes)`);
      this.handleFileLoadProgress(filename, perc);
    }

    // Create a new stream which has the read data we just read
    const stream = new ReadableStream({
      start(controller) {
        controller.enqueue(data);
        controller.close();
      }
    })

    const resp = new Response(stream, {
      body: stream,
      headers: response.headers,
      status: response.status,
      statusText: response.statusText,
    });
    
    // Hack to set readonly Response.url
    Object.defineProperty(resp, "url", { value: response.url });
    return resp;
  }

  complete() {

    // Stop intercepting fetch now that dotnet is loaded
    window.fetch = this.origFetch;

    let missing_assemblies = this.collectFalseKeys(this.mono_config);

    if (missing_assemblies.length > 0) {
      this.log(`Warning: Didn't load the following assemblies:`, missing_assemblies);
    }
  }

  collectFalseKeys(dict) {
    const falseKeys = Object.entries(dict)
      .filter(([key, value]) => !value)
      .map(([key, value]) => key);

    return falseKeys;
  }
}

export let loadState = new LoadState();
