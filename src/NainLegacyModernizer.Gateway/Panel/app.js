// Legacy Modernizer control panel. Plain ES module: no framework, no build step.
// Presentation only: every figure comes from /_modernizer/api; this file formats and renders.

const API = "/_modernizer/api";
const MODES = ["Legacy", "Shadow", "Canary", "Modern"];
const STAGE_WEIGHT = { Legacy: 0, Shadow: 1 / 3, Canary: 2 / 3, Modern: 1 };
const TRAFFIC_ROWS = 80;
const AUDIT_ROWS = 120;
const SPARK_BUCKET_MS = 5_000;
const SPARK_BUCKETS = 24;
const DEMO_BATCH = 50;

const I18N = {
  en: {
    subtitle: "Strangler-fig control panel",
    legacy: "Legacy", modern: "Modern",
    connecting: "Connecting", live: "Live", offline: "Reconnecting",
    sendTraffic: `Send ${DEMO_BATCH} requests`, sending: "Sending…", auto: "Auto",
    autoTitle: "Keep sending demo traffic (2 requests per second)", clearTitle: "Reset observed data",
    kpiProgress: "Migration progress", kpiParity: "Parity", kpiParityHint: "legacy vs modern on shadowed traffic",
    kpiLatency: "Latency · p50", kpiLatencyHint: "who answers faster", kpiTraffic: "Traffic observed",
    kpiTrafficHint: "requests through the gateway", sparkLabel: "last 2 minutes",
    routesOnModern: (n, total) => `${n} of ${total} routes fully on the modern stack`,
    noAudits: "no shadow samples yet", matched: "matched", discrepancies: "discrepancies", errors: "errors",
    faster: (p) => `${p}% faster`, slower: (p) => `${p}% slower`, sameSpeed: "same speed", awaitingData: "awaiting data",
    modernVsLegacy: "modern vs legacy", requests: "requests",
    boardTitle: "Migration board",
    boardHint: "Each route moves left to right. Promote when the badge says Ready; roll back with one click at any time.",
    policy: (n, p) => `Cutover rule: ≥ ${n} shadow samples and ≥ ${p} parity, zero errors`,
    colLegacy: "Not started", colLegacySub: "monolith serves everything",
    colShadow: "Shadow", colShadowSub: "legacy answers, modern is compared",
    colCanary: "Canary", colCanarySub: "a share of real traffic goes modern",
    colModern: "Migrated", colModernSub: "monolith no longer involved",
    columnEmpty: "No routes at this stage",
    samples: "Samples", parity: "Parity", p50Legacy: "p50 legacy", p50Modern: "p50 modern",
    readiness: { Collecting: "Collecting", Blocked: "Blocked", Ready: "Ready", Completed: "Migrated" },
    mode: { Legacy: "Legacy", Shadow: "Shadow", Canary: "Canary", Modern: "Modern" },
    canaryShare: (p) => `${p}% to modern`,
    confirmCutover: "Send 100% of this route to the modern stack?", yes: "Promote", no: "Cancel",
    modeChanged: (id, mode) => `${id} → ${mode}`, modeFailed: "Could not change the mode",
    trafficTitle: "Live traffic", trafficEmpty: "No requests yet. Send demo traffic or point a client at the gateway.",
    colTime: "Time", colRequest: "Request", colMode: "Mode", colServedBy: "Served by", colStatus: "Status", colLatency: "Latency",
    auditsTitle: "Parity audits", auditEmpty: "Shadow routes record an audit for every request. Nothing audited yet.",
    filterAll: "All", filterDiscrepancy: "Discrepancies", filterErrors: "Errors",
    verdict: { Matched: "Matched", Discrepancy: "Discrepancy", ExecutionError: "Execution error" },
    nDiffs: (n) => (n === 1 ? "1 field differs" : `${n} fields differ`),
    kind: { ValueMismatch: "value", TypeMismatch: "type", MissingInModern: "missing in modern", MissingInLegacy: "missing in legacy" },
    field: "Field", legacyValue: "Legacy", modernValue: "Modern", kindCol: "Kind",
    status: "status", latency: "latency", noBody: "(no body captured)",
    sentBatch: (n) => `${n} requests sent`, cleared: "Observed data cleared", noSamples: "No sample requests configured",
    requestsShort: (n) => `${n} shown`,
  },
  es: {
    subtitle: "Panel de control strangler-fig",
    legacy: "Legacy", modern: "Moderno",
    connecting: "Conectando", live: "En vivo", offline: "Reconectando",
    sendTraffic: `Enviar ${DEMO_BATCH} peticiones`, sending: "Enviando…", auto: "Auto",
    autoTitle: "Enviar tráfico de demo de forma continua (2 por segundo)", clearTitle: "Borrar datos observados",
    kpiProgress: "Progreso de migración", kpiParity: "Paridad", kpiParityHint: "legacy vs moderno en tráfico sombra",
    kpiLatency: "Latencia · p50", kpiLatencyHint: "quién responde antes", kpiTraffic: "Tráfico observado",
    kpiTrafficHint: "peticiones a través del gateway", sparkLabel: "últimos 2 minutos",
    routesOnModern: (n, total) => `${n} de ${total} rutas ya en la pila moderna`,
    noAudits: "sin muestras sombra todavía", matched: "coinciden", discrepancies: "discrepancias", errors: "errores",
    faster: (p) => `${p}% más rápido`, slower: (p) => `${p}% más lento`, sameSpeed: "misma velocidad", awaitingData: "esperando datos",
    modernVsLegacy: "moderno vs legacy", requests: "peticiones",
    boardTitle: "Tablero de migración",
    boardHint: "Cada ruta avanza de izquierda a derecha. Promociona cuando la etiqueta diga Lista; vuelve atrás con un clic en cualquier momento.",
    policy: (n, p) => `Regla de corte: ≥ ${n} muestras sombra y ≥ ${p} de paridad, cero errores`,
    colLegacy: "Sin empezar", colLegacySub: "el monolito lo sirve todo",
    colShadow: "Sombra", colShadowSub: "legacy responde, moderno se compara",
    colCanary: "Canario", colCanarySub: "parte del tráfico real va al moderno",
    colModern: "Migrada", colModernSub: "el monolito ya no interviene",
    columnEmpty: "Ninguna ruta en esta fase",
    samples: "Muestras", parity: "Paridad", p50Legacy: "p50 legacy", p50Modern: "p50 moderno",
    readiness: { Collecting: "Recogiendo", Blocked: "Bloqueada", Ready: "Lista", Completed: "Migrada" },
    mode: { Legacy: "Legacy", Shadow: "Sombra", Canary: "Canario", Modern: "Moderno" },
    canaryShare: (p) => `${p}% al moderno`,
    confirmCutover: "¿Enviar el 100% de esta ruta a la pila moderna?", yes: "Promocionar", no: "Cancelar",
    modeChanged: (id, mode) => `${id} → ${mode}`, modeFailed: "No se pudo cambiar el modo",
    trafficTitle: "Tráfico en vivo", trafficEmpty: "Aún no hay peticiones. Envía tráfico de demo o apunta un cliente al gateway.",
    colTime: "Hora", colRequest: "Petición", colMode: "Modo", colServedBy: "Servida por", colStatus: "Estado", colLatency: "Latencia",
    auditsTitle: "Auditorías de paridad", auditEmpty: "Las rutas en sombra registran una auditoría por petición. Nada auditado todavía.",
    filterAll: "Todas", filterDiscrepancy: "Discrepancias", filterErrors: "Errores",
    verdict: { Matched: "Coincide", Discrepancy: "Discrepancia", ExecutionError: "Error de ejecución" },
    nDiffs: (n) => (n === 1 ? "1 campo difiere" : `${n} campos difieren`),
    kind: { ValueMismatch: "valor", TypeMismatch: "tipo", MissingInModern: "falta en moderno", MissingInLegacy: "falta en legacy" },
    field: "Campo", legacyValue: "Legacy", modernValue: "Moderno", kindCol: "Tipo",
    status: "estado", latency: "latencia", noBody: "(cuerpo no capturado)",
    sentBatch: (n) => `${n} peticiones enviadas`, cleared: "Datos observados borrados", noSamples: "No hay peticiones de ejemplo configuradas",
    requestsShort: (n) => `${n} mostradas`,
  },
};

