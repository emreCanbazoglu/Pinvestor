# Unity MCP Setup (Coplay)

This repository is configured for [Coplay Unity MCP](https://github.com/coplaydev/unity-mcp).

## Added to This Repo

- Unity package dependency in `/Users/emre/Desktop/MM-Projects/Pinvestor/Packages/manifest.json`
  - `com.coplaydev.unity-mcp` via Git URL package source
- Codex MCP client config in `/Users/emre/Desktop/MM-Projects/Pinvestor/.codex/config.toml`
  - HTTP endpoint: `http://localhost:8080/mcp`

## Unity Editor Setup (Required)

1. Open the project in Unity and let Package Manager resolve dependencies.
2. Open `Window > Unity MCP`.
3. Start the MCP server (default endpoint `http://localhost:8080/mcp`).
4. Keep the Unity Editor running while using Codex MCP features.

## Usage Notes

- The server runs inside Unity; Codex will not connect unless Unity MCP is running.
- If you change the port/host, update `/Users/emre/Desktop/MM-Projects/Pinvestor/.codex/config.toml`.
- Use MCP for inspection/debugging assistance, not as a replacement for source changes and tests.

