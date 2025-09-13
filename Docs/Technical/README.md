# Sharp Bridge Technical Documentation

This section contains technical documentation for developers working on Sharp Bridge. For user documentation, see the [User Guide](../UserGuide/README.md).

## Overview

Sharp Bridge is a real-time bridge application that connects iPhone's VTube Studio to VTube Studio on PC. It follows an **orchestrated pipeline architecture** with resilient design, configuration-driven behavior, and a console-based user interface.

## Documentation Structure

### [Architecture](Architecture.md)
- **Component relationships** - How the main components interact
- **Data flow** - Real-time pipeline from iPhone to PC
- **Interfaces** - Key contracts between components
- **Design decisions** - Why architectural choices were made

### [Code Organization](CodeOrganization.md)
- **Module structure** - How code is organized across layers
- **Design patterns** - Key patterns used throughout
- **Dependency management** - How modules depend on each other
- **Extension points** - Where new features can be added

### [Release Process](ReleaseProcess.md)
- **Release workflow** - GitHub Actions automation
- **Version management** - Semantic versioning and tagging
- **Build process** - Self-contained executable creation
- **Quality gates** - Coverage and testing requirements

## Key Architectural Principles

1. **Resilient Design** - Built-in recovery mechanisms for all components
2. **Configuration-Driven** - Hot-reload capabilities and user customization
3. **Event-Driven** - Reactive data processing with real-time updates
4. **Dependency Injection** - Loose coupling through interface-based design
5. **Console UI** - Real-time status display with interactive controls

## Quick Reference

- **Main Components**: ApplicationOrchestrator, VTubeStudioPCClient, VTubeStudioPhoneClient, TransformationEngine
- **Data Flow**: iPhone (UDP) → PhoneClient → TransformEngine → PCClient → PC (WebSocket)
- **Configuration**: Single `ApplicationConfig.json` with hot-reload
- **UI**: Console-based with multiple modes (Main, System Help, Network Status)
- **Recovery**: Automatic service recovery with configurable policies

## Getting Started

1. **Understand the architecture** - Start with [Architecture](Architecture.md)
2. **Explore the code** - Review [Code Organization](CodeOrganization.md)
3. **Learn the release process** - Check [Release Process](ReleaseProcess.md) for workflows
4. **Read the code** - The implementation is the source of truth

## Cross-References

- **User Documentation**: [User Guide](../UserGuide/README.md)
- **Project Overview**: [ProjectOverview](../ProjectOverview.md) (legacy reference)
- **Source Code**: `src/` directory
- **Tests**: `tests/` directory
