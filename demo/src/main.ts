import { MODE_PERCENT, MODES, clampPercent, modeFor, routeStatus, targetFor, type Mode, type RouteStatus, type Target } from "./routing.ts";
import { comparedFields, compare } from "./parity.ts";
import { scenarios } from "./fixtures.ts";
import { cutoverPolicy, demoRoutes, progressOf, readinessOf, type Readiness } from "./routes.ts";
import { composeYaml, defaultSetup, parityCommand, parseAliases, parseList, settingsJson, validate, workflowYaml, type ClientSetup, type SetupError } from "./config.ts";
import { resolveLang, t, type Key, type Lang } from "./i18n.ts";
import { LANG_KEY, THEME_KEY, readPref, resolveTheme, writePref, type Theme } from "./prefs.ts";
import { layoutFor, type Layout, type NodeBox } from "./layout.ts";
import { TOUR_STEP_MS, advance, finished, idle, modeAt, start, stop, type TourState } from "./tour.ts";

const SVG_NS = "http://www.w3.org/2000/svg";

const SPEED = 0.22; // px per ms
const SPAWN_MS = 600;

const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
const prefersLight = window.matchMedia("(prefers-color-scheme: light)");
const narrowScreen = window.matchMedia("(max-width: 640px)");
let layout: Layout = layoutFor(narrowScreen.matches);

function byId<T extends Element>(id: string): T {
  const el = document.getElementById(id);
  if (!el) throw new Error(`Missing element #${id}`);
  return el as unknown as T;
}

const state = {
  lang: resolveLang(readPref(LANG_KEY), new URLSearchParams(window.location.search).get("lang")) as Lang,
  theme: resolveTheme(readPref(THEME_KEY), prefersLight.matches) as Theme,
  percent: MODE_PERCENT.canary,
  shadow: false,
  paused: false,
  scenarioId: scenarios[0]?.id ?? "ORD-001",
};

const slider = byId<HTMLInputElement>("migrated");
const sliderOut = byId<HTMLOutputElement>("migrated-out");
const ordersStatus = byId<HTMLElement>("orders-status");
const wireModern = byId<SVGLineElement>("wire-modern");
const wireShadowLabel = byId<SVGTextElement>("wire-shadow-label");
const packetsLayer = byId<SVGGElement>("packets");
const motionButton = byId<HTMLButtonElement>("motion");
const themeButton = byId<HTMLButtonElement>("theme");
const modeButtons = Array.from(document.querySelectorAll<HTMLButtonElement>("[data-mode]"));
const langButtons = Array.from(document.querySelectorAll<HTMLButtonElement>("[data-lang]"));

const modeKey: Record<Mode, Key> = { legacy: "stageLegacy", shadow: "stageShadow", canary: "stageCanary", modern: "stageModern" };
const readinessKey: Record<Readiness, Key> = {
  notStarted: "readyNotStarted",
  verifying: "readyVerifying",
  blocked: "readyBlocked",
  live: "readyLive",
  done: "readyDone",
};
const routeNameKey: Record<string, Key> = {
  invoices: "routeInvoices",
  orders: "routeOrders",
  customers: "routeCustomers",
  catalog: "routeCatalog",
  pricing: "routePricing",
};

const statusKey: Record<RouteStatus, Key> = {
  legacy: "statusLegacy",
  transition: "statusTransition",
  migrated: "statusMigrated",
};

/* ---------- language ---------- */

function applyLang(): void {
  document.documentElement.lang = state.lang;
  document.title = t(state.lang, "pageTitle");
  document.querySelector('meta[name="description"]')?.setAttribute("content", t(state.lang, "pageDescription"));
  for (const el of document.querySelectorAll<HTMLElement>("[data-i18n]")) {
    el.textContent = t(state.lang, el.dataset["i18n"] as Key);
  }
  for (const el of document.querySelectorAll<HTMLElement>("[data-i18n-aria]")) {
    el.setAttribute("aria-label", t(state.lang, el.dataset["i18nAria"] as Key));
  }
  for (const el of document.querySelectorAll<HTMLElement>("[data-i18n-title]")) {
    el.title = t(state.lang, el.dataset["i18nTitle"] as Key);
  }
  for (const button of langButtons) {
    button.setAttribute("aria-pressed", String(button.dataset["lang"] === state.lang));
  }
  renderRoute();
  renderMotionButton();
  renderTheme();
  renderBoard();
  renderRule();
  renderShadow();
  renderSetup();
  renderTour();
}

