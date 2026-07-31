document.addEventListener('DOMContentLoaded', async () => {
  document.getElementById('extId').textContent = chrome.runtime.id;

  const { whitelist = [] } = await chrome.storage.sync.get('whitelist');
  document.getElementById('whitelist').value = whitelist.join('\n');

  document.getElementById('saveBtn').onclick = async () => {
    const patterns = document.getElementById('whitelist').value
      .split('\n').map(s => s.trim()).filter(Boolean);
    await chrome.storage.sync.set({ whitelist: patterns });

    const st = document.getElementById('saveStatus');
    st.textContent = 'Saved!'; st.style.color = 'green';
    setTimeout(() => { st.textContent = ''; }, 2000);
  };
});
