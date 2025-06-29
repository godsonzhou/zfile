# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

### Main Solution (.NET)
```bash
dotnet build Files.sln
```

### FluentFTP (.NET)
```bash
dotnet build FluentFTP.sln
```

### MCPSharp (.NET)
```bash
dotnet build MCPSharp.sln
```

### Node.js Projects
```bash
cd mcp_servers/sqlite
npm install
npm run build
```

### Python Projects
```bash
cd mcp_servers/sqlite/src/<service>
pip install -e .
```

### Makefile Projects
```bash
cd src/monotorrent
make
```

## Test Commands

### .NET Projects
```bash
dotnet test
```

### Node.js Projects
```bash
cd mcp_servers/sqlite
npm test
```

## Architecture Overview

- Core solution: Files.sln (.NET)
- Key components:
  - FluentFTP: FTP client library
  - ICSharpCode.TextEditor: Text editing component
  - MCPSharp: Managed Code Platform framework
  - Sheng.Winform.Controls: UI control library
- mcp_servers: Microservices using SQLite
- Docker support for various services

## Repository Structure

- config/: Configuration files
- doc/: Documentation
- mcp_servers/: SQLite-based microservices
- src/: Source projects
- util_f4menu/: External tool

For service-specific details, refer to individual project READMEs.