/* ---------- guided tour ---------- */

let tour: TourState = idle;
let tourTimer = 0;
const tourButton = byId<HTMLButtonElement>("tour");
const tourCaptionKey: Record<Mode, Key> = { legacy: "tourLegacy", shadow: "tourShadow", canary: "tourCanary", modern: "tourModern" };

function renderTour(): void {
  const caption = byId<HTMLElement>("tour-caption");
  const mode = modeAt(tour);
  caption.hidden = mode === null && !finished(tour);
  if (mode !== null) {
    byId<HTMLElement>("tour-step").textContent = t(state.lang, "tourStep", { n: tour.index + 1 });
    byId<HTMLElement>("tour-text").textContent = t(state.lang, tourCaptionKey[mode]);
  } else if (finished(tour)) {
    byId<HTMLElement>("tour-step").textContent = "";
    byId<HTMLElement>("tour-text").textContent = t(state.lang, "tourDone");
  }
  tourButton.setAttribute("aria-pressed", String(tour.running));
  byId<HTMLElement>("tour-label").textContent = t(
    state.lang,
    tour.running ? (reducedMotion.matches ? "tourNext" : "tourStop") : "ctaTour",
  );
}

function tourTick(): void {
  tour = advance(tour);
  const mode = modeAt(tour);
  if (mode !== null) selectMode(mode);
  renderTour();
  if (tour.running && !reducedMotion.matches) tourTimer = window.setTimeout(tourTick, TOUR_STEP_MS);
}

function startTour(): void {
  tour = start();
  selectMode("legacy");
  renderTour();
  byId<HTMLElement>("diagram").scrollIntoView({ block: "start" });
  if (!reducedMotion.matches) tourTimer = window.setTimeout(tourTick, TOUR_STEP_MS);
}

/** Any manual choice ends the tour; the caption stays until the next render. */
function stopTour(): void {
  if (!tour.running && !finished(tour)) return;
  window.clearTimeout(tourTimer);
  tour = stop();
  renderTour();
}

/* ---------- cut-over rule ---------- */

function renderRule(): void {
  const locale = state.lang === "es" ? "es-ES" : "en-GB";
  byId<HTMLElement>("rule-samples").textContent = String(cutoverPolicy.minimumSamples);
  byId<HTMLElement>("rule-parity").textContent = new Intl.NumberFormat(locale, { style: "percent", minimumFractionDigits: 1 }).format(cutoverPolicy.minimumParityRatio);
  byId<HTMLElement>("rule-errors").textContent = String(cutoverPolicy.maximumErrors);
}

/* ---------- setup generator ---------- */

type Output = "settings" | "cli" | "workflow" | "compose";
let output: Output = "settings";
const outputHint: Record<Output, Key> = { settings: "outSettingsHint", cli: "outCliHint", workflow: "outWorkflowHint", compose: "outComposeHint" };
const outputButtons = Array.from(document.querySelectorAll<HTMLButtonElement>("[data-out]"));
const copyButton = byId<HTMLButtonElement>("copy");
let copyTimer = 0;

function readSetup(): ClientSetup {
  const mode = byId<HTMLSelectElement>("f-mode").value as Mode;
  return {
    legacy: byId<HTMLInputElement>("f-legacy").value.trim(),
    modern: byId<HTMLInputElement>("f-modern").value.trim(),
    routePath: byId<HTMLInputElement>("f-route").value.trim(),
    mode: MODES.includes(mode) ? mode : defaultSetup.mode,
    canaryPercent: Number(byId<HTMLInputElement>("f-canary").value),
    ignoredFields: parseList(byId<HTMLInputElement>("f-ignored").value),
    aliases: parseAliases(byId<HTMLInputElement>("f-aliases").value),
    authHeader: byId<HTMLInputElement>("f-auth").checked,
  };
}

const errorField: Record<SetupError, string> = { legacy: "f-legacy", modern: "f-modern", routePath: "f-route", canaryPercent: "f-canary" };

