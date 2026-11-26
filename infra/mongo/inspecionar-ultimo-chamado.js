// Mostra o último chamado e tudo que o pipeline gravou sobre ele. Somente leitura. Rode como helpdesk_app.
// DateTimeOffset do driver .NET pode vir como { DateTime, Ticks, Offset } (formato antigo) ou como Date (após MongoSerialization).
const fmt = (d) => (d?.toISOString ? d.toISOString() : d?.DateTime ? d.DateTime.toISOString() : String(d ?? "-"));

const ticket = db.tickets.find().sort({ created_at: -1 }).limit(1).toArray()[0];
if (!ticket) {
  print("Nenhum chamado encontrado em helpdesk_database.tickets.");
  quit();
}

print("=== CHAMADO ===");
printjson({
  protocol: ticket.protocol, tenant_id: ticket.tenant_id, status: ticket.status, severity: ticket.severity ?? null,
  queue_id: ticket.queue_id ?? null, assigned_to: ticket.assigned_to ?? null,
  created_at: ticket.created_at, sla_deadline: ticket.sla_deadline ?? null, version: ticket.version,
});

print("\n=== HISTÓRICO (ticket_history) ===");
db.ticket_history.find({ ticket_id: ticket._id }).sort({ occurred_at: 1 })
  .forEach((h) => print(`${fmt(h.occurred_at)}  ${h.change_type}  ${h.from_status ?? "-"} -> ${h.to_status ?? "-"}  (${h.actor})`));

print("\n=== OUTBOX (outbox_events) ===");
const events = db.outbox_events.find({ tenant_id: ticket.tenant_id }).sort({ occurred_at: 1 }).toArray();
events.forEach((e) => print(`${e.event_type}  status=${e.status}  tentativas=${e.attempts}${e.last_error ? "  erro=" + e.last_error : ""}`));

print("\n=== INBOX (processed_messages) ===");
db.processed_messages.find().sort({ processed_at: 1 })
  .forEach((p) => print(`${p.consumer}  evento=${p._id.substring(0, 8)}...`));

const expected = {
  "classificado": ticket.severity != null,
  "despachado": ticket.queue_id != null,
  "status final EmAtendimento": ticket.status === "EmAtendimento",
  "SLA calculado": ticket.sla_deadline != null,
  "outbox sem pendência": events.length > 0 && events.every((e) => e.status === "published"),
};
print("\n=== RESULTADO ===");
Object.entries(expected).forEach(([name, ok]) => print(`${ok ? "OK    " : "FALHA "} ${name}`));
