// Cria a database do Help Desk no MongoDB do servidor (ADR-003). Idempotente: pode rodar mais de uma vez.
// Como usar: Compass > conectar (SSH + usuário admin) > abrir o MONGOSH > colar este arquivo inteiro.
// A aplicação também cria os índices ao iniciar (MongoBootstrap); se criar aqui, mantenha estes nomes.

const dbName = "helpdesk_database";
const hd = db.getSiblingDB(dbName);

// 0. Diagnóstico: transações (Outbox) exigem replica set. Se setName vier vazio, o servidor é standalone.
const hello = hd.hello();
print(`Servidor: ${hello.me} | replica set: ${hello.setName ?? "NENHUM (standalone)"} | primary: ${hello.isWritablePrimary}`);
if (!hello.setName) {
  print("ATENÇÃO: sem replica set, abrir chamado falha (transação). Veja a seção 'Replica set' do guia.");
}

// 1. Coleções (checklist B06 + sequência de protocolo B08)
const collections = [
  "tickets", "ticket_history", "ticket_comments", "ticket_attachments", "ticket_categories",
  "support_queues", "support_teams", "sla_policies", "outbox_events", "processed_messages", "protocol_sequences",
];
const existing = hd.getCollectionNames();
collections.filter((name) => !existing.includes(name)).forEach((name) => hd.createCollection(name));

// 2. Índices (mesmos nomes e chaves do MongoBootstrap.cs)
hd.tickets.createIndex({ tenant_id: 1, created_at: 1 }, { name: "ix_tickets_tenant_created" });
hd.tickets.createIndex({ tenant_id: 1, status: 1, updated_at: 1 }, { name: "ix_tickets_tenant_status_updated" });
hd.tickets.createIndex({ tenant_id: 1, "requester.external_user_id": 1, created_at: 1 }, { name: "ix_tickets_tenant_requester_created" });
hd.tickets.createIndex({ tenant_id: 1, assigned_to: 1, status: 1 }, { name: "ix_tickets_tenant_assignee_status" });
hd.tickets.createIndex({ tenant_id: 1, queue_id: 1, status: 1 }, { name: "ix_tickets_tenant_queue_status" });
hd.tickets.createIndex({ protocol: 1 }, { name: "ux_tickets_protocol", unique: true });
hd.tickets.createIndex({ idempotency_key: 1 }, { name: "ux_tickets_idempotency", unique: true, sparse: true });
hd.ticket_history.createIndex({ tenant_id: 1, ticket_id: 1, occurred_at: 1 }, { name: "ix_history_tenant_ticket_time" });
hd.outbox_events.createIndex({ status: 1, next_attempt_at: 1 }, { name: "ix_outbox_status_next" });
hd.ticket_comments.createIndex({ tenant_id: 1, ticket_id: 1, created_at: 1 }, { name: "ix_comments_tenant_ticket_time" });
hd.ticket_attachments.createIndex({ tenant_id: 1, ticket_id: 1 }, { name: "ix_attachments_tenant_ticket" });

// 3. Matriz de SLA ativa (ADR-008). Só insere se ainda não existir.
hd.sla_policies.updateOne(
  { _id: "active" },
  { $setOnInsert: { rules_version: "1", targets_hours: { "Crítica": 2, "Alta": 6, "Média": 24, "Baixa": 48 } } },
  { upsert: true },
);

// 4. Fila padrão do dispatch (B17): recebe toda categoria sem fila própria. Agentes reais: configurar-filas.js.
hd.support_queues.updateOne(
  { _id: "suporte-geral" },
  { $setOnInsert: { category_ids: [] } },
  { upsert: true },
);
hd.support_teams.updateOne(
  { _id: "suporte-geral" },
  { $setOnInsert: { agents: [], next_index: NumberLong(0) } },
  { upsert: true },
);

// 5. Usuário da aplicação com privilégio mínimo (ADR-003): só readWrite na database do Help Desk.
//    A senha é pedida no terminal. Se o seu MONGOSH não aceitar passwordPrompt(), troque por uma string digitada localmente (não salve em arquivo).
const admin = db.getSiblingDB("admin");
if (!admin.getUser("helpdesk_app")) {
  admin.createUser({
    user: "helpdesk_app",
    pwd: passwordPrompt(),
    roles: [{ role: "readWrite", db: dbName }],
  });
  print("Usuário helpdesk_app criado em 'admin' (authSource=admin).");
} else {
  print("Usuário helpdesk_app já existe; nada alterado.");
}

print(`Database ${dbName} pronta. Coleções: ${hd.getCollectionNames().sort().join(", ")}`);