function renderSetup(): void {
  const setup = readSetup();
  byId<HTMLElement>("l-canary").hidden = setup.mode !== "canary";
  const errors = validate(setup).filter((e) => e !== "canaryPercent" || setup.mode === "canary");
  for (const key of Object.keys(errorField) as SetupError[]) {
    const invalid = errors.includes(key);
    byId<HTMLElement>(`e-${key}`).hidden = !invalid;
    byId<HTMLInputElement>(errorField[key]).setAttribute("aria-invalid", String(invalid));
  }
  for (const button of outputButtons) button.setAttribute("aria-pressed", String(button.dataset["out"] === output));
  byId<HTMLElement>("out-hint").textContent = t(state.lang, outputHint[output]);
  const text = output === "settings" ? settingsJson(setup) : output === "cli" ? parityCommand(setup) : output === "workflow" ? workflowYaml(setup) : composeYaml(setup);
  byId<HTMLElement>("out-code").textContent = text;
  copyButton.disabled = errors.length > 0;
}

async function copyOutput(): Promise<void> {
  const text = byId<HTMLElement>("out-code").textContent ?? "";
  let state_: "copied" | "failed" = "failed";
  try {
    await navigator.clipboard.writeText(text);
    state_ = "copied";
  } catch {
    const range = document.createRange();
    range.selectNodeContents(byId<HTMLElement>("out-code"));
    window.getSelection()?.removeAllRanges();
    window.getSelection()?.addRange(range);
  }
  copyButton.dataset["state"] = state_;
  byId<HTMLElement>("copy-label").textContent = t(state.lang, state_ === "copied" ? "copied" : "copyFailed");
  window.clearTimeout(copyTimer);
  copyTimer = window.setTimeout(() => {
    delete copyButton.dataset["state"];
    byId<HTMLElement>("copy-label").textContent = t(state.lang, "copy");
  }, 2000);
}

/* ---------- migration board ---------- */

function cell(...children: (Node | string)[]): HTMLTableCellElement {
  const td = document.createElement("td");
  td.append(...children);
  return td;
}

function renderBoard(): void {
  const progress = progressOf(demoRoutes);
  const share = Math.round((progress.done / progress.total) * 100);
  byId<SVGCircleElement>("ring-value").style.strokeDasharray = `${share} 100`;
  byId<HTMLElement>("progress-value").textContent = t(state.lang, "progressOf", { done: progress.done, total: progress.total });
  byId<HTMLElement>("stage-counts").replaceChildren(
    ...MODES.map((mode) => {
      const li = document.createElement("li");
      const label = document.createElement("span");
      label.textContent = t(state.lang, modeKey[mode]);
      const count = document.createElement("strong");
      count.textContent = String(progress.byMode[mode]);
      li.append(label, count);
      return li;
    }),
  );

  const rows = demoRoutes.map((route) => {
    const tr = document.createElement("tr");
    const name = document.createElement("div");
    name.className = "route-name";
    const title = document.createElement("strong");
    const key = routeNameKey[route.id];
    title.textContent = key ? t(state.lang, key) : route.id;
    const path = document.createElement("code");
    path.textContent = route.pathPattern.replace("{**rest}", "*");
    name.append(title, path);

    const track = document.createElement("div");
    track.className = "track";
    track.dataset["mode"] = route.mode;
    const position = MODES.indexOf(route.mode);
    for (let i = 0; i < MODES.length; i++) {
      const bar = document.createElement("i");
      bar.className = i < position ? "on" : i === position ? "now" : "";
      track.appendChild(bar);
    }
    const trackLabel = document.createElement("span");
    trackLabel.className = "track-label";
    trackLabel.textContent =
      route.mode === "canary" && route.canaryPercent !== null
        ? `${t(state.lang, modeKey[route.mode])} · ${t(state.lang, "canaryShare", { p: route.canaryPercent })}`
        : t(state.lang, modeKey[route.mode]);
    const trackWrap = document.createElement("div");
    trackWrap.append(track, trackLabel);

    const readiness = readinessOf(route);
    const ready = document.createElement("span");
    ready.className = `ready ready-${readiness}`;
    ready.textContent = t(state.lang, readinessKey[readiness]);
    if (route.finding === "rounding") {
      const small = document.createElement("small");
      small.textContent = t(state.lang, "findingRounding");
      ready.appendChild(small);
    }

    tr.append(cell(name), cell(trackWrap), cell(ready));
    return tr;
  });
  byId<HTMLElement>("board-rows").replaceChildren(...rows);
}

/* ---------- theme ---------- */

function renderTheme(): void {
  document.documentElement.dataset["theme"] = state.theme;
  document.querySelector('meta[name="theme-color"]')?.setAttribute("content", state.theme === "dark" ? "#060D1A" : "#EDF1F6");
  themeButton.setAttribute("aria-label", t(state.lang, state.theme === "dark" ? "themeLight" : "themeDark"));
}

/* ---------- route diagram ---------- */