const state = {
  lang: "en",
  theme: "dark",
  overview: null,
  traffic: [],
  audits: [],
  auditFilter: "all",
  pendingConfirm: null,
  busyRoute: null,
  auto: false,
  autoTimer: null,
  sending: false,
  sparkCounts: new Map(),
  openAudit: null,
};

const $ = (id) => document.getElementById(id);
const t = (key, ...args) => {
  const value = I18N[state.lang][key] ?? I18N.en[key] ?? key;
  return typeof value === "function" ? value(...args) : value;
};
const esc = (value) =>
  String(value ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
const storage = {
  get(key) { try { return localStorage.getItem(key); } catch { return null; } },
  set(key, value) { try { localStorage.setItem(key, value); } catch { /* private mode */ } },
};

const fmt = {
  pct(ratio, digits = 1) {
    return new Intl.NumberFormat(locale(), { style: "percent", maximumFractionDigits: digits, minimumFractionDigits: 0 }).format(ratio ?? 0);
  },
  int(n) { return new Intl.NumberFormat(locale()).format(n ?? 0); },
  ms(n) {
    if (n == null || Number.isNaN(n)) return "—";
    return `${new Intl.NumberFormat(locale(), { maximumFractionDigits: n < 10 ? 1 : 0 }).format(n)} ms`;
  },
  time(iso) {
    return new Intl.DateTimeFormat(locale(), { hour: "2-digit", minute: "2-digit", second: "2-digit" }).format(new Date(iso));
  },
};
const locale = () => (state.lang === "es" ? "es-ES" : "en-GB");

// ---------- API ----------
async function api(path, options) {
  const response = await fetch(API + path, { headers: { Accept: "application/json", ...(options?.headers ?? {}) }, ...options });
  if (!response.ok) {
    let detail = response.statusText;
    try { detail = (await response.text()) || detail; } catch { /* ignore */ }
    throw new Error(detail);
  }
  return response.status === 204 ? null : response.json();
}

async function loadOverview() {
  state.overview = await api("/overview");
  renderOverview();
}

async function loadHistory() {
  const [traffic, audits] = await Promise.all([api(`/traffic?take=${TRAFFIC_ROWS}`), api(`/audits?take=${AUDIT_ROWS}`)]);
  state.traffic = traffic;
  state.audits = audits;
  state.sparkCounts.clear();
  for (const sample of traffic) bumpSpark(sample.observedAtUtc);
  renderTraffic();
  renderAudits();
  renderSpark();
}

function connectStream() {
  const live = $("live");
  const setLive = (s) => {
    live.dataset.state = s;
    $("liveLabel").textContent = t(s === "live" ? "live" : s === "offline" ? "offline" : "connecting");
  };
  setLive("connecting");

  const source = new EventSource(`${API}/stream`);
  source.addEventListener("connected", () => setLive("live"));
  source.addEventListener("traffic", (e) => {
    const sample = JSON.parse(e.data);
    sample._new = true;
    state.traffic.unshift(sample);
    if (state.traffic.length > TRAFFIC_ROWS) state.traffic.length = TRAFFIC_ROWS;
    bumpSpark(sample.observedAtUtc);
    renderTraffic();
    renderSpark();
  });
  source.addEventListener("audit", (e) => {
    const audit = JSON.parse(e.data);
    audit._new = true;
    state.audits.unshift(audit);
    if (state.audits.length > AUDIT_ROWS) state.audits.length = AUDIT_ROWS;
    renderAudits();
  });
  source.addEventListener("cleared", () => {
    state.traffic = [];
    state.audits = [];
    state.sparkCounts.clear();
    renderTraffic();
    renderAudits();
    renderSpark();
    loadOverview().catch(() => {});
  });
  source.onerror = () => setLive("offline");
}

// ---------- Rendering: KPIs ----------
function renderOverview() {
  const o = state.overview;
  if (!o) return;

  $("legacyHost").textContent = o.legacyBaseAddress.replace(/^https?:\/\//, "");
  $("modernHost").textContent = o.modernBaseAddress.replace(/^https?:\/\//, "");

  const routes = o.routes;
  const progress = routes.length ? routes.reduce((acc, r) => acc + STAGE_WEIGHT[r.mode], 0) / routes.length : 0;
  $("ringValue").style.strokeDasharray = `${Math.round(progress * 100)} 100`;
  $("kpiProgress").textContent = fmt.pct(progress, 0);
  $("kpiProgressSub").textContent = t("routesOnModern", o.totals.routesModern, o.totals.routes);
  $("stageCounts").innerHTML = MODES.map((mode) => {
    const n = routes.filter((r) => r.mode === mode).length;
    return `<div class="stage"><i style="background:var(--${mode.toLowerCase()})"></i><strong>${n}</strong><span>${esc(t(`col${mode}`))}</span></div>`;
  }).join("");

  const tt = o.totals;
  const audited = tt.auditedSamples;
  $("kpiParity").innerHTML = audited ? fmt.pct(tt.parityRatio, 2) : `<small>${esc(t("noAudits"))}</small>`;
  const segs = [["matched", tt.matched], ["discrepancy", tt.discrepancies], ["error", tt.executionErrors]];
  $("parityStack").innerHTML = segs.filter(([, n]) => n > 0).map(([k, n]) => `<span class="seg-${k}" style="flex:${n}"></span>`).join("");
  $("parityLegend").innerHTML = [
    ["var(--modern)", t("matched"), tt.matched],
    ["var(--warn)", t("discrepancies"), tt.discrepancies],
    ["var(--bad)", t("errors"), tt.executionErrors],
  ].map(([c, label, n]) => `<li><i style="background:${c}"></i>${esc(label)} <strong>${fmt.int(n)}</strong></li>`).join("");

  const lp = tt.legacyP50Ms, mp = tt.modernP50Ms;
  let delta = `<small>${esc(t("awaitingData"))}</small>`;
  if (lp > 0 && mp > 0) {
    const ratio = (lp - mp) / lp;
    const txt = Math.abs(ratio) < 0.02 ? t("sameSpeed") : ratio > 0 ? t("faster", Math.round(ratio * 100)) : t("slower", Math.round(-ratio * 100));
    delta = `${esc(txt)}<small>${esc(t("modernVsLegacy"))}</small>`;
  }
  $("kpiLatencyDelta").innerHTML = delta;
  const max = Math.max(lp, mp, 1);
  $("latencyBars").innerHTML = [
    ["legacy", t("legacy"), lp], ["modern", t("modern"), mp],
  ].map(([k, label, v]) => `
    <div class="bar-row">
      <span class="label"><i style="background:var(--${k})"></i>${esc(label)}</span>
      <div class="track"><div class="fill fill-${k}" style="width:${Math.max(2, (v / max) * 100)}%"></div></div>
      <span class="value">${esc(fmt.ms(v))}</span>
    </div>`).join("");

  $("kpiRequests").innerHTML = `${fmt.int(tt.totalRequests)}<small>${esc(t("requests"))}</small>`;

  $("policy").textContent = t("policy", o.policy.minimumSamples, fmt.pct(o.policy.minimumParityRatio, 1));
  renderBoard();
}

// ---------- Rendering: board ----------
function renderBoard(force = false) {
  const o = state.overview;
  if (!o) return;
  // A periodic refresh must not pull the slider or a pending confirmation from under the operator.
  const active = document.activeElement;
  const interacting = state.pendingConfirm !== null || (active && $("columns").contains(active) && active.matches("input[type=range]"));
  if (interacting && !force) return;
  $("columns").innerHTML = MODES.map((mode, index) => {
    const cards = o.routes.filter((r) => r.mode === mode);
    return `
      <section class="column column-${mode}" aria-label="${esc(t(`col${mode}`))}">
        <header class="column-head">
          <span class="step">${index + 1}</span>
          <span class="title">${esc(t(`col${mode}`))}<span class="subtitle">${esc(t(`col${mode}Sub`))}</span></span>
          <span class="count">${cards.length}</span>
        </header>
        <div class="column-body">
          ${cards.length ? cards.map(routeCard).join("") : `<p class="column-empty">${esc(t("columnEmpty"))}</p>`}
        </div>
      </section>`;
  }).join("");
}

function routeCard(r) {
  const s = r.summary;
  const rd = r.readiness;
  const busy = state.busyRoute === r.id;
  const suggested = rd.state === "Ready" ? nextMode(r.mode) : null;
  const confirming = state.pendingConfirm === r.id;

  const parity = s.auditedSamples
    ? [["matched", s.matched], ["discrepancy", s.discrepancies], ["error", s.executionErrors]]
      .filter(([, n]) => n > 0)
      .map(([k, n]) => `<span class="seg-${k}" style="flex:${n};background:var(--${k === "matched" ? "modern" : k === "discrepancy" ? "warn" : "bad"})"></span>`).join("")
    : "";

  return `
    <article class="card" data-mode="${r.mode}" data-route="${esc(r.id)}">
      <div class="card-head">
        <code class="path" title="${esc(r.id)}">${esc(r.pathPattern)}</code>
        <span class="badge badge-${rd.state}"><i class="dot"></i>${esc(t("readiness")[rd.state])}</span>
      </div>
      <p class="card-desc">${esc(r.description)}</p>
      <div class="stats">
        <div class="stat"><span class="label">${esc(t("samples"))}</span><strong>${fmt.int(s.auditedSamples || s.totalRequests)}</strong></div>
        <div class="stat"><span class="label">${esc(t("parity"))}</span><strong>${s.auditedSamples ? fmt.pct(s.parityRatio, 1) : "—"}</strong></div>
        <div class="stat legacy"><span class="label">${esc(t("p50Legacy"))}</span><strong>${s.legacyLatency.count ? esc(fmt.ms(s.legacyLatency.p50Ms)) : "—"}</strong></div>
        <div class="stat modern"><span class="label">${esc(t("p50Modern"))}</span><strong>${s.modernLatency.count ? esc(fmt.ms(s.modernLatency.p50Ms)) : "—"}</strong></div>
      </div>
      ${parity ? `<div class="parity" title="${esc(fmt.int(s.matched))} / ${esc(fmt.int(s.auditedSamples))}">${parity}</div>` : ""}
      <p class="card-reason" title="${esc(rd.reason)}">${esc(rd.reason)}</p>
      ${r.mode === "Canary" ? `
        <label class="canary-row">
          <input type="range" min="0" max="100" step="5" value="${r.canaryPercent}" data-canary="${esc(r.id)}" aria-label="Canary share">
          <output>${esc(t("canaryShare", r.canaryPercent))}</output>
        </label>` : ""}
      ${confirming ? `
        <div class="confirm">
          <span>${esc(t("confirmCutover"))}</span>
          <span>
            <button class="btn btn-ghost" data-confirm="no" data-route="${esc(r.id)}">${esc(t("no"))}</button>
            <button class="btn btn-primary" data-confirm="yes" data-route="${esc(r.id)}">${esc(t("yes"))}</button>
          </span>
        </div>` : ""}
      <div class="switch" role="group" aria-label="Mode">
        ${MODES.map((m) => `
          <button type="button" data-mode-btn="${m}" data-route="${esc(r.id)}" aria-pressed="${m === r.mode}"
            class="${suggested === m ? "is-suggested" : ""}" ${busy ? "disabled" : ""}>${esc(t("mode")[m])}</button>`).join("")}
      </div>
    </article>`;
}

const nextMode = (mode) => MODES[Math.min(MODES.indexOf(mode) + 1, MODES.length - 1)];

async function changeMode(routeId, mode, canaryPercent) {
  state.busyRoute = routeId;
  state.pendingConfirm = null;
  renderBoard(true);
  try {
    const body = { mode };
    if (canaryPercent != null) body.canaryPercent = canaryPercent;
    await api(`/routes/${encodeURIComponent(routeId)}/mode`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    toast(t("modeChanged", routeId, t("mode")[mode]));
    await loadOverview();
    document.querySelector(`.card[data-route="${CSS.escape(routeId)}"]`)?.classList.add("is-flash");
  } catch (err) {
    toast(`${t("modeFailed")}: ${err.message}`, true);
  } finally {
    state.busyRoute = null;
    renderBoard(true);
  }
}

// ---------- Rendering: traffic ----------
function renderTraffic() {
  const rows = state.traffic;
  $("trafficEmpty").hidden = rows.length > 0;
  $("trafficCount").textContent = rows.length ? t("requestsShort", rows.length) : "";
  const maxMs = Math.max(60, ...rows.map((r) => r.latencyMs));
  $("trafficBody").innerHTML = rows.map((r) => {
    const cls = `s${Math.floor(r.statusCode / 100)}`;
    const isNew = r._new; r._new = false;
    return `
      <tr class="${isNew ? "is-new" : ""}">
        <td class="time">${esc(fmt.time(r.observedAtUtc))}</td>
        <td class="req" title="${esc(r.path)}"><span class="method">${esc(r.method)}</span> <span class="path">${esc(r.path)}</span></td>
        <td><span class="pill pill-${r.mode}">${esc(t(`col${r.mode}`))}</span></td>
        <td><span class="pill pill-${r.target}"><i class="dot" style="background:var(--${r.target.toLowerCase()})"></i>${esc(t(r.target.toLowerCase()))}</span></td>
        <td><span class="status ${cls}">${r.statusCode}</span></td>
        <td><span class="lat"><span class="track"><span class="fill fill-${r.target.toLowerCase()}" style="width:${Math.min(100, (r.latencyMs / maxMs) * 100)}%"></span></span><span class="ms">${esc(fmt.ms(r.latencyMs))}</span></span></td>
      </tr>`;
  }).join("");
}

// ---------- Rendering: audits ----------
function renderAudits() {
  const list = state.auditFilter === "all" ? state.audits : state.audits.filter((a) => a.verdict === state.auditFilter);
  $("auditEmpty").hidden = list.length > 0;
  $("auditList").innerHTML = list.map((a) => {
    const isNew = a._new; a._new = false;
    const marks = { Matched: "✓", Discrepancy: "≠", ExecutionError: "!" };
    const meta = a.verdict === "Discrepancy" ? t("nDiffs", a.discrepancies.length)
      : a.verdict === "ExecutionError" ? (a.modern.error || a.legacy.error || t("verdict").ExecutionError)
      : `${esc(fmt.ms(a.legacy.latencyMs))} → ${esc(fmt.ms(a.modern.latencyMs))}`;
    return `
      <li>
        <button class="audit ${isNew ? "is-new" : ""}" data-audit="${esc(a.id)}">
          <span class="mark mark-${a.verdict}" aria-label="${esc(t("verdict")[a.verdict])}">${marks[a.verdict]}</span>
          <span class="main">
            <span class="path"><span class="method">${esc(a.method)}</span> ${esc(a.requestPath)}</span>
            <span class="meta">${esc(a.routeId)} · ${meta}</span>
          </span>
          <span class="side"><strong>${esc(t("verdict")[a.verdict])}</strong><span>${esc(fmt.time(a.observedAtUtc))}</span></span>
        </button>
      </li>`;
  }).join("");
}

function openAudit(id) {
  const a = state.audits.find((x) => x.id === id);
  if (!a) return;
  state.openAudit = a;
  $("drawerTitle").innerHTML = `<span class="verdict verdict-${a.verdict}">${esc(t("verdict")[a.verdict])}</span> <span class="muted small">${esc(a.routeId)} · ${esc(fmt.time(a.observedAtUtc))}</span>`;
  $("drawerPath").textContent = `${a.method} ${a.requestPath}`;

  const fields = new Set(a.discrepancies.map((d) => d.field.split(".").pop().replace(/\[\d+\]$/, "")));
  const aliasTargets = new Set();
  for (const d of a.discrepancies) aliasTargets.add(d.field.toLowerCase());

  const diffs = a.discrepancies.length ? `
    <table class="diffs">
      <thead><tr><th>${esc(t("field"))}</th><th>${esc(t("legacyValue"))}</th><th>${esc(t("modernValue"))}</th><th>${esc(t("kindCol"))}</th></tr></thead>
      <tbody>${a.discrepancies.map((d) => `
        <tr>
          <td class="field">${esc(d.field)}</td>
          <td class="legacy">${d.legacyValue == null ? "—" : esc(d.legacyValue)}</td>
          <td class="modern">${d.modernValue == null ? "—" : esc(d.modernValue)}</td>
          <td class="kind">${esc(t("kind")[d.kind] ?? d.kind)}</td>
        </tr>`).join("")}
      </tbody>
    </table>` : "";

  $("drawerBody").innerHTML = `
    ${diffs}
    <div class="side-by-side">
      ${sidePanel("legacy", t("legacy"), a.legacy, fields)}
      ${sidePanel("modern", t("modern"), a.modern, fields)}
    </div>`;

  $("drawer").hidden = false;
  $("drawerBackdrop").hidden = false;
  $("drawerClose").focus();
}

function sidePanel(kind, label, obs, fields) {
  const meta = `<span class="meta"><span>${esc(t("status"))} <strong>${obs.statusCode ?? "—"}</strong></span><span>${esc(t("latency"))} <strong>${esc(fmt.ms(obs.latencyMs))}</strong></span></span>`;
  let body;
  if (obs.error) body = `<p class="error">${esc(obs.error)}</p>`;
  else if (obs.body == null || obs.body === "") body = `<p class="error muted">${esc(t("noBody"))}</p>`;
  else body = `<pre>${highlight(pretty(obs.body), fields)}</pre>`;
  return `<section class="side side-${kind}"><header><span>${esc(label)}</span>${meta}</header>${body}</section>`;
}

function pretty(text) {
  try { return JSON.stringify(JSON.parse(text), null, 2); } catch { return text; }
}

function highlight(json, fields) {
  return json.split("\n").map((line) => {
    const m = line.match(/^\s*"([^"]+)"\s*:/);
    const hit = m && [...fields].some((f) => f.toLowerCase() === m[1].toLowerCase());
    return hit ? `<mark>${esc(line)}</mark>` : esc(line);
  }).join("\n");
}

function closeDrawer() {
  $("drawer").hidden = true;
  $("drawerBackdrop").hidden = true;
  state.openAudit = null;
}

// ---------- Rendering: sparkline ----------
function bumpSpark(iso) {
  const bucket = Math.floor(new Date(iso).getTime() / SPARK_BUCKET_MS);
  state.sparkCounts.set(bucket, (state.sparkCounts.get(bucket) ?? 0) + 1);
}

function renderSpark() {
  const now = Math.floor(Date.now() / SPARK_BUCKET_MS);
  const values = [];
  for (let i = SPARK_BUCKETS - 1; i >= 0; i--) values.push(state.sparkCounts.get(now - i) ?? 0);
  for (const key of state.sparkCounts.keys()) if (key < now - SPARK_BUCKETS) state.sparkCounts.delete(key);

  const w = 240, h = 56, pad = 3;
  const max = Math.max(1, ...values);
  const step = (w - pad * 2) / (values.length - 1);
  const pts = values.map((v, i) => [pad + i * step, h - pad - (v / max) * (h - pad * 2)]);
  const line = pts.map(([x, y], i) => `${i ? "L" : "M"}${x.toFixed(1)} ${y.toFixed(1)}`).join(" ");
  const area = `${line} L${(pad + (values.length - 1) * step).toFixed(1)} ${h - pad} L${pad} ${h - pad} Z`;
  const [lx, ly] = pts[pts.length - 1];
  $("spark").innerHTML = `<path class="area" d="${area}"/><path class="line" d="${line}"/><circle class="last" cx="${lx.toFixed(1)}" cy="${ly.toFixed(1)}" r="3"/>`;
}

// ---------- Demo traffic ----------
function sampleRequests() {
  const o = state.overview;
  if (!o) return [];
  return o.routes.flatMap((r) => r.sampleRequests);
}

async function sendBatch(count) {
  const samples = sampleRequests();
  if (!samples.length) { toast(t("noSamples"), true); return; }
  const btn = $("btnTraffic");
  state.sending = true;
  btn.disabled = true;
  btn.querySelector("span").textContent = t("sending");
  try {
    const jobs = Array.from({ length: count }, (_, i) => fireSample(samples[(i * 7 + Math.floor(Math.random() * samples.length)) % samples.length]));
    await Promise.allSettled(jobs);
    toast(t("sentBatch", count));
    await loadOverview();
  } finally {
    state.sending = false;
    btn.disabled = false;
    btn.querySelector("span").textContent = t("sendTraffic");
  }
}

function fireSample(sample) {
  const init = { method: sample.method, headers: { Accept: "application/json" } };
  if (sample.jsonBody) {
    init.headers["Content-Type"] = "application/json";
    init.body = sample.jsonBody;
  }
  return fetch(sample.path, init).then((r) => r.arrayBuffer()).catch(() => {});
}

function toggleAuto() {
  state.auto = !state.auto;
  $("btnAuto").setAttribute("aria-pressed", String(state.auto));
  clearInterval(state.autoTimer);
  if (state.auto) {
    state.autoTimer = setInterval(() => {
      const samples = sampleRequests();
      if (samples.length) fireSample(samples[Math.floor(Math.random() * samples.length)]);
    }, 500);
  }
}

// ---------- UI plumbing ----------
let toastTimer;
function toast(message, isError = false) {
  const el = $("toast");
  el.textContent = message;
  el.classList.toggle("is-error", isError);
  el.hidden = false;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => { el.hidden = true; }, 2600);
}

function applyLanguage(lang) {
  state.lang = lang;
  storage.set("modernizer.lang", lang);
  document.documentElement.lang = lang;
  $("btnLang").textContent = lang === "en" ? "ES" : "EN";
  document.querySelectorAll("[data-i18n]").forEach((el) => { el.textContent = t(el.dataset.i18n); });
  document.querySelectorAll("[data-i18n-title]").forEach((el) => { el.title = t(el.dataset.i18nTitle); });
  renderOverview();
  renderTraffic();
  renderAudits();
  if (state.openAudit) openAudit(state.openAudit.id);
}

function applyTheme(theme) {
  state.theme = theme;
  storage.set("modernizer.theme", theme);
  document.documentElement.dataset.theme = theme;
}

function wireEvents() {
  $("btnLang").addEventListener("click", () => applyLanguage(state.lang === "en" ? "es" : "en"));
  $("btnTheme").addEventListener("click", () => applyTheme(state.theme === "dark" ? "light" : "dark"));
  $("btnTraffic").addEventListener("click", () => { if (!state.sending) sendBatch(DEMO_BATCH); });
  $("btnAuto").addEventListener("click", toggleAuto);
  $("btnClear").addEventListener("click", async () => {
    await api("/audits", { method: "DELETE" });
    toast(t("cleared"));
  });

  $("columns").addEventListener("click", (e) => {
    const modeBtn = e.target.closest("[data-mode-btn]");
    if (modeBtn) {
      const routeId = modeBtn.dataset.route;
      const mode = modeBtn.dataset.modeBtn;
      const route = state.overview.routes.find((r) => r.id === routeId);
      if (!route || route.mode === mode) return;
      if (mode === "Modern") {
        state.pendingConfirm = routeId;
        renderBoard(true);
        return;
      }
      changeMode(routeId, mode);
      return;
    }
    const confirm = e.target.closest("[data-confirm]");
    if (confirm) {
      const routeId = confirm.dataset.route;
      if (confirm.dataset.confirm === "yes") changeMode(routeId, "Modern");
      else { state.pendingConfirm = null; renderBoard(true); }
    }
  });

  $("columns").addEventListener("input", (e) => {
    const range = e.target.closest("[data-canary]");
    if (range) range.closest(".canary-row").querySelector("output").textContent = t("canaryShare", range.value);
  });
  $("columns").addEventListener("change", (e) => {
    const range = e.target.closest("[data-canary]");
    if (range) changeMode(range.dataset.canary, "Canary", Number(range.value));
  });

  $("auditFilters").addEventListener("click", (e) => {
    const chip = e.target.closest(".chip");
    if (!chip) return;
    state.auditFilter = chip.dataset.filter;
    document.querySelectorAll("#auditFilters .chip").forEach((c) => c.classList.toggle("is-active", c === chip));
    renderAudits();
  });
  $("auditList").addEventListener("click", (e) => {
    const item = e.target.closest("[data-audit]");
    if (item) openAudit(item.dataset.audit);
  });
  $("drawerClose").addEventListener("click", closeDrawer);
  $("drawerBackdrop").addEventListener("click", closeDrawer);
  document.addEventListener("keydown", (e) => { if (e.key === "Escape" && !$("drawer").hidden) closeDrawer(); });
}

async function boot() {
  const savedTheme = storage.get("modernizer.theme");
  applyTheme(savedTheme ?? (window.matchMedia?.("(prefers-color-scheme: light)").matches ? "light" : "dark"));
  const savedLang = storage.get("modernizer.lang") ?? (navigator.language?.toLowerCase().startsWith("es") ? "es" : "en");
  state.lang = savedLang;
  wireEvents();
  applyLanguage(savedLang);

  try {
    await loadOverview();
    await loadHistory();
  } catch (err) {
    toast(err.message, true);
  }
  connectStream();
  setInterval(() => loadOverview().catch(() => {}), 3_000);
  setInterval(renderSpark, SPARK_BUCKET_MS);
}

boot();
