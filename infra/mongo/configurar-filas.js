// Ajusta a fila padrão e os agentes num banco já criado. Idempotente. Rode como helpdesk_app ou hd_admin.
// Categoria sem fila própria já cai em "suporte-geral" (TeamRoster.cs). Sem agentes, o chamado espera na fila.

// Preencha com o "sub" do token de cada agente (o unique id do usuário no Bubble). Vazio = sem atribuição automática.
const AGENTES = [];

const FILA_PADRAO = "suporte-geral";

if (db.getName() !== "helpdesk_database") {
  print("Recusado: conecte na helpdesk_database.");
} else {
  db.support_queues.updateOne({ _id: FILA_PADRAO }, { $setOnInsert: { category_ids: [] } }, { upsert: true });
  db.support_teams.updateOne(
    { _id: FILA_PADRAO },
    { $set: { agents: AGENTES }, $setOnInsert: { next_index: NumberLong(0) } },
    { upsert: true },
  );
  print(`Fila ${FILA_PADRAO}: ${AGENTES.length} agente(s) no rodízio.`);
}