function moveNode(name: keyof Layout["nodes"], box: NodeBox): void {
  const g = document.querySelector<SVGGElement>(`[data-node="${name}"]`);
  if (!g) return;
  g.setAttribute("transform", `translate(${box.x} ${box.y})`);
}

function setLine(id: string, from: { x: number; y: number }, to: { x: number; y: number }): void {
  const line = byId<SVGLineElement>(id);
  line.setAttribute("x1", String(from.x));
  line.setAttribute("y1", String(from.y));
  line.setAttribute("x2", String(to.x));
  line.setAttribute("y2", String(to.y));
}

function setText(id: string, at: { x: number; y: number }): void {
  const text = byId<SVGTextElement>(id);
  text.setAttribute("x", String(at.x));
  text.setAttribute("y", String(at.y));
}

function applyLayout(): void {
  layout = layoutFor(narrowScreen.matches);
  const stage = byId<SVGSVGElement>("stage");
  stage.setAttribute("viewBox", layout.viewBox);
  const bg = byId<SVGRectElement>("stage-bg");
  bg.setAttribute("width", String(layout.width));
  bg.setAttribute("height", String(layout.height));
  for (const [name, box] of Object.entries(layout.nodes) as [keyof Layout["nodes"], NodeBox][]) moveNode(name, box);
  const g = layout.nodes.gateway;
  const gatewayIn = narrowScreen.matches ? { x: layout.gateway.x, y: g.y } : { x: g.x, y: layout.gateway.y };
  const gatewayOut = narrowScreen.matches ? { x: layout.gateway.x, y: g.y + g.height } : { x: g.x + g.width, y: layout.gateway.y };
  setLine("wire-in", layout.client, gatewayIn);
  setLine("wire-modern", gatewayOut, layout.dest.modern);
  setLine("wire-legacy", gatewayOut, layout.dest.legacy);
  setText("label-orders", layout.labels.orders);
  setText("label-invoices", layout.labels.invoices);
  setText("wire-shadow-label", layout.labels.shadow);
  for (const p of packets) p.el.remove();
  packets.length = 0;
}

function renderRoute(): void {
  const status = routeStatus(state.percent);
  const mode = modeFor(state.percent, state.shadow);
  sliderOut.textContent = `${state.percent}%`;
  slider.value = String(state.percent);
  slider.setAttribute("aria-valuetext", t(state.lang, "sliderValueText", { p: state.percent }));
  ordersStatus.textContent = t(state.lang, statusKey[status]);
  ordersStatus.className = `chip chip-${status}`;
  const wireClass = mode === "shadow" ? "wire-shadow" : status === "legacy" ? "wire-legacy-state" : status === "transition" ? "wire-transition" : "wire-migrated";
  wireModern.setAttribute("class", `wire ${wireClass}`);
  wireShadowLabel.setAttribute("visibility", mode === "shadow" ? "visible" : "hidden");
  for (const button of modeButtons) {
    button.setAttribute("aria-pressed", String(button.dataset["mode"] === mode));
  }
  renderSplit();
}

/** Deterministic share for any 100 consecutive orders requests (REQ-DEMO-05). */
function renderSplit(): void {
  const modern = state.percent;
  const legacy = 100 - modern;
  byId<HTMLElement>("split-legacy").style.width = `${legacy}%`;
  byId<HTMLElement>("split-modern").style.width = `${modern}%`;
  byId<HTMLElement>("count-legacy").textContent = t(state.lang, "servedCount", { n: legacy });
  byId<HTMLElement>("count-modern").textContent = t(state.lang, "servedCount", { n: modern });
  byId<HTMLElement>("split").setAttribute(
    "aria-label",
    `${t(state.lang, "servedLegacy")} ${legacy}%, ${t(state.lang, "servedModern")} ${modern}%`,
  );
}

function selectMode(mode: Mode): void {
  state.percent = MODE_PERCENT[mode];
  state.shadow = mode === "shadow";
  renderRoute();
}

interface Packet {
  el: SVGGElement;
  target: Target;
  age: number;
  first: number;
  second: number;
}

const packets: Packet[] = [];
let spawned = 0;
let ordersSpawned = 0;
let sinceSpawn = SPAWN_MS;
let lastFrame = 0;
let frame = 0;

function distance(a: { x: number; y: number }, b: { x: number; y: number }): number {
  return Math.hypot(b.x - a.x, b.y - a.y);
}

