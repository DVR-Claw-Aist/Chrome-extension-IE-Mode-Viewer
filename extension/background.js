const NATIVE_HOST = 'com.chrom_ext.ie_host';

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  switch (request.type) {
    case 'OPEN_IN_IE':
      openInIE(request.url).then(sendResponse);
      return true;

    case 'CHECK_HOST':
      checkHost().then(sendResponse);
      return true;

    case 'AUTO_OPEN':
      checkAutoOpen(request.url).then(sendResponse);
      return true;
  }
});

async function openInIE(url) {
  try {
    const resp = await chrome.runtime.sendNativeMessage(NATIVE_HOST, {
      type: 'OPEN', url
    });
    return { success: resp?.success !== false, error: resp?.error };
  } catch (err) {
    return { success: false, error: err.message };
  }
}

async function checkHost() {
  try {
    const resp = await chrome.runtime.sendNativeMessage(NATIVE_HOST, { type: 'PING' });
    return resp?.type === 'PONG';
  } catch {
    return false;
  }
}

async function checkAutoOpen(url) {
  const { whitelist = [] } = await chrome.storage.sync.get('whitelist');
  return whitelist.some(p => matchUrl(url, p));
}

function matchUrl(url, pattern) {
  const r = pattern.replace(/[.+^${}()|[\]\\]/g, '\\$&').replace(/\*/g, '.*');
  return new RegExp('^' + r + '$', 'i').test(url);
}
