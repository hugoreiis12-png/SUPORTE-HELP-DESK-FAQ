# Arquitetura pretendida --- Help Desk do Comprove

**Documento de arquitetura e plano de evolução**\
**Versão:** 0.1 --- proposta inicial\
**Data:** 23/11/2026\
**Status:** Arquitetura em definição; decisões pendentes explicitadas ao
longo do documento.

------------------------------------------------------------------------

## 1. Objetivo deste documento

Este documento consolida o entendimento atual e a arquitetura pretendida
para o novo serviço de Help Desk integrado ao ERP Comprove. Ele
registra:

-   o contexto e os objetivos do projeto;
-   as decisões já informadas;
-   a arquitetura lógica proposta;
-   os limites de responsabilidade entre Bubble e o backend .NET;
-   a estratégia de persistência, mensageria e processamento assíncrono;
-   os domínios funcionais e o modelo de dados inicial;
-   requisitos transversais de segurança, escalabilidade e
    observabilidade;
-   as decisões que ainda precisam ser validadas;
-   a sequência recomendada de trabalho até a implementação e operação.

O documento é uma **base de arquitetura**, não uma especificação final
de implementação. Onde ainda não houve decisão, isso está indicado como
**pendente de validação**. Não se deve tratar tecnologias candidatas ou
exemplos de modelagem como escolhas irrevogáveis.

## 2. Contexto do projeto

O Help Desk fará parte do ecossistema do ERP Comprove, cuja camada de
interface atual utiliza Bubble.

A decisão arquitetural informada é que o Help Desk será um **serviço
independente, executado em infraestrutura externa**, com backend próprio
em C# utilizando .NET. O Bubble será a camada de frontend do Comprove e
consumirá os recursos do Help Desk por meio de uma API.

O serviço deverá ser projetado para um cenário de escala elevado, com
possibilidade de múltiplas empresas, departamentos, filas e equipes de
atendimento.

### 2.1. Premissas confirmadas pelo responsável pelo projeto

  Tema              Premissa confirmada
  ----------------- -----------------------------------------------------
  ERP atual         Comprove, desenvolvido na plataforma Bubble
  Papel do Bubble   Camada de frontend da aplicação
  Help Desk         Serviço independente, hospedado em servidor externo
  Backend           C# com framework .NET
  Arquitetura       Microserviço independente em relação ao ERP
  Processamento     Assíncrono por Workers
  Banco de dados    MongoDB compartilhado, conforme premissa informada
  Escala esperada   Grande

### 2.2. Interpretação arquitetural

"Microserviço independente" significa que o Help Desk terá implantação,
API, regras de negócio e ciclo de evolução próprios. Isso **não
implica** que cada domínio interno do Help Desk deva ser separado em um
microserviço diferente.

A recomendação inicial é manter o Help Desk como um serviço coeso,
organizado internamente por módulos e limites de domínio. A API e os
Workers poderão ser executados em processos ou containers distintos,
compartilhando os mesmos componentes de domínio e aplicação.

### 2.3. Objetivos

1.  Permitir abertura e acompanhamento de chamados a partir do Comprove.
2.  Disponibilizar uma estrutura própria para a equipe de suporte
    administrar e resolver chamados.
3.  Separar as regras de negócio do Help Desk da implementação visual do
    Bubble.
4.  Processar atividades demoradas ou independentes de forma assíncrona.
5.  Manter histórico e rastreabilidade das ações realizadas nos
    chamados.
6.  Suportar múltiplas empresas e equipes, com isolamento de dados e
    permissões.
7.  Permitir evolução e eventual expansão do serviço sem reescrever suas
    regras centrais.

## 3. Escopo funcional identificado no fluxo inicial

O diagrama inicial apresentado descreve, em linhas gerais, o seguinte
fluxo:

1.  O usuário acessa a interface do Comprove.
2.  O usuário preenche e envia uma solicitação de suporte.
3.  A solicitação é recebida pelo backend do Help Desk.
4.  O backend valida e registra o chamado.
5.  O processamento assíncrono pode classificar, organizar ou encaminhar
    o chamado.
6.  A equipe de suporte consulta os chamados em uma interface
    administrativa.
7.  O responsável atua no chamado e registra interações e/ou resolução.
8.  O solicitante acompanha o andamento e a resolução no Comprove.

A interpretação dos detalhes do desenho original é parcial. Portanto,
regras específicas de prioridade, filas, distribuição automática, etapas
de aprovação e critérios de encerramento ainda precisam ser confirmadas
com o responsável pelo processo.

## 4. Princípios arquiteturais

A solução deverá seguir estes princípios:

-   **Backend como autoridade:** regras de negócio, validações,
    autorização e persistência pertencem ao Help Desk, não ao frontend
    Bubble.
-   **Contratos explícitos:** Bubble e Help Desk se comunicam por APIs e
    contratos versionados, sem acesso direto às collections internas do
    serviço.
-   **Baixo acoplamento:** o Help Desk não deve depender de detalhes
    internos da implementação do Bubble.
