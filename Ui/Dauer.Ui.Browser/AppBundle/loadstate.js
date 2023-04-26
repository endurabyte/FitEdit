import * as ProgressBar from './progressbar.min.js'

class LoadState {

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
    // We can keep a dictionary of these with the file name as the key
    // mapping to a boolean indicating whether the file is downloaded.
    // We can use this information to infer startup progress.
    this.mono_config = {};
    this.originalTitle = document.title;

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
  extractFileName(response) {
    if (!response || !(response instanceof Response) || !response.url) {
      return null;
    }

    const url = new URL(response.url);
    const pathname = url.pathname;
    const filename = pathname.split('/').pop();

    // console.log(`Got file ${filename}`)
    return filename;
  }

  notifyAssemblyLoadProgress(percent, numLoaded, total) {
    let title = this.originalTitle;
    if (percent < 100){
      title = `${this.originalTitle} (${percent.toFixed(0)}% loaded)`
    }

    if (title !== document.title) {
      document.title = title;
    }

    this.progressBar.animate(percent / 100);

    //console.log(`Assembly load progress: ${percent.toFixed(1)}% (${numLoaded}/${total})`);
  }

  // Log a message showing assembly load progress
  handleFileLoadProgress(fileName) {
    if (this.mono_config === null || !(fileName in this.mono_config)) {
      return;
    }

    this.mono_config[fileName] = true;

    const numLoaded = Object.values(this.mono_config).reduce((count, value) => {
      return count + (value === true ? 1 : 0);
    }, 0);
    const total = Object.values(this.mono_config).length;
    const percent = numLoaded / total * 100;

    this.notifyAssemblyLoadProgress(percent, numLoaded, total);
    return { percent, numLoaded, total};
  }


  start() {

    // Intercept fetch so we can show loading progress
    const { fetch: origFetch } = window;
    this.origFetch = origFetch;

    window.fetch = async (...args) => {
      // console.log("main.js: fetch called with args:", args);
      const response = await origFetch(...args);

      // console.log(`main.js: Got response`, response);
      const fileName = this.extractFileName(response);

      if (fileName === null) {
        return;
      }

      if (fileName === "mono-config.json") {
        this.mono_config = await this.getMonoConfig(response.clone());
        console.log(`Got mono_config: ${JSON.stringify(this.mono_config)}`);
      }

      this.handleFileLoadProgress(fileName);

      return response;
    };
  }

  complete() {

    // Stop intercepting fetch now that dotnet is loaded
    window.fetch = this.origFetch;

    var missing_assemblies = this.collectFalseKeys(this.mono_config);

    if (missing_assemblies.length > 0) {
      console.log(`main.js: Warning: Didn't load the following assemblies:`, missing_assemblies);
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
