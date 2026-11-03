/** Diagram geometry for wide screens (left to right) and narrow screens (top to bottom). */
export interface Point {
  x: number;
  y: number;
}

export interface NodeBox {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface Layout {
  viewBox: string;
  width: number;
  height: number;
  nodes: { client: NodeBox; gateway: NodeBox; modern: NodeBox; legacy: NodeBox };
  /** Packet waypoints: client exit, gateway centre, destination entry. */
  client: Point;
  gateway: Point;
  dest: { modern: Point; legacy: Point };
  labels: { orders: Point; invoices: Point; shadow: Point };
}

export const wide: Layout = {
  viewBox: "0 0 720 360",
  width: 720,
  height: 360,
  nodes: {
    client: { x: 20, y: 140, width: 110, height: 80 },
    gateway: { x: 230, y: 130, width: 130, height: 100 },
    modern: { x: 540, y: 24, width: 170, height: 92 },
    legacy: { x: 540, y: 244, width: 170, height: 92 },
  },
  client: { x: 130, y: 180 },
  gateway: { x: 295, y: 180 },
  dest: { modern: { x: 540, y: 70 }, legacy: { x: 540, y: 290 } },
  labels: { orders: { x: 372, y: 96 }, invoices: { x: 372, y: 304 }, shadow: { x: 400, y: 186 } },
};

export const narrow: Layout = {
  viewBox: "0 0 360 560",
  width: 360,
  height: 560,
  nodes: {
    client: { x: 125, y: 16, width: 110, height: 80 },
    gateway: { x: 115, y: 176, width: 130, height: 100 },
    modern: { x: 8, y: 452, width: 165, height: 92 },
    legacy: { x: 187, y: 452, width: 165, height: 92 },
  },
  client: { x: 180, y: 96 },
  gateway: { x: 180, y: 226 },
  dest: { modern: { x: 90, y: 452 }, legacy: { x: 270, y: 452 } },
  labels: { orders: { x: 14, y: 360 }, invoices: { x: 236, y: 360 }, shadow: { x: 14, y: 320 } },
};

export function layoutFor(isNarrow: boolean): Layout {
  return isNarrow ? narrow : wide;
}

export function inside(box: NodeBox, layout: Layout): boolean {
  return box.x >= 0 && box.y >= 0 && box.x + box.width <= layout.width && box.y + box.height <= layout.height;
}