-   **Modularidade interna:** separar domínios e responsabilidades
    dentro do serviço, sem criar microsserviços prematuramente.
-   **Assincronismo onde fizer sentido:** notificações, distribuição,
    classificação e tarefas demoradas não devem bloquear
    desnecessariamente a resposta da API.
-   **Consistência e resiliência:** prever falhas de mensageria,
    repetição de mensagens, idempotência e rastreabilidade.
-   **Segurança por padrão:** identidade verificada no servidor,
    autorização por operação e isolamento por empresa/tenant.
-   **Evolução incremental:** começar com uma arquitetura
    operacionalmente simples, preparada para crescer com métricas e
    necessidades reais.

## 5. Arquitetura lógica proposta

``` text
                         COMPROVE — BUBBLE
                  Frontend / experiência do usuário
                                |
                                | HTTPS / REST / JSON
                                v
                     HELP DESK — BACKEND .NET
                  +-----------------------------+
                  |          HelpDesk.Api       |
                  |                             |
                  |  Tickets / Workflow         |
                  |  Assignment / SLA           |
                  |  Comments / Attachments     |
                  |  Authorization / Contracts  |
                  +--------------+--------------+
                                 |
                    +------------+------------+
                    |                         |
                    v                         v
             MongoDB                         Outbox
       Database do Help Desk                   |
                                               v
                                      Publicador de eventos
                                               |
                                               v
                                          Message Broker
                                               |
                         +---------------------+-------------------+
                         |                     |                   |
                         v                     v                   v
                   Worker de             Worker de           Worker de
                   notificações          distribuição        SLA / tarefas
                         |                     |                   |
                         +---------------------+-------------------+
                                               |
                                               v
                                      Atualizações no MongoDB
```

Esse desenho é lógico. A topologia física, a quantidade de servidores,
os containers e a tecnologia final do broker dependem da infraestrutura
disponível e de requisitos de disponibilidade, volume e operação.

### 5.1. Componentes

  -----------------------------------------------------------------------
  Componente                          Responsabilidade
  ----------------------------------- -----------------------------------
  Bubble / Comprove                   Interface e experiência do usuário;
                                      coleta de dados e consumo da API

  HelpDesk.Api                        Entrada HTTP, autenticação,
                                      autorização, validação e
                                      orquestração de casos de uso

  Domain                              Entidades, regras e invariantes de
                                      negócio

  Application                         Casos de uso, coordenação dos
                                      domínios e interfaces necessárias

  Infrastructure                      MongoDB, mensageria, armazenamento
                                      de arquivos e integrações externas

  HelpDesk.Worker                     Consumo de mensagens e execução de
                                      tarefas em segundo plano

  MongoDB                             Persistência dos dados do Help Desk

  Message Broker                      Transporte e distribuição de
                                      eventos e comandos assíncronos

  Outbox Publisher                    Publicação confiável dos eventos
                                      registrados na Outbox

  Object Storage                      Armazenamento de anexos e arquivos,
                                      se adotado
  -----------------------------------------------------------------------

## 6. Stack tecnológica pretendida

### 6.1. Backend

-   **Linguagem:** C#.
-   **Framework:** .NET / ASP.NET Core.
-   **API:** ASP.NET Core Web API.
-   **Workers:** .NET Worker Service, usando `BackgroundService` ou
    `IHostedService` quando adequado.
-   **Documentação de API:** OpenAPI/Swagger.
-   **Persistência:** driver oficial `MongoDB.Driver`.

A versão do .NET ainda deve ser escolhida no início da implementação,
considerando suporte vigente, compatibilidade de bibliotecas e ambiente
de hospedagem.

Não há decisão de utilizar Entity Framework Core. Para MongoDB, a
proposta inicial é utilizar o driver oficial diretamente, encapsulado em
repositórios e serviços de persistência quando isso trouxer valor.

### 6.2. Mensageria

**RabbitMQ é a primeira opção candidata**, ainda não uma decisão
definitiva.

A seleção deve considerar:

-   ambiente de hospedagem e operação;
-   necessidade de alta disponibilidade;
-   volume e taxa de mensagens;
-   garantias de entrega e confirmação;
-   suporte a filas de erro e dead-letter;
-   facilidade de monitoramento;
-   experiência operacional da equipe.

Se a infraestrutura for baseada em nuvem e oferecer mensageria
gerenciada adequada, comparar essa opção com RabbitMQ antes de fechar a
escolha.

### 6.3. Infraestrutura

-   Docker para empacotamento e execução consistente.
-   API e Worker em processos/containers independentes.
-   Pipeline de build, testes e deploy.
-   Segredos e configurações fora do código-fonte.
-   Reverse proxy ou gateway HTTPS, conforme infraestrutura.
-   Monitoramento de logs, métricas e traces.

A plataforma de hospedagem, sistema operacional, orquestrador e provedor
de Object Storage permanecem pendentes.

## 7. Organização da Solution .NET

A proposta inicial é manter uma única Solution e separar projetos por
responsabilidade:

``` text
Comprove.HelpDesk/
│
├── Comprove.HelpDesk.sln
│
├── src/
│   ├── Comprove.HelpDesk.Api/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Authentication/
│   │   ├── Authorization/
│   │   └── Program.cs
│   │
│   ├── Comprove.HelpDesk.Worker/
│   │   ├── Consumers/
│   │   ├── HostedServices/
│   │   └── Program.cs
│   │
│   ├── Comprove.HelpDesk.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Events/
│   │   ├── ValueObjects/
│   │   └── Exceptions/
│   │
│   ├── Comprove.HelpDesk.Application/
│   │   ├── Tickets/
│   │   ├── Workflow/
│   │   ├── Assignment/
│   │   ├── Comments/
│   │   ├── Notifications/
│   │   ├── SLA/
│   │   └── Common/
│   │
│   ├── Comprove.HelpDesk.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Repositories/
│   │   ├── Messaging/
│   │   ├── Outbox/
│   │   ├── Storage/
│   │   └── ExternalServices/
│   │
│   └── Comprove.HelpDesk.Contracts/
│       ├── Requests/
│       ├── Responses/
│       └── Events/
│
├── tests/
│   ├── Comprove.HelpDesk.UnitTests/
│   ├── Comprove.HelpDesk.IntegrationTests/
│   └── Comprove.HelpDesk.ArchitectureTests/
│
├── deploy/
│   ├── docker/
│   └── scripts/
│
└── docs/
```

### 7.1. Regras de dependência

-   `Domain` não depende de Infrastructure, API ou MongoDB.
-   `Application` depende de Domain e define os casos de uso e
    abstrações necessárias.
-   `Infrastructure` implementa as abstrações de persistência,
    mensageria e serviços externos.
-   `Api` e `Worker` são pontos de entrada e compõem as dependências por
    injeção de dependência.
-   `Contracts` contém contratos de comunicação estáveis; não deve se
    tornar um depósito de entidades internas do domínio.

A organização poderá ser ajustada conforme a quantidade real de módulos.
Evitar criar projetos ou abstrações sem responsabilidade concreta.

## 8. Integração entre Bubble e Help Desk

### 8.1. Modelo de comunicação

O Bubble consumirá a API do Help Desk por HTTPS, utilizando requisições
REST e payloads JSON.

O Bubble não deverá:

-   acessar diretamente o MongoDB;
-   conhecer nomes de collections;
-   manipular regras internas de workflow;
-   determinar sozinho se um usuário tem permissão;
-   publicar diretamente eventos internos do Help Desk;
-   possuir credenciais administrativas do banco ou do broker.

O Help Desk deverá expor operações de negócio, e não uma API genérica
para manipular documentos.

### 8.2. Responsabilidades do Bubble

-   Exibir formulários e telas.
-   Coletar dados do solicitante.
-   Apresentar chamados e informações retornadas pela API.
-   Enviar comandos do usuário, como abrir chamado ou adicionar
    comentário.
-   Exibir status, protocolo, histórico permitido e resolução.
-   Manter a experiência visual integrada ao Comprove.

### 8.3. Responsabilidades do Help Desk

-   Validar a identidade recebida.
-   Autorizar cada operação.
-   Validar e normalizar os dados.
-   Aplicar regras de negócio.
-   Gerar protocolo e identificadores.
-   Persistir dados.
-   Registrar histórico e eventos.
-   Publicar eventos assíncronos.
-   Processar atribuição, notificações e SLA.
-   Controlar acesso aos chamados e anexos.

### 8.4. Contratos e versionamento

Adotar uma rota versionada, por exemplo:

``` text
/api/v1/tickets
/api/v1/tickets/{ticketId}
/api/v1/tickets/{ticketId}/comments
/api/v1/tickets/{ticketId}/history
/api/v1/tickets/{ticketId}/resolve
```

Os exemplos são ilustrativos. Os endpoints definitivos devem ser
derivados dos casos de uso e das regras de autorização.

Os contratos públicos devem usar DTOs próprios, validação consistente e
respostas de erro padronizadas. Alterações incompatíveis devem exigir
nova versão ou estratégia de compatibilidade documentada.

## 9. Autenticação e autorização

A integração de identidade é uma das decisões mais importantes ainda
pendentes.

O usuário espera acessar o Help Desk com a identidade utilizada no
Comprove, mas isso não significa que o Help Desk deva confiar em
informações enviadas livremente pelo navegador.

### 9.1. Princípio obrigatório

Nunca confiar em `userId`, `companyId`, `tenantId`, `role` ou `isAdmin`
simplesmente porque esses valores chegaram no JSON do frontend.

A identidade e as permissões devem ser verificadas no servidor, por um
mecanismo criptograficamente confiável ou por consulta a uma fonte de
identidade confiável.

### 9.2. Fluxo a definir

Uma possibilidade é:

1.  O usuário autentica no Comprove/Bubble.
2.  Um mecanismo confiável de backend valida a identidade do usuário.
3.  O Help Desk recebe uma credencial assinada, curta e destinada
    especificamente à API do Help Desk, ou valida a identidade por
    integração servidor-servidor.
