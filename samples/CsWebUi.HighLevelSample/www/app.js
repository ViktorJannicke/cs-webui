const byId = (id) => document.getElementById(id);
const encoder = new TextEncoder();
const decoder = new TextDecoder();
const sleep = (milliseconds) => new Promise((resolve) => window.setTimeout(resolve, milliseconds));

const setOutput = (id, value) => {
  byId(id).textContent = value;
};

window.showToast = (message) => {
  const toast = byId("toast");
  toast.textContent = message;
  toast.classList.add("visible");
  window.setTimeout(() => toast.classList.remove("visible"), 3_000);
};

window.receiveRaw = (bytes) => {
  const payload = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);
  setOutput("raw-result", `Received ${payload.length} byte(s): “${decoder.decode(payload)}”`);
};

async function getBridge(timeoutMilliseconds = 5_000) {
  const deadline = Date.now() + timeoutMilliseconds;

  while (!window.webui?.isConnected()) {
    if (Date.now() >= deadline) {
      throw new Error("WebUI did not connect to the .NET backend in time.");
    }

    await sleep(50);
  }

  return window.webui;
}

async function callBackend(name, ...args) {
  return (await getBridge()).call(name, ...args);
}

async function callAndReport(outputId, action) {
  try {
    setOutput(outputId, "Working…");
    setOutput(outputId, await action());
  } catch (error) {
    setOutput(outputId, `Error: ${error.message ?? error}`);
  }
}

async function refreshStatus() {
  try {
    byId("connection-status").textContent = await callBackend("getStatus");
  } catch (error) {
    byId("connection-status").textContent = `Backend unavailable: ${error.message ?? error}`;
  }
}

byId("refresh-status").addEventListener("click", refreshStatus);

byId("calculate-button").addEventListener("click", () => callAndReport("calculation-result", async () => {
  const result = await callBackend("calculate", Number(byId("left").value), Number(byId("right").value));
  return `Result from .NET: ${result}`;
}));

byId("greet").addEventListener("click", () => callAndReport("greeting-result", () =>
  callBackend("delayedGreeting", byId("name").value)));

byId("send-raw").addEventListener("click", () => callAndReport("raw-result", () =>
  callBackend("sendRaw", encoder.encode(byId("raw-message").value))));

byId("kiosk").addEventListener("change", async (event) => {
  try {
    showToast(await callBackend("setKiosk", event.target.checked));
  } catch (error) {
    showToast(`Could not change kiosk mode: ${error.message ?? error}`);
  }
});

byId("backend-notification").addEventListener("click", () => {
  void callBackend("notifyFromBackend").catch((error) => showToast(`Backend error: ${error.message ?? error}`));
});

byId("open-webui").addEventListener("click", () => {
  void callBackend("openWebUiSite").catch((error) => showToast(`Backend error: ${error.message ?? error}`));
});

byId("exit").addEventListener("click", () => {
  void callBackend("exitApplication").catch(() => window.close());
});

window.addEventListener("load", () => void refreshStatus());
