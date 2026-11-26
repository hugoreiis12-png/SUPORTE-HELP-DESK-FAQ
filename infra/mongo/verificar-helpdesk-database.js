// Verifica se o banco do Help Desk está pronto para a API. Rode autenticado como helpdesk_app (o usuário da aplicação).
const expectedCollections = [
  "tickets", "ticket_history", "ticket_comments", "ticket_attachments", "ticket_categories",
  "support_queues", "support_teams", "sla_policies", "outbox_events", "processed_messages", "protocol_sequences",
];
const expectedTicketIndexes = [
  "ix_tickets_tenant_created", "ix_tickets_tenant_status_updated", "ix_tickets_tenant_requester_created",
  "ix_tickets_tenant_assignee_status", "ix_tickets_tenant_queue_status", "ux_tickets_protocol", "ux_tickets_idempotency",
];

const checks = [];
const check = (name, ok, detail = "") => checks.push({ name, ok, detail });

const hello = db.hello();
check("replica set rs0", hello.setName === "rs0", `setName=${hello.setName}`);
check("nó é primary", hello.isWritablePrimary === true);

const present = db.getCollectionNames();
const missing = expectedCollections.filter((name) => !present.includes(name));
check("11 coleções criadas", missing.length === 0, missing.length ? `faltam: ${missing.join(", ")}` : "");

const indexNames = db.tickets.getIndexes().map((index) => index.name);
const missingIndexes = expectedTicketIndexes.filter((name) => !indexNames.includes(name));
check("7 índices de tickets", missingIndexes.length === 0, missingIndexes.length ? `faltam: ${missingIndexes.join(", ")}` : "");

const sla = db.sla_policies.findOne({ _id: "active" });
check("matriz de SLA ativa", !!sla && sla.targets_hours["Crítica"] === 2 && sla.targets_hours["Baixa"] === 48);
const ficticios = db.support_teams.countDocuments({ agents: "agente-1" });
check("sem agente fictício no rodízio", ficticios === 0, ficticios ? "rode configurar-filas.js" : "");

// Escrita real (a API grava em transação): insere e remove um documento de teste dentro de uma transação.
const session = db.getMongo().startSession();
try {
  session.startTransaction();
  const probe = session.getDatabase(db.getName()).processed_messages;
  probe.insertOne({ _id: "__verificacao__", consumer: "verificar", processed_at: new Date() });
  session.abortTransaction();
  check("transação funciona", true);
} catch (error) {
  check("transação funciona", false, String(error.message ?? error));
} finally {
  session.endSession();
}

checks.forEach((c) => print(`${c.ok ? "OK    " : "FALHA "} ${c.name}${c.detail ? "  -> " + c.detail : ""}`));
print(checks.every((c) => c.ok) ? "\nBANCO PRONTO PARA A API" : "\nHÁ ITENS COM FALHA: corrija antes de subir a API");