function packetElement(isOrders: boolean, className: string): SVGGElement {
  const g = document.createElementNS(SVG_NS, "g");
  g.setAttribute("class", className);
  // Orders are circles and invoices are squares, so the route is readable without colour.
  const shape = isOrders ? document.createElementNS(SVG_NS, "circle") : document.createElementNS(SVG_NS, "rect");
  if (isOrders) {
    shape.setAttribute("r", "8");
  } else {
    shape.setAttribute("x", "-7");
    shape.setAttribute("y", "-7");
    shape.setAttribute("width", "14");
    shape.setAttribute("height", "14");
  }
  g.appendChild(shape);
  packetsLayer.appendChild(g);
  return g;
}

function push(el: SVGGElement, target: Target, age = 0): void {
  packets.push({ el, target, age, first: distance(layout.client, layout.gateway), second: distance(layout.gateway, layout.dest[target]) });
}

function spawn(): void {
  const isOrders = spawned % 2 === 0;
  spawned++;
  let target: Target = "legacy";
  if (isOrders) {
    target = targetFor(ordersSpawned, state.percent);
    ordersSpawned++;
  }
  push(packetElement(isOrders, target === "modern" ? "pkt-modern" : "pkt-legacy"), target);
  // Shadow mode: the monolith answers and a copy of the orders request is replayed to the modern API.
  if (isOrders && modeFor(state.percent, state.shadow) === "shadow") {
    push(packetElement(true, "pkt-shadow"), "modern", -120);
  }
}

function place(p: Packet): void {
  const travelled = Math.max(0, p.age) * SPEED;
  let x: number;
  let y: number;
  const { client, gateway } = layout;
  if (travelled <= p.first) {
    const k = travelled / p.first;
    x = client.x + (gateway.x - client.x) * k;
    y = client.y + (gateway.y - client.y) * k;
  } else {
    const k = Math.min(1, (travelled - p.first) / p.second);
    const to = layout.dest[p.target];
    x = gateway.x + (to.x - gateway.x) * k;
    y = gateway.y + (to.y - gateway.y) * k;
  }
  p.el.setAttribute("transform", `translate(${x.toFixed(1)} ${y.toFixed(1)})`);
}

function tick(now: number): void {
  const dt = Math.min(100, now - lastFrame);
  lastFrame = now;
  sinceSpawn += dt;
  if (sinceSpawn >= SPAWN_MS) {
    sinceSpawn = 0;
    spawn();
  }
  for (let i = packets.length - 1; i >= 0; i--) {
    const p = packets[i];
    if (!p) continue;
    p.age += dt;
    if (p.age * SPEED >= p.first + p.second) {
      p.el.remove();
      packets.splice(i, 1);
    } else {
      place(p);
    }
  }
  frame = requestAnimationFrame(tick);
}

function animating(): boolean {
  return !state.paused && !reducedMotion.matches;
}

function syncAnimation(): void {
  cancelAnimationFrame(frame);
  if (reducedMotion.matches) {
    for (const p of packets) p.el.remove();
    packets.length = 0;
  }
  if (animating()) {
    lastFrame = performance.now();
    frame = requestAnimationFrame(tick);
  }
  motionButton.hidden = reducedMotion.matches;
  renderMotionButton();
}

function renderMotionButton(): void {
  byId<HTMLElement>("motion-label").textContent = t(state.lang, state.paused ? "play" : "pause");
  motionButton.setAttribute("aria-pressed", String(state.paused));
}

/* ---------- shadow run panel ---------- */

function row(name: string, value: string, differs: boolean): HTMLElement {
  const wrap = document.createElement("div");
  wrap.className = differs ? "row diff" : "row";
  const dt = document.createElement("dt");
  dt.textContent = name;
  const dd = document.createElement("dd");
  dd.textContent = value;
  if (differs) {
    const marker = document.createElement("span");
    marker.className = "marker";
    marker.textContent = ` (${t(state.lang, "differsMarker")})`;
    dd.appendChild(marker);
  }
  wrap.append(dt, dd);
  return wrap;
}

