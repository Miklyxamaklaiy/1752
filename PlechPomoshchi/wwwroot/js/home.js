(() => {
  const mapEl = document.getElementById('mapBox');
  if (!mapEl) return;

  // Leaflet is loaded from CDN in HTML. If the CDN is blocked,
  // try a second CDN (unpkg) before showing an error.
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

  (async () => {
    const ok = await ensureLeaflet();
    if (!ok) {
      mapEl.innerHTML = '<div class="p-4 text-muted">Карта не загрузилась (Leaflet не найден). Проверь интернет/блокировщики и перезагрузи страницу.</div>';
      return;
    }

    const isAuth = document.body.dataset.auth === '1';
    const csrf = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content') || '';

    // Map init
    const map = L.map('mapBox', { zoomControl: true });
    map.setView([55.751244, 37.618423], 4);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    if (window.L?.Control?.Geocoder) {
      L.Control.geocoder({
        defaultMarkGeocode: true
      }).addTo(map);
    }

    let markers = [];
    let favSet = new Set();

    const orgList = document.getElementById('orgList');
    const orgCount = document.getElementById('orgCount');

    const filterQ = document.getElementById('filterQ');
    const filterCity = document.getElementById('filterCity');
    const filterCategory = document.getElementById('filterCategory');
    const filterFav = document.getElementById('filterFav');
    const btnApply = document.getElementById('btnApply');

    const api = {
      orgs: '/api/organizations',
      cities: '/api/organizations/cities',
      categories: '/api/organizations/categories',
      favList: '/api/favorites',
      favToggle: '/api/favorites/toggle'
    };

  async function loadFilters() {
    try {
      const [cities, cats] = await Promise.all([
        fetch(api.cities).then(r => r.json()),
        fetch(api.categories).then(r => r.json())
      ]);

      cities.forEach(c => {
        const opt = document.createElement('option');
        opt.value = c;
        opt.textContent = c;
        filterCity.appendChild(opt);
      });

      cats.forEach(c => {
        const opt = document.createElement('option');
        opt.value = c;
        opt.textContent = c;
        filterCategory.appendChild(opt);
      });
    } catch (e) {
      console.warn('filters load failed', e);
    }
  }

  async function loadFavorites() {
    if (!isAuth) return;
    try {
      const ids = await fetch(api.favList).then(r => r.json());
      favSet = new Set(ids);
    } catch (e) {
      console.warn('fav load failed', e);
    }
  }

  function clearMarkers() {
    markers.forEach(m => m.remove());
    markers = [];
  }

  function renderList(items) {
    orgCount.textContent = `${items.length}`;
    orgList.innerHTML = '';

    if (items.length === 0) {
      orgList.innerHTML = '<div class="p-3 text-muted">Ничего не найдено.</div>';
      return;
    }

    items.forEach(o => {
      const row = document.createElement('div');
      row.className = 'pp-orgitem';

      const left = document.createElement('div');
      left.style.minWidth = '0';

      const name = document.createElement('div');
      name.className = 'pp-orgname text-truncate';
      name.textContent = o.name;

      const meta = document.createElement('div');
      meta.className = 'pp-orgmeta text-truncate';
      meta.textContent = [o.category, o.city, o.address].filter(Boolean).join(' · ');

      left.appendChild(name);
      left.appendChild(meta);

      const btn = document.createElement('button');
      btn.className = 'pp-orgbtn';
      btn.type = 'button';

      const setHeart = (on) => {
        btn.textContent = on ? '♥' : '♡';
        btn.classList.toggle('hearted', !!on);
      };
      setHeart(favSet.has(o.id));

      btn.addEventListener('click', async (e) => {
        e.stopPropagation();
        if (!isAuth) {
          alert('Чтобы использовать избранное — войдите в аккаунт.');
          return;
        }
        const form = new URLSearchParams();
        form.set('organizationId', String(o.id));

        const resp = await fetch(api.favToggle, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-CSRF-TOKEN': csrf
          },
          body: form.toString()
        });

        if (!resp.ok) return;

        const data = await resp.json();
        if (data.favorited) favSet.add(o.id);
        else favSet.delete(o.id);

        setHeart(data.favorited);

        if (filterFav.checked) {
          // refresh if in favorites-only mode
          await loadOrganizations();
        }
      });

      row.appendChild(left);
      row.appendChild(btn);

      row.addEventListener('click', () => {
        if (o.lat != null && o.lng != null) {
          map.setView([o.lat, o.lng], 12, { animate: true });
        }
      });

      orgList.appendChild(row);
    });
  }

  function renderMarkers(items) {
    clearMarkers();
    items.forEach(o => {
      if (o.lat == null || o.lng == null) return;

      const m = L.marker([o.lat, o.lng]).addTo(map);
      const link = o.website ? `<div><a href="${o.website}" target="_blank" rel="noopener">сайт</a></div>` : '';
      m.bindPopup(`<div style="min-width:220px">
        <div style="font-weight:700;margin-bottom:4px">${escapeHtml(o.name)}</div>
        <div style="color:#6c757d;font-size:12px">${escapeHtml([o.category,o.city,o.address].filter(Boolean).join(' · '))}</div>
        ${link}
      </div>`);
      markers.push(m);
    });
  }

  function escapeHtml(s) {
    return String(s ?? '').replace(/[&<>"']/g, (ch) => ({
      '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'
    }[ch]));
  }

  async function loadOrganizations() {
    try {
      orgList.innerHTML = '<div class="p-3 text-muted">Загрузка…</div>';

      const params = new URLSearchParams();
      if (filterQ.value.trim()) params.set('q', filterQ.value.trim());
      if (filterCity.value) params.set('city', filterCity.value);
      if (filterCategory.value) params.set('category', filterCategory.value);
      if (filterFav.checked) params.set('favoritesOnly', 'true');

      const url = api.orgs + '?' + params.toString();
      const items = await fetch(url).then(r => r.json());

      renderList(items);
      renderMarkers(items);

      // if there are markers, fit bounds
      const pts = items.filter(x => x.lat != null && x.lng != null).map(x => [x.lat, x.lng]);
      if (pts.length >= 2) {
        const b = L.latLngBounds(pts);
        map.fitBounds(b, { padding: [20, 20] });
      }
    } catch (e) {
      console.error(e);
      orgList.innerHTML = '<div class="p-3 text-danger">Ошибка загрузки организаций.</div>';
    }
  }

  btnApply?.addEventListener('click', loadOrganizations);
  filterQ?.addEventListener('keydown', (e) => { if (e.key === 'Enter') loadOrganizations(); });

  (async function init() {
    await loadFilters();
    await loadFavorites();
    await loadOrganizations();
  })();
})();

})();