4.  O Help Desk valida assinatura, emissor, audiência, expiração e
    demais claims necessários.
5.  O Help Desk resolve as permissões e o contexto de empresa/tenant.
6.  Cada endpoint verifica se a ação é permitida para aquele usuário e
    recurso.

O mecanismo exato --- por exemplo, provedor de identidade compartilhado,
token delegado ou troca de credenciais servidor-servidor --- deve ser
escolhido após avaliar os recursos de autenticação disponíveis no Bubble
e a forma atual de autenticação do Comprove.

Não expor token administrativo do Bubble no navegador. Não usar
credenciais administrativas como mecanismo de autenticação de usuários
finais.

### 9.3. Autorização

A autorização deverá considerar, no mínimo:

-   usuário;
-   empresa/tenant;
-   perfil ou papel;
-   equipe e fila;
-   relação do usuário com o chamado;
-   ação solicitada;
-   estado atual do chamado.

A autorização deve ocorrer no backend em todas as operações relevantes,
inclusive leitura, download de anexos, comentários e histórico.

## 10. Persistência MongoDB

### 10.1. Estratégia de compartilhamento

A premissa informada é que o MongoDB será compartilhado. A recomendação
é compartilhar, se adequado, o **cluster**, mas manter uma database
lógica exclusiva para o Help Desk.

Exemplo:

``` text
MongoDB Cluster
│
├── erp_database
│   └── dados do ERP
│
├── helpdesk_database
│   ├── tickets
│   ├── ticket_history
│   ├── ticket_comments
│   ├── ticket_attachments
│   ├── ticket_categories
│   ├── support_queues
│   ├── support_teams
│   ├── sla_policies
│   └── outbox_events
│
└── outras_databases
```

A separação efetiva deve ser confirmada com quem administra a
infraestrutura. Se "MongoDB compartilhado" significar compartilhar a
mesma database ou collections, será necessário revisar isolamento,
permissões, índices e governança antes de adotar esse modelo.

### 10.2. Isolamento e acesso

-   O Help Desk deve possuir credencial própria, com privilégios
    mínimos.
-   O serviço não deve depender de acesso irrestrito às collections do
    ERP.
-   Dados de usuários e empresas devem ser obtidos por contratos de
    integração ou mantidos como referências externas.
-   Toda consulta multiempresa deve aplicar o contexto de tenant no
    backend.
-   Índices devem refletir os padrões reais de consulta.
-   Considerar backup, restauração, retenção e monitoramento desde o
    início.

### 10.3. Collections iniciais propostas

  -----------------------------------------------------------------------
  Collection                          Conteúdo
  ----------------------------------- -----------------------------------
  `tickets`                           Estado atual e dados principais do
                                      chamado

  `ticket_history`                    Transições e alterações auditáveis

  `ticket_comments`                   Mensagens e interações

  `ticket_attachments`                Metadados e referências de arquivos

  `ticket_categories`                 Categorias e classificações

  `support_queues`                    Filas de atendimento

  `support_teams`                     Equipes e configurações

  `sla_policies`                      Políticas e parâmetros de SLA

  `outbox_events`                     Eventos aguardando publicação

  `processed_messages`                Controle de mensagens já
                                      processadas, se necessário
  -----------------------------------------------------------------------

A lista é inicial. Collections adicionais só devem ser criadas quando
houver necessidade de domínio ou consulta bem definida.

### 10.4. Exemplo conceitual de ticket

``` json
{
  "_id": "ticket_id",
  "tenant_id": "company_id",
  "protocol": "SUP-2026-000001",
  "requester": {
    "external_user_id": "bubble_user_id"
  },
  "category_id": "category_id",
  "title": "Problema na emissão",
  "description": "Descrição do problema",
  "priority": "high",
  "status": "open",
  "queue_id": null,
  "assigned_to": null,
  "created_at": "2026-11-23T12:00:00Z",
  "updated_at": "2026-11-23T12:00:00Z"
}
```

Este exemplo não é o schema definitivo. Os campos de tenant,
solicitante, empresa, responsáveis, datas e status deverão ser
especificados formalmente antes da implementação.

### 10.5. Histórico separado

Evitar manter um array ilimitado de histórico dentro do documento
principal do ticket.

A collection `ticket_history` deverá registrar, conforme cada tipo de
evento:

-   identificador do ticket;
-   tenant;
-   tipo da alteração;
-   estado anterior e novo estado, quando aplicável;
-   ator responsável;
-   data/hora;
-   justificativa ou observação;
-   identificador de correlação da operação.

A estrutura final deve distinguir histórico de auditoria, comentários e
eventos técnicos. São conceitos relacionados, mas não equivalentes.

### 10.6. Índices

Os índices devem ser definidos a partir das consultas previstas e
validados com testes de desempenho. Candidatos iniciais:

