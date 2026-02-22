(() => {
  const mapBox = document.getElementById('mapBox');
  if (!mapBox) return;

  const qEl = document.getElementById('filterQ');
  const cityEl = document.getElementById('filterCity');
  const catEl = document.getElementById('filterCategory');
  const favEl = document.getElementById('filterFav');
  const btnEl = document.getElementById('btnApply');
  const listEl = document.getElementById('orgList');
  const countEl = document.getElementById('orgCount');

  // Favorites in localStorage (for GitHub Pages demo)
  const favKey = 'ppFav';
  const loadFav = () => new Set(JSON.parse(localStorage.getItem(favKey) || '[]'));
  const saveFav = (set) => localStorage.setItem(favKey, JSON.stringify([...set]));
  let fav = loadFav();

  // Leaflet is loaded from CDN in HTML. If that CDN is blocked,
  // we try a second CDN (unpkg) before showing an error.
  function ensureCss(href) {
    if ([...document.querySelectorAll('link[rel="stylesheet"]')].some(l => (l.href || '').includes(href))) return;
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
  }

  function loadScript(src) {
    return new Promise((resolve, reject) => {
      const s = document.createElement('script');
      s.src = src;
      s.async = true;
      s.onload = resolve;
      s.onerror = reject;
      document.head.appendChild(s);
    });
  }

  async function ensureLeaflet() {
    if (window.L) return true;
    try {
      ensureCss('https://unpkg.com/leaflet@1.9.4/dist/leaflet.css');
      await loadScript('https://unpkg.com/leaflet@1.9.4/dist/leaflet.js');
    } catch (_) {
      // ignore
    }
    try {
      ensureCss('https://unpkg.com/leaflet-control-geocoder@2.4.0/dist/Control.Geocoder.css');
      await loadScript('https://unpkg.com/leaflet-control-geocoder@2.4.0/dist/Control.Geocoder.js');
    } catch (_) {
      // geocoder is optional
    }
    return !!window.L;
  }

  let map;
  async function initMap() {
    const ok = await ensureLeaflet();
    if (!ok) {
      mapBox.innerHTML = '<div class="p-4 text-muted">Карта не загрузилась (Leaflet не найден). Проверь интернет/блокировщики и перезагрузи страницу.</div>';
      return null;
    }
    map = L.map('mapBox', { zoomControl: true }).setView([55.751244, 37.618423], 4);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '&copy; OpenStreetMap contributors' }).addTo(map);
    if (window.L?.Control?.Geocoder) {
      L.Control.geocoder({ defaultMarkGeocode: true }).addTo(map);
    }
    return map;
  }

  let all = [];
  let layers = [];

  function clearLayers() {
    layers.forEach(l => map.removeLayer(l));
    layers = [];
  }

  function match(org, q, city, cat, onlyFav) {
    if (onlyFav && !fav.has(org.id)) return false;
    if (city && org.city !== city) return false;
    if (cat && org.category !== cat) return false;
    if (q) {
      const s = (org.name + ' ' + org.address + ' ' + org.city + ' ' + org.category).toLowerCase();
      if (!s.includes(q)) return false;
    }
    return true;
  }

  function renderList(items) {
    listEl.innerHTML = '';
    if (!items.length) {
      listEl.innerHTML = '<div class="p-3 text-muted">Ничего не найдено.</div>';
      return;
    }

    for (const o of items) {
      const card = document.createElement('div');
      card.className = 'pp-orgitem';
            card.innerHTML = `
        <div style="min-width:0">
          <div class="pp-orgname text-truncate">${escapeHtml(o.name)}</div>
          <div class="pp-orgmeta text-truncate">${escapeHtml([o.category, o.city, o.address].filter(Boolean).join(' · '))}</div>
        </div>
        <button class="pp-orgbtn ${fav.has(o.id) ? 'hearted' : ''}" type="button" title="В избранное" aria-label="В избранное">${fav.has(o.id) ? '♥' : '♡'}</button>
      `;

      card.querySelector('.pp-orgbtn').addEventListener('click', (e) => {
        e.stopPropagation();
        if (fav.has(o.id)) fav.delete(o.id); else fav.add(o.id);
        saveFav(fav);
        apply();
      });

      card.addEventListener('click', () => {
        map.setView([o.lat, o.lng], 12, { animate: true });
      });

      listEl.appendChild(card);
    }
  }

  function renderMap(items) {
    clearLayers();

    // circle markers are faster for big sets
    for (const o of items) {
      const m = L.circleMarker([o.lat, o.lng], { radius: 5, weight: 1, fillOpacity: 0.75 });
      m.bindPopup(`<div style="font-weight:600">${escapeHtml(o.name)}</div><div>${escapeHtml(o.address)}</div>`);
      m.addTo(map);
      layers.push(m);
    }
  }

  function apply() {
    const q = (qEl?.value || '').trim().toLowerCase();
    const city = cityEl?.value || '';
    const cat = catEl?.value || '';
    const onlyFav = !!favEl?.checked;

    const items = all.filter(o => match(o, q, city, cat, onlyFav));
    countEl.textContent = `${items.length}`;
    renderList(items.slice(0, 200)); // list shows first 200 to keep it snappy
    renderMap(items.slice(0, 1000));
  }

  function escapeHtml(s) {
    return String(s || '')
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }

  async function init() {
    const m = await initMap();
    if (!m) return;
    const resp = await fetch('./assets/data/orgs.json');
    all = await resp.json();

    // Fill dropdowns
    const cities = [...new Set(all.map(o => o.city))].sort((a,b) => a.localeCompare(b, 'ru'));
    const cats = [...new Set(all.map(o => o.category))].sort((a,b) => a.localeCompare(b, 'ru'));

    cities.forEach(c => {
      const opt = document.createElement('option');
      opt.value = c;
      opt.textContent = c;
      cityEl.appendChild(opt);
    });

    cats.forEach(c => {
      const opt = document.createElement('option');
      opt.value = c;
      opt.textContent = c;
      catEl.appendChild(opt);
    });

    btnEl?.addEventListener('click', apply);
    [qEl, cityEl, catEl, favEl].forEach(el => el?.addEventListener('change', apply));
    qEl?.addEventListener('input', () => {
      // tiny debounce
      clearTimeout(window.__ppT);
      window.__ppT = setTimeout(apply, 150);
    });

    apply();
  }

  init().catch(err => {
    console.error(err);
    listEl.innerHTML = '<div class="p-3 text-danger">Ошибка загрузки данных.</div>';
  });
})();