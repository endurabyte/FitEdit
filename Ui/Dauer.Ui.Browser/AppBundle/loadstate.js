import * as ProgressBar from './progressbar.min.js'

class LoadState {

  verbose;
  lores;

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
    this.lores = true;
  }

  log(...args) {
    if (this.verbose) {
      console.log(`loadstate.js: `, ...args);
    }
  }

  error(...args) {
    console.error(`loadstate.js: `, ...args);
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
    this.log(`${filename} read progress: ${perc.toFixed(1)}%`);

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

  async fetchWithAxios(url, options) {
    // Set up the Axios configuration
    const axiosConfig = {
      url,
      method: options?.method || 'GET',
      data: options?.body,
      headers: options?.headers,
      responseType: 'blob', // To handle binary data (e.g. images)
      onDownloadProgress: (progressEvent) => {
        const percentCompleted = Math.floor((progressEvent.loaded * 100) / progressEvent.total);
        let filename = url.split('/').pop();
        this.handleFileLoadProgress(filename, percentCompleted)
      },
    };

    try {
      // Make the request using Axios
      const response = await axios(axiosConfig);

      // Create a new Response object to mimic the Fetch API response
      const customResponse = new Response(response.data, {
        status: response.status,
        statusText: response.statusText,
        headers: response.headers,
      });
      //
      // Hack to set readonly Response.url
      Object.defineProperty(customResponse, "url", { value: url });

      return customResponse;
    } catch (error) {
      // If there's an error, call the original fetch function
      this.error('fetchWithAxios: ', error);
      return originalFetch(url, options);
    }
  };

  start() {

    // Intercept fetch so we can show loading progress
    const { fetch: origFetch } = window;
    this.origFetch = origFetch;

    window.fetch = async (...args) => {
      this.log("fetch called with args:", args);

      let filename = "";
      if (args instanceof Array && args.length > 0 && typeof args[0] === 'string') {
        filename = args[0].split('/').pop();
      }

      const origResponse = await origFetch(...args);
      let response = null;

      if (this.lores === true) {
        response = origResponse;
      } else {
        this.log("Fetching with Axios...");
        response = await this.fetchWithAxios(...args);
      }

      await this.handleGotResponse(filename, response);
      return response;
    };
  }

  async handleGotResponse(filename, response) {

    this.log(`Got response`, response);

    const respFilename = this.extractFileName(response);

    if (respFilename === null) {
      this.log(`No response. Expected ${filename}, got null`)
      return;
    }

    if (respFilename !== filename) {
      this.log(`Mismatched response. Expected ${filename}, got ${respFilename}`)
      return;
    }

    if (filename === "mono-config.json") {
      this.mono_config = await this.getMonoConfig(response.clone());

      this.log(`Got mono_config: ${JSON.stringify(this.mono_config)}`);
    }

    this.handleFileLoadProgress(filename, 100);
  }

  complete() {

    // Stop intercepting fetch now that dotnet is loaded
    window.fetch = this.origFetch;

    let missing_files = this.collectFalseKeys(this.mono_config);

    if (missing_files.length > 0) {
      this.log(`Warning: Didn't load the following files:`, missing_files);
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
