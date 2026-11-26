// Executado por New-HelpDeskInfra.ps1 via `mongosh --file` dentro do container. Idempotente.
const alreadyInitiated = (() => {
  try {
    return rs.status().ok === 1;
  } catch (error) {
    return false;
  }
})();

if (!alreadyInitiated) {
  rs.initiate({ _id: "rs0", members: [{ _id: 0, host: "127.0.0.1:27017" }] });
}

for (let attempt = 0; attempt < 30 && !db.hello().isWritablePrimary; attempt++) {
  sleep(1000);
}

if (!db.hello().isWritablePrimary) {
  throw new Error("Replica set rs0 não elegeu PRIMARY em 30s");
}

print("rs0 pronto: PRIMARY ativo");
