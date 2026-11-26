// APAGA os dados operacionais de TESTE do Help Desk (chamados, histórico, comentários, anexos, outbox, inbox, sequência de protocolo).
// NÃO apaga a configuração: sla_policies, support_queues, support_teams e ticket_categories ficam intactas.
// Use só em desenvolvimento. Para executar de verdade, troque a constante abaixo para true e rode como hd_admin ou helpdesk_app.
const APAGAR_DADOS_DE_TESTE = false;

const operational = [
  "tickets", "ticket_history", "ticket_comments", "ticket_attachments",
  "outbox_events", "processed_messages", "protocol_sequences", "attachments.files", "attachments.chunks",
];

print(`Database: ${db.getName()}`);
operational.forEach((name) => print(`${name}: ${db.getCollection(name).countDocuments()} documento(s)`));

if (!APAGAR_DADOS_DE_TESTE) {
  print("\nNada foi apagado. Para apagar, edite APAGAR_DADOS_DE_TESTE para true e rode de novo.");
} else if (db.getName() !== "helpdesk_database") {
  print("\nRecusado: conecte na helpdesk_database.");
} else {
  operational.forEach((name) => db.getCollection(name).deleteMany({}));
  print("\nDados de teste apagados. A numeração de protocolo recomeça em SUP-AAAA-000001.");
}
