document.addEventListener('DOMContentLoaded', async () => {
  const status = document.getElementById('status');
  const openBtn = document.getElementById('openBtn');
  const extIdBtn = document.getElementById('extIdBtn');

  const avail = await chrome.runtime.sendMessage({ type: 'CHECK_HOST' });
  if (avail) {
    status.textContent = 'Native host: OK';
    status.className = 'status ok';
    openBtn.disabled = false;
  } else {
    status.textContent = 'Native host not installed — run install.ps1';
    status.className = 'status error';
  }

  openBtn.addEventListener('click', async () => {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tab?.url) return;

    openBtn.disabled = true; openBtn.textContent = 'Opening...';
    const r = await chrome.runtime.sendMessage({ type: 'OPEN_IN_IE', url: tab.url });

    if (r.success) {
      status.textContent = 'Opened in IE viewer';
      status.className = 'status ok';
    } else {
      status.textContent = 'Error: ' + (r.error || 'unknown');
      status.className = 'status error';
    }
    openBtn.disabled = false; openBtn.textContent = 'Open in IE';
  });

  extIdBtn.addEventListener('click', async () => {
    await navigator.clipboard.writeText(chrome.runtime.id);
    extIdBtn.textContent = 'Copied!';
    setTimeout(() => { extIdBtn.textContent = 'Copy Extension ID'; }, 2000);
  });

  document.getElementById('optionsLink').onclick = e => {
    e.preventDefault(); chrome.runtime.openOptionsPage();
  };
});