-   `tenant_id` + data de criação;
-   `tenant_id` + status + data de atualização;
-   `tenant_id` + solicitante + data de criação;
-   `tenant_id` + responsável + status;
-   `tenant_id` + fila + status;
-   protocolo, com unicidade definida no escopo correto.

A estratégia de unicidade do protocolo deve considerar se o protocolo é
global ou por tenant. A decisão ainda está pendente.

## 11. Processamento assíncrono

### 11.1. Princípio

A API não deverá depender da conclusão de tarefas secundárias para
responder ao usuário quando a operação principal já estiver validada e
persistida.

Exemplo de abertura:

1.  Bubble envia o pedido de abertura.
2.  API autentica e autoriza.
3.  Application valida os dados e aplica as regras.
4.  Help Desk persiste o ticket e o evento de Outbox.
5.  API retorna o protocolo e a confirmação da criação.
6.  Um publicador lê a Outbox e envia o evento ao broker.
7.  Um Worker consome o evento e executa tarefas assíncronas.
8.  O resultado de cada tarefa é registrado quando necessário.

### 11.2. Transactional Outbox

Adotar Outbox para reduzir o risco de persistir um ticket sem publicar o
evento correspondente.

O registro do ticket e o registro da Outbox devem ser gravados na mesma
transação MongoDB, desde que o deployment do MongoDB suporte transações.

O publicador deve:

-   localizar eventos pendentes;
-   publicar no broker;
-   aguardar confirmação de publicação conforme a tecnologia escolhida;
-   marcar o evento como publicado;
-   aplicar retry com backoff;
-   permitir recuperação após falha.

A Outbox não elimina duplicidade de entrega. O consumidor deve ser
idempotente.

### 11.3. Consumidores e idempotência

Cada consumidor deverá ser preparado para receber uma mesma mensagem
mais de uma vez sem repetir indevidamente os efeitos.

Usar identificador único de evento/mensagem e persistir o controle de
processamento quando necessário.

Definir:

-   política de retry;
-   backoff;
-   limite de tentativas;
-   dead-letter queue;
-   tratamento de mensagens inválidas;
-   alertas para mensagens paradas ou acumuladas;
-   procedimento de reprocessamento seguro.

### 11.4. Eventos candidatos

Nomes conceituais:

``` text
helpdesk.ticket.created
helpdesk.ticket.updated
helpdesk.ticket.assigned
helpdesk.ticket.status-changed
helpdesk.ticket.resolved
helpdesk.comment.created
helpdesk.notification.requested
helpdesk.sla.deadline-approaching
helpdesk.sla.breached
```

Os eventos finais devem representar fatos ocorridos, possuir versão de
schema e carregar somente os dados necessários ao consumidor.

### 11.5. Workers candidatos

  Worker                Responsabilidade
  --------------------- ---------------------------------------
  TicketCreatedWorker   Reações à criação do chamado
  AssignmentWorker      Distribuição ou encaminhamento
  NotificationWorker    Envio de notificações
  SlaWorker             Verificação de prazos e violações
  IntegrationWorker     Integrações externas e sincronizações

A separação física dos Workers deve seguir o volume, o perfil de carga e
a necessidade de escala. Não é obrigatório iniciar com um executável
para cada responsabilidade.

## 12. Domínios funcionais

### 12.1. Tickets

Responsável pelo chamado, protocolo, solicitante, descrição, prioridade,
status e dados principais.

### 12.2. Workflow

Responsável pelas transições válidas entre estados, pré-condições e
efeitos de cada transição.

Estados candidatos:

-   `open`;
-   `triage`;
-   `in_progress`;
-   `waiting`;
-   `resolved`;
-   `closed`.

Os estados e suas regras precisam ser validados com o processo real de
suporte. Por exemplo, definir se um chamado resolvido pode ser reaberto
e em quais condições.

### 12.3. Assignment

Responsável por filas, equipes, responsáveis e regras de distribuição. A
distribuição automática só deve ser implementada após definir critérios
de negócio, como categoria, empresa, disponibilidade, carga e
especialização.

### 12.4. Comments

Responsável pela comunicação entre solicitante e equipe de suporte. Deve
diferenciar mensagens públicas de notas internas.

### 12.5. Attachments

Responsável pelos metadados, autorização de acesso e referências a
arquivos. A recomendação é armazenar binários em Object Storage, não
diretamente nos documentos MongoDB, salvo justificativa específica.

### 12.6. SLA

Responsável por prazos, calendário de atendimento, pausas, vencimentos e
indicadores. As regras de contagem devem considerar horário comercial,
feriados, fuso horário e estados de espera, conforme definição do
negócio.

### 12.7. Categories

Responsável pela classificação dos chamados e pela configuração de
categorias.

### 12.8. Notifications

Responsável por definir e executar notificações por eventos. Os canais
(e-mail, notificações internas, outros) e provedores ainda precisam ser
escolhidos.

### 12.9. Audit

Responsável pela rastreabilidade de ações sensíveis e alterações
relevantes. Definir política de retenção e acesso aos registros.

### 12.10. Reporting

