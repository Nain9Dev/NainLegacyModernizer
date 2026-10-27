import { clampPercent, routeStatus, targetFor, type RouteStatus, type Target } from "./routing.ts";
import { comparedFields, compare } from "./parity.ts";
import { scenarios } from "./fixtures.ts";
import { detectLang, t, type Key, type Lang } from "./i18n.ts";

const SVG_NS = "http://www.w3.org/2000/svg";

// Coordinates match the viewBox in index.html.
const CLIENT = { x: 130, y: 180 };
const GATEWAY = { x: 295, y: 180 };
const DEST = { modern: { x: 540, y: 75 }, legacy: { x: 540, y: 285 } } as const;
const SPEED = 0.22; // px per ms
const SPAWN_MS = 600;

const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

function byId<T extends Element>(id: string): T {
  const el = document.getElementById(id);
  if (!el) throw new Error(`Missing element #${id}`);
  return el as unknown as T;
}

const state = {
  lang: detectLang(navigator.languages?.length ? navigator.languages : [navigator.language]) as Lang,
  percent: 50,
  paused: false,
  scenarioId: scenarios[0]?.id ?? "ORD-001",
};

const slider = byId<HTMLInputElement>("migrated");
const sliderOut = byId<HTMLOutputElement>("migrated-out");
const ordersStatus = byId<HTMLElement>("orders-status");
const wireModern = byId<SVGLineElement>("wire-modern");
const packetsLayer = byId<SVGGElement>("packets");
const motionButton = byId<HTMLButtonElement>("motion");
const langButton = byId<HTMLButtonElement>("lang");

const statusKey: Record<RouteStatus, Key> = {
  legacy: "statusLegacy",
  transition: "statusTransition",
  migrated: "statusMigrated",
};

/* ---------- language ---------- */

function applyLang(): void {
  document.documentElement.lang = state.lang;
  document.title = t(state.lang, "pageTitle");
  for (const el of document.querySelectorAll<HTMLElement>("[data-i18n]")) {
    el.textContent = t(state.lang, el.dataset["i18n"] as Key);
    const aria = el.dataset["i18nAria"];
    if (aria) el.setAttribute("aria-label", t(state.lang, aria as Key));
  }
  langButton.lang = state.lang === "en" ? "es" : "en";
  renderRoute();
  renderMotionButton();
  renderShadow();
}

/* ---------- route diagram ---------- */

function renderRoute(): void {
  const status = routeStatus(state.percent);
  sliderOut.textContent = `${state.percent}%`;
  slider.setAttribute("aria-valuetext", t(state.lang, "sliderValueText", { p: state.percent }));
  ordersStatus.textContent = t(state.lang, statusKey[status]);
  ordersStatus.className = `chip chip-${status}`;
  wireModern.setAttribute(
    "class",
    `wire ${status === "legacy" ? "wire-legacy-state" : status === "transition" ? "wire-transition" : "wire-migrated"}`,
  );
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

function spawn(): void {
  const isOrders = spawned % 2 === 0;
  spawned++;
  let target: Target = "legacy";
  if (isOrders) {
    target = targetFor(ordersSpawned, state.percent);
    ordersSpawned++;
  }
  const g = document.createElementNS(SVG_NS, "g");
  g.setAttribute("class", target === "modern" ? "pkt-modern" : "pkt-legacy");
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
  packets.push({
    el: g,
    target,
    age: 0,
    first: distance(CLIENT, GATEWAY),
    second: distance(GATEWAY, DEST[target]),
  });
}

function place(p: Packet): void {
  const travelled = p.age * SPEED;
  let x: number;
  let y: number;
  if (travelled <= p.first) {
    const k = travelled / p.first;
    x = CLIENT.x + (GATEWAY.x - CLIENT.x) * k;
    y = CLIENT.y + (GATEWAY.y - CLIENT.y) * k;
  } else {
    const k = Math.min(1, (travelled - p.first) / p.second);
    const to = DEST[p.target];
    x = GATEWAY.x + (to.x - GATEWAY.x) * k;
    y = GATEWAY.y + (to.y - GATEWAY.y) * k;
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
  motionButton.textContent = t(state.lang, state.paused ? "play" : "pause");
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

  const legacyRows = byId<HTMLElement>("legacy-rows");
  const modernRows = byId<HTMLElement>("modern-rows");
  legacyRows.replaceChildren(
    ...comparedFields.map((f) => row(f.legacyName, f.legacy(scenario.legacy), differing.has(f.field))),
  );
  modernRows.replaceChildren(
    ...comparedFields.map((f) => row(f.modernName, f.modern(scenario.modern), differing.has(f.field))),
  );

  const verdict = byId<HTMLElement>("verdict");
  verdict.textContent = t(state.lang, result.verdict === "Matched" ? "verdictMatched" : "verdictDiscrepancy");
  verdict.className = `verdict verdict-${result.verdict.toLowerCase()}`;
  byId<HTMLElement>("summary").textContent = t(state.lang, "comparedSummary", {
    compared: result.compared,
    differ: result.differences.length,
  });

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
  state.percent = clampPercent(Number(slider.value));
  renderRoute();
});

motionButton.addEventListener("click", () => {
  state.paused = !state.paused;
  syncAnimation();
});

langButton.addEventListener("click", () => {
  state.lang = state.lang === "en" ? "es" : "en";
  applyLang();
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

slider.value = String(state.percent);
applyLang();
syncAnimation();
