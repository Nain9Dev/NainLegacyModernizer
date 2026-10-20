/**
 * Synthetic scenarios copied from tests/NainLegacyModernizer.Tests/ShadowAuditorTests.cs.
 * Legacy shape mirrors `LegacyDto(OrderId, Total, Currency)`, modern shape mirrors
 * `ModernDto(Id, TotalAmount, Currency)`. Totals are kept as decimal strings so no
 * floating point is involved.
 */
export interface LegacyOrder {
  OrderId: string;
  Total: string;
  Currency: string;
}

export interface ModernOrder {
  id: string;
  totalAmount: string;
  currency: string;
}

export interface Scenario {
  id: string;
  requestPath: string;
  legacy: LegacyOrder;
  modern: ModernOrder;
}

export const scenarios: readonly Scenario[] = [
  {
    id: "ORD-001",
    requestPath: "/api/orders/ORD-001",
    legacy: { OrderId: "ORD-001", Total: "100.00", Currency: "EUR" },
    modern: { id: "ORD-001", totalAmount: "100.00", currency: "EUR" },
  },
  {
    id: "ORD-002",
    requestPath: "/api/orders/ORD-002",
    legacy: { OrderId: "ORD-002", Total: "99.99", Currency: "EUR" },
    modern: { id: "ORD-002", totalAmount: "100.00", currency: "EUR" },
  },
];