Responsável por indicadores, consultas agregadas e relatórios.
Relatórios pesados não devem prejudicar as consultas operacionais do
atendimento; avaliar projeções, agregações pré-calculadas ou
armazenamento analítico se o volume justificar.

## 13. Segurança, isolamento e multiempresa

O sistema deve ser projetado para múltiplos tenants desde o início,
ainda que a primeira implantação tenha um conjunto limitado de empresas.

### Requisitos

-   Toda entidade pertencente a uma empresa deve ter contexto de tenant.
-   O tenant deve ser derivado de uma identidade validada, e não
    confiado cegamente no corpo da requisição.
-   Toda consulta e operação deve aplicar isolamento por tenant.
-   Verificações de autorização devem ocorrer no servidor.
-   IDs imprevisíveis não substituem autorização.
-   Segredos não devem ser armazenados no repositório.
-   Usar HTTPS em todas as comunicações externas.
-   Aplicar rate limiting e validação de payloads.
-   Registrar operações administrativas e alterações críticas.
-   Definir política de retenção e proteção de dados pessoais.
-   Anexos devem ter acesso controlado e URLs temporárias quando
    aplicável.

Também será necessário decidir se as equipes de suporte serão globais,
por empresa ou híbridas, e como usuários internos do suporte poderão
acessar chamados de diferentes tenants.

## 14. Escalabilidade e disponibilidade

A escala esperada é grande, mas a arquitetura não deve assumir números
de volume ainda desconhecidos.

A estratégia proposta:

-   API stateless sempre que possível, permitindo múltiplas instâncias.
-   Workers independentes da API.
-   Escalonamento horizontal por tipo de carga.
-   Broker com persistência e disponibilidade adequadas.
-   MongoDB dimensionado conforme volume, índices e perfil de consulta.
-   Paginação em listagens.
-   Limites de tamanho para payloads e anexos.
-   Processamento em lote quando apropriado.
-   Timeouts e políticas de retry para dependências externas.
-   Backpressure e limites de concorrência nos consumidores.
-   Monitoramento de filas e latência.

Antes de definir réplicas, CPU, memória, shard key ou quantidade de
Workers, levantar estimativas de usuários simultâneos, tickets/dia,
tamanho médio de anexos, picos de acesso e retenção.

## 15. Observabilidade e operação

A aplicação deverá produzir logs estruturados e correlacionáveis.

Campos recomendados:

-   timestamp;
-   nível;
-   serviço e ambiente;
-   trace/correlation ID;
-   request ID;
-   event ID;
-   ticket ID, quando permitido;
-   tenant ID, com cuidado para não expor dados sensíveis;
-   nome do caso de uso ou consumidor;
-   resultado e duração.

Métricas iniciais:

-   requisições por endpoint e status HTTP;
-   latência da API;
-   erros por operação;
-   tickets criados por período;
-   mensagens publicadas e consumidas;
-   tamanho e idade das filas;
-   retries e mensagens em DLQ;
-   duração dos Workers;
-   falhas de integração;
-   tempo de atendimento e cumprimento de SLA.

Adotar tracing distribuído quando API, broker e Workers precisarem ser
acompanhados como uma única operação.

Não registrar tokens, senhas, conteúdo sensível de chamados ou dados
pessoais desnecessários nos logs.

## 16. Testes e qualidade

### Testes unitários

Validar regras de domínio e casos de uso sem depender de infraestrutura
externa.

### Testes de integração

Validar MongoDB, broker, Outbox, repositórios e endpoints com
dependências reais ou ambientes efêmeros controlados.

### Testes de contrato

Verificar compatibilidade dos contratos entre Bubble e API, e entre
publicadores e consumidores de eventos.

### Testes de autorização

Testar acesso entre tenants, papéis distintos, chamados de outros
usuários e anexos não autorizados.

### Testes de resiliência

Simular indisponibilidade do broker, falha de publicação, mensagens
duplicadas, falhas de consumidor e recuperação da Outbox.

### Testes de carga

Medir listagem, criação, atualização, busca e processamento assíncrono
com volumes representativos.

## 17. Implantação e ambientes

Proposta inicial de ambientes:

-   **Development:** desenvolvimento local e integração.
-   **Staging/Homologation:** validação integrada com ambiente de teste
    do Comprove.
-   **Production:** ambiente produtivo.

Cada ambiente deverá possuir configurações e credenciais próprias.

### Processos de implantação

1.  Compilar a Solution.
2.  Executar testes automatizados.
3.  Construir imagens Docker.
4.  Publicar imagens em registry.
5.  Aplicar configurações e segredos do ambiente.
6.  Executar migrações ou tarefas de criação/atualização de índices de
    forma controlada.
7.  Implantar API e Workers.
8.  Executar verificações de saúde.
9.  Monitorar erros e métricas após deploy.
10. Manter estratégia de rollback.

A tecnologia de CI/CD, registry, hospedagem e orquestração ainda está
pendente de escolha.

## 18. Plano de desenvolvimento do projeto

