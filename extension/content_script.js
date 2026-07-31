// Auto-open from whitelist
(async () => {
  const { whitelist = [] } = await chrome.storage.sync.get('whitelist');
  const url = location.href;
  const match = whitelist.some(p => {
    const r = p.replace(/[.+^${}()|[\]\\]/g, '\\$&').replace(/\*/g, '.*');
    return new RegExp('^' + r + '$', 'i').test(url);
  });
  if (!match) return;

  chrome.runtime.sendMessage(
    { type: 'OPEN_IN_IE', url },
    resp => { if (!resp?.success) console.warn('IE Mode:', resp?.error); }
  );
})();
