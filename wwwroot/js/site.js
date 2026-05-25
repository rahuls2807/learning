let workerMandiInstallPrompt = null;

window.addEventListener("beforeinstallprompt", (event) => {
  event.preventDefault();
  workerMandiInstallPrompt = event;

  const installButton = document.getElementById("pwaInstallButton");
  if (installButton) {
    installButton.hidden = false;
  }
});

window.addEventListener("appinstalled", () => {
  workerMandiInstallPrompt = null;

  const installButton = document.getElementById("pwaInstallButton");
  if (installButton) {
    installButton.hidden = true;
  }
});

if ("serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker.register("/service-worker.js").catch(() => {
      // The app remains fully usable online if service worker registration is unavailable.
    });
  });
}

document.addEventListener("DOMContentLoaded", () => {
  const installButton = document.getElementById("pwaInstallButton");
  if (installButton) {
    installButton.addEventListener("click", async () => {
      if (!workerMandiInstallPrompt) {
        return;
      }

      workerMandiInstallPrompt.prompt();
      await workerMandiInstallPrompt.userChoice;
      workerMandiInstallPrompt = null;
      installButton.hidden = true;
    });
  }

  const assistant = document.getElementById("appAssistant");
  const toggle = document.getElementById("assistantToggle");
  const close = document.getElementById("assistantClose");
  const form = document.getElementById("assistantForm");
  const input = document.getElementById("assistantInput");
  const messages = document.getElementById("assistantMessages");

  if (!assistant || !toggle || !close || !form || !input || !messages) {
    return;
  }

  const addMessage = (text, type) => {
    const message = document.createElement("div");
    message.className = `assistant-message assistant-message-${type}`;
    message.textContent = text;
    messages.appendChild(message);
    messages.scrollTop = messages.scrollHeight;
  };

  toggle.addEventListener("click", () => {
    assistant.classList.add("assistant-open");
    input.focus();
  });

  close.addEventListener("click", () => {
    assistant.classList.remove("assistant-open");
  });

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const text = input.value.trim();
    if (!text) {
      return;
    }

    const token = assistant.querySelector("input[name='__RequestVerificationToken']")?.value;
    addMessage(text, "user");
    input.value = "";
    input.disabled = true;

    addMessage("Thinking...", "bot");
    const loadingNode = messages.lastElementChild;

    try {
      const formData = new FormData();
      formData.append("Message", text);
      formData.append("Page", document.title || window.location.pathname);

      const response = await fetch("/Chatbot/Ask", {
        method: "POST",
        headers: token ? { RequestVerificationToken: token } : {},
        body: formData
      });

      const data = await response.json();
      loadingNode.textContent = data.reply || "I could not answer that. Try asking about booking, payment, or account access.";
    } catch {
      loadingNode.textContent = "I could not reach the assistant service. You can still use the menu to open bookings, payments, jobs, or admin pages.";
    } finally {
      input.disabled = false;
      input.focus();
    }
  });
});