O desenvolvimento deverá ocorrer em etapas, validando as decisões
estruturais antes de ampliar o escopo.

### Fase 1 --- Descoberta e validação

**Objetivo:** fechar requisitos e dependências externas.

Atividades:

-   mapear o processo atual de abertura e atendimento;
-   validar atores, empresas, equipes e permissões;
-   confirmar autenticação atual do Bubble;
-   identificar como o Bubble pode validar ou delegar identidade com
    segurança;
-   confirmar a infraestrutura MongoDB e o significado de
    "compartilhado";
-   levantar volume estimado e requisitos de disponibilidade;
-   definir interface de suporte: Bubble, frontend separado ou abordagem
    híbrida;
-   definir requisitos de notificações, anexos, SLA e auditoria.

**Entrega:** requisitos funcionais, requisitos não funcionais e decisões
arquiteturais registradas.

### Fase 2 --- Arquitetura técnica detalhada

**Objetivo:** converter a arquitetura lógica em especificações
implementáveis.

Atividades:

-   escolher versão do .NET;
-   decidir broker;
-   definir mecanismo de autenticação;
-   especificar contratos REST;
-   especificar eventos e schemas;
-   desenhar collections e índices;
-   definir estratégia de protocolo;
-   definir workflow e transições;
-   definir estratégia multi-tenant;
-   definir hospedagem e ambientes.

**Entrega:** diagramas técnicos, ADRs, OpenAPI inicial, schemas e plano
de implantação.

### Fase 3 --- Fundação técnica

**Objetivo:** criar a base executável e testável.

Atividades:

-   criar Solution e projetos;
-   configurar injeção de dependência;
-   configurar validação e tratamento de erros;
-   configurar MongoDB;
-   configurar broker;
-   configurar logging, health checks e tracing;
-   configurar Docker;
-   criar pipeline inicial;
-   criar testes unitários e de integração.

**Entrega:** API e Worker executáveis em ambiente de desenvolvimento.

### Fase 4 --- MVP de chamados

**Objetivo:** entregar o ciclo básico de abertura e acompanhamento.

Escopo sugerido:

-   autenticação e autorização;
-   criação de chamado;
-   geração de protocolo;
-   consulta de chamados autorizados;
-   atualização de status com regras;
-   histórico;
-   comentários;
-   integração Bubble → API;
-   Outbox e evento de criação;
-   primeiro Worker e notificação básica, se o canal já estiver
    definido.

**Entrega:** fluxo ponta a ponta funcional em homologação.

### Fase 5 --- Operação do suporte

**Objetivo:** habilitar o atendimento interno.

Escopo:

-   filas;
-   equipes;
-   atribuição de responsáveis;
-   painel de atendimento;
-   filtros e pesquisa;
-   notas internas;
-   resolução e encerramento;
-   reabertura, se prevista;
-   permissões específicas da equipe de suporte.

**Entrega:** operação de suporte utilizável e auditável.

### Fase 6 --- Recursos avançados

**Objetivo:** ampliar automação e gestão.

Escopo candidato:

-   políticas de SLA;
-   distribuição automática;
-   classificação;
-   notificações adicionais;
-   anexos;
-   relatórios e indicadores;
-   escalonamento de chamados;
-   integrações adicionais.

**Entrega:** conjunto de recursos avançados priorizados pelo negócio.

### Fase 7 --- Escala e otimização

**Objetivo:** preparar a operação para o volume real.

Atividades:

-   testes de carga;
-   ajuste de índices;
-   análise de consultas;
-   escalonamento de API e Workers;
-   monitoramento de filas;
-   política de retenção;
-   backup e testes de restauração;
-   revisão de custos e capacidade;
-   revisão de segurança.

**Entrega:** operação validada para metas de volume e disponibilidade
acordadas.

## 19. Decisões pendentes e perguntas obrigatórias

As decisões abaixo não devem ser assumidas sem validação.

  -----------------------------------------------------------------------
  ID                      Decisão pendente        Por que importa
  ----------------------- ----------------------- -----------------------
  D-01                    Qual versão do .NET     Suporte, bibliotecas e
                          será utilizada?         runtime

  D-02                    Qual infraestrutura     Deploy, rede,
                          hospedará o serviço?    disponibilidade e custo

  D-03                    RabbitMQ ou outro       Operação, entrega e
                          broker?                 escalabilidade

  D-04                    Como será validada a    Segurança e experiência
                          identidade Bubble?      de login

  D-05                    O MongoDB compartilhado Isolamento e governança
                          é cluster, database ou  
                          collections?            

  D-06                    Haverá interface        Escopo de frontend e
                          administrativa própria? autenticação

  D-07                    Como serão definidos    Modelo de dados e
                          tenants e empresas?     autorização

  D-08                    O protocolo será global Unicidade e consulta
                          ou por empresa?         

  D-09                    Quais estados e         Workflow
                          transições serão        
                          permitidos?             

  D-10                    Como funcionam filas,   Operação do suporte
                          equipes e atribuição?   

  D-11                    Quais são as regras de  Calendários, pausas e
                          SLA?                    vencimentos

  D-12                    Quais canais de         Provedores e Workers
                          notificação serão       
                          usados?                 

  D-13                    Onde os anexos serão    Storage, segurança e
                          armazenados?            custos

  D-14                    Qual volume esperado de Dimensionamento
                          tickets e usuários?     

  D-15                    Quais são os requisitos Infraestrutura e
                          de disponibilidade e    continuidade
                          recuperação?            
  -----------------------------------------------------------------------