function renderShadow(): void {
  const scenario = scenarios.find((s) => s.id === state.scenarioId) ?? scenarios[0];
  if (!scenario) return;
  const result = compare(scenario.legacy, scenario.modern);
  const differing = new Set(result.differences.map((d) => d.field));

  byId<HTMLElement>("request-path").textContent = scenario.requestPath;
  byId<HTMLElement>("legacy-rows").replaceChildren(
    ...comparedFields.map((f) => row(f.legacyName, f.legacy(scenario.legacy), differing.has(f.field))),
  );
  byId<HTMLElement>("modern-rows").replaceChildren(
    ...comparedFields.map((f) => row(f.modernName, f.modern(scenario.modern), differing.has(f.field))),
  );

  const matched = result.verdict === "Matched";
  const verdict = byId<HTMLElement>("verdict");
  verdict.textContent = t(state.lang, matched ? "verdictMatched" : "verdictDiscrepancy");
  verdict.className = `verdict verdict-${result.verdict.toLowerCase()}`;
  byId<HTMLElement>("summary").textContent = t(state.lang, "comparedSummary", {
    compared: result.compared,
    differ: result.differences.length,
  });
  const hint = byId<HTMLElement>("ready-hint");
  hint.textContent = t(state.lang, matched ? "readyHint" : "notReadyHint");
  hint.dataset["ready"] = matched ? "yes" : "no";

  const list = byId<HTMLElement>("differences");
  const items = result.differences.map((d) => {
    const li = document.createElement("li");
    li.textContent = t(state.lang, "differenceLine", { field: d.field, legacy: d.legacyValue, modern: d.modernValue });
    return li;
  });
  if (items.length === 0) {
    const li = document.createElement("li");
    li.textContent = t(state.lang, "differencesNone");
    items.push(li);
  }
  list.replaceChildren(...items);
}

/* ---------- wiring ---------- */

slider.addEventListener("input", () => {
  stopTour();
  state.percent = clampPercent(Number(slider.value));
  renderRoute();
});

for (const button of modeButtons) {
  button.addEventListener("click", () => {
    stopTour();
    const mode = button.dataset["mode"];
    if (MODES.includes(mode as Mode)) selectMode(mode as Mode);
    if (button.classList.contains("stage")) byId<HTMLElement>("diagram").scrollIntoView({ block: "start" });
  });
}

for (const step of document.querySelectorAll<HTMLAnchorElement>("[data-step-mode]")) {
  step.addEventListener("click", () => {
    const mode = step.dataset["stepMode"];
    if (MODES.includes(mode as Mode)) selectMode(mode as Mode);
  });
}

if (!reducedMotion.matches && "IntersectionObserver" in window) {
  document.documentElement.classList.add("reveal");
  const observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      }
    },
    { rootMargin: "0px 0px -10% 0px" },
  );
  const panels = document.querySelectorAll(".panel");
  for (const panel of panels) observer.observe(panel);
  // Nothing stays hidden: whatever the observer has not reached shows after a short wait.
  window.setTimeout(() => {
    for (const panel of panels) panel.classList.add("is-visible");
    observer.disconnect();
  }, 1200);
}

tourButton.addEventListener("click", () => {
  if (!tour.running) {
    startTour();
  } else if (reducedMotion.matches) {
    tourTick();
  } else {
    stopTour();
  }
});

byId<HTMLFormElement>("setup-form").addEventListener("input", renderSetup);
byId<HTMLFormElement>("setup-form").addEventListener("submit", (event) => event.preventDefault());
for (const button of outputButtons) {
  button.addEventListener("click", () => {
    output = button.dataset["out"] as Output;
    renderSetup();
  });
}
copyButton.addEventListener("click", () => void copyOutput());

motionButton.addEventListener("click", () => {
  state.paused = !state.paused;
  syncAnimation();
});

for (const button of langButtons) {
  button.addEventListener("click", () => {
    const lang = button.dataset["lang"];
    if (lang !== "es" && lang !== "en") return;
    state.lang = lang;
    writePref(LANG_KEY, lang);
    applyLang();
  });
}

themeButton.addEventListener("click", () => {
  state.theme = state.theme === "dark" ? "light" : "dark";
  writePref(THEME_KEY, state.theme);
  renderTheme();
});

prefersLight.addEventListener("change", () => {
  if (readPref(THEME_KEY)) return;
  state.theme = resolveTheme(null, prefersLight.matches);
  renderTheme();
});

for (const radio of document.querySelectorAll<HTMLInputElement>('input[name="scenario"]')) {
  radio.addEventListener("change", () => {
    if (radio.checked) {
      state.scenarioId = radio.value;
      renderShadow();
    }
  });
}

reducedMotion.addEventListener("change", syncAnimation);
narrowScreen.addEventListener("change", applyLayout);

byId<HTMLElement>("year").textContent = String(new Date().getFullYear());
applyLayout();
applyLang();
syncAnimation();
