# ChatApplication
Repository for real time chat applicaiton

## MongoDB (local dev)

Messages are persisted in MongoDB. Start it via Docker (from WSL) — it runs on port **27018** to avoid clashing with any native MongoDB install on 27017:

```
wsl
cd /mnt/d/Tests/ChatApp/ChatApplication
docker compose up -d
```

Connect with MongoDB Compass to `mongodb://localhost:27018` to inspect the `ChatApp.messages` collection.

Note: WSL stops the distro (and anything running in it, including Docker) shortly after the last attached shell closes. Keep a `wsl` terminal open while developing, or the container will exit.