## 20. ADRs --- Registro inicial de decisões arquiteturais

Um ADR (Architecture Decision Record) registra uma decisão, seu contexto
e suas consequências. Recomenda-se manter um arquivo por decisão
relevante.

### ADR-001 --- Help Desk como serviço independente

**Status:** Aceito como premissa do projeto.

O Help Desk será hospedado externamente e terá backend próprio em
C#/.NET. O Bubble será consumidor da API e camada de frontend do
Comprove.

### ADR-002 --- API e Workers como processos separados

**Status:** Proposto.

A API atenderá requisições síncronas; Workers processarão tarefas
assíncronas. Inicialmente poderão compartilhar repositórios e projetos
de domínio/aplicação.

### ADR-003 --- Database lógica própria para o Help Desk

**Status:** Recomendado, pendente de confirmação da infraestrutura.

Compartilhar o cluster MongoDB, se apropriado, mas isolar a database do
Help Desk e suas permissões.

### ADR-004 --- Transactional Outbox

**Status:** Recomendado, pendente de validação técnica.

Persistir alterações de negócio e eventos pendentes na mesma transação
MongoDB. Publicar os eventos posteriormente com um processo dedicado.

### ADR-005 --- RabbitMQ

**Status:** Candidato, não aprovado.

Avaliar RabbitMQ frente às alternativas gerenciadas disponíveis na
infraestrutura.

### ADR-006 --- Multi-tenancy desde o início

**Status:** Recomendado.

Projetar dados e autorização considerando múltiplas empresas, com
isolamento aplicado no backend.

## 21. Critérios de conclusão da primeira entrega

O MVP não deve ser considerado concluído apenas porque a criação de
tickets funciona.

Critérios mínimos:

-   usuário autenticado consegue abrir um chamado;
-   backend valida identidade, tenant e permissão;
-   protocolo é gerado sem colisões dentro do escopo definido;
-   ticket é persistido;
-   evento de criação é registrado na Outbox;
-   evento pode ser publicado e consumido;
-   mensagens duplicadas não provocam efeitos duplicados;
-   usuário autorizado consegue consultar o chamado;
-   alterações de status respeitam o workflow;
-   histórico é persistido;
-   falhas são registradas e observáveis;
-   testes automatizados cobrem regras essenciais e isolamento entre
    tenants;
-   integração com Bubble funciona em homologação;
-   procedimento de deploy e rollback está documentado.

## 22. Próximos passos imediatos

A sequência recomendada para continuar o projeto é:

1.  **Validar identidade e autenticação:** entender como o Bubble
    autentica usuários e quais mecanismos seguros de integração
    servidor-servidor estão disponíveis.
2.  **Validar infraestrutura MongoDB:** identificar cluster, versão,
    topologia, permissões e suporte a transações.
3.  **Definir experiência de suporte:** decidir se a equipe
    administrativa utilizará Bubble ou frontend independente.
4.  **Fechar domínio funcional:** confirmar status, categorias, filas,
    equipes, atribuição, comentários e resolução.
5.  **Definir stack final:** versão do .NET, broker, storage e
    hospedagem.
6.  **Especificar contratos:** desenhar API REST e eventos.
7.  **Criar os ADRs e diagramas definitivos.**
8.  **Criar a Solution e iniciar a fundação técnica.**

------------------------------------------------------------------------

## 23. Resumo executivo

O projeto será um serviço de Help Desk independente, desenvolvido em
C#/.NET e hospedado em infraestrutura externa. O Bubble permanecerá como
frontend do Comprove e consumirá a API do Help Desk por HTTPS/REST.

O serviço terá organização interna modular, com API e Workers executados
independentemente. O MongoDB será utilizado para persistência,
preferencialmente com database própria para o Help Desk, mesmo que o
cluster seja compartilhado. O processamento assíncrono será feito por um
broker ainda a escolher, com RabbitMQ como candidato inicial. A
estratégia Transactional Outbox é recomendada para publicação confiável
de eventos.

A solução será projetada para multi-tenancy, autorização no backend,
histórico auditável, observabilidade e escalabilidade horizontal.

A implementação seguirá etapas: descoberta, arquitetura detalhada,
fundação técnica, MVP de chamados, operação do suporte, recursos
avançados e otimização de escala.

**Regra de governança:** nenhuma tecnologia candidata, regra de negócio
ou mecanismo de integração marcado como pendente deverá ser considerado
definitivamente aprovado sem validação explícita. As decisões devem ser
registradas e atualizadas neste documento ou em ADRs correspondentes.
