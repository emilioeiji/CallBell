const state = {
  audioContext: null,
  applyingSectorSelection: false
};

window.renderMonitorBoard = payload => {
  const message = typeof payload === "string" ? JSON.parse(payload) : payload;
  if (message?.type !== "board") {
    return;
  }

  renderBoard(message.data || {});
  if (message.playAlert) {
    playBell();
  }
};

window.chrome?.webview?.addEventListener("message", event => {
  window.renderMonitorBoard(event.data);
});

function renderBoard(data) {
  document.getElementById("boardTitle").textContent = data.title || "CallBell Monitor";
  document.getElementById("openCount").textContent = String(data.totalOpenRequests || 0);
  document.getElementById("generatedAt").textContent = data.generatedAt || "--:--:--";
  syncSectorSelector(data.sectors || [], data.selectedSectorId || 0);

  const body = document.getElementById("boardBody");
  const requests = data.requests || [];

  if (!requests.length) {
    body.className = "board-body empty-state";
    body.innerHTML = `
      <div class="empty-card">
        <h2>Nenhuma solicitacao aberta</h2>
        <p>Assim que chegar um novo chamado, o painel atualiza automaticamente.</p>
      </div>`;
    return;
  }

  body.className = "board-body board-layout";
  const [first, ...rest] = requests;
  const rightCards = rest.slice(0, 2).map(item => renderCard(item, false)).join("");
  body.innerHTML = `
    ${renderCard(first, true)}
    <div class="right-stack">${rightCards}</div>
  `;
}

function syncSectorSelector(sectors, selectedSectorId) {
  const select = document.getElementById("sectorSelect");
  if (!select) {
    return;
  }

  const currentOptions = Array.from(select.options).map(option => Number(option.value));
  const nextOptions = sectors.map(item => Number(item.id));
  const sameOptions = currentOptions.length === nextOptions.length
    && currentOptions.every((value, index) => value === nextOptions[index]);

  if (!sameOptions) {
    select.innerHTML = sectors.map(item => `
      <option value="${Number(item.id)}">${escapeHtml(item.name || "")}</option>
    `).join("");
  }

  state.applyingSectorSelection = true;
  select.value = String(selectedSectorId);
  state.applyingSectorSelection = false;
}

function renderCard(item, isBig) {
  const reasonPrefix = item.machineCode ? `${escapeHtml(item.machineCode)} ` : "";
  const reasonPt = `${reasonPrefix}${escapeHtml(item.reasonNamePt || "-")}`;
  const reasonJp = item.machineCode
    ? `${escapeHtml(item.machineCode)} ${escapeHtml(item.reasonNameJp || "-")}`
    : escapeHtml(item.reasonNameJp || "-");

  return `
    <article class="request-card ${isBig ? "big" : "small"}">
      <div class="area-strip">
        <h2>${escapeHtml(item.workAreaNamePt || "-")}</h2>
        <p>${escapeHtml(item.sectorNamePt || "-")} / ${escapeHtml(item.workAreaNameJp || "-")}</p>
      </div>
      <div class="reason-strip">
        <div class="reason-main">
          <div class="reason-pt">${reasonPt}</div>
          <div class="reason-jp">${reasonJp}</div>
        </div>
        <div class="reason-meta">
          <div>
            <strong>${escapeHtml(item.requestedAt || "--:--")}</strong>
          </div>
        </div>
      </div>
    </article>
  `;
}

function playBell() {
  const AudioContextCtor = window.AudioContext || window.webkitAudioContext;
  if (!AudioContextCtor) {
    return;
  }

  state.audioContext ??= new AudioContextCtor();
  const ctx = state.audioContext;

  if (ctx.state === "suspended") {
    ctx.resume();
  }

  const sequence = [
    { frequency: 784, start: 0, duration: 0.34, gain: 0.22 },
    { frequency: 587, start: 0.38, duration: 0.4, gain: 0.2 }
  ];

  sequence.forEach(note => {
    const filter = ctx.createBiquadFilter();
    filter.type = "lowpass";
    filter.frequency.value = 1800;
    filter.Q.value = 0.6;
    filter.connect(ctx.destination);

    const layers = [
      { type: "triangle", ratio: 1, level: 1 },
      { type: "sine", ratio: 0.5, level: 0.72 },
      { type: "triangle", ratio: 2, level: 0.28 }
    ];

    layers.forEach(layer => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = layer.type;
      osc.frequency.value = note.frequency * layer.ratio;

      const attackTime = ctx.currentTime + note.start + 0.015;
      const sustainTime = ctx.currentTime + note.start + Math.max(0.08, note.duration - 0.12);
      const endTime = ctx.currentTime + note.start + note.duration;
      const peak = note.gain * layer.level;

      gain.gain.setValueAtTime(0.0001, ctx.currentTime + note.start);
      gain.gain.exponentialRampToValueAtTime(peak, attackTime);
      gain.gain.exponentialRampToValueAtTime(peak * 0.48, sustainTime);
      gain.gain.exponentialRampToValueAtTime(0.0001, endTime);

      osc.connect(gain);
      gain.connect(filter);
      osc.start(ctx.currentTime + note.start);
      osc.stop(endTime + 0.05);
    });
  });
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#39;");
}

document.getElementById("sectorSelect")?.addEventListener("change", event => {
  if (state.applyingSectorSelection) {
    return;
  }

  const sectorId = Number(event.target.value || 0);
  window.chrome?.webview?.postMessage({
    type: "sectorSelection",
    sectorId
  });
